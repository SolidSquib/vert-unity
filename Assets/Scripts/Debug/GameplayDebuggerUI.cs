using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameplayDebuggerUI : MonoBehaviour
{
    [SerializeField] VerticalLayoutGroup activeAbilitiesLayout;
    [SerializeField] VerticalLayoutGroup activeEffectsLayout;
    [SerializeField] VerticalLayoutGroup activeTagsLayout;
    [SerializeField] VerticalLayoutGroup attributesPanel;
    [SerializeField] GameObject textLabelTemplate;
    AbilitySystem _boundAbilitySystem;
    Dictionary<int, GameObject> _createdLabels = new Dictionary<int, GameObject>();

    public void BindAbilitySystemEvents(AbilitySystem parentSystem)
    {
        if (parentSystem == null || parentSystem == _boundAbilitySystem)
        {
            return;
        }

        if (_boundAbilitySystem)
        {
            // remove bound delegates.
        }

        _boundAbilitySystem = parentSystem;

        _boundAbilitySystem.onAbilityActivated += OnAbilityActivated;
        _boundAbilitySystem.onAbilityEnded += OnAbilityEnded;
        _boundAbilitySystem.dynamicOwnedTags.TagCountChanged += OnTagsChanged;
        _boundAbilitySystem.onActiveEffectAdded += OnEffectAdded;
        _boundAbilitySystem.onActiveEffectRemoved += OnEffectRemoved;

        foreach (var attribute in _boundAbilitySystem.attributeSet.attributes)
        {
            GameObject newLabel = Instantiate(textLabelTemplate, attributesPanel.transform);
            Text labelText = newLabel.GetComponent<Text>();
            if (labelText != null)
            {
                labelText.text = $"{attribute.name}\n\tBase: {_boundAbilitySystem.GetAttributeBaseValue(attribute)}\n\tCurrent: {_boundAbilitySystem.GetAttributeCurrentValue(attribute)}";
            }
            _createdLabels.Add(attribute.GetHashCode(), newLabel);

            _boundAbilitySystem.RegisterOnAttributeChangedCallback(attribute, OnAttributeChanged);
        }
    }

    protected GameObject AddNewLabel(string text, VerticalLayoutGroup parentGroup)
    {
        if (parentGroup != null && textLabelTemplate != null)
        {
            GameObject newLabel = Instantiate(textLabelTemplate, parentGroup.transform);
            Text labelText = newLabel.GetComponent<Text>();
            if (labelText != null)
            {
                labelText.text = text;
            }

            return newLabel;
        }

        return null;
    }

    protected void OnAbilityActivated(SGameplayAbility ability)
    {
        GameObject newLabel = AddNewLabel(ability.name, activeAbilitiesLayout);
        if (newLabel != null)
        {
            _createdLabels.Add(ability.GetHashCode(), newLabel);
        }
    }

    protected void OnAbilityEnded(SGameplayAbility ability)
    {
        int key = ability.GetHashCode();
        if (_createdLabels.ContainsKey(key))
        {
            Destroy(_createdLabels[key]);
            _createdLabels.Remove(key);
        }
    }

    protected void OnTagsChanged(object sender, CountingTagEventArgs args)
    {
        int key = args.tag.GetHashCode();
        if (_createdLabels.ContainsKey(key))
        {
            GameObject label = _createdLabels[key];
            Text text = label.GetComponent<Text>();
            text.text = $"{args.tag.GetFullPath('.')} ({args.count})";

            if (args.count <= 0)
            {
                Destroy(label);
                _createdLabels.Remove(key);
            }
        }
        else if (args.count > 0)
        {
            GameObject newLabel = AddNewLabel($"{args.tag.GetFullPath('.')} ({args.count})", activeTagsLayout);
            _createdLabels.Add(key, newLabel);
        }
    }

    protected void OnEffectAdded(ActiveEffectHandle handle, GameplayEffectSpec spec)
    {
        GameObject newLabel = AddNewLabel(spec.effectTemplate.name, activeEffectsLayout);
        if (newLabel != null)
        {
            _createdLabels.Add(handle.GetHashCode(), newLabel);
        }
    }

    protected void OnEffectRemoved(ActiveEffectHandle handle, GameplayEffectSpec spec)
    {
        int key = handle.GetHashCode();
        if (_createdLabels.ContainsKey(key))
        {
            Destroy(_createdLabels[key]);
            _createdLabels.Remove(key);
        }
    }

    protected void OnAttributeChanged(AttributeEventArgs args)
    {
        SAttribute attribute = args.attribute;
        float baseValue = args.attributeValues.baseValue;
        float currentValue = args.attributeValues.currentValue;
        int key = attribute.GetHashCode();

        if (_createdLabels.ContainsKey(key))
        {
            GameObject label = _createdLabels[key];
            Text labelText = label.GetComponent<Text>();
            labelText.text = $"{attribute.name}\n\tBase: {baseValue}\n\tCurrent: {currentValue}";
        }
    }
}
