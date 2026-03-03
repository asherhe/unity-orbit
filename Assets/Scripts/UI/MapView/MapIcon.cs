using MathNet.Numerics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
            set
            {
                iconGraphic.color = value;
                iconGraphic.raycastTarget = color.a != 0.0f;
            }
        }

        public event Action<PointerEventData> OnHoverEnter;
        public event Action<PointerEventData> OnHoverLeave;
        public event Action<PointerEventData> OnClick;

        protected virtual void Awake()
        {
            rectTransform = transform as RectTransform;
        }

        public void SetIcon(Sprite sprite) { (iconGraphic as Image).sprite = sprite; }
        public void SetIcon(string spriteName)
        {
            // load all subsprites in master icon spritesheet
            Addressables.LoadAssetAsync<Sprite>($"Assets/UI/icons.png[{spriteName}]").Completed += handle =>{   SetIcon(handle.Result);};
        }

        public void OnPointerEnter(PointerEventData data) => OnHoverEnter?.Invoke(data);
        public void OnPointerExit(PointerEventData data) => OnHoverLeave?.Invoke(data);
        public void OnPointerClick(PointerEventData data) => OnClick?.Invoke(data);
    }
}