using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum CameraView
{
    FlightView,
    MapView,
};

public class MapViewManager : SingletonBehaviour<MapViewManager>
{
    private InputActions _inputActions;

    public CameraView activeView { get; private set; } = CameraView.FlightView;

    /// <summary>
    /// the camera responsible for rendering map view
    /// </summary>
    public Camera activeMapCamera { get; private set; }

    [SerializeField]
    private Camera minimapCamera;

    /// <summary>
    /// render layers for flight view
    /// </summary>
    [SerializeField]
    private LayerMask flightCullingMask;
    /// <summary>
    /// render layers for map view
    /// </summary>
    [SerializeField]
    private LayerMask mapCullingMask;

    /// <summary>
    /// gets invoked when map view is toggled
    /// </summary>
    public event Action OnMapToggled;

    protected override void Awake()
    {
        activeMapCamera = minimapCamera;

        InputReader.WhenInstantiated(() =>
        {
            _inputActions = InputReader.Instance.Actions;
            _inputActions.Camera.Enable();
            _inputActions.Camera.ToggleMap.performed += (context) => ToggleMapView();
        });

        base.Awake();
    }

    public void ToggleMapView()
    {
        if (activeView == CameraView.FlightView) EnterMapView();
        else EnterFlightView();
    }

    public void EnterMapView()
    {
        if (activeView == CameraView.MapView) return;
        activeView = CameraView.MapView;

        activeMapCamera = Camera.main;
        Camera.main.cullingMask = mapCullingMask;
        _inputActions.MapView.Enable();

        OnMapToggled.Invoke();
    }

    public void EnterFlightView()
    {
        if (activeView == CameraView.FlightView) return;
        activeView = CameraView.FlightView;

        activeMapCamera = minimapCamera;
        Camera.main.cullingMask = flightCullingMask;
        _inputActions.MapView.Disable();

        OnMapToggled.Invoke();
    }
}