using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Repeatable Show Dialog")]
public class RepeatableDialogBehavior : NPCInteractionBehavior
{
    [TextArea(3, 5)] public string dialogText;

    public override IEnumerator Execute(NPC npc)
    {
        bool dialogDone = false;
        DialogSystem.Instance.ShowDialog(npc.npcName, dialogText, () => dialogDone = true);
        // Wait until dialog is done
        yield return new WaitUntil(() => dialogDone);
    }
}
