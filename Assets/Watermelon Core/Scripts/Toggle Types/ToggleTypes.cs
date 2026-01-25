using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Watermelon
{
    /// <summary>
    /// Toggle wrapper: if Enabled, overrides the incoming value with Value.
    /// </summary>
    [Serializable]
    public class ToggleType<T>
    {
        [SerializeField] private bool enabled;
        public bool Enabled => enabled;

        [FormerlySerializedAs("newValue")]
        [SerializeField] private T value;
        public T Value => value;

        public ToggleType() { }

        public ToggleType(bool enabled, T value)
        {
            this.enabled = enabled;
            this.value = value;
        }

        public T Handle(T current)
        {
            return enabled ? value : current;
        }
    }

    #region Primitive Toggles

    [Serializable] public class BoolToggle   : ToggleType<bool>     { public BoolToggle(bool enabled, bool value)     : base(enabled, value) { } }
    [Serializable] public class FloatToggle  : ToggleType<float>    { public FloatToggle(bool enabled, float value)   : base(enabled, value) { } }
    [Serializable] public class IntToggle    : ToggleType<int>      { public IntToggle(bool enabled, int value)       : base(enabled, value) { } }
    [Serializable] public class LongToggle   : ToggleType<long>     { public LongToggle(bool enabled, long value)     : base(enabled, value) { } }
    [Serializable] public class StringToggle : ToggleType<string>   { public StringToggle(bool enabled, string value) : base(enabled, value) { } }
    [Serializable] public class DoubleToggle : ToggleType<double>   { public DoubleToggle(bool enabled, double value) : base(enabled, value) { } }

    [Serializable]
    public class ObjectToggle : ToggleType<GameObject>
    {
        public ObjectToggle(bool enabled, GameObject value) : base(enabled, value) { }
    }

    [Serializable]
    public class AudioClipToggle : ToggleType<AudioClip>
    {
        public AudioClipToggle(bool enabled, AudioClip value) : base(enabled, value) { }
    }

    #endregion

    #region Generic List Toggle

    /// <summary>
    /// Generic list toggle. If Enabled, overrides the incoming list with Value (reference swap).
    /// </summary>
    [Serializable]
    public class ListToggle<TItem> : ToggleType<List<TItem>>
    {
        public ListToggle() { }
        
        public ListToggle(bool enabled) : base(enabled, new List<TItem>()) { }
        public ListToggle(bool enabled, List<TItem> value) : base(enabled, value) { }

        /// <summary>
        /// Optional helper if you want to ensure the output isn't null.
        /// </summary>
        public List<TItem> Handle(List<TItem> current, bool ensureNonNullResult)
        {
            var result = Handle(current);
            if (ensureNonNullResult && result == null)
                result = new List<TItem>();
            return result;
        }
    }

    #endregion

    #region Composite Toggle (Single Enable gates both direct fields)

    /// <summary>
    /// Composite toggle:
    /// One Enabled checkbox gates overriding BOTH a value and a list.
    /// If Enabled is false, incoming values pass through unchanged.
    /// If Enabled is true, currentValue/currentList get replaced with the serialized Value/List.
    /// </summary>
    [Serializable]
    public class CompositeToggle<TValue, TItem>
    {
        [SerializeField] private bool enabled;
        public bool Enabled => enabled;

        [SerializeField] private TValue value;
        public TValue Value => value;

        [SerializeField] private List<TItem> list = new List<TItem>();
        public List<TItem> List => list;

        [Tooltip("If true and Enabled is on, ensures the result list is non-null (creates empty list if needed).")]
        [SerializeField] private bool ensureNonNullListResult = false;
        public bool EnsureNonNullListResult => ensureNonNullListResult;

        public CompositeToggle() { }

        public CompositeToggle(bool enabled, TValue value)
        {
            this.enabled = enabled;
            this.value = value;
        }

        public CompositeToggle(bool enabled, TValue value, List<TItem> list, bool ensureNonNullListResult = true)
        {
            this.enabled = enabled;
            this.value = value;
            this.list = list;
            this.ensureNonNullListResult = ensureNonNullListResult;
        }

        public void Apply(ref TValue currentValue, ref List<TItem> currentList)
        {
            if (!enabled) return;

            currentValue = value;
            currentList = list;

            if (ensureNonNullListResult && currentList == null)
                currentList = new List<TItem>();
        }

        public (TValue value, List<TItem> list) Handle(TValue currentValue, List<TItem> currentList)
        {
            Apply(ref currentValue, ref currentList);
            return (currentValue, currentList);
        }
    }

    #endregion
}
