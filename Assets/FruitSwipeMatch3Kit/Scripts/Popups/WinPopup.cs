﻿// Copyright (C) 2019 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.UI;

namespace FruitSwipeMatch3Kit
{
    /// <summary>
    /// This class contains the logic associated to the win popup.
    /// </summary>
	public class WinPopup : EndGamePopup
	{
#pragma warning disable 649
		[SerializeField]
        private Image star1;

        [SerializeField]
        private Image star2;

        [SerializeField]
        private Image star3;

        [SerializeField]
        private ParticleSystem star1Particles;

        [SerializeField]
        private ParticleSystem star2Particles;

        [SerializeField]
        private ParticleSystem star3Particles;

        [SerializeField]
        private Sprite disabledStarSprite;
#pragma warning restore 649

        protected override void Awake()
        {
            base.Awake();
            Assert.IsNotNull(star1);
            Assert.IsNotNull(star2);
            Assert.IsNotNull(star3);
            Assert.IsNotNull(star1Particles);
            Assert.IsNotNull(star2Particles);
            Assert.IsNotNull(star3Particles);
            Assert.IsNotNull(disabledStarSprite);
        }

        public void SetStars(int stars)
        {
            if (stars == 0)
            {
                star1.sprite = disabledStarSprite;
                star2.sprite = disabledStarSprite;
                star3.sprite = disabledStarSprite;
                star1Particles.gameObject.SetActive(false);
                star2Particles.gameObject.SetActive(false);
                star3Particles.gameObject.SetActive(false);
            }
            else if (stars == 1)
            {
                star2.sprite = disabledStarSprite;
                star3.sprite = disabledStarSprite;
                star2Particles.gameObject.SetActive(false);
                star3Particles.gameObject.SetActive(false);
            }
            else if (stars == 2)
            {
                star3.sprite = disabledStarSprite;
                star3Particles.gameObject.SetActive(false);
            }
        }
	}
}
