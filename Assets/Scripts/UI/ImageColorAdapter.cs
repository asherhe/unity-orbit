using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI.Colorable
{
    [RequireComponent(typeof(Image))]
    public class ImageColorAdapter : ColorableUIObject
    {
        public Image image { get; private set; }

        public override Color Color
        {
            get => image.color;
            set => image.color = value;
        }

        private void Awake()
        {
            image = GetComponent<Image>();
        }
    }
}