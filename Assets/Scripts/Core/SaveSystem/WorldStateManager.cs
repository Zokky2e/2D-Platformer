using System.Collections.Generic;

// Named flags for quest and dialog progress (e.g. "Merchant_Amulet_Given"). SaveSystem persists them.
public class WorldStateManager : Singleton<WorldStateManager>
{

    private Dictionary<string, bool> boolStates = new();
    private Dictionary<string, int> intStates = new();
    private Dictionary<string, string> stringStates = new();

    protected override void Awake()
    {
        base.Awake();
        if (IsDuplicate) return;
        // Restore flags before any NPC.Start reads them
        WorldStateData saved = SaveSystem.Instance.LoadedData?.world;
        if (saved != null)
            LoadFromData(saved);
    }

    public void SetBool(string key, bool value) => boolStates[key] = value;
    public bool GetBool(string key) => boolStates.TryGetValue(key, out var v) && v;

    public void SetInt(string key, int value) => intStates[key] = value;
    public int GetInt(string key) => intStates.TryGetValue(key, out var v) ? v : 0;

    public void SetString(string key, string value) => stringStates[key] = value;
    public string GetString(string key) => stringStates.TryGetValue(key, out var v) ? v : null;

    public WorldStateData GetData() => new WorldStateData(boolStates, intStates, stringStates);

    public void LoadFromData(WorldStateData data)
    {
        boolStates = new(data.boolStates ?? new());
        intStates = new(data.intStates ?? new());
        stringStates = new(data.stringStates ?? new());
    }
}
