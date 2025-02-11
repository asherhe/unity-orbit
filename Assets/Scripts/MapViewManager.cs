using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CinemachineVirtualCamera))]
public class MapViewManager : MonoBehaviour
{
    private CinemachineVirtualCamera _virtualCamera;
    private InputActions _inputActions;

    public static MapViewManager Instance { get; private set; }

    public bool isInMapView { get; private set; } = false;

    /// <summary>
    /// the camera responsible for rendering map view
    /// </summary>
    public Camera activeCamera { get; private set; }

    [SerializeField]
    private Camera minimapCamera;

    /// <summary>
    /// layermask for map-only rendering (hide stuff we don't want to show in map view)
    /// </summary>
    [SerializeField]
    private LayerMask mapCullingMask;

    /* saved camera properties from flight view (so we can revert back to it when we exit map view) */
    private LayerMask oldCullingMask;

    /// <summary>
    /// gets invoked when map view is toggled
    /// </summary>
    public event Action MapToggled;

    private void Awake()
    {
        if (Instance == null || Instance == this) Instance = this;
        else Destroy(this);

        _virtualCamera = GetComponent<CinemachineVirtualCamera>();

        activeCamera = minimapCamera;

        _inputActions = new InputActions();
        _inputActions.Flight.Enable();
        _inputActions.Flight.ToggleMap.performed += (context) => ToggleMapView();
    }

    public void ToggleMapView()
    {
        if (isInMapView) ExitMapView();
        else EnterMapView();
    }

    public void EnterMapView()
    {
        isInMapView = true;

        activeCamera = Camera.main;

        oldCullingMask = Camera.main.cullingMask;
        Camera.main.cullingMask = mapCullingMask;

        _virtualCamera.Priority = 20;

        MapToggled.Invoke();
    }

    public void ExitMapView()
    {
        isInMapView = false;

        activeCamera = minimapCamera;

        Camera.main.cullingMask = oldCullingMask;

        _virtualCamera.Priority = 0;

        MapToggled.Invoke();
    }
}