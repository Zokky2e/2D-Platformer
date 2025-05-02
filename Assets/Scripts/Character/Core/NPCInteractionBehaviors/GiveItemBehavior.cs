using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Give Item")]
public class GiveItemBehavior : NPCInteractionBehavior
{
    public List<int> itemIdsToGive; // Just item IDs, quantity is always 1

    public override void Execute(NPC npc)
    {
        ItemSystem.Instance.AddToPlayerInventory(itemIdsToGive.ToArray());
    }
}
