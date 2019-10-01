﻿// Copyright (C) 2019 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;

namespace FruitSwipeMatch3Kit
{
    /// <summary>
    /// Miscellaneous file utilities.
    /// </summary>
    public static class FileUtils
    {
        public static LevelData LoadLevel(int levelNum)
        {
            return Resources.Load<LevelData>($"Levels/{levelNum}");
        }
		
        public static bool FileExists(string path)
        {
            var level = Resources.Load<LevelData>(path);
            return level != null;
        }
    }
}