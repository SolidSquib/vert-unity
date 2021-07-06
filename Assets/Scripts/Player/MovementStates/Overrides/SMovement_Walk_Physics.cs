using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Movement State/Defaults/Walk_PHYSICS")]
public class SMovement_Walk_Physics : SPlayerMovementState
{
    public SAttribute moveSpeedAttribute = null;
    public float _gravityScale = 1f;
    public SPlayerMovementState _fallingState;
    [SerializeField] private float _raycastGroundCheckHeight;

    public override bool CanJump() { Debug.Log("CANJUMP WALK"); return true; }

    private Rigidbody GetRigidBody (PlayerMovement playerMovement)
    {
        Rigidbody controller = playerMovement.GetComponent<Rigidbody>();
        if (controller == null)
        {
            Debug.LogError($"Movement state [{name}] is expecting a Rigidbody but none is present.");
            return null;
        }

        return controller;
    }

    public override void EnterState(PlayerMovement playerMovement)
    {
        Animator animator = playerMovement.GetComponentInChildren<Animator>();
        animator.SetBool("isFalling", false);
    }

    public override void UpdateState(PlayerMovement playerMovement, Vector3 inputVector)
    {
        Debug.Log("Walk(Physics) inputVector: " + inputVector);

        Rigidbody controller = GetRigidBody(playerMovement);
        AbilitySystem abilitySystem = playerMovement.GetComponent<AbilitySystem>();
        float moveSpeedModifier = moveSpeedAttribute != null ? abilitySystem.GetAttributeCurrentValue(moveSpeedAttribute) : 1.0f;

        Vector3 localMovementVector = GameplayStatics.ProjectInputVectorToCamera(Camera.main, playerMovement.transform, playerMovement.inputVector);
        Vector3 movementVector = new Vector3(localMovementVector.x * baseMoveSpeed * moveSpeedModifier, controller.velocity.y, localMovementVector.z * baseMoveSpeed * moveSpeedModifier);
        movementVector.y += inputVector.y;

        // Apply gravity
        movementVector.y += (Physics.gravity.y * _gravityScale * Time.deltaTime);

        controller.AddForce(playerMovement.transform.forward * baseMoveSpeed);

        //controller.AddForce(movementVector * Time.deltaTime);
        //controller.Move(movementVector * Time.deltaTime);
    }   


    public override bool CheckShouldSwitchState(PlayerMovement playerMovement, ref SPlayerMovementState newState)
    {
        //Rigidbody controller = GetRigidBody(playerMovement);
        if (Physics.Raycast(playerMovement.transform.position, Vector3.down, _raycastGroundCheckHeight, playerMovement.groundMask))
        {
            return false;
        }

        newState = _fallingState;
        return true;
    }
}
