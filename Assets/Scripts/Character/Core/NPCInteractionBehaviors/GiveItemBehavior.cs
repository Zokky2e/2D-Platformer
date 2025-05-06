using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Give Item")]
public class GiveItemBehavior : NPCInteractionBehavior
{
    public string NPCSHasGivenItemString = "NPCSHasGivenItemString";
    public bool HasGivenItem = false;
    public List<int> itemIdsToGive; // Just item IDs, quantity is always 1

    public override IEnumerator Execute(NPC npc)
    {
        HasGivenItem = WorldStateManager.Instance.GetBool(NPCSHasGivenItemString);
        if (!HasGivenItem)
        {
            ItemSystem.Instance.AddToPlayerInventory(itemIdsToGive.ToArray());
            WorldStateManager.Instance.SetBool(NPCSHasGivenItemString, true);
        }
        yield return null;
    }
}
