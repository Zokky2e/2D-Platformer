using System.Collections.Generic;

// Quest/dialog flags as stored in the save file
public class WorldStateData
{
    public Dictionary<string, bool> boolStates = new();
    public Dictionary<string, int> intStates = new();
    public Dictionary<string, string> stringStates = new();

    // Newtonsoft needs this to deserialize (it would pass nulls to the constructor below)
    public WorldStateData() { }

    public WorldStateData(Dictionary<string, bool> b, Dictionary<string, int> i, Dictionary<string, string> s)
    {
        boolStates = new(b);
        intStates = new(i);
        stringStates = new(s);
    }
}
