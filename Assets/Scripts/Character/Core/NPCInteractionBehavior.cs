using System.Collections;
using UnityEngine;

public abstract class NPCInteractionBehavior : ScriptableObject
{
    public abstract IEnumerator Execute(NPC npc);
}