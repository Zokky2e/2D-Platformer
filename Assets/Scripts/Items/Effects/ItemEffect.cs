using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public abstract class ItemEffect<T> : ScriptableObject
{
    // Base class for all item effects
    public abstract string AdjustDescription(string description);
    public abstract void ApplyEffect(T target);
    public abstract void RemoveEffect(T target);
    public abstract void UseItem(T item);
}
