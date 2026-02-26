using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// any object in orbit around a celestial body
    /// </summary>
    public class OrbitingObject : MonoBehaviour
    {
        public OrbitState orbit { get; protected set; }

        protected IOrbitPropagator prop;

        /// <summary>
        /// cached orbit state
        /// </summary>
        private StateVectors cachedState = new(double.NaN, Vector2d.zero, Vector2d.zero);

        /// <summary>
        /// if the cached state is out of date, recalculate it
        /// </summary>
        private void RenewCachedState()
        {
            if (Universe.Instance.UT != cachedState.time)
                cachedState = prop.GetStateVectors(Universe.Instance.UT);
        }

        // orbital propagators
        public Vector2d Position
        {
            get
            {
                RenewCachedState();
                return cachedState.pos;
            }
        }
        public Vector2d Velocity
        {
            get
            {
                RenewCachedState();
                return cachedState.vel;
            }
        }
        public StateVectors StateVectors
        {
            get
            {
                RenewCachedState();
                return cachedState;
            }
        }

        public Vector2d GetPositionAt(double t) => prop.GetPosition(t);
        public Vector2d GetVelocityAt(double t) => prop.GetVelocity(t);
        public StateVectors GetStateVectorsAt(double t) => prop.GetStateVectors(t);
    }
}