using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class SpacecraftIconManager : MonoBehaviour
    {
        [SerializeField]
        private GameObject _iconPrefab;

        private void Awake()
        {
            Spacecraft.OnSpacecraftLoaded += CreateIcon;
        }

        private void CreateIcon(Spacecraft craft)
        {
            var icon = Instantiate(_iconPrefab, transform);
            icon.name = $"Icon ({craft.name})";
            icon.GetComponent<SpacecraftIcon>().Craft = craft;
        }
    }
}
