using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// base class for map-screen labels of IOrbitingObjects.
    /// extend this to create a specific implementation of the ObjectLabel behaviours you want
    /// </summary>
    [RequireComponent(typeof(FollowWorldTransformFromScreen))]
    public abstract class ObjectLabel : MapLabel
    {
        protected FollowWorldTransformFromScreen _follow;

        private Orbit.IOrbitingObject _owner;
        public Orbit.IOrbitingObject Owner
        {
            get => _owner;
            set
            {
                if (_owner == value) return;
                _owner = value;
                OnOwnerUpdated?.Invoke();
            }
        }

        protected event Action OnOwnerUpdated;

        public abstract string Name { get; }

        /// <summary>
        /// whether this label is the active target
        /// </summary>
        protected bool isTargeted = false;

        protected override void Awake()
        {
            base.Awake();

            _follow = GetComponent<FollowWorldTransformFromScreen>();

            OnOwnerUpdated += () => _follow.follow = Owner.gameObject.transform;
            OnOwnerUpdated += UpdateVisuals;
            OnOwnerUpdated += SetName;

            icon.OnClick += ToggleTarget;
            TargetingSystem.Instance.OnTargetChanged += SetName;
        }

        private void ToggleTarget()
        {
            isTargeted = !isTargeted;
            ShowLabel = isTargeted;
            if (isTargeted) TargetingSystem.Instance.Target = Owner;
            else TargetingSystem.Instance.Target = null;
        }

        private void SetName()
        {
            if (isTargeted) labelText.text = $">{Name}<";
            else labelText.text = Name;
        }
    }
}