using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class ApsisLabel : OrbitPOILabel
    {
        [SerializeField]
        private Sprite _periapsisIcon, _apoapsisIcon;

        private Image _iconImage;

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
            _iconImage = icon.iconGraphic as Image;
            UpdateIcon();
        }

        protected override Vector2d GetPosition()
        {
            MatchTrajectoryColor();

            // position of apsis in perifocal space
            var perifocal = Vector2d.right;
            if (Mode == DisplayMode.Periapsis) perifocal *= Orbit.periapsis;
            else perifocal *= -Orbit.apoapsis;

            // hide label if it's not supposed to be shown
            var isEscaped = Mode == DisplayMode.Apoapsis && Orbit.apoapsis > Orbit.body.soiRadius;
            var isUnderground = Mode == DisplayMode.Periapsis && Orbit.periapsis < Orbit.body.radius;
            if (isEscaped || isUnderground) return null;

            return Orbit.PerifocalToBody(perifocal);
        }
        private void UpdateIcon()
        {
            _iconImage.sprite = (Mode == DisplayMode.Periapsis) ? _periapsisIcon : _apoapsisIcon;
        }
    }
}