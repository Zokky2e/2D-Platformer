using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Show Dialog")]
public class ShowDialogBehavior : NPCInteractionBehavior
{
    public string NPCHasShownDialogString = "NPCHasShownDialogString"; // World-state flag, saved with the game
    [TextArea(3, 5)] public string dialogText;

    public override IEnumerator Execute(NPC npc)
    {
        // Read the flag each time instead of caching it on this asset (shared, and persists in the Editor)
        bool hasShownDialog = WorldStateManager.Instance.GetBool(NPCHasShownDialogString);
        if (!hasShownDialog)
        {
            bool dialogDone = false;
            DialogSystem.Instance.ShowDialog(npc.npcName, dialogText, () => dialogDone = true);
            WorldStateManager.Instance.SetBool(NPCHasShownDialogString, true);
            yield return new WaitUntil(() => dialogDone);
        }
        else
        {
            yield return null;
        }
    }
}
