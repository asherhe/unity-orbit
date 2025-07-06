using Parts;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(VerticalLayoutGroup))]
public class ResourceTable : MonoBehaviour
{
    private Spacecraft _craft;

    [SerializeField]
    private GameObject _rowPrefab;

    private Dictionary<string, ResourceRow> _rows = new();

    private void Awake()
    {
        _craft = ActiveCraftController.Instance.craft;
        _craft.OnLoaded += GenerateTable;
    }

    private void GenerateTable()
    {
        Dictionary<string, ResourceContainerPlugin.Resource> resources = new();
        foreach (var part in _craft.parts)
        {
            foreach (var plugin in part.plugins)
            {
                if (typeof(ResourceContainerPlugin).IsAssignableFrom(plugin.GetType()))
                {
                    foreach (var resource in ((ResourceContainerPlugin)plugin).Resources)
                    {
                        ResourceContainerPlugin.Resource tableResource;
                        if (!resources.ContainsKey(resource.type))
                        {
                            tableResource = new ResourceContainerPlugin.Resource(resource);
                            resources.Add(resource.type, tableResource);
                        }
                        else
                        {
                            tableResource = resources[resource.type];
                            tableResource.amount += resource.amount;
                            tableResource.maxAmount += resource.maxAmount;
                        }
                    }
                }
            }
        }

        foreach (var resource in resources.Values)
        {
            var rowGameObject = Instantiate(_rowPrefab, transform);
            var resourceRow = rowGameObject.GetComponent<ResourceRow>();
            resourceRow.Name = ResourceManager.GetName(resource.type);
            resourceRow.Amount = resource.amount;
            resourceRow.MaxAmount = resource.maxAmount;

            _rows.Add(resource.type, resourceRow);
        }

        _craft.OnResourceChanged += (resourceContainer, type, diff) =>
        {
            _rows[type].Amount += diff;
        };
    }
}
