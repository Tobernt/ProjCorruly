using UnityEngine;

/// <summary>
/// Configuration settings for dungeon generation, used to pass parameters to algorithms.
/// </summary>
public class DungeonGenSettings
{
    public int roomCount;
    public int minSize;
    public int maxSize;
    public bool allowCircular;
    public bool allowOverlap;
    public bool onlyRound = false;
    public bool autoConnectVertical = true; 
    public DungeonGenerator.VerticalConnectorType verticalConnectorStyle = DungeonGenerator.VerticalConnectorType.StraightRamp;
    public int floors;
    public bool allowElevation;
    public int seed;
    public DungeonGenerator.AlgorithmType algorithm;
    public bool addRoofs;
}
