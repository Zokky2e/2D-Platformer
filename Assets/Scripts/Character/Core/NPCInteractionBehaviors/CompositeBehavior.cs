using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Composite")]
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
