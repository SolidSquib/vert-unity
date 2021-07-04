using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Concrete class for a wrapped list of Tags.  Includes events for adding and removing tags.
/// The provided functions allow containers to be checked for matching tags.
/// </summary>
[System.Serializable]
public class CountingTagContainer : DictionaryWrapper<Tag, int>
{
    public event CountingTagEventHandler TagCountChanged;
    public event TagEventHandler TagAdded;
    public event TagEventHandler TagRemoved;

    public CountingTagContainer() { }
    public CountingTagContainer(List<Tag> tags)
    {
        foreach (Tag tag in tags)
        {
            AddTag(tag);
        }
    }


    public void TriggerTagCountChanged(Tag tag, int newCount)
    {
        if (TagCountChanged != null && tag != null) { TagCountChanged(this, new CountingTagEventArgs(tag, newCount)); }
    }
    public void TriggerTagAdded(Tag tag, int newCount)
    {
        if (TagAdded != null && tag != null) { TagAdded(this, new TagEventArgs(tag)); }
    }
    public void TriggerTagRemoved(Tag tag, int newCount)
    {
        if (TagRemoved != null && tag != null) { TagRemoved(this, new TagEventArgs(tag)); }
    }


    #region COMPARISON_FUNCTIONS
    public bool ContainsTag(Tag tag)
    {
        return dictionary.ContainsKey(tag);
    }

    public bool ContainsChildOf(Tag tag)
    {
        foreach (Tag existingTag in dictionary.Keys)
        {
            if (existingTag.IsChildOf(tag))
            {
                return true;
            }
        }
        return false;
    }

    public bool ContainsParentOf(Tag tag)
    {
        foreach (Tag existingTag in dictionary.Keys)
        {
            if (tag.IsChildOf(existingTag))
            {
                return true;
            }
        }
        return false;
    }



    public int GetTagQuantity(Tag tag)
    {
        int count;
        if (dictionary.TryGetValue(tag, out count)) { return count; }
        return 0;
    }

    public bool AnyTagsMatch(TagContainer container)
    {
        foreach (Tag oTag in container.list)
        {
            if (ContainsChildOf(oTag))
            {
                return true;
            }
        }
        return false;
    }

    public bool AllTagsMatch(TagContainer container)
    {
        foreach (Tag tag in container.list)
        {
            if (!ContainsChildOf(tag))
            {
                return false;
            }
        }
        return true;
    }

    public bool NoTagsMatch(TagContainer container)
    {
        foreach (Tag tag in container.list)
        {
            if (ContainsChildOf(tag))
            {
                return false;
            }
        }
        return true;
    }
    #endregion


    #region MANAGEMENT_FUNCTIONS
    public void AddTag(Tag tag)
    {
        int count;
        if (dictionary.TryGetValue(tag, out count))
        {
            dictionary[tag] = (count + 1);
        }
        else
        {
            count = 1;
            dictionary.Add(tag, count);
            TriggerTagAdded(tag, count);
        }

        TriggerTagCountChanged(tag, count);        
    }
    public void RemoveTag(Tag tag)
    {
        int count;
        if (dictionary.TryGetValue(tag, out count))
        {
            count -= 1;
            dictionary[tag] = count;

            if (count <= 0)
            {
                dictionary.Remove(tag);
                TriggerTagRemoved(tag, count);
            }
            
            TriggerTagCountChanged(tag, count);            
        }
    }
    public void ClearTags()
    {
        dictionary.Clear();
    }

    public void AddTags(TagContainer oContainer)
    {
        for (int i = 0; i < oContainer.list.Count; ++i)
        {
            AddTag(oContainer.list[i]);
        }
    }
    public void RemoveTags(TagContainer oContainer)
    {
        for (int i = 0; i < oContainer.list.Count; ++i)
        {
            RemoveTag(oContainer.list[i]);
        }
    }
    #endregion

    public static explicit operator TagContainer(CountingTagContainer tc) => new TagContainer(new List<Tag>(tc.dictionary.Keys));
    public static explicit operator CountingTagContainer(TagContainer tc) => new CountingTagContainer(tc.list);
}
