using System;
using System.Collections;
using System.Collections.Generic;
using UI.Colorable;
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
        
        /// <summary>
        /// image object this map icon is
        /// </summary>
        private Image _image;

        [SerializeField]
        public ColorableUIObject iconObject;

        public virtual Color color
        {
            get => iconObject.Color;
            set {
                iconObject.Color = value;
                //image.raycastTarget = image.color.a != 0.0f;
            }
        }

        public event Action OnInitialized;

        public event Action OnHoverEnter;
        public event Action OnHoverLeave;
        public event Action OnClick;

        protected virtual void Awake()
        {
            rectTransform = transform as RectTransform;

            OnInitialized?.Invoke();
        }

        public void OnPointerEnter(PointerEventData data) { OnHoverEnter?.Invoke(); }
        public void OnPointerExit(PointerEventData data) { OnHoverLeave?.Invoke(); }
        public void OnPointerClick(PointerEventData data) { OnClick?.Invoke(); }
    }
}