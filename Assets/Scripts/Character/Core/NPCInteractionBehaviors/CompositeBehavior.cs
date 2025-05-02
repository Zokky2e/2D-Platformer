using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/BehaviorsNPC/Behaviors/Give Item/Composite")]
public class CompositeNPCBehavior : NPCInteractionBehavior
{
    public List<NPCInteractionBehavior> behaviors;

    public override void Execute(NPC npc)
    {
        foreach (var behavior in behaviors)
        {
            if (behavior != null)
            {
                behavior.Execute(npc);
            }
        }
    }
}
