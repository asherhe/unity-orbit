using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour, ISerializationCallbackReceiver
{
    private CameraView ActiveView => MapViewManager.Instance.activeView;

    private InputActions _inputActions;

    public float zoomSpeed = 0.1f;
    public Dictionary<CameraView, Vector2> zoomBounds = new Dictionary<CameraView, Vector2>()
    {
        { CameraView.FlightView, new Vector2(0.1f, 100f) },
        { CameraView.MapView, new Vector2(1e5f, 1e12f) },
    };
    public Dictionary<CameraView, float> zoomLevels = new Dictionary<CameraView, float>()
    {
        { CameraView.FlightView, 10f },
        { CameraView.MapView, 5e6f }
    };

    private float _tweenTime = 0.25f;

    private Camera _camera;

    /// <summary>
    /// whether or not a pan operation is cative
    /// </summary>
    private bool isPanning = false;
    /// <summary>
    /// location of pointer, scaled to world size, at the start of panning.
    /// note that this is not the actual world space position, just a screen space mouse position scaled to world space
    /// </summary>
    private Vector3 panStartWorld;

    /// <summary>
    /// saved pan positions for different camera views
    /// </summary>
    public Dictionary<CameraView, Vector3> panPositions = new()
    {
        { CameraView.FlightView, new(0f, 0f, -10f) },
        { CameraView.MapView, new(0f, 0f, -10f) },
    };

    // block input when camera is tweening
    private bool isTweening = false;

    private void Awake()
    {
        _camera = Camera.main;

        _inputActions = new InputActions();
        _inputActions.Camera.Enable();

        MapViewManager.Instance.OnMapToggled += () =>
        {
            isTweening = true;
            _camera.DOOrthoSize(zoomLevels[ActiveView], _tweenTime)
                .OnComplete(() =>
                {
                    // for large map view zoom levels orthographic size might end up as 0 at the end because of floating point errors
                    _camera.orthographicSize = zoomLevels[ActiveView];
                    isTweening = false;
                });
            _camera.transform.DOMove(panPositions[ActiveView], _tweenTime)
                .OnComplete(() =>
                {
                    _camera.transform.position = panPositions[ActiveView];
                });
        };
    }

    private void Update()
    {
        if (!isTweening)
        {
            float zoomInput = _inputActions.Camera.Zoom.ReadValue<float>();

            if (zoomInput != 0.0f)
            {
                float zoomLevel = zoomLevels[ActiveView];
                zoomLevel *= Mathf.Exp(zoomInput * zoomSpeed * Time.deltaTime);
                Vector2 currentBounds = zoomBounds[ActiveView];
                zoomLevel = Mathf.Clamp(
                    zoomLevel,
                    currentBounds.x,
                    currentBounds.y
                );
                zoomLevels[ActiveView] = zoomLevel;
                _camera.DOOrthoSize(zoomLevel, 0.2f).SetEase(Ease.OutCubic);
            }

            if (_inputActions.Camera.Pan.WasPressedThisFrame() && _inputActions.Camera.Pan.IsPressed())
            {
                isPanning = true;
                panStartWorld = GetMouseWorldPos();
            }
            if (_inputActions.Camera.Pan.WasReleasedThisFrame())
            {
                isPanning = false;
            }

            if (isPanning)
            {
                var displacement = panStartWorld - GetMouseWorldPos();
                panPositions[ActiveView] += displacement;
                _camera.transform.position = panPositions[ActiveView];
            }
        }
    }

    private Vector3 GetMouseWorldPos()
    {
        var screen2world = 2.0f * _camera.orthographicSize / Math.Min(Screen.width, Screen.height);
        Vector2 screenPos = _inputActions.Camera.MousePosition.ReadValue<Vector2>();
        return _camera.ScreenToWorldPoint(screenPos);
    }

    [Serializable]
    private class ViewZoomBound
    {
        public CameraView view;
        public float min, max;
    }
    [SerializeField]
    private ViewZoomBound[] _zoomBounds;
    [Serializable]
    private class ViewZoomLevel
    {
        public CameraView view;
        public float zoomLevel;
    }
    [SerializeField]
    private ViewZoomLevel[] _zoomLevels;
    public void OnBeforeSerialize()
    {
        int i;

        _zoomBounds = new ViewZoomBound[zoomBounds.Count];
        i = 0;
        foreach (var kvp in zoomBounds)
        {
            _zoomBounds[i] = new ViewZoomBound();
            _zoomBounds[i].view = kvp.Key;
            _zoomBounds[i].min = kvp.Value.x;
            _zoomBounds[i].max = kvp.Value.y;
            i++;
        }

        _zoomLevels = new ViewZoomLevel[zoomLevels.Count];
        i = 0;
        foreach (var kvp in zoomLevels)
        {
            _zoomLevels[i] = new ViewZoomLevel();
            _zoomLevels[i].view = kvp.Key;
            _zoomLevels[i].zoomLevel = kvp.Value;
            i++;
        }
    }
    public void OnAfterDeserialize()
    {
        zoomBounds.Clear();
        foreach (var bound in _zoomBounds)
            zoomBounds.Add(bound.view, new Vector2(bound.min, bound.max));
        zoomLevels.Clear();
        foreach (var level in _zoomLevels)
            zoomLevels.Add(level.view, level.zoomLevel);
    }
}
