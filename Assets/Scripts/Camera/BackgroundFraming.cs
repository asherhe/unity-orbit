using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundFraming : MonoBehaviour
{
    private Camera _camera;

    private SpriteRenderer _spriteRenderer;
    private Material _material;

    /// <summary>
    /// camera orthographic size at scale points for background zoom
    /// </summary>
    [SerializeField]
    private Vector2 _cameraSizeMinMax = new(8.0f, 1e12f);
    /// <summary>
    /// zoom level of the background corresponding to the camera orthographic size
    /// </summary>
    [SerializeField]
    private Vector2 _backgroundZoomMinMax = new(2.0f, 1.0f);

    private Vector2 _logCameraSizeMinMax;
    private float _zoomPerLogSize;

    private void Awake()
    {
        _camera = Camera.main;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _material = _spriteRenderer.material;

        _logCameraSizeMinMax = new(Mathf.Log10(_cameraSizeMinMax.x), Mathf.Log10(_cameraSizeMinMax.y));
        _zoomPerLogSize = (_backgroundZoomMinMax.y - _backgroundZoomMinMax.x) / (_logCameraSizeMinMax.y - _logCameraSizeMinMax.x);
    }

    private void LateUpdate()
    {
        var size = _camera.orthographicSize;
        var fitCamera = 2 * size* Mathf.Max(_camera.aspect, 1.0f);
        var zoom = _backgroundZoomMinMax.x + (Mathf.Log10(size) - _logCameraSizeMinMax.x) * _zoomPerLogSize;
        transform.localScale = Vector3.one * (fitCamera * zoom);
        //_spriteRenderer.size = Vector2.one / zoom;
    }
}
