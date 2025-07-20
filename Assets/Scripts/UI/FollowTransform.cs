using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class FollowTransform : MonoBehaviour
    {
        /// <summary>
        /// transform to follow
        /// </summary>
        public Transform follow;

        public bool shouldFollowPosition = true;
        public bool shouldFollowRotation = false;

        private void LateUpdate()
        {
            if (follow != null)
            {
                if (shouldFollowPosition) transform.position = follow.position;
                if (shouldFollowRotation) transform.rotation = follow.rotation;
            }
        }
    }
}
