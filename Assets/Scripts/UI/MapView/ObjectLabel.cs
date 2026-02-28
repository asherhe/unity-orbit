using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UI
{
    /// <summary>
    /// base class for map-screen labels of IOrbitingObjects.
    /// extend this to create a specific implementation of the ObjectLabel behaviours you want
    /// </summary>
    [RequireComponent(typeof(FollowWorldTransformFromScreen))]
    public abstract class ObjectLabel : MapLabel
    {
        private InputActions _inputActions;

        protected FollowWorldTransformFromScreen _follow;

        private Orbit.OrbitingObject _owner;
        public Orbit.OrbitingObject Owner
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

        private bool _isTargeted = false;
        /// <summary>
        /// whether this label is the active target
        /// </summary>
        protected bool IsTargeted
        {
            get => _isTargeted;
            set
            {
                if (_isTargeted == value) return;
                _isTargeted = value;
                ShowLabel = _isTargeted;
            }
        }

        protected override void Awake()
        {
            base.Awake();

            _inputActions = new InputActions();
            icon.OnHoverEnter += data => _inputActions.Ctx_ObjectLabel.Enable();
            icon.OnHoverLeave += data => _inputActions.Ctx_ObjectLabel.Disable();

            _follow = GetComponent<FollowWorldTransformFromScreen>();

            OnOwnerUpdated += () => _follow.follow = Owner.gameObject.transform;
            OnOwnerUpdated += UpdateVisuals;
            OnOwnerUpdated += UpdateTargeting;

            _inputActions.Ctx_ObjectLabel.Target.performed += ctx => ToggleTarget();

            TargetingSystem.WhenInstantiated(() =>
            {
                TargetingSystem.Instance.OnTargetChanged += UpdateTargeting;
            });
        }

        private void ToggleTarget()
        {
            IsTargeted = !IsTargeted;
            if (IsTargeted) TargetingSystem.Instance.Target = Owner;
            else TargetingSystem.Instance.Target = null;
        }

        private void UpdateTargeting()
        {
            IsTargeted = TargetingSystem.Instance.Target == Owner;
            if (IsTargeted) labelText.text = $">{Name}<";
            else labelText.text = Name;
        }
    }
}