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
            if (orbit == null)
                cachedState = new(Universe.Instance.UT, Vector2d.zero, Vector2d.zero);
            else if (Universe.Instance.UT != cachedState.time)
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

        public Vector2d GetPositionAt(double t) =>
            orbit != null ? prop.GetPosition(t) : Vector2d.zero;
        public Vector2d GetVelocityAt(double t) =>
            orbit != null ? prop.GetVelocity(t) : Vector2d.zero;
        public StateVectors GetStateVectorsAt(double t) =>
            orbit != null ? prop.GetStateVectors(t) : new(t, Vector2d.zero, Vector2d.zero);

        /// <summary>
        /// get the current position of this body, with the sun at the origin
        /// </summary>
        public Vector2d GetHeliocentricPosition()
        {
            var parentPos = Vector2d.zero;
            if (orbit != null && orbit.body != null) parentPos = orbit.body.GetHeliocentricPosition();
            return parentPos + Position;
        }

        /// <summary>
        /// get the vector that points to a direction in prograde-radial space
        /// </summary>
        public Vector2d GetPRDirection(PRDirection mode)
        {
            var prograde = Velocity.Normalized;
            var dir = orbit.h > 0 ? 1.0f : -1.0f;
            switch (mode)
            {
                case PRDirection.Prograde:
                    return prograde;
                case PRDirection.Retrograde:
                    return -prograde;
                case PRDirection.RadialOut:
                    return new(dir * prograde.y, -dir * prograde.x);
                case PRDirection.RadialIn:
                    return new(-dir * prograde.y, dir * prograde.x);
                default:
                    return Vector2d.zero;
            }
        }

        /// <summary>
        /// determine position of obj2 relative to obj1
        /// </summary>
        /// <param name="obj1">object to compare against</param>
        /// <param name="obj2">object being compared</param>
        /// <returns>displacement from obj1 to obj2</returns>
        public static Vector2d GetRelativePosition(OrbitingObject obj1, OrbitingObject obj2)
        {
            if (obj1 == obj2) return Vector2d.zero;

            // get lowest common ancestor of the two orbits
            OrbitingObject cur;
            LinkedList<OrbitingObject> path1 = new(), path2 = new();
            path1.AddFirst(cur = obj1);
            while (cur.orbit != null)
            {
                cur = cur.orbit.body;
                path1.AddFirst(cur);
            }

            path2.AddFirst(cur = obj2);
            while (cur.orbit != null)
            {
                cur = cur.orbit.body;
                path2.AddFirst(cur);
            }
            // look for the first difference
            OrbitingObject common = path1.First.Value;
            LinkedListNode<OrbitingObject> node1 = path1.First, node2 = path2.First;
            while (node1.Value == node2.Value)
            {
                common = node1.Value;
                if ((node1 = node1.Next) == null || (node2 = node2.Next) == null) break;
            }

            // position of the two objects in common space
            Vector2d pos1 = Vector2d.zero, pos2 = Vector2d.zero;
            node1 = path1.Last;
            while (node1.Value != common)
            {
                pos1 += new UniversalPropagator(node1.Value.orbit).GetPosition(Universe.Instance.UT);
                node1 = node1.Previous;
            }
            node2 = path2.Last;
            while (node2.Value != common)
            {
                pos2 += new UniversalPropagator(node2.Value.orbit).GetPosition(Universe.Instance.UT);
                node2 = node2.Previous;
            }

            return pos2 - pos1;
        }
    }
}