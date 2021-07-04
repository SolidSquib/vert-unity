using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public delegate void ActiveEffectDelegate(ActiveEffectHandle handle, GameplayEffectSpec spec);

public class ActiveGameplayEffectsContainer
{    
    public ActiveEffectDelegate onActiveEffectRemoved;
    public ActiveEffectDelegate onActiveEffectAdded;

    private int _currentId = 0;
    private AbilitySystem abilitySystem { get; set; }
    private Dictionary<ActiveEffectHandle, GameplayEffectSpec> activeEffectSpecs { get; set; } = new Dictionary<ActiveEffectHandle, GameplayEffectSpec>(); // Infinite and Duration based effects that are currently applied to this ability system.
    private Dictionary<Tag, List<ActiveEffectHandle>> tagChangeListeners { get; set; } = new Dictionary<Tag, List<ActiveEffectHandle>>();
    public ActiveGameplayEffectsContainer(AbilitySystem abilitySystem)
    {
        this.abilitySystem = abilitySystem;

        this.abilitySystem.dynamicOwnedTags.TagAdded += OnTagsChanged;
        this.abilitySystem.dynamicOwnedTags.TagRemoved += OnTagsChanged;
    }

    private void OnTagsChanged(object sender, TagEventArgs args)
    {
        List<ActiveEffectHandle> listeningEffects;
        if (tagChangeListeners.TryGetValue(args.tag, out listeningEffects))
        {
            foreach (var handle in listeningEffects)
            {
                GameplayEffectSpec spec;
                if (TryGetSpecFromHandle(handle, out spec))
                {
                    if (spec.effectTemplate.removalTagRequirements.RequirementsMet(abilitySystem.dynamicOwnedTags))
                    {
                        RemoveActiveEffectByHandle(handle);
                    }
                }
            }
        }
    }

    public bool TryGetSpecFromHandle(ActiveEffectHandle handle, out GameplayEffectSpec spec)
    {
        spec = null;
        if (activeEffectSpecs.ContainsKey(handle))
        {
            spec = activeEffectSpecs[handle];
            return true;
        }
        return false;
    }

    protected ActiveEffectHandle CreateNewActiveSpecHandle(GameplayEffectSpec spec)
    {
        ActiveEffectHandle handle = new ActiveEffectHandle();
        handle.id = _currentId++;
        handle.source = spec.source;
        handle.target = spec.target;
        return handle;
    }

    protected void AddTagListener(Tag tag, ActiveEffectHandle handle)
    {
        List<ActiveEffectHandle> handles;
        if (tagChangeListeners.TryGetValue(tag, out handles))
        {
            handles.Add(handle);
        }
        else
        {
            handles = new List<ActiveEffectHandle>();
            handles.Add(handle);
            tagChangeListeners.Add(tag, handles);
        }
    }

    public ActiveEffectHandle AddActiveGameplayEffect(GameplayEffectSpec spec)
    {
        if (spec == null)
        {
            return ActiveEffectHandle.Invalid;
        }

        spec.applicationTime = Time.time;
        ActiveEffectHandle handle = CreateNewActiveSpecHandle(spec);
        activeEffectSpecs.Add(handle, spec);

        foreach (Tag tag in spec.effectTemplate.removalTagRequirements.required.list)
        {
            AddTagListener(tag, handle);
        }

        foreach (Tag tag in spec.effectTemplate.removalTagRequirements.ignored.list)
        {
            AddTagListener(tag, handle);
        }

        foreach (Tag tag in spec.effectTemplate.ongoingTagRequirements.required.list)
        {
            AddTagListener(tag, handle);
        }

        foreach (Tag tag in spec.effectTemplate.ongoingTagRequirements.ignored.list)
        {
            AddTagListener(tag, handle);
        }

        RemoveEffectsWithTags(spec.effectTemplate.removeGameplayEffectsWithTags);

        if (onActiveEffectAdded != null)
        {
            onActiveEffectAdded(handle, spec);
        }

        return handle;
    }

    public void RemoveExpiredGameplayEffects()
    {
        HashSet<ActiveEffectHandle> keysForRemoval = new HashSet<ActiveEffectHandle>();

        foreach (var pair in activeEffectSpecs)
        {
            GameplayEffectSpec spec = pair.Value;
            if (spec.effectTemplate.durationPolicy == EEffectDurationPolicy.Duration && (Time.time - spec.applicationTime) >= spec.effectTemplate.duration.baseMagnitude) // TODO this will not account for duration when a custom mod or set by caller is applied.
            {
                keysForRemoval.Add(pair.Key);
            }
        }

        foreach (var key in keysForRemoval)
        {
            RemoveActiveEffectByHandle(key);
        }
    }

    public void RemoveEffectsWithTags(TagContainer tags)
    {
        if (tags != null)
        {
            foreach (var pair in activeEffectSpecs)
            {
                GameplayEffectSpec spec = pair.Value;
                if (spec.effectTemplate.effectTags.AnyTagsMatch(tags))
                {
                    RemoveActiveEffectByHandle(pair.Key);
                }
            }
        }
    }

    public void RemoveActiveEffectByHandle(ActiveEffectHandle handle)
    {
        GameplayEffectSpec foundSpec;

        if (activeEffectSpecs.TryGetValue(handle, out foundSpec))
        {
            activeEffectSpecs.Remove(handle);

            if (onActiveEffectRemoved != null)
            {
                onActiveEffectRemoved(handle, foundSpec);
            }
        }
    }
}