using Colourful;
using System;
using System.Collections;
using System.Collections.Generic;
using UI.Colorable;
using UnityEngine;

namespace UI
{
    public class TagIcon : MapIcon
    {
        /// <summary>
        /// gameobject that represents the tag display
        /// </summary>
        [SerializeField]
        private ColorableUIObject _tagObject;

        private IColorConverter<RGBColor, LabColor> _conv;

        private Color _color;
        public override Color color
        {
            get => _color;
            set
            {
                _color = value;
                _tagObject.Color = _color;
                
                LabColor lab = _conv.Convert(new RGBColor(_color.r, _color.g, _color.b));
                var val = lab.L < 90 ? 1.0f : 0.0f;
                iconObject.Color = new Color(val, val, val, _color.a);
            }
        }

        protected override void Awake()
        {
            base.Awake();
            _conv = new ConverterBuilder().FromRGB().ToLab().Build();
        }
    }
}