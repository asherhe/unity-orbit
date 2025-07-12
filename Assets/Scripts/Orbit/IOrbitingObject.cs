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
    }
}