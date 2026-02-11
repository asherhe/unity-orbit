using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    [RequireComponent(typeof(FollowWorldTransform))]
    public class SpacecraftIcon : MonoBehaviour
    {
        private FollowWorldTransform _follow;

        [SerializeField]
        private Spacecraft _craft;
        public Spacecraft Craft
        {
            get => _craft;
            set
            {
                _craft = value;
                _follow.follow = _craft.transform;
            }
        }

        private void Awake()
        {
            _follow = GetComponent<FollowWorldTransform>();
            _follow.shouldFollowPosition = true;
            _follow.shouldFollowRotation = true;

            // in case _craft was set in inspector
            if (_craft != null)
                Craft = _craft;
        }
    }
}
