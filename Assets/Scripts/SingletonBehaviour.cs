using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static Action OnInstantiated;

    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else if (Instance == null) Instance = this as T;

        OnInstantiated();
    }

    /// <summary>
    /// register some code to run when this SingletonBehaviour instantiates
    /// </summary>
    /// <returns>whether or not callback was immediately run (singleton already instantiated)</returns>
    public static bool WhenInstantiated(Action callback)
    {
        if (Instance != null) { callback(); return true; }
        else { OnInstantiated += callback; return false; }
    }
}
