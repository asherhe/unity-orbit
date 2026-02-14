using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
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
}
