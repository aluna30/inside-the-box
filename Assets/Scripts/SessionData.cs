using System.Collections.Generic;
using UnityEngine.UI;

public static class SessionData
{
    public static int mazeSize { get; set; }
    public static bool isCustomLevel { get; set; }
    public static bool isSingleMazeLevel { get; set; }
    public static int nextLevelIdx { get; set; }
    public static List<Button> levelButtons { get; set; }
    public static List<int> unlockedLevelButtonsIdxs { get; set; }

    public static readonly List<int> levelSizes = new List<int>() { 5, 7, 9, 11, 15, 17, 21, 27, 33, 41 };
}
