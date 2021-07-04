using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Orientation Control/Defaults/2D")]
public class S2DPlayerOrientationControl : SPlayerOrientationControl
{
    public List<SPlayerMovementState> allowedMovementStates = new List<SPlayerMovementState>();

    protected override Vector3 GetLookAtDirection(PlayerMovement playerMovement)
    {
        if (playerMovement.inputVector.x != 0 && allowedMovementStates.Contains(playerMovement.activeMovementState))
        {
            return new Vector3(playerMovement.inputVector.x, 0, 0);
        }

        return playerMovement.transform.forward;
    }
}
