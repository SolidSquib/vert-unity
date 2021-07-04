using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class SModifierMagnitudeCalculation : ScriptableObject
{
    public abstract float GetModifierMagnitude();
}

public enum EModifierMethod { Add, Multiply, Divide, Override }
public enum EModifierCalculation { ScalableFloat, AttributeBased, CustomCalculationClass, SetByCaller }
public enum EEffectDurationPolicy { Instant, Infinite, Duration }

[System.Serializable]
public struct EffectModifierMagnitudeInfo
{
    public EModifierCalculation magnitudeCalculation;
    public SAttribute attribute;
    public Tag setByCallerTag;
    public float baseMagnitude;
    public SModifierMagnitudeCalculation customCalculationClass;
}

public struct CachedEffectModifierMagnitude
{
    public SAttribute attribute;
    public EModifierMethod method;
    public float magnitude;
}

public class GameplayEffectSpec
{
    public AbilitySystem source { get; private set; } = null;
    public AbilitySystem target { get; set; } = null;
    public Dictionary<Tag, float> setByCallerValues { get; private set; } = new Dictionary<Tag, float>();
    public SGameplayEffect effectTemplate { get; private set; } = null;
    public float applicationTime { get; set; } = 0;

    public List<CachedEffectModifierMagnitude> cachedModifiers = new List<CachedEffectModifierMagnitude>();

    public GameplayEffectSpec(AbilitySystem source, SGameplayEffect effect)
    {
        this.source = source;
        this.effectTemplate = effect;
    }

    public GameplayEffectSpec(AbilitySystem source, AbilitySystem target, SGameplayEffect effect)
    {
        this.source = source;
        this.target = target;
        this.effectTemplate = effect;
    }

    public void RecalculateModifierMagitudes(AbilitySystem owner)
    {
        if (owner == null)
        {
            Debug.LogWarning("Owning Ability System required to calculate magnitudes.");
            return;
        }

        cachedModifiers.Clear();

        foreach (GameplayEffectAttributeModifier modifierInfo in effectTemplate.modifiers)
        {
            CachedEffectModifierMagnitude calculatedModifier = new CachedEffectModifierMagnitude() { attribute = modifierInfo.attribute, method = modifierInfo.method };

            switch (modifierInfo.magnitude.magnitudeCalculation)
            {
                case EModifierCalculation.ScalableFloat:
                    calculatedModifier.magnitude = modifierInfo.magnitude.baseMagnitude;
                    break;
                case EModifierCalculation.AttributeBased:
                    calculatedModifier.magnitude = owner.GetAttributeCurrentValue(modifierInfo.attribute);
                    break;
                case EModifierCalculation.CustomCalculationClass:
                    if (modifierInfo.magnitude.customCalculationClass != null)
                    {
                        calculatedModifier.magnitude = modifierInfo.magnitude.customCalculationClass.GetModifierMagnitude();
                    }
                    break;
                case EModifierCalculation.SetByCaller:
                    if (setByCallerValues.ContainsKey(modifierInfo.magnitude.setByCallerTag))
                    {
                        calculatedModifier.magnitude = setByCallerValues[modifierInfo.magnitude.setByCallerTag];
                    }
                    break;
            }

            cachedModifiers.Add(calculatedModifier);
        }
    }
}

[System.Serializable]
public struct GameplayEffectAttributeModifier
{
    public SAttribute attribute;
    public EModifierMethod method;
    public EffectModifierMagnitudeInfo magnitude;
}

[CreateAssetMenu(menuName = "Ability System/Gameplay Effect")]
public class SGameplayEffect : ScriptableObject
{
    public EEffectDurationPolicy durationPolicy;
    public EffectModifierMagnitudeInfo duration;
    public TagContainer effectTags = new TagContainer();
    public TagContainer grantedTags = new TagContainer();

    [Header("Application Requirements")]
    public TagRequirementsContainer ongoingTagRequirements;
    public TagRequirementsContainer applicationTagRequirements;
    public TagRequirementsContainer removalTagRequirements;
    public TagContainer removeGameplayEffectsWithTags;

    [Header("Attribute Modifiers")]
    public List<GameplayEffectAttributeModifier> modifiers;

    [Header("Granted Abilities")]
    public List<GrantedAbilityInfo> grantedAbilities;
}
