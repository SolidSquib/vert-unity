using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SPlayerMovementState : ScriptableObject
{
    [SerializeField] private float _baseMoveSpeed = 10;

    public float baseMoveSpeed { get { return _baseMoveSpeed; } protected set { _baseMoveSpeed = value; } }

    public virtual bool CanJump() { return false; }
    public virtual bool IsFallingState() { return false; }
    public virtual void EnterState(PlayerMovement playerMovement) {}
    public abstract void UpdateState(PlayerMovement playerMovement, Vector3 inputVector);
    public virtual void LeaveState(PlayerMovement playerMovement) {}

    public virtual bool CheckShouldSwitchState(PlayerMovement playerMovement, ref SPlayerMovementState newState) { return false; }
}
