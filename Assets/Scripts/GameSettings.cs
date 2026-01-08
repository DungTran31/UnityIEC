using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSettings : ScriptableObject
{
    public int BoardSizeX = 4;

    public int BoardSizeY = 6;

    public int BottomCells = 5;

    public int MatchesMin = 3;

    public int LevelMoves = 16;

    public float LevelTime = 60f;

    public float TimeForHint = 5f;
}
