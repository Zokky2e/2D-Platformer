using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class WorldStateManager : Singleton<WorldStateManager>
{

    private Dictionary<string, bool> boolStates = new();
    private Dictionary<string, int> intStates = new();
    private Dictionary<string, string> stringStates = new();
    [SerializeField] private string saveFileName = "worldstate.json";

    protected override void Awake()
    {
        base.Awake();
        Save();
    }

    public void SetBool(string key, bool value) => boolStates[key] = value;
    public bool GetBool(string key) => boolStates.TryGetValue(key, out var v) && v;

    public void SetInt(string key, int value) => intStates[key] = value;
    public int GetInt(string key) => intStates.TryGetValue(key, out var v) ? v : 0;

    public void SetString(string key, string value) => stringStates[key] = value;
    public string GetString(string key) => stringStates.TryGetValue(key, out var v) ? v : null;

    public void LoadFromData(WorldStateData data)
    {
        boolStates = new(data.boolStates);
        intStates = new(data.intStates);
        stringStates = new(data.stringStates);
    }

    public void Save()
    {
        var data = new WorldStateData(boolStates, intStates, stringStates);
        var json = JsonConvert.SerializeObject(data, Formatting.Indented);
        File.WriteAllText(Path.Combine(Application.persistentDataPath, saveFileName), json);
        Debug.Log(json);
    }

    public void Load()
    {
        var path = Path.Combine(Application.persistentDataPath, saveFileName);
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<WorldStateData>(json);
            boolStates = data.boolStates;
            intStates = data.intStates;
            stringStates = data.stringStates;
        }
    }
}
