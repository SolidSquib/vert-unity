using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Jump Behaviour/Defaults/BasicDash")]

public class SBasicPlayerDash : SPlayerDashBehaviour
{
    protected override bool ExecuteDash_Internal(PlayerMovement playerMovement, out Vector3 jumpTargetVelocity)
    {
        CharacterController controller = playerMovement.GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError($"Jump behaviour [{name}] is expecting a CharacterController but none is present.");
            jumpTargetVelocity = Vector3.zero;
            return false;
        }

        jumpTargetVelocity = new Vector3(0, DashVelocity, 0);
        return true;
    }
}
