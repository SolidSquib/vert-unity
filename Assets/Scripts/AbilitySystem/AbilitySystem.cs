
using System.Diagnostics.Contracts;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum EAbilityRemovalPolicy { CancelImmediately, WaitForEnd };
[Serializable]
public struct GrantedAbilityInfo
{
    public SGameplayAbility ability;
    public string inputActionBinding;
    public EAbilityRemovalPolicy removalPolicy;
};

public class ActiveAbilitySpec
{
    public SGameplayAbility ability { get; set; } = null;
    public SGameplayAbility abilityTemplate { get; set; } = null;
    public EAbilityRemovalPolicy removalPolicy { get; set; } = EAbilityRemovalPolicy.CancelImmediately;
    public bool active { get; set; } = false;
    public bool inputActive { get; set; } = false;
}

public struct GameplayEventData
{
    AbilitySystem Source;
    AbilitySystem Target;
};

public struct ActiveEffectHandle
{
    public int id;
    public AbilitySystem target;
    public AbilitySystem source;

    public bool IsValid()
    {
        return id > -1 && target != null && source != null && target.GetActiveGameplayEffectSpecFromHandle(this) != null;
    }

    public static bool operator ==(ActiveEffectHandle a, ActiveEffectHandle b)
    {
        return a.id == b.id && a.source == b.source && a.target == b.target;
    }

    public static bool operator !=(ActiveEffectHandle a, ActiveEffectHandle b)
    {
        return a.id != b.id || a.source != b.source || a.target != b.target;
    }

    public override bool Equals(object obj)
    {
        if (!(obj is ActiveEffectHandle handle))
        {
            return false;
        }

        return this == handle;
    }

    public override int GetHashCode()
    {
        unchecked // doesn't check for overflow, just wraps
        {
            int hash = 17;
            hash *= (23 + id.GetHashCode());
            hash *= (23 + target.GetHashCode());
            hash *= (23 + source.GetHashCode());
            return hash;
        }
    }
    public static ActiveEffectHandle Invalid = new ActiveEffectHandle { id = -1, source = null, target = null };
}

public class AbilitySystem : MonoBehaviour
{
    public delegate void AbilityDelegate(SGameplayAbility ability);
    public delegate void GameplayEventNotify(Tag eventTag, GameplayEventData eventData);

    public AbilityDelegate onAbilityEnded;
    public AbilityDelegate onAbilityActivated;
    public AbilityDelegate onAbilityActivationFailed;
    public ActiveEffectDelegate onActiveEffectAdded;
    public ActiveEffectDelegate onActiveEffectRemoved;
    private Dictionary<Tag, GameplayEventNotify> _genericGameplayEventCallbacks;
    private SAttributeSet _attributeSetInstance;
    private ActiveGameplayEffectsContainer _activeGameplayEffects = null;

    // Internal properties
    public Animator animator { get; private set; }
    public GameObject owner { get; private set; }
    public GameObject avatar { get; private set; }
    public SAttributeSet attributeSetInstance { get { return _attributeSetInstance; } private set { _attributeSetInstance = value; } }
    public SAttributeSet attributeSet { get { return _attributeSet; } private set { _attributeSet = value; } }
    public bool initialized { get; private set; }
    CountingTagContainer _dynamicOwnedTags = new CountingTagContainer();
    CountingTagContainer _activationBlockedTags = new CountingTagContainer();

    public CountingTagContainer dynamicOwnedTags { get { return _dynamicOwnedTags; } private set { _dynamicOwnedTags = value; } }
    public CountingTagContainer activationBlockedTags { get { return _activationBlockedTags; } private set { _activationBlockedTags = value; } }

    List<ActiveAbilitySpec> _ownedAbilities = new List<ActiveAbilitySpec>();
    Dictionary<string, List<ActiveAbilitySpec>> _inputBoundAbilities = new Dictionary<string, List<ActiveAbilitySpec>>();

    [SerializeField] private GrantedAbilityInfo[] _startupAbilities;
    [SerializeField] private SGameplayEffect[] _startupEffects;
    [SerializeField] private SAttributeSet _attributeSet;

    private void Awake()
    {
        PlayerInput playerInput = GetComponent<PlayerInput>();
        if (playerInput)
        {
            playerInput.onActionTriggered += ProcessInputAction;
        }
        else
        {
            Debug.LogWarning("No PlayerInput object found on the part of an AbilitySystem. Abilities will not be activated by input.");
        }

        _activeGameplayEffects = new ActiveGameplayEffectsContainer(this);
        _activeGameplayEffects.onActiveEffectAdded += HandleActiveEffectAdded;
        _activeGameplayEffects.onActiveEffectRemoved += HandleActiveEffectRemoved;

        if (_attributeSet != null)
        {
            _attributeSetInstance = ScriptableObject.Instantiate(_attributeSet);
            _attributeSetInstance.Initialize(this);
        }

        foreach (var ability in _startupAbilities)
        {
            if (ability.ability != null)
            {
                if (!GrantAbility(ability.ability, ability.inputActionBinding, ability.removalPolicy))
                {
                    Debug.LogWarning($"Failed to grant ability {ability.ability.ToString()}.");
                }
            }
        }

        foreach (var effect in _startupEffects)
        {
            if (effect != null)
            {
                ApplyGameplayEffectToSelf(effect);
            }
        }
    }

    private void Update()
    {
        _activeGameplayEffects.RemoveExpiredGameplayEffects();
    }

    private void LateUpdate()
    {
        // Sends attribute updated notifiers for attributes that have been changed this frame.
        if (_attributeSetInstance != null)
        {
            _attributeSetInstance.FlushDirtyAttributes();
        }
    }

    public void InitializeAbilitySystem(GameObject newOwner, GameObject newAvatar)
    {
        owner = newOwner;
        avatar = newAvatar;
        animator = newAvatar.GetComponentInChildren<Animator>();
        initialized = true;
    }

    public ActiveAbilitySpec GetAbilitySpecFromAbility(SGameplayAbility ability)
    {
        return _ownedAbilities.Find(item => item.ability == ability);
    }

    public bool HasAbility(SGameplayAbility ability)
    {
        return _ownedAbilities.Find(item => item.abilityTemplate == ability || item.ability == ability) != null;
    }

    public List<ActiveAbilitySpec> GetActiveAbilitySpecs()
    {
        return _ownedAbilities.FindAll(item => item.active);
    }

    public List<ActiveAbilitySpec> GetAbilitySpecsBoundToInput(string inputAction)
    {
        return _inputBoundAbilities.ContainsKey(inputAction) ? _inputBoundAbilities[inputAction] : new List<ActiveAbilitySpec>();
    }

    public bool GrantAbility(SGameplayAbility ability, string inputActionBinding = "", EAbilityRemovalPolicy removalPolicy = EAbilityRemovalPolicy.CancelImmediately)
    {
        GrantedAbilityInfo abilityToGrant;
        abilityToGrant.ability = ability;
        abilityToGrant.inputActionBinding = inputActionBinding;
        abilityToGrant.removalPolicy = removalPolicy;
        return GrantAbility(abilityToGrant);
    }

    public bool GrantAbility(GrantedAbilityInfo abilityToGrant)
    {
        if (HasAbility(abilityToGrant.ability))
        {
            Debug.LogWarning($"Attempted to add ability {abilityToGrant.ability.name} when it has already been previously granted.");
            return false;
        }

        ActiveAbilitySpec newSpec = new ActiveAbilitySpec();
        newSpec.ability = Instantiate(abilityToGrant.ability);
        newSpec.ability.name = $"{abilityToGrant.ability.name}_instance";
        newSpec.ability.abilitySystem = this;
        newSpec.ability.spec = newSpec;
        newSpec.abilityTemplate = abilityToGrant.ability;
        newSpec.removalPolicy = abilityToGrant.removalPolicy;
        newSpec.ability.InitializeAbility();
        _ownedAbilities.Add(newSpec);

        if (abilityToGrant.inputActionBinding.Length > 0)
        {
            if (!_inputBoundAbilities.ContainsKey(abilityToGrant.inputActionBinding))
            {
                _inputBoundAbilities.Add(abilityToGrant.inputActionBinding, new List<ActiveAbilitySpec>());
            }

            List<ActiveAbilitySpec> currentBoundAbilities = _inputBoundAbilities[abilityToGrant.inputActionBinding];
            if (!currentBoundAbilities.Contains(newSpec))
            {
                currentBoundAbilities.Add(newSpec);
            }
        }

        return true;
    }

    public int ProcessGameplayEvent(Tag eventTag, GameplayEventData payload)
    {
        int numActivatedAbilities = 0;
        List<ActiveAbilitySpec> eventResponders = _ownedAbilities.FindAll(spec =>
        {
            return spec.ability.tagTriggers.Find(trigger => eventTag.IsChildOf(trigger.triggerTag) && trigger.triggerMethod == GameplayAbilityTrigger.ETriggerMethod.GameplayEvent) != null;
        });

        foreach (var spec in eventResponders)
        {
            TryActivateAbilitySpec(spec, payload);
        }

        GameplayEventNotify genericCallbacks;
        if (_genericGameplayEventCallbacks.TryGetValue(eventTag, out genericCallbacks))
        {
            genericCallbacks(eventTag, payload);
        }

        return numActivatedAbilities;
    }

    public void RegisterGenericGameplayEventCallback(Tag eventTag, GameplayEventNotify callback)
    {
        GameplayEventNotify existingCallback;
        if (_genericGameplayEventCallbacks.TryGetValue(eventTag, out existingCallback))
        {
            existingCallback += callback;
        }
        else
        {
            _genericGameplayEventCallbacks.Add(eventTag, callback);
        }
    }

    public void UnregisterGenericGameplayEventCallback(Tag eventTag, GameplayEventNotify callback)
    {
        GameplayEventNotify existingCallback;
        if (_genericGameplayEventCallbacks.TryGetValue(eventTag, out existingCallback))
        {
            existingCallback -= callback;

            if (existingCallback == null)
            {
                _genericGameplayEventCallbacks.Remove(eventTag);
            }
        }
    }

    public void RegisterOnAttributeChangedCallback(SAttribute attribute, AttributeEventHandler callback)
    {
        if (_attributeSetInstance != null)
        {
            _attributeSetInstance.RegisterOnAttributeChangedCallback(attribute, callback);
        }
    }

    public void UnregisterOnAttributeChangedCallback(SAttribute attribute, AttributeEventHandler callback)
    {
        if (_attributeSetInstance != null)
        {
            _attributeSetInstance.UnregisterOnAttributeChangedCallback(attribute, callback);
        }
    }

    private void ProcessInputAction(InputAction.CallbackContext context)
    {
        List<ActiveAbilitySpec> abilitiesToActivate = GetAbilitySpecsBoundToInput(context.action.name);

        foreach (var spec in abilitiesToActivate)
        {
            if (context.performed)
            {
                if (!TryActivateAbilitySpec(spec, new GameplayEventData()) && spec.active && !spec.inputActive)
                {
                    spec.ability.InputKeyDown();
                }

                spec.inputActive = true;
            }
            else if (context.canceled)
            {
                if (spec.active && spec.inputActive)
                {
                    spec.ability.InputKeyUp();
                }

                spec.inputActive = false;
            }
        }
    }

    private bool TryActivateAbilitySpec(ActiveAbilitySpec spec, GameplayEventData payload)
    {
        if (!CanActivateAbilitySpec(spec))
        {
            return false;
        }

        ActivateAbilitySpec_Internal(spec, payload);
        return true;
    }

    private void ActivateAbilitySpec_Internal(ActiveAbilitySpec spec, GameplayEventData payload)
    {
        spec.ability.onAbilityEnded = NotifyAbilityEnded;
        _dynamicOwnedTags.AddTags(spec.abilityTemplate.activationOwnedTags);

        if (onAbilityActivated != null)
        {
            onAbilityActivated(spec.ability);
        }

        spec.ability.ActivateAbility(payload);
    }

    private bool CanActivateAbilitySpec(ActiveAbilitySpec spec)
    {
        if (spec.ability == null)
        {
            Debug.LogWarning($"Unable to start a null ability.");
            return false;
        }

        if (!_dynamicOwnedTags.AllTagsMatch(spec.ability.activationRequiredTags.required) || _dynamicOwnedTags.AnyTagsMatch(spec.ability.activationRequiredTags.ignored))
        {
            Debug.LogWarning($"Unable to activate ability \"{spec.ability.name}\", tag requirements not met.");
            return false;
        }

        if (_activationBlockedTags.AnyTagsMatch(spec.ability.abilityTags))
        {
            Debug.LogWarning($"Unable to activate ability \"{spec.ability.name}\", ability blocked by tags.");
            return false;
        }

        if (!spec.ability.CanActivateAbility())
        {
            Debug.LogWarning($"Unable to activate ability \"{spec.ability.name}\", because its implementation of CanActivateAbility returned false.");
            return false;
        }

        if (spec.active && !spec.ability.retriggerInstancedAbility)
        {
            return false;
        }

        return true;
    }

    private void NotifyAbilityEnded(SGameplayAbility ability)
    {
        if (HasAbility(ability))
        {
            _dynamicOwnedTags.RemoveTags(ability.activationOwnedTags);

            ability.onAbilityEnded = null;

            if (onAbilityEnded != null)
            {
                onAbilityEnded(ability);
            }
        }
    }

    public GameplayEffectSpec MakeGameplayEffectSpec(SGameplayEffect effect)
    {
        return new GameplayEffectSpec(this, effect);
    }

    public GameplayEffectSpec GetActiveGameplayEffectSpecFromHandle(ActiveEffectHandle handle)
    {
        GameplayEffectSpec spec;
        if (_activeGameplayEffects.TryGetSpecFromHandle(handle, out spec))
        {
            return spec;
        }

        return null;
    }

    public ActiveEffectHandle ApplyGameplayEffectToSelf(SGameplayEffect effect)
    {
        if (effect != null)
        {
            GameplayEffectSpec spec = MakeGameplayEffectSpec(effect);
            return ApplyGameplayEffectSpecToSelf(spec);
        }

        return ActiveEffectHandle.Invalid;
    }

    public ActiveEffectHandle ApplyGameplayEffectToTarget(SGameplayEffect effect, AbilitySystem target)
    {
        if (effect != null)
        {
            GameplayEffectSpec spec = MakeGameplayEffectSpec(effect);
            return ApplyGameplayEffectSpecToTarget(spec, target);
        }

        return ActiveEffectHandle.Invalid;
    }

    public ActiveEffectHandle ApplyGameplayEffectSpecToSelf(GameplayEffectSpec spec)
    {
        if (spec == null)
        {
            return ActiveEffectHandle.Invalid;
        }

        spec.target = this;

        /* Check the attribute set requirements and make sure all attributes are valid before 
         * commiting to application. */
        foreach (GameplayEffectAttributeModifier modifier in spec.effectTemplate.modifiers)
        {
            if (modifier.attribute == null)
            {
                Debug.LogWarning($"{spec.effectTemplate.name} has a null modifier or modifier attribute.");
                return ActiveEffectHandle.Invalid;
            }
        }

        // TODO: maybe add a "chance to add" property to effects if required.

        if (!spec.effectTemplate.applicationTagRequirements.RequirementsMet(_dynamicOwnedTags))
        {
            return ActiveEffectHandle.Invalid;
        }

        if (!spec.effectTemplate.removalTagRequirements.IsEmpty() && spec.effectTemplate.removalTagRequirements.RequirementsMet(_dynamicOwnedTags))
        {
            return ActiveEffectHandle.Invalid;
        }

        if (spec.effectTemplate.durationPolicy == EEffectDurationPolicy.Instant)
        {
            _attributeSetInstance.ExecuteEffectSpec(spec);
        }
        else
        {
            ActiveEffectHandle handle = _activeGameplayEffects.AddActiveGameplayEffect(spec);
            _attributeSetInstance.ApplyActiveEffectSpec(spec);

            return handle;
        }

        return ActiveEffectHandle.Invalid;
    }

    public ActiveEffectHandle ApplyGameplayEffectSpecToTarget(GameplayEffectSpec spec, AbilitySystem target)
    {
        if (target != null)
        {
            return target.ApplyGameplayEffectSpecToSelf(spec);
        }

        return ActiveEffectHandle.Invalid;
    }

    public void RemoveActiveEffectByHandle(ActiveEffectHandle handle)
    {
        _activeGameplayEffects.RemoveActiveEffectByHandle(handle);
    }

    protected void HandleActiveEffectAdded(ActiveEffectHandle handle, GameplayEffectSpec spec)
    {
        dynamicOwnedTags.AddTags(spec.effectTemplate.grantedTags);

        if (onActiveEffectAdded != null)
        {
            onActiveEffectAdded(handle, spec);
        }
    }

    protected void HandleActiveEffectRemoved(ActiveEffectHandle handle, GameplayEffectSpec spec)
    {
        dynamicOwnedTags.RemoveTags(spec.effectTemplate.grantedTags);

        _attributeSetInstance.RemoveActiveEffectSpec(spec);

        if (onActiveEffectRemoved != null)
        {
            onActiveEffectRemoved(handle, spec);
        }
    }

    public float GetAttributeCurrentValue(SAttribute attribute)
    {
        if (_attributeSetInstance != null)
        {
            return _attributeSetInstance.GetAttributeCurrentValue(attribute);
        }

        return 0.0f;
    }

    public float GetAttributeBaseValue(SAttribute attribute)
    {
        if (_attributeSetInstance != null)
        {
            return _attributeSetInstance.GetAttributeBaseValue(attribute);
        }

        return 0.0f;
    }
}
