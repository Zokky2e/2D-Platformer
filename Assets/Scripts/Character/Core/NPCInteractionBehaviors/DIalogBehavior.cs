using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Show Dialog")]
public class ShowDialogBehavior : NPCInteractionBehavior
{
    public string NPCHasShownDialogString = "NPCHasShownDialogString";
    public bool HasShownDialog = false;
    [TextArea(3, 5)] public string dialogText;

    public override IEnumerator Execute(NPC npc)
    {
        HasShownDialog = WorldStateManager.Instance.GetBool(NPCHasShownDialogString);
        if (!HasShownDialog)
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
