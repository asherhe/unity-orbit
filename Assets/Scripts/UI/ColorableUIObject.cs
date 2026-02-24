using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI.Colorable
{
    public abstract class ColorableUIObject : MonoBehaviour
    {
        public abstract Color Color { get; set; }
    }
}