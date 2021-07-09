using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SPlayerDashBehaviour : ScriptableObject
{
    public float DashVelocity = 100.0f;
    public SPlayerMovementState DashMovementState;

    public bool ExecuteDash(PlayerMovement playerMovement, out Vector3 jumpTargetVelocity)
    {
        if (ExecuteDash_Internal(playerMovement, out jumpTargetVelocity))
        {
            if (DashMovementState != null)
            {
                playerMovement.overrideMovementState = DashMovementState;
            }
            return true;
        }

        return false;
    }

    protected abstract bool ExecuteDash_Internal(PlayerMovement playerMovement, out Vector3 jumpTargetVelocity);
}
