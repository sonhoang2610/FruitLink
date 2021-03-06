// Copyright (C) 2019 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store EULA,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Collections.Generic;
using DG.Tweening;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using UnityEngine.Assertions;

namespace FruitSwipeMatch3Kit
{
    /// <summary>
    /// This system is responsible for actually resolving the booster tiles
    /// during a game (i.e., exploding the appropriate tiles as dictated by
    /// the booster's arrows).
    /// </summary>
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(BoosterCreationSystem))]
    [AlwaysUpdateSystem]
    public class BoosterResolutionSystem : ComponentSystem
    {
        private EntityQuery resolveBoostersQuery;
        private EntityQuery pendingBoosterQuery;
        private EntityQuery gravityFillQuery;

        private LevelCreationSystem levelCreationSystem;
        private PlayerInputSystem inputSystem;

        private ParticlePools particlePools;

        private readonly List<int> indexes = new List<int>(16);

        private bool chainingBoosters;
        private Entity selectedBooster = Entity.Null;
        private ObjectPool boosterPool;
        private float gravityAccTime;

        protected override void OnCreate()
        {
            Enabled = false;
            resolveBoostersQuery = GetEntityQuery(
                ComponentType.ReadOnly<ResolveBoostersData>());
            pendingBoosterQuery = GetEntityQuery(
                ComponentType.ReadOnly<BoosterData>(),
                ComponentType.ReadOnly<PendingBoosterData>());
            gravityFillQuery = GetEntityQuery(new EntityQueryDesc
            {
                Any = new []
                {
                    ComponentType.ReadOnly<ApplyGravityData>(), 
                    ComponentType.ReadOnly<FillEmptySlotsData>()
                }
            });
        }

        public void Initialize()
        {
            levelCreationSystem = World.GetExistingSystem<LevelCreationSystem>();
            inputSystem = World.GetExistingSystem<PlayerInputSystem>();
            particlePools = Object.FindObjectOfType<ParticlePools>();
            boosterPool = particlePools.BoosterEffectPool;
        }

        protected override void OnUpdate()
        {
            if(gravityFillQuery.CalculateEntityCount() > 0)
                return;

            if (chainingBoosters)
            {
                chainingBoosters = false;
                inputSystem.SetBoosterChainResolving(false);
                PostUpdateCommands.AddComponent(selectedBooster, new PendingBoosterData());
                selectedBooster = Entity.Null;
            }
            else if (GameState.IsBoosting)
            {
                gravityAccTime += Time.deltaTime;
                if (gravityAccTime >= GameplayConstants.GravityAfterBoosterDelay)
                {
                    gravityAccTime = 0.0f;
                    GameState.IsBoosting = false;
                    
                    var applyGravity = EntityManager.CreateArchetype(typeof(ApplyGravityData));
                    var e = PostUpdateCommands.CreateEntity(applyGravity);
                    PostUpdateCommands.SetComponent(e, new ApplyGravityData());
                }
            }
            else
            {
                var entities = resolveBoostersQuery.ToEntityArray(Allocator.TempJob);
                if (entities.Length == 0)
                {
                    entities.Dispose();
                    return;
                }

                var entities2 = pendingBoosterQuery.ToEntityArray(Allocator.TempJob);
                if (entities2.Length == 0)
                {
                    entities.Dispose();
                    entities2.Dispose();
                    return;
                }

                foreach (var entity in entities)
                    PostUpdateCommands.DestroyEntity(entity);
                entities.Dispose();
                
                var boosterData = pendingBoosterQuery.ToComponentDataArray<BoosterData>(Allocator.TempJob);
                ResolveBooster(entities2[0], boosterData[0]);
                boosterData.Dispose();
                entities2.Dispose();
            }
        }

        private void ResolveBooster(Entity entity, BoosterData boosterData)
        {
            indexes.Clear();
            
            switch (boosterData.Type)
            {
                case BoosterType.Horizontal:
                    ResolveHorizontalBooster(entity);
                    break;

                case BoosterType.Vertical:
                    ResolveVerticalBooster(entity);
                    break;

                case BoosterType.DiagonalLeft:
                    ResolveDiagonalLeftBooster(entity);
                    break;

                case BoosterType.DiagonalRight:
                    ResolveDiagonalRightBooster(entity);
                    break;

                case BoosterType.Cross:
                    ResolveCrossBooster(entity);
                    break;

                case BoosterType.Star:
                    ResolveStarBooster(entity);
                    break;
            }
    
            GameState.IsBoosting = true; 
            SoundPlayer.PlaySoundFx("Booster");
            var seg = DOTween.Sequence();
            seg.AppendInterval(GameplayConstants.BoosterEffectDelay);
            seg.AppendCallback(() =>
            {
                var tilesToExplode = new List<int>(indexes.Count);
                for (var i = 0; i < indexes.Count; ++i)
                {
                    var idx = indexes[i];
                    var tileEntity = levelCreationSystem.TileEntities[idx];

                    if (inputSystem.PendingBoosterTiles.Contains(tileEntity))
                    {
                        if (selectedBooster == Entity.Null)
                        {
                            inputSystem.PendingBoosterTiles.Remove(tileEntity);
                            selectedBooster = tileEntity;
                            chainingBoosters = true;
                        }
                        continue;
                    }
                
                    if (EntityManager.HasComponent<BoosterData>(tileEntity) &&
                        !EntityManager.HasComponent<PendingBoosterData>(tileEntity))
                    {
                        if (selectedBooster == Entity.Null)
                        {
                            selectedBooster = tileEntity;
                            chainingBoosters = true;
                        }
                        else
                        {
                            inputSystem.PendingBoosterTiles.Insert(0, tileEntity);
                        }
                    }
                    else
                    {
                        tilesToExplode.Add(idx);
                    }
                }

                var entities = levelCreationSystem.TileEntities;
                var gos = levelCreationSystem.TileGos;
                var slots = levelCreationSystem.Slots;
                var width = levelCreationSystem.Width;
                var height = levelCreationSystem.Height;
                TileUtils.DestroyTiles(tilesToExplode, entities, gos, slots, particlePools, width, height, true);

                if (selectedBooster == Entity.Null)
                    inputSystem.SetBoosterExploding(false);
                inputSystem.SetBoosterChainResolving(chainingBoosters);
                boosterPool.Reset();
            });
        }

        private void ResolveHorizontalBooster(Entity entity)
        {
            Assert.IsTrue(EntityManager.HasComponent<TileData>(entity));
            var tilePos = EntityManager.GetComponentData<TilePosition>(entity);
            var boosterGO = boosterPool.GetObject();
            boosterGO.transform.position = EntityManager.GetComponentData<Translation>(entity).Value;
            boosterGO.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.right);
            var y = tilePos.Y;
            var width = levelCreationSystem.Width;
            for (var i = 0; i < width; i++)
            {
                var idx = i + y * width;
                if (!EntityManager.HasComponent<HoleSlotData>(levelCreationSystem.TileEntities[idx]))
                    indexes.Add(idx);
            }
        }

        private void ResolveVerticalBooster(Entity entity)
        {
            Assert.IsTrue(EntityManager.HasComponent<TileData>(entity));
            var tilePos = EntityManager.GetComponentData<TilePosition>(entity);
            var boosterGO = boosterPool.GetObject();
            boosterGO.transform.position = EntityManager.GetComponentData<Translation>(entity).Value;
            boosterGO.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);
            var x = tilePos.X;
            var width = levelCreationSystem.Width;
            var height = levelCreationSystem.Height;
            for (var j = 0; j < height; j++)
            {
                var idx = x + j * width;
                if (!EntityManager.HasComponent<HoleSlotData>(levelCreationSystem.TileEntities[idx]))
                    indexes.Add(idx);
            }
        }

        private void ResolveDiagonalLeftBooster(Entity entity)
        {
            Assert.IsTrue(EntityManager.HasComponent<TileData>(entity));
            var tilePos = EntityManager.GetComponentData<TilePosition>(entity);
            var boosterGO = boosterPool.GetObject();
            boosterGO.transform.position = EntityManager.GetComponentData<Translation>(entity).Value;
            boosterGO.transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(-1, 1, 0));
            var x = tilePos.X;
            var y = tilePos.Y;
            var width = levelCreationSystem.Width;
            var height = levelCreationSystem.Height;

            var i = x;
            var j = y;
            indexes.Add(i + j * width);

            while (i >= 0 && j >= 0)
            {
                var idx = i + j * width;
                if (!EntityManager.HasComponent<HoleSlotData>(levelCreationSystem.TileEntities[idx]))
                    indexes.Add(idx);
                i -= 1;
                j -= 1;
            }

            i = x;
            j = y;

            while (i < width && j < height)
            {
                var idx = i + j * width;
                if (!EntityManager.HasComponent<HoleSlotData>(levelCreationSystem.TileEntities[idx]))
                    indexes.Add(idx);
                i += 1;
                j += 1;
            }
        }

        private void ResolveDiagonalRightBooster(Entity entity)
        {
            Assert.IsTrue(EntityManager.HasComponent<TileData>(entity));
            var tilePos = EntityManager.GetComponentData<TilePosition>(entity);
            var boosterGO = boosterPool.GetObject();
            boosterGO.transform.position = EntityManager.GetComponentData<Translation>(entity).Value;
            boosterGO.transform.rotation = Quaternion.LookRotation(Vector3.forward, new Vector3(1, 1, 0));
            var x = tilePos.X;
            var y = tilePos.Y;
            var width = levelCreationSystem.Width;
            var height = levelCreationSystem.Height;

            var i = x;
            var j = y;

            while (i >= 0 && j < height)
            {
                var idx = i + j * width;
                if (!EntityManager.HasComponent<HoleSlotData>(levelCreationSystem.TileEntities[idx]))
                    indexes.Add(idx);
                i -= 1;
                j += 1;
            }

            i = x;
            j = y;
            while (i < width && j >= 0)
            {
                var idx = i + j * width;
                if (!EntityManager.HasComponent<HoleSlotData>(levelCreationSystem.TileEntities[idx]))
                    indexes.Add(idx);
                i += 1;
                j -= 1;
            }
        }

        private void ResolveCrossBooster(Entity entity)
        {
            ResolveHorizontalBooster(entity);
            ResolveVerticalBooster(entity);
        }

        private void ResolveStarBooster(Entity entity)
        {
            ResolveHorizontalBooster(entity);
            ResolveVerticalBooster(entity);
            ResolveDiagonalLeftBooster(entity);
            ResolveDiagonalRightBooster(entity);
        }
    }
}
