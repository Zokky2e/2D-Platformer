using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "NPC/Behaviors/Composite")]
public class CompositeNPCBehavior : NPCInteractionBehavior
{
    public List<NPCInteractionBehavior> behaviors;

    public override IEnumerator Execute(NPC npc)
    {
        foreach (var behavior in behaviors)
        {
            if (behavior != null)
            {
                yield return npc.StartCoroutine(behavior.Execute(npc));
            }
        }
    }
}
