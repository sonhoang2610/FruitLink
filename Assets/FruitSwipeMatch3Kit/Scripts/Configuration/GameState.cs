﻿// Copyright (C) 2019 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store EULA,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Collections.Generic;
using DG.Tweening;

namespace FruitSwipeMatch3Kit
{
    /// <summary>
    /// Stores the current state of a game.
    /// </summary>
    public class GameState : IRestartable
    {
        public int Score;
        public static int SwapCount = 0;
        public static bool IsBoosting = false;
        public static bool IsPlayingEndGameSequence = false;
        public static bool HasJelly = false;
        public static bool IsSwapping = false;
        public static bool IsTutorial = false;
        public static bool IsPlayerWon = false;
        public static List<int> SuggestIndexes = new List<int>();
        public static Sequence SuggestSequence = DOTween.Sequence();
        public static Sequence TutorialSequence = DOTween.Sequence();
        public void OnGameRestarted()
        {
            Score = 0;
            Reset();
        }

        public static void Reset()
        {
            SwapCount = 0;
            IsBoosting = false;
            IsPlayingEndGameSequence = false;
            HasJelly = false;
            IsSwapping = false;
            IsTutorial = false;
            IsPlayerWon = false;
            SuggestIndexes.Clear();
            SuggestSequence.Kill();
            TutorialSequence.Kill();
        }
    }
}
