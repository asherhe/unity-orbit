using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI{
    public class CelestialBodyIcon : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        public event Action OnHoverEnter;
        public event Action OnHoverLeave;
        public event Action OnClick;

        public void OnPointerEnter(PointerEventData data) { OnHoverEnter?.Invoke(); }
        public void OnPointerExit(PointerEventData data) { OnHoverLeave?.Invoke(); }
        public void OnPointerClick(PointerEventData data) { OnClick?.Invoke(); }
    }
}