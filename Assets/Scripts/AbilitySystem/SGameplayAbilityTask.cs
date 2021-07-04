using System.Collections;
using UnityEngine;

public abstract class SGameplayAbilityTask : MonoBehaviour
{   
    SGameplayAbility owningAbility;
    AbilitySystem owningSystem;

    public virtual void InitTask(SGameplayAbility executingAbility, AbilitySystem executingSystem)
    {
        owningAbility = executingAbility;
        owningSystem = executingSystem;
    }

    public abstract IEnumerator RunTask();

    public virtual void EndTask()
    {
        // Do nothing by default.
    }
}
