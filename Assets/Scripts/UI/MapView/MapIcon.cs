using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// represents an icon for map view labels. attach this to the GameObject that is intended to recieve hover events
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class MapIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public RectTransform rectTransform { get; private set; }
        
        [SerializeField]
        public Graphic iconGraphic;

        public virtual Color color
        {
            get => iconGraphic.color;
            set {
                iconGraphic.color = value;
                iconGraphic.raycastTarget = color.a != 0.0f;
            }
        }

        public event Action OnHoverEnter;
        public event Action OnHoverLeave;
        public event Action OnClick;

        protected virtual void Awake()
        {
            rectTransform = transform as RectTransform;
        }

        public void OnPointerEnter(PointerEventData data) { OnHoverEnter?.Invoke(); }
        public void OnPointerExit(PointerEventData data) { OnHoverLeave?.Invoke(); }
        public void OnPointerClick(PointerEventData data) { OnClick?.Invoke(); }
    }
}