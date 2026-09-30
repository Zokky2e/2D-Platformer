using System;
using UnityEngine;

// Raises an event so that every listener runs even if one of them throws. The exception is logged instead of
// propagating, so one broken window can't stop the others from refreshing or abort the change that raised it.
public static class SafeEvent
{
    public static void Invoke(Action handlers)
    {
        if (handlers == null)
            return;
        foreach (Action handler in handlers.GetInvocationList())
        {
            try
            {
                handler();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }
    }
}
