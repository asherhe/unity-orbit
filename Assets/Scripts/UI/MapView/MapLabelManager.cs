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
        private GameObject _apsisLabelPrefab;
        [SerializeField]
        private GameObject _SOITransitionLabelPrefab;
        [SerializeField]
        private GameObject _encounterLabelGroupPrefab;

        [SerializeField]
        private GameObject _celestialBodyLabelPrefab;
        [SerializeField]
        private GameObject _spacecraftLabelPrefab;

        // gameobjects that each hold a label type under one object, used to establish proper draw order
        private Transform _apsisContainer, _SOITransitionContainer, _encounterContainer, _celestialBodyContainer, _spacecraftContainer;

        private readonly HashSet<MapLabel> _labels = new();

        protected override void Awake()
        {
            base.Awake();

            // spacecraft > celestial body > SOI transition > encounters > apses
            // instantiate in reverse order because draw order shows newest first

            _apsisContainer = new GameObject("Apsis Label Container").transform;
            _apsisContainer.SetParent(transform, worldPositionStays: false);

            _encounterContainer = new GameObject("Encounter Label Group Container").transform;
            _encounterContainer.SetParent(transform, worldPositionStays: false);

            _SOITransitionContainer = new GameObject("SOI Transition Label Container").transform;
            _SOITransitionContainer.SetParent(transform, worldPositionStays: false);

            _celestialBodyContainer = new GameObject("Celestial Body Label Container").transform;
            _celestialBodyContainer.SetParent(transform, worldPositionStays: false);

            _spacecraftContainer = new GameObject("Spacecraft Label Container").transform;
            _spacecraftContainer.SetParent(transform, worldPositionStays: false);


            MapViewManager.WhenInstantiated(() =>
            {
                MapViewManager.Instance.OnMapToggled += UpdateLabelVisibility;
            });
        }

        private void Start()
        {
            UpdateLabelVisibility();
        }

        private void UpdateLabelVisibility()
        {
            gameObject.SetActive(MapViewManager.Instance.activeView == CameraView.MapView);
        }

        public ApsisLabel AddApsis(OrbitState orbit, ApsisLabel.DisplayMode mode)
        {
            var labelObject = Instantiate(_apsisLabelPrefab, _apsisContainer);
            var apsisLabel = labelObject.GetComponent<ApsisLabel>();
            apsisLabel.Orbit = orbit;
            apsisLabel.Mode = mode;
            return apsisLabel;
        }
        public SOITransitionLabel AddSOITransition(Patch patch)
        {
            var labelObject = Instantiate(_SOITransitionLabelPrefab, _SOITransitionContainer);
            var soiLabel = labelObject.GetComponent<SOITransitionLabel>();
            soiLabel.Patch = patch;
            return soiLabel;
        }
        public EncounterLabelGroup AddEncounterLabelGroup()
        {
            var labelObject = Instantiate(_encounterLabelGroupPrefab, _encounterContainer);
            return labelObject.GetComponent<EncounterLabelGroup>();
        }

        public CelestialBodyLabel AddCelestialBody(CelestialBody body)
        {
            var labelObject = Instantiate(_celestialBodyLabelPrefab, _celestialBodyContainer);
            labelObject.name = $"{body.bodyName} Label";
            return (CelestialBodyLabel)SetupObjectLabel(labelObject, body);
        }

        public SpacecraftLabel AddSpacecraft(Spacecraft craft)
        {
            var labelObject = Instantiate(_spacecraftLabelPrefab, _spacecraftContainer);
            labelObject.name = $"{craft.craftName} Label";
            return (SpacecraftLabel)SetupObjectLabel(labelObject, craft);
        }

        private ObjectLabel SetupObjectLabel(GameObject labelObject, OrbitingObject obj)
        {
            var label = labelObject.GetComponent<ObjectLabel>();
            label.Owner = obj;
            _labels.Add(label);
            return label;
        }
    }
}