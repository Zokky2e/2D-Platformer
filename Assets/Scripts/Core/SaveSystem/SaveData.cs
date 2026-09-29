using System.Collections.Generic;

// Everything written to the save file. Plain public fields only: Newtonsoft (de)serializes this as JSON,
// so renaming a field breaks existing saves (bump SaveSystem.CurrentVersion and migrate instead).
public class SaveData
{
    public int version = SaveSystem.CurrentVersion;
    public WorldStateData world = new();
    public PlayerSaveData player; // Null if the save was written without a player in the scene
}

public class PlayerSaveData
{
    public int gold;
    public float health;
    public float? mana; // Null in saves from before mana existed
    public int dungeonLevel;
    public List<int> equippedItemIds = new();
    public List<int> inventoryItemIds = new();

    // Last activated checkpoint, empty when none. Stored as floats because Vector3 doesn't
    // round-trip through Newtonsoft cleanly.
    public string checkpointScene;
    public float checkpointX;
    public float checkpointY;
}
