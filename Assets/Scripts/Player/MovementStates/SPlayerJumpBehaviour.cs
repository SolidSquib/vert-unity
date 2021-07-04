using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SPlayerJumpBehaviour : ScriptableObject
{
    public float jumpVelocity = 100.0f;
    public SPlayerMovementState jumpMovementState;

    public bool ExecuteJump(PlayerMovement playerMovement, out Vector3 jumpTargetVelocity)
    {
        if (ExecuteJump_Internal(playerMovement, out jumpTargetVelocity))
        {
            if (jumpMovementState != null)
            {
                playerMovement.overrideMovementState = jumpMovementState;
            }
            return true;
        }

        return false;
    }

    protected abstract bool ExecuteJump_Internal(PlayerMovement playerMovement, out Vector3 jumpTargetVelocity);
}