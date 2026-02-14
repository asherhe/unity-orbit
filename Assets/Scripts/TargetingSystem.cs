using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class TargetingSystem : SingletonBehaviour<TargetingSystem>
{
    private IOrbitingObject _target;
    /// <summary>
    /// currently active targeted object. null if no target is active
    /// </summary>
    public IOrbitingObject Target
    {
        get => _target;
        set
        {
            if (_target == value) return;
            _target = value;
            OnTargetChanged?.Invoke();
        }
    }

    public event Action OnTargetChanged;

    protected override void Awake()
    {
        base.Awake();

        OnTargetChanged += () => AnnouncementDisplay.Instance.Announce(Target == null ? "Targeting Cancelled" : $"Targeting {Target.gameObject.name}");
    }
}
