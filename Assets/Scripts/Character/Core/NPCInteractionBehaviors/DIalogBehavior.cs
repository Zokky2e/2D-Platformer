using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Show Dialog")]
public class ShowDialogBehavior : NPCInteractionBehavior
{
    public string NPCHasShownDialogString = "NPCHasShownDialogString";
    public bool HasShownDialog = false;
    [TextArea(3, 5)] public string dialogText;

    public override void Execute(NPC npc)
    {
        HasShownDialog = WorldStateManager.Instance.GetBool(NPCHasShownDialogString);
        if (!HasShownDialog) 
        {
            DialogSystem.Instance.ShowDialog(npc.npcName, dialogText);
            WorldStateManager.Instance.SetBool(NPCHasShownDialogString, true);
        }
    }
}
