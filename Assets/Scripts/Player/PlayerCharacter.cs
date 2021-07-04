using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(AbilitySystem))]
public class PlayerCharacter : MonoBehaviour
{
    PlayerMovement _playerMovement;
    PlayerInput _playerInput;
    AbilitySystem _abilitySystem;
    GameplayDebuggerUI _gameplayDebugger;

    void Awake()
    {
        _playerMovement = GetComponent<PlayerMovement>();
        _playerInput = GetComponent<PlayerInput>();    
        _abilitySystem = GetComponent<AbilitySystem>();

        _playerInput.onActionTriggered += OnInputActionTriggered;
    }

    void Start()
    {
        if (_gameplayDebugger != null)
        {
            _gameplayDebugger.BindAbilitySystemEvents(_abilitySystem);
            _gameplayDebugger.gameObject.SetActive(false);
        }
    }

    void OnInputActionTriggered(InputAction.CallbackContext context)
    {
        if (context.action.name == "Move")
        {
            Vector2 value = context.ReadValue<Vector2>();
            _playerMovement.inputVector = new Vector3(value.x, value.y, 0);
        }
        else if (context.action.name == "GameplayDebugger" && context.performed)
        {
            if (_gameplayDebugger != null)
            {
                _gameplayDebugger.gameObject.SetActive(!_gameplayDebugger.gameObject.activeInHierarchy);
            }
        }
    }
}
