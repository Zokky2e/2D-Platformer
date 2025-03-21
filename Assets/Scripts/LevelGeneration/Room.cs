using System;
using UnityEngine;

public enum RoomType
{
    Empty,
    Start,
    Enemy,
    Loot,
    Corridor,
    Parkour,
    Boss,
    End
}
public class Room
{
    public RoomType Type { get; set; } = RoomType.Empty;
    public Vector2Int GridPosition { get; set; }
    public bool HasTopExit, HasBottomExit, HasLeftExit, HasRightExit;

    public Room(RoomType type, Vector2Int position)
    {
        Type = type;
        GridPosition = position;
    }
}