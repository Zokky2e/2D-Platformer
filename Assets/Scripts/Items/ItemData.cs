using System.Collections.Generic;

[System.Serializable]
public class ItemData
{
    public int id;
    public string name;
    public string description;
    public ItemType type;
    public string spriteName;
    public int price;
    public bool isSellable;

    public List<EffectData> characterStatsEffects = new();
    public List<EffectData> healthEffects = new();
    public List<EffectData> onActivateCharacterStatsEffects = new();
    public List<EffectData> onActivateHealthEffects = new();
}

[System.Serializable]
public class EffectData
{
    public string effectType; // "Armor", "Health", etc.
    public float value;       // e.g., bonus value like +10 health
}
