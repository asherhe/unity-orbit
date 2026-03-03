using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class SOITransitionLabel : PatchPOILabel
    {
        [SerializeField]
        private Sprite _enterIcon, _exitIcon;

        private Image _iconImage;

        public enum DisplayMode { Enter, Exit }

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

            switch (Mode)
            {
                case DisplayMode.Enter:
                    if (Patch.prevPatch != null && Patch.prevPatch.HasTransition)
                        return Patch.prevPatch.NextTransition.NextState.pos;
                    break;
                case DisplayMode.Exit:
                    if (Patch.HasTransition)
                        return Patch.NextTransition.State.pos;
                    break;
            }
            return null;
        }
        protected override string GetLabelText()
        {
            OrbitTransitionHandler transition;
            switch (Mode)
            {
                case DisplayMode.Enter:
                    if (Patch.prevPatch == null) return "";
                    transition = Patch.prevPatch.NextTransition;
                    if (transition == Patch.prevPatch.soiEscape) return $"{Patch.prevPatch.patchOrbit.body.bodyName} Escape";
                    if (transition == Patch.prevPatch.soiIntercept) return $"{Patch.prevPatch.soiIntercept.nextCaptureBody.bodyName} Capture";
                    break;
                case DisplayMode.Exit:
                    transition = Patch.NextTransition;
                    if (transition == null) return "";
                    if (transition == Patch.soiEscape) return $"{Patch.patchOrbit.body.bodyName} Escape";
                    if (transition == Patch.soiIntercept) return $"{Patch.soiIntercept.nextCaptureBody.bodyName} Capture";
                    break;
            }
            return "Orbit transition";
        }
        private void UpdateIcon()
        {
            _iconImage.sprite = (Mode == DisplayMode.Enter) ? _enterIcon : _exitIcon;
        }
    }
}