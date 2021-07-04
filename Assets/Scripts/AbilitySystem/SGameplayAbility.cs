using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GameplayAbilityTrigger
{
    public enum ETriggerMethod { GameplayEvent, TagPresent, TagAdded, TagRemoved }

    public Tag triggerTag = null;
    public ETriggerMethod triggerMethod = ETriggerMethod.GameplayEvent;
}

public abstract class SGameplayAbility : ScriptableObject
{
    public delegate void AbilityDelegate(SGameplayAbility ability);
    public AbilityDelegate onAbilityEnded;

    public TagContainer abilityTags;
    public TagContainer activationOwnedTags;
    public TagContainer activationBlockedTags;
    public TagRequirementsContainer activationRequiredTags;
    public TagContainer cancelAbilitiesWithTags;
    public TagContainer blockAbilitiesWithTags;

    public bool retriggerInstancedAbility = false;

    [Header("Ability Triggers")]
    public List<GameplayAbilityTrigger> tagTriggers = new List<GameplayAbilityTrigger>();
    public bool activatedAbilityWhenGranted;

    public AbilitySystem abilitySystem { get; set; }

    // Internal 
    private ActiveAbilitySpec _spec;
    private GameplayEventData _eventData;
    private List<SGameplayAbilityTask> _activeTasks = new List<SGameplayAbilityTask>();
    public ActiveAbilitySpec spec { get { return _spec; } set { _spec = value; } }
    public bool isActive { get { return _spec.active; } }
    public GameplayEventData eventData { get { return _eventData; } private set { _eventData = value; } }

    public virtual void InitializeAbility() {}

    public virtual void ActivateAbility(GameplayEventData payload)
    {
        _spec.active = true;
        _eventData = payload;
    }

    public virtual void EndAbility(bool wasCancelled)
    {
        foreach (var task in _activeTasks)
        {
            task.EndTask();
        }
        _eventData = new GameplayEventData();

        if (onAbilityEnded != null)
        {
            onAbilityEnded(this);
        }

        _spec.active = false;
    }

    public virtual bool CanActivateAbility()
    {
        return true;
    }

    // If this ability is bound to activate by input then this event will fire each time (except the initial activation via input) the bound action is activated
    public virtual void InputKeyDown() { }

    // If this ability is bound to activate by input then this event will fire each time the bound action is cancelled
    public virtual void InputKeyUp() { }
}
