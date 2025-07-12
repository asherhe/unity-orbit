using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [ExecuteInEditMode]
    public class IntegerCanvasScale : MonoBehaviour
    {
        public Vector2 referenceResolution = new Vector2(1000, 800);

        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            float scaleX = Screen.width / referenceResolution.x;
            float scaleY = Screen.height / referenceResolution.y;
            float scaleFactor = Mathf.Min(scaleX, scaleY);
            scaleFactor = Mathf.Ceil(scaleFactor);
            _canvas.scaleFactor = scaleFactor;
        }
    }
}
