using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Show Dialog")]
public class ShowDialogBehavior : NPCInteractionBehavior
{
    [TextArea(3, 5)] public string dialogText;

    public override void Execute(NPC npc)
    {
        DialogSystem.Instance.ShowDialog(npc.npcName, dialogText);
    }
}
