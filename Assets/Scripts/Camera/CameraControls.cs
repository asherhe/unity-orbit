using DG.Tweening;
using Orbit;
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
    private float Zoom
    {
        get => zoomLevels[ActiveView];
        set
        {
            zoomLevels[ActiveView] = value;
            Vector2 currentBounds = zoomBounds[ActiveView];
            zoomLevels[ActiveView] = Mathf.Clamp(
                zoomLevels[ActiveView],
                currentBounds.x,
                currentBounds.y
            );
            _camera.DOOrthoSize(zoomLevels[ActiveView], 0.15f).SetEase(Ease.OutCubic);
        }
    }

    [SerializeField]
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
    private Vector2 panStartWorld;

    /// <summary>
    /// saved pan positions for different camera views
    /// </summary>
    public Dictionary<CameraView, Vector2> panPositions = new Dictionary<CameraView, Vector2>()
    {
        { CameraView.FlightView, new Vector2(0f, 0f) },
        { CameraView.MapView, new Vector2(0f, 0f) },
    };
    private Vector2 Pan
    {
        get => panPositions[ActiveView];
        set
        {
            panPositions[ActiveView] = value;
            _camera.transform.position = new Vector3(
                panPositions[ActiveView].x,
                panPositions[ActiveView].y,
                _camera.transform.position.z
            );
        }
    }

    /// <summary>
    /// currently focused object (from CameraFocus). used to determine the pan tween from old to new whenever the focus is changed
    /// </summary>
    private OrbitingObject _prevFocus;

    // block input when camera is tweening
    private bool isTweening = false;

    private void Awake()
    {
        _camera = Camera.main;

        _inputActions = new InputActions();
        _inputActions.Camera.Enable();

        MapViewManager.WhenInstantiated(() =>
        {
            MapViewManager.Instance.OnMapToggled += () =>
            {
                isTweening = true;
                _camera.DOOrthoSize(Zoom, _tweenTime)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        // for large map view zoom levels orthographic size might end up as 0 at the end because of floating point errors
                        Zoom = Zoom;
                        isTweening = false;
                    });
                _camera.transform.DOMove(new Vector3(Pan.x, Pan.y, _camera.transform.position.z), _tweenTime)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        Pan = Pan;
                    });
            };
        });

        CameraFocus.WhenInstantiated(() =>
        {
            _prevFocus = CameraFocus.Instance.Focus;
            CameraFocus.Instance.OnFocusChanged += () =>
            {
                isTweening = true;

                var focus = CameraFocus.Instance.Focus;
                var disp = CameraFocus.Instance.TransformObject(_prevFocus) + Pan;
                _prevFocus = focus;

                if (focus is CelestialBody body) Zoom = 3.0f * (float)body.radius;
                else Zoom = 2.0f * (float)focus.Position.Magnitude;
                _camera.DOOrthoSize(Zoom, _tweenTime)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        Zoom = Zoom;
                        isTweening = false;
                    });

                Pan = disp;
                _camera.transform.DOMove(new Vector3(0f, 0f, _camera.transform.position.z), _tweenTime)
                    .SetEase(Ease.OutQuad)
                    .OnComplete(() =>
                    {
                        Pan = Vector2.zero;
                    });
            };
        });
    }

    private void Update()
    {
        if (!isTweening)
        {
            float zoomInput = _inputActions.Camera.Zoom.ReadValue<float>();

            if (zoomInput != 0.0f)
                Zoom *= Mathf.Exp(zoomInput * zoomSpeed * Time.deltaTime);

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
                Pan += displacement;
            }
        }
    }

    private Vector2 GetMouseWorldPos()
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
