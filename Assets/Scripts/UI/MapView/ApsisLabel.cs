using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class ApsisLabel : POILabel
    {
        [SerializeField]
        private Sprite _periapsisIcon, _apoapsisIcon;

        public enum DisplayMode
        {
            Periapsis, Apoapsis
        };
        private DisplayMode _mode;
        public DisplayMode Mode
        {
            get => _mode;
            set
            {
                if (_mode == value) return;
                _mode = value;
                UpdateIcon();
            }
        }

        protected override void Awake()
        {
            base.Awake();
            SetColors(iconColor, new Color(0.1882353f, 0.7568628f, 0.3529412f));
        }

        protected override Vector2d GetPosition()
        {
            // position of apsis in perifocal space
            var perifocal = Vector2d.right;
            if (Mode == DisplayMode.Periapsis) perifocal *= Orbit.periapsis;
            else perifocal *= -Orbit.apoapsis;

            return Orbit.PerifocalToBody(perifocal);
        }
        private void UpdateIcon()
        {
            iconImage.sprite = Mode == DisplayMode.Periapsis ? _periapsisIcon : _apoapsisIcon;
        }
    }
}