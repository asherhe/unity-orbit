using Orbit;
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
        private GameObject _celestialBodyLabelPrefab;
        [SerializeField]
        private GameObject _spacecraftLabelPrefab;

        private readonly HashSet<MapLabel> _labels = new();

        public CelestialBodyLabel AddCelestialBody(CelestialBody body)
        {
            var labelObject = Instantiate(_celestialBodyLabelPrefab, transform);
            labelObject.name = $"{body.bodyName} Label";
            return (CelestialBodyLabel)SetupObjectLabel(labelObject, body);
        }

        public SpacecraftLabel AddSpacecraft(Spacecraft craft)
        {
            var labelObject = Instantiate(_spacecraftLabelPrefab, transform);
            labelObject.name = $"{craft.craftName} Label";
            return (SpacecraftLabel)SetupObjectLabel(labelObject, craft);
        }

        private ObjectLabel SetupObjectLabel(GameObject labelObject, IOrbitingObject obj)
        {
            var label = labelObject.GetComponent<ObjectLabel>();
            label.Owner = obj;
            _labels.Add(label);
            return label;
        }
    }
}