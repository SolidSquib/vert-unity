using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    // Delegates
    public delegate void SimpleMovementDelegate();
    public delegate void MovementStateChangedDelegate(SPlayerMovementState newState, SPlayerMovementState oldState);
    public SimpleMovementDelegate onJumpApexReached { get; set; }
    public SimpleMovementDelegate onLanded { get; set; }
    public MovementStateChangedDelegate onMovementStateChanged { get; set; }

    // Properties
    private Vector3 _jumpTargetVelocity;

    public Vector3 inputVector { get; set; }
    public int currentNumJumps { get; private set; }
    public bool isCurrentlyJumping { get; private set; }
    public SPlayerMovementState activeMovementState { get; private set; }
    public SPlayerMovementState overrideMovementState { get; set; }

    #region EditorProperties
    /// <summary>
    /// Whether we want to run state updates on the FixedUpdate event (true) or on the Update event (false).
    /// It is recommended to use Fixed update if movement is based on forces attached to a rigidbody.
    /// </summary>
    /// <value>false</value>
    [SerializeField] private bool _useFixedUpdate = false;

    [Header("Jump Settings")]
    [SerializeField] private int _maxJumps = 1;
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private SPlayerMovementState _defaultMovementState;
    [SerializeField] private SPlayerJumpBehaviour _jumpBehaviour;
    [SerializeField] private SPlayerOrientationControl _playerOrientationOverride;
    #endregion

    #region PropertyAccessors
    public int maxJumps { get { return _maxJumps; } set { _maxJumps = value; } }
    public LayerMask groundMask { get { return _groundLayerMask; } }
    #endregion

    private void Awake()
    {
        currentNumJumps = 0;
    }

    public bool CanJump()
    {
        return (maxJumps < 0 || currentNumJumps < maxJumps) && _jumpBehaviour != null && activeMovementState != null && activeMovementState.CanJump();
    }

    public bool IsJumpingOrFalling()
    {
        return activeMovementState != null && activeMovementState.IsFallingState();
    }

    public bool Jump()
    {
        if (!CanJump())
        {
            return false;
        }

        if (!_jumpBehaviour.ExecuteJump(this, out _jumpTargetVelocity))
        {
            return false;
        }

        currentNumJumps += 1;
        isCurrentlyJumping = true;
        return true;
    }

    public void NotifyReachedJumpApex()
    {
        if (onJumpApexReached != null)
        {
            onJumpApexReached();
        }
    }

    private void NotifyLanded()
    {
        if (onLanded != null)
        {
            onLanded();
        }
    }

    private void NotifyStateChanged(SPlayerMovementState newState, SPlayerMovementState previousState)
    {
        if (newState == null)
        {
            Debug.LogWarning($"NotifyStateChanged has been called following a null newState.");
        }

        if (!newState.IsFallingState())
        {
            currentNumJumps = 0;

            if (previousState != null && previousState.IsFallingState())
            {
                NotifyLanded();
            }
        }
        else if (currentNumJumps <= 0)
        {
            // Walking off a ledge should could as the initial jump.
            currentNumJumps += 1;
        }

        if (onMovementStateChanged != null)
        {
            onMovementStateChanged(newState, previousState);
        }
    }

    private void FixedUpdate()
    {
        if (_useFixedUpdate)
        {
            Update_Internal();
        }
    }

    private void Update()
    {
        if (!_useFixedUpdate)
        {
            Update_Internal();
        }
    }

    protected virtual void Update_Internal()
    {
        if (activeMovementState == null)
        {
            if (_defaultMovementState == null)
            {
                Debug.LogError($"No default movement state specified for {name}, unable to move.");
                return;
            }
            activeMovementState = _defaultMovementState;
            activeMovementState.EnterState(this);

            NotifyStateChanged(_defaultMovementState, null);
        }

        if (activeMovementState != null)
        {
            SPlayerMovementState targetState = null, previousState = null;
            bool hasStateChangedThisFrame = false;

            if (overrideMovementState != null)
            {
                if (overrideMovementState != activeMovementState)
                {
                    previousState = activeMovementState;
                    activeMovementState.LeaveState(this);
                    activeMovementState = overrideMovementState;
                    activeMovementState.EnterState(this);
                    hasStateChangedThisFrame = true;
                }

                overrideMovementState = null;
            }

            if (!hasStateChangedThisFrame && activeMovementState.CheckShouldSwitchState(this, ref targetState))
            {
                if (targetState != null)
                {
                    previousState = activeMovementState;
                    activeMovementState.LeaveState(this);
                    activeMovementState = targetState;
                    activeMovementState.EnterState(this);
                    hasStateChangedThisFrame = true;
                }
            }

            if (hasStateChangedThisFrame)
            {
                NotifyStateChanged(activeMovementState, previousState);
            }

            activeMovementState.UpdateState(this, _jumpTargetVelocity);
        }

        if (_playerOrientationOverride != null)
        {
            _playerOrientationOverride.OrientPlayer(this);
        }

        _jumpTargetVelocity = Vector3.zero;
    }
}
