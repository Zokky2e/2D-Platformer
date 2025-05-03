using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Conditional Action")]
public class ConditionalActionBehavior : NPCInteractionBehavior
{
    public string ConditionalStateString = "ConditionalActionString";
    public NPCAction conditionalAction = NPCAction.None;

    public override void Execute(NPC npc)
    {
        bool isConditionMet = WorldStateManager.Instance.GetBool(ConditionalStateString);
        if (isConditionMet) 
        {
            npc.SetAction(conditionalAction);
        }
    }
}
