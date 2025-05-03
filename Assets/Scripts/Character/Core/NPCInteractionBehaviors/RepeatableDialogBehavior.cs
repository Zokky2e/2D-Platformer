using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Repeatable Show Dialog")]
public class RepeatableDialogBehavior : NPCInteractionBehavior
{
    [TextArea(3, 5)] public string dialogText;

    public override void Execute(NPC npc)
    {
        DialogSystem.Instance.ShowDialog(npc.npcName, dialogText);
    }
}
