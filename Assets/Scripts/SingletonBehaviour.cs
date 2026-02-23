using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class SingletonBehaviour<T> : MonoBehaviour where T : MonoBehaviour
{
    private static event Action OnInstantiated;

    public static T Instance { get; private set; }

    protected virtual void Awake()
    {
        if (Instance != null && Instance != this) Destroy(this);
        else if (Instance == null) Instance = this as T;

        OnInstantiated?.Invoke();
    }

    /// <summary>
    /// register some code to run when this SingletonBehaviour instantiates
    /// </summary>
    /// <returns>whether or not callback was immediately run (singleton already instantiated)</returns>
    public static bool WhenInstantiated(Action callback)
    {
        if (Instance != null) { callback(); return true; }

        Action handler = null;
        handler = () =>
        {
            OnInstantiated -= handler;
            callback();
        };
        OnInstantiated += handler;
        return false;
    }

    public static Task WaitForInstantiation()
    {
        if (Instance != null) return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        Action handler = null;
        handler = () =>
        {
            OnInstantiated -= handler;
            tcs.TrySetResult(true);
        };
        OnInstantiated += handler;
        return tcs.Task;
    }
}
