using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player Movement/Movement State/Defaults/Fall_PHYSICS")]

public class SMovement_Fall_Physics : SPlayerMovementState
{
    public SAttribute moveSpeedAttribute = null;
    [SerializeField] private float _fallingGravityScale = 1.0f;
    [SerializeField] private float _jumpingGravityScale = 1.0f;
    [SerializeField] private float _JumpHeight = 1.0f;
    public SPlayerMovementState _landedState;
    [Header("Transition Tuning")]
    [SerializeField] private float _groundCheckPreventionTime = 0.5f;
    [SerializeField] private float _raycastGroundCheckHeight;

    private float _stateStartedTimestamp;

    public float fallGravityScale { get { return _fallingGravityScale; } }
    public float jumpGravityScale { get { return _jumpingGravityScale; } }

    public override bool CanJump() { Debug.Log("CANJUMP FALL"); return true; }
    public override bool IsFallingState() { return true; }

    private Rigidbody GetCharacterController(PlayerMovement playerMovement)
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
        _stateStartedTimestamp = Time.time;

        Animator animator = playerMovement.GetComponentInChildren<Animator>();
        animator.SetBool("isFalling", true);
    }

    public override void UpdateState(PlayerMovement playerMovement, Vector3 inputVector)
    {
        Rigidbody controller = GetCharacterController(playerMovement);
        AbilitySystem abilitySystem = playerMovement.GetComponent<AbilitySystem>();
        float moveSpeedModifier = moveSpeedAttribute != null ? abilitySystem.GetAttributeCurrentValue(moveSpeedAttribute) : 1.0f;

        bool jumpFrame = inputVector.y > 0;

        //Vector3 localMovementVector = GameplayStatics.ProjectInputVectorToCamera(Camera.main, playerMovement.transform, playerMovement.inputVector);
        //Vector3 movementVector = new Vector3(localMovementVector.x * baseMoveSpeed * moveSpeedModifier, controller.velocity.y, localMovementVector.z * baseMoveSpeed * moveSpeedModifier);

        if (jumpFrame)
        {
            controller.AddForce(Vector3.up * _JumpHeight);

            //movementVector.y = inputVector.y;
        }

        // Apply gravity
        //bool isFalling = !jumpFrame && controller.velocity.y <= 0;
        //movementVector.y += (Physics.gravity.y * (isFalling ? fallGravityScale : jumpGravityScale) * Time.deltaTime);

        //controller.Move(movementVector * Time.deltaTime);
    }

    public override bool CheckShouldSwitchState(PlayerMovement playerMovement, ref SPlayerMovementState newState)
    {
        if (Time.time < _stateStartedTimestamp + _groundCheckPreventionTime)
        {
            return false;
        }

        //Rigidbody controller = GetRigidBody(playerMovement);
        if (!Physics.Raycast(playerMovement.transform.position, Vector3.down, _raycastGroundCheckHeight, playerMovement.groundMask))
        {
            Debug.Log("Physics Raycast Fail");
            return false;
        }

        Debug.Log("Physics Raycast Success");

        newState = _landedState;
        return true;
    }
}
