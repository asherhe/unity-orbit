using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// indictes an important point on an orbit
    /// </summary>
    public class PointOfInterest : MonoBehaviour
    {
        protected Sprite _icon;
        
        protected Image _iconObject;
        protected TMP_Text _labelText;

        /// <summary>
        /// text to show on label
        /// </summary>
        protected virtual string GetText() => "";
    }
}