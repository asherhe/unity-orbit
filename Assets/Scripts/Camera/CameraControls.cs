using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControls : MonoBehaviour, ISerializationCallbackReceiver
{
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

    // block input when camera is tweening
    private bool isTweening = false;

    private void Start()
    {
        _inputActions = new InputActions();
        _inputActions.Camera.Enable();
        MapViewManager.Instance.OnMapToggled += () =>
        {
            isTweening = true;
            Camera.main.DOOrthoSize(zoomLevels[MapViewManager.Instance.activeView], 0.25f)
                .OnComplete(() =>
                {
                    // for large map view zoom levels orthographic size might end up as 0 at the end because of floating point errors
                    Camera.main.orthographicSize = zoomLevels[MapViewManager.Instance.activeView];
                    isTweening = false;
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
                float zoomLevel = zoomLevels[MapViewManager.Instance.activeView];
                zoomLevel *= Mathf.Exp(zoomInput * zoomSpeed * Time.deltaTime);
                Vector2 currentBounds = zoomBounds[MapViewManager.Instance.activeView];
                zoomLevel = Mathf.Clamp(
                    zoomLevel,
                    currentBounds.x,
                    currentBounds.y
                );
                zoomLevels[MapViewManager.Instance.activeView] = zoomLevel;
                Camera.main.DOOrthoSize(zoomLevel, 0.2f).SetEase(Ease.OutCubic);
            }
        }
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
    private ViewZoomLevel []_zoomLevels;
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
        foreach(var kvp in zoomLevels)
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
