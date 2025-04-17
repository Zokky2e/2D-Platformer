using System;
using System.Collections.Generic;
using UnityEngine;


[System.Serializable]
public struct DungeonRoomType
{
    public Room tilePrefab;
    public float spawnChance;
}
[System.Serializable]
public class DungeonRuleEntry
{
    public RoomType roomType;
    public List<DungeonDirectionEntry> directions;
}

[System.Serializable]
public class DungeonDirectionEntry
{
    public NodeShouldGoTo direction;
    public List<DungeonRoomType> roomOptions;
}
public class RoomGeneration : Singleton<RoomGeneration>
{
    [Header("Dungeon Rules")]
    public List<DungeonRuleEntry> ruleEntries;
    public Dictionary<RoomType, Dictionary<NodeShouldGoTo, DungeonRoomType[]>> rules;
    private new void Awake()
    {
        base.Awake();
        rules = new Dictionary<RoomType, Dictionary<NodeShouldGoTo, DungeonRoomType[]>>();

        foreach (var entry in ruleEntries)
        {
            if (!rules.ContainsKey(entry.roomType))
                rules[entry.roomType] = new Dictionary<NodeShouldGoTo, DungeonRoomType[]>();

            foreach (var dir in entry.directions)
            {
                rules[entry.roomType][dir.direction] = dir.roomOptions.ToArray();
            }
        }
    }
}
