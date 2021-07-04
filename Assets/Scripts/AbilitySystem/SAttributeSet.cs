using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttributeInstance
{
    public float currentValue { get; set; } = 0f;
    public float baseValue { get; set; } = 0f;
}

public delegate void AttributeEventHandler(AttributeEventArgs args);
public sealed class AttributeEventArgs : System.EventArgs
{
    public SAttribute attribute { get; private set; }
    public AttributeInstance attributeValues { get; private set; }
    public AbilitySystem owner { get; private set; }

    public AttributeEventArgs(SAttribute attribute, AttributeInstance attributeValues, AbilitySystem owner)
    {
        this.attribute = attribute;
        this.attributeValues = attributeValues;
        this.owner = owner;
    }
}

/// <summary>
/// Instantiate an attribute set at runtime on the AbilitySystem script so that we can easily serialize and deserialize all 
/// relevant player attributes when required.
/// </summary>
[CreateAssetMenu(menuName = "Ability System/Attribute Set")]
public class SAttributeSet : ScriptableObject
{
    [SerializeField] private List<SAttribute> _attributes;

    private Dictionary<SAttribute, AttributeInstance> _currentAttributeValues = new Dictionary<SAttribute, AttributeInstance>();
    private Dictionary<SAttribute, AttributeEventHandler> _attributeModifiedCallbacks = new Dictionary<SAttribute, AttributeEventHandler>();
    private Dictionary<SAttribute, List<SAttribute>> _attributeDependencies = new Dictionary<SAttribute, List<SAttribute>>();
    private List<GameplayEffectSpec> _appliedModifyingEffects = new List<GameplayEffectSpec>();
    private HashSet<SAttribute> _dirtyAttributes = new HashSet<SAttribute>();
    private AbilitySystem _abilitySystem;
    public bool isInitialized { get; private set; }
    public List<SAttribute> attributes { get { return _attributes; } private set { _attributes = value; } }

    public void Initialize(AbilitySystem owner)
    {
        _abilitySystem = owner;

        foreach (var attribute in _attributes)
        {
            _currentAttributeValues.Add(attribute, new AttributeInstance());

            // work out dependencies for max values:
            if (attribute.maxAttribute != null)
            {
                RegisterOnAttributeChangedCallback(attribute.maxAttribute, OnAttributeDependencyUpdated);

                if (_attributeDependencies.ContainsKey(attribute.maxAttribute))
                {
                    _attributeDependencies[attribute.maxAttribute].Add(attribute);
                }
                else
                {
                    _attributeDependencies.Add(attribute.maxAttribute, new List<SAttribute>() { attribute });
                }
            }
        }
    }

    public float GetAttributeCurrentValue(SAttribute attribute)
    {
        AttributeInstance instance;
        if (_currentAttributeValues.TryGetValue(attribute, out instance))
        {
            return instance.currentValue;
        }
        return 0;
    }

    public float GetAttributeBaseValue(SAttribute attribute)
    {
        AttributeInstance instance;
        if (_currentAttributeValues.TryGetValue(attribute, out instance))
        {
            return instance.baseValue;
        }
        return 0;
    }

    public void RegisterOnAttributeChangedCallback(SAttribute attribute, AttributeEventHandler callback)
    {
        if (callback == null)
        {
            return;
        }

        AttributeEventHandler existingDelegate;
        if (_attributeModifiedCallbacks.TryGetValue(attribute, out existingDelegate))
        {
            existingDelegate += callback;
        }
        else
        {
            _attributeModifiedCallbacks.Add(attribute, callback);
        }
    }

    public void UnregisterOnAttributeChangedCallback(SAttribute attribute, AttributeEventHandler callback)
    {
        if (callback == null)
        {
            return;
        }

        AttributeEventHandler existingDelegate;
        if (_attributeModifiedCallbacks.TryGetValue(attribute, out existingDelegate))
        {
            existingDelegate -= callback;

            if (existingDelegate == null)
            {
                _attributeModifiedCallbacks.Remove(attribute);
            }
        }
    }

    public void FlushDirtyAttributes()
    {
        foreach (var attribute in _dirtyAttributes)
        {
            NotifyAttributeUpdatedListeners(attribute);
        }

        _dirtyAttributes.Clear();
    }

    protected void NotifyAttributeUpdatedListeners(SAttribute attribute)
    {
        AttributeEventHandler handler;
        if (_attributeModifiedCallbacks.TryGetValue(attribute, out handler))
        {
            if (handler != null)
            {
                handler(new AttributeEventArgs(attribute, _currentAttributeValues[attribute], _abilitySystem));
            }
        }
    }

    protected void OnAttributeDependencyUpdated(AttributeEventArgs args)
    {

    }

    /// <summary>
    /// Apply instant modifiers.
    /// </summary>
    /// <param name="modifier"></param>
    /// <param name="setByCallerTags"></param>
    public void ExecuteEffectSpec(GameplayEffectSpec spec)
    {
        if (spec == null)
        {
            Debug.LogError("Cannot execute modifiers on a null effect spec.");
            return;
        }

        spec.RecalculateModifierMagitudes(_abilitySystem);

        foreach (var modifier in spec.cachedModifiers)
        {
            AttributeInstance attributeInstance;
            if (_currentAttributeValues.TryGetValue(modifier.attribute, out attributeInstance))
            {
                switch (modifier.method)
                {
                    case EModifierMethod.Add:
                        attributeInstance.baseValue += modifier.magnitude;
                        break;
                    case EModifierMethod.Multiply:
                        attributeInstance.baseValue *= modifier.magnitude;
                        break;
                    case EModifierMethod.Divide:
                        attributeInstance.baseValue /= modifier.magnitude;
                        break;
                    case EModifierMethod.Override:
                        attributeInstance.baseValue = modifier.magnitude;
                        break;
                }

                AttributeInstance attributeCapInstance;
                if (modifier.attribute.maxAttribute != null && _currentAttributeValues.TryGetValue(modifier.attribute.maxAttribute, out attributeCapInstance))
                {
                    attributeInstance.baseValue = Mathf.Min(attributeInstance.baseValue, attributeCapInstance.currentValue);
                }

                _dirtyAttributes.Add(modifier.attribute);
            }
        }

        RecalculateCurrentAttributeValues();
    }

    public void ApplyActiveEffectSpec(GameplayEffectSpec spec)
    {
        if (spec == null)
        {
            Debug.LogError("Cannot apply modifiers on a null effect spec.");
            return;
        }

        spec.RecalculateModifierMagitudes(_abilitySystem);
        if (spec.cachedModifiers.Count > 0)
        {
            _appliedModifyingEffects.Add(spec);
            RecalculateCurrentAttributeValues();
        }
    }

    protected void RecalculateCurrentAttributeValues()
    {
        // TODO this function could do with some optimization at some point...
        List<float> previousValues = new List<float>();

        // reset the current values on all attributes.
        foreach (var attribute in _currentAttributeValues)
        {
            previousValues.Add(attribute.Value.currentValue);
            attribute.Value.currentValue = attribute.Value.baseValue;
        }

        foreach (var spec in _appliedModifyingEffects)
        {
            foreach (var modifier in spec.cachedModifiers)
            {
                AttributeInstance attributeInstance;
                if (_currentAttributeValues.TryGetValue(modifier.attribute, out attributeInstance))
                {
                    switch (modifier.method)
                    {
                        case EModifierMethod.Add:
                            attributeInstance.currentValue += modifier.magnitude;
                            break;
                        case EModifierMethod.Multiply:
                            attributeInstance.currentValue *= modifier.magnitude;
                            break;
                        case EModifierMethod.Divide:
                            attributeInstance.currentValue /= modifier.magnitude;
                            break;
                        case EModifierMethod.Override:
                            attributeInstance.currentValue = modifier.magnitude;
                            break;
                    }

                    AttributeInstance attributeCapInstance;
                    if (modifier.attribute.maxAttribute != null && _currentAttributeValues.TryGetValue(modifier.attribute.maxAttribute, out attributeCapInstance))
                    {
                        attributeInstance.currentValue = Mathf.Min(attributeInstance.currentValue, attributeCapInstance.currentValue);
                    }
                }
            }
        }

        int i = 0;
        foreach (var attribute in _currentAttributeValues)
        {
            if (previousValues[i] != attribute.Value.currentValue)
            {
                _dirtyAttributes.Add(attribute.Key);
            }

            i+=1;
        }
    }

    public void RemoveActiveEffectSpec(GameplayEffectSpec spec)
    {
        if (_appliedModifyingEffects.Remove(spec))
        {
            RecalculateCurrentAttributeValues();
        }
    }
}