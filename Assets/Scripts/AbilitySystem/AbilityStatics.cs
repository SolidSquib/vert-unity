using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AbilityStatics
{
    public static void SendGameplayEventToAbilitySystem(AbilitySystem target, Tag eventTag, GameplayEventData eventData)
    {
        if (target == null)
        {
            return;
        }

        target.ProcessGameplayEvent(eventTag, eventData);
    }
}
