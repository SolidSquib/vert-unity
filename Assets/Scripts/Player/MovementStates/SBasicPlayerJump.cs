using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Jump Behaviour/Defaults/BasicJump")]
public class SBasicPlayerJump : SPlayerJumpBehaviour
{
    protected override bool ExecuteJump_Internal(PlayerMovement playerMovement, out Vector3 jumpTargetVelocity)
    {
        CharacterController controller = playerMovement.GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError($"Jump behaviour [{name}] is expecting a CharacterController but none is present.");
            jumpTargetVelocity = Vector3.zero;
            return false;
        }
        
        jumpTargetVelocity = new Vector3(0, jumpVelocity, 0);
        return true;
    }
}