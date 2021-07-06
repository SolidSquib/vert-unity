using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Jump Behaviour/Defaults/BasicJump_PHYSICS")]

public class SBasicPlayerJump_Physics : SPlayerJumpBehaviour
{
    protected override bool ExecuteJump_Internal(PlayerMovement playerMovement, out Vector3 jumpTargetVelocity)
    {
        Rigidbody controller = playerMovement.GetComponent<Rigidbody>();
        if (controller == null)
        {
            Debug.LogError($"Jump behaviour [{name}] is expecting a Rigidbody but none is present.");
            jumpTargetVelocity = Vector3.zero;
            return false;
        }

        jumpTargetVelocity = new Vector3(0, jumpVelocity, 0);
        return true;
    }
}
