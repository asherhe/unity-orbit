using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// any object in orbit around a celestial body
    /// </summary>
    public interface IOrbitingObject
    {
        public OrbitState orbit { get; }

        public Vector2d GetPosition();
        public Vector2d GetVelocity();
        public StateVectors GetStateVectors();
        public Vector2d GetPosition(double t);
        public Vector2d GetVelocity(double t);
        public StateVectors GetStateVectors(double t);
    }
}