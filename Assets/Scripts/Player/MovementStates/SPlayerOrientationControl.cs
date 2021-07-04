using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SPlayerOrientationControl : ScriptableObject
{
    protected abstract Vector3 GetLookAtDirection(PlayerMovement playerMovement);

    public void OrientPlayer(PlayerMovement playerMovement)
    {
        Vector3 lookAtDirection = GetLookAtDirection(playerMovement);
        playerMovement.transform.forward = lookAtDirection.normalized;
        //playerMovement.transform.LookAt(playerMovement.transform.position + lookAtDirection.normalized);
    }
}
