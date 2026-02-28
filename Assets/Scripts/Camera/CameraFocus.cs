using Orbit;
using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class CameraFocus : SingletonBehaviour<CameraFocus>
{
    [SerializeField]
    private OrbitingObject _defaultFocus;

    private OrbitingObject _focus;
    /// <summary>
    /// the object that the camera is focused on
    /// </summary>
    public OrbitingObject Focus
    {
        get => _focus;
        set
        {
            _focus = value;
            OnFocusChanged?.Invoke();
        }
    }

    public event Action OnFocusChanged;

    /// <summary>
    /// focused objects for different camera views
    /// </summary>
    private Dictionary<CameraView, OrbitingObject> viewFocuses;

    protected override void Awake()
    {
        _focus = _defaultFocus;
        viewFocuses = new Dictionary<CameraView, OrbitingObject>()
        {
            { CameraView.FlightView, _defaultFocus },
            { CameraView.MapView, _defaultFocus },
        };

        OnFocusChanged += () => AnnouncementDisplay.Instance.Announce($"Focusing {Focus.name}");

        MapViewManager.WhenInstantiated(() =>
        {
            OnFocusChanged += () => viewFocuses[MapViewManager.Instance.activeView] = Focus;
            MapViewManager.Instance.OnMapToggled += () => Focus = viewFocuses[MapViewManager.Instance.activeView];
        });

        base.Awake();
    }

    /// <summary>
    /// get the current location of an orbit, relative to the location of the focused object.
    /// </summary>
    public Vector2d TransformObject(OrbitingObject obj) => OrbitingObject.GetRelativePosition(Focus, obj);
}
