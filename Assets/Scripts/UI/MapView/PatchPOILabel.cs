using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public abstract class PatchPOILabel : POILabel
    {
        private Patch _patch;
        /// <summary>
        /// patch that is attached to this POILabel
        /// </summary>
        public Patch Patch
        {
            get => _patch;
            set
            {
                if (_patch == value) return;
                if (_patch != null)
                {
                    _patch.OnTransitionUpdate -= RefreshLabel;
                    _patch.manager.OnTransition -= RefreshLabel;
                }
                _patch = value;
                _patch.OnTransitionUpdate += RefreshLabel;
                _patch.manager.OnTransition += OnPatchTransition;
            }
        }

        protected override OrbitState LabelOrbit => Patch.patchOrbit;

        protected virtual void OnPatchTransition()
        {
            IsTextActive = false;
            RefreshLabel();
            if (IsActive) IsActive = Patch.IsActive;
        }
    }
}