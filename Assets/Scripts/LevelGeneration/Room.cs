using System;
using UnityEngine;

public enum RoomType
{
    Empty,
    Start,
    Enemy,
    Loot,
    CorridorLR,
    ParkourLTRB,
    ParkourLTR,
    ParkourLRB,
    Boss,
    End
}
public class Room : MonoBehaviour
{
    public RoomType Type;
    public Tuple<int, int> location;

}