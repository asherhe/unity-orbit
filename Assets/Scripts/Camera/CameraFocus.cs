using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class CameraFocus : SingletonBehaviour<CameraFocus>
{
    [SerializeField]
    private OrbitingObject _focus;

    /// <summary>
    /// the object that the camera is focused on
    /// </summary>
    public OrbitingObject Focus {
        get => _focus;
        set {
            _focus = value;
            OnFocusChanged?.Invoke();
        }
    }

    public event Action OnFocusChanged;

    protected override void Awake()
    {
        base.Awake();

        OnFocusChanged += () => AnnouncementDisplay.Instance.Announce($"Focusing {Focus.name}");
    }

    /// <summary>
    /// get the current location of an orbit, relative to the location of the focused object.
    /// </summary>
    public Vector2d TransformObject(OrbitingObject obj) => OrbitingObject.GetRelativePosition(Focus, obj);
}
