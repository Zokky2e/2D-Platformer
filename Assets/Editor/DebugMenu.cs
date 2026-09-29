using System.Linq;
using UnityEditor;
using UnityEngine;

// Play-mode shortcuts for testing content that can't be obtained in-game yet
public static class DebugMenu
{
    // items.json ids of the weapons with on-hit bleed, poison or burn
    private static readonly int[] StatusEffectWeapons = { 0, 2, 5, 7, 10, 13 };

    [MenuItem("Tools/Debug/Give Status Effect Weapons")]
    private static void GiveStatusEffectWeapons()
    {
        ItemSystem.Instance.AddToPlayerInventory(StatusEffectWeapons);
        Debug.Log("Added the bleed, poison and burn weapons to the inventory");
    }

    [MenuItem("Tools/Debug/Give Status Effect Weapons", true)]
    private static bool CanGiveStatusEffectWeapons() => Application.isPlaying;

    [MenuItem("Tools/Debug/Give All Items")]
    private static void GiveAllItems()
    {
        int[] ids = ItemDatabase.Instance.AllItems.Select(item => item.Id).ToArray();
        ItemSystem.Instance.AddToPlayerInventory(ids);
        Debug.Log($"Added all {ids.Length} items to the inventory");
    }

    [MenuItem("Tools/Debug/Give All Items", true)]
    private static bool CanGiveAllItems() => Application.isPlaying;

    [MenuItem("Tools/Debug/Give 100 Gold")]
    private static void GiveGold()
    {
        InventorySystem.Instance.UpdateGold(100);
    }

    [MenuItem("Tools/Debug/Give 100 Gold", true)]
    private static bool CanGiveGold() => Application.isPlaying;
}
