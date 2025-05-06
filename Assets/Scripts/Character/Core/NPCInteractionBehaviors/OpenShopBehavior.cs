using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Open Shop")]
public class OpenShopBehavior : NPCInteractionBehavior
{
    public ShopInventory shopInventory;

    public override IEnumerator Execute(NPC npc)
    {
        if (shopInventory != null && shopInventory.itemsData?.Count > 0) 
        {
            shopInventory.SetItems();
            ShopUI shopUI = FindAnyObjectByType<ShopUI>();
            if (shopUI != null) 
            {
                shopUI.shopKeeperName = npc.npcName;
                shopUI.SetShopInventory(shopInventory);
                shopUI.ToggleShopInventory();
            }
        }
        yield return null; // Let it yield to next behavior
    }
}
