using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Give Item")]
public class GiveItemBehavior : NPCInteractionBehavior
{
    public string NPCSHasGivenItemString = "NPCSHasGivenItemString"; // World-state flag, saved with the game
    public List<int> itemIdsToGive; // Just item IDs, quantity is always 1

    public override IEnumerator Execute(NPC npc)
    {
        // Read the flag each time instead of caching it on this asset (shared, and persists in the Editor)
        bool hasGivenItem = WorldStateManager.Instance.GetBool(NPCSHasGivenItemString);
        if (!hasGivenItem)
        {
            ItemSystem.Instance.AddToPlayerInventory(itemIdsToGive.ToArray());
            WorldStateManager.Instance.SetBool(NPCSHasGivenItemString, true);
        }
        yield return null;
    }
}
