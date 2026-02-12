using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// map view labels for the location of objects
    /// </summary>
    public class MapLabelManager : SingletonBehaviour<MapLabelManager>
    {
        [SerializeField]
        private GameObject _labelPrefab;

        private readonly HashSet<MapLabel> _labels = new();

        public CelestialBodyLabel AddCelestialBody(CelestialBody body)
        {
            var labelObject = Instantiate(_labelPrefab, transform);
            labelObject.name = $"{body.bodyName} Label";
            var label = labelObject.GetComponent<CelestialBodyLabel>();
            label.Owner = body;
            _labels.Add(label);
            return label;
        }
    }
}