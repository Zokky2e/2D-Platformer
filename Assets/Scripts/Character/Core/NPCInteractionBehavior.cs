using UnityEngine;

public abstract class NPCInteractionBehavior : ScriptableObject
{
    public abstract void Execute(NPC npc);
}