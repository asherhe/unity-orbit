using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [ExecuteInEditMode]
    public class IntegerCanvasScale : SingletonBehaviour<IntegerCanvasScale>
    {
        public Vector2 referenceResolution = new Vector2(1000, 800);

        /// <summary>
        /// conversion factor from world space length to canvas space length
        /// </summary>
        public float Canvas2World { get; private set; }

        private Canvas _canvas;

        protected override void Awake()
        {
            base.Awake();
            _canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            float scaleX = Screen.width / referenceResolution.x;
            float scaleY = Screen.height / referenceResolution.y;
            float scaleFactor = Mathf.Min(scaleX, scaleY);
            scaleFactor = Mathf.Ceil(scaleFactor);
            _canvas.scaleFactor = scaleFactor;

            // determine world -> canvas conversion factor
            Vector2 worldA = Vector2.zero, worldB = Vector2.right * Camera.main.orthographicSize;
            var screenA = Camera.main.WorldToScreenPoint(worldA);
            var screenB = Camera.main.WorldToScreenPoint(worldB);
            Vector2 canvasA, canvasB;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_canvas.transform, screenA, null, out canvasA
            );
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_canvas.transform, screenB, null, out canvasB
            );
            Canvas2World = (worldA - worldB).magnitude / (canvasA - canvasB).magnitude;
        }
    }
}
