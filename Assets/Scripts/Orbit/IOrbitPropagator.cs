using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Orbit
{
    /// <summary>
    /// provides procedures to obtain the state of an orbit at any given time
    /// </summary>
    public interface IOrbitPropagator
    {
        public Vector2d GetPosition(double t);
        public Vector2d GetVelocity(double t);
        public StateVectors GetStateVectors(double t);
    }
}