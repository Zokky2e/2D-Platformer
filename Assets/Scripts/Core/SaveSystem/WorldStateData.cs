using System.Collections.Generic;

[System.Serializable]
public class WorldStateData
{
    public Dictionary<string, bool> boolStates;
    public Dictionary<string, int> intStates;
    public Dictionary<string, string> stringStates;

    public WorldStateData(Dictionary<string, bool> b, Dictionary<string, int> i, Dictionary<string, string> s)
    {
        boolStates = new(b);
        intStates = new(i);
        stringStates = new(s);
    }
}
