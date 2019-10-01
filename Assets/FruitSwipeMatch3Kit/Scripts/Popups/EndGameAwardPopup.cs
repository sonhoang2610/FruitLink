﻿// Copyright (C) 2019 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using System.Collections;
using UnityEngine;

namespace FruitSwipeMatch3Kit
{
    /// <summary>
    /// This class contains the logic associated to the end of game award popup.
    /// </summary>
    public class EndGameAwardPopup : Popup
    {
        protected override void Start()
        {
            base.Start();
            StartCoroutine(AutoClose());
        }

        private IEnumerator AutoClose()
        {
            yield return new WaitForSeconds(1.5f);
            Close();
        }
    }
}
