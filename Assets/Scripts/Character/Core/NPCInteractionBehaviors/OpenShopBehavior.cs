using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Open Shop")]
public class OpenShopBehavior : NPCInteractionBehavior
{
    public ShopInventory shopInventory;

    public override IEnumerator Execute(NPC npc)
    {
        if (shopInventory != null && shopInventory.items?.Count > 0) 
        {
            ShopUI shopUI = FindAnyObjectByType<ShopUI>();
            if (shopUI != null) 
            {
                shopUI.ToggleShopInventory();
            }
        }
        yield return null; // Let it yield to next behavior
    }
}
