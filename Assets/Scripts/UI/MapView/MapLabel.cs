using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// base class for map-screen labels of IOrbitingObjects.
    /// extend this to create a specific implementation of the MapLabel behaviours you want
    /// </summary>
    [RequireComponent(typeof(FollowWorldTransformFromScreen))]
    public class MapLabel : MonoBehaviour
    {
        private FollowWorldTransformFromScreen _follow;

        private Orbit.IOrbitingObject _owner;
        public Orbit.IOrbitingObject Owner
        {
            get => _owner;
            set {
                if (_owner == value) return;
                _owner = value;
                UpdateVisuals();
            }
        }

        protected virtual void Awake()
        {
            _follow = GetComponent<FollowWorldTransformFromScreen>();
        }

        protected virtual void UpdateVisuals()
        {
            _follow.follow = Owner.gameObject.transform;
        }
    }
}
