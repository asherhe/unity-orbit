using Colourful;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TagIcon : MapIcon
    {
        /// <summary>
        /// gameobject that represents the tag display
        /// </summary>
        [SerializeField]
        private Graphic _tagGraphic;

        private IColorConverter<RGBColor, LabColor> _conv;

        public override Color color
        {
            get => _tagGraphic.color;
            set
            {
                _tagGraphic.color = value;
                _tagGraphic.raycastTarget = color.a != 0.0f;

                LabColor lab = _conv.Convert(new RGBColor(color.r, color.g, color.b));
                var val = lab.L < 90 ? 1.0f : 0.0f;
                iconGraphic.color = new Color(val, val, val, color.a);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            iconGraphic.raycastTarget = false;
            _conv = new ConverterBuilder().FromRGB().ToLab().Build();
        }
    }
}