using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Ability System/Abilities/Dash")]

public class SDash_GA : SGameplayAbility
{
    PlayerMovement _movement;
    public override void InitializeAbility()
    {
        _movement = abilitySystem.GetComponent<PlayerMovement>();
    }

    public override void ActivateAbility(GameplayEventData payload)
    {
        base.ActivateAbility(payload);

        if (_movement != null)
        {
            _movement.Dash();
        }

        EndAbility(false);
    }

    public override bool CanActivateAbility()
    {
        return _movement != null && _movement.CanDash();
    }
}
