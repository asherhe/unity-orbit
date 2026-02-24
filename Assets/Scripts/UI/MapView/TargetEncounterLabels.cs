using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace UI
{
    public class TargetEncounterLabels : MonoBehaviour
    {
        /// <summary>
        /// object pool for encounter label objects
        /// </summary>
        private ObjectPool<EncounterLabelGroup> _labelPool;
        /// <summary>
        /// currently active labels in the pool
        /// </summary>
        private LinkedList<EncounterLabelGroup> _activeLabels = new();

        private void Awake()
        {
            _labelPool = new(
                CreatePooledItem,
                OnTakeFromPool,
                OnReturnedToPool,
                OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: 4,
                maxSize: 40
            );

            TargetingSystem.WhenInstantiated(() => { TargetingSystem.Instance.OnEncounterUpdate += UpdateLabels; });
        }

        private void UpdateLabels()
        {
            foreach (var label in _activeLabels) _labelPool.Release(label);
            _activeLabels.Clear();
            foreach (var enc in TargetingSystem.Instance.Encounters)
            {
                var label = _labelPool.Get();
                _activeLabels.AddLast(label);
                label.Encounter = enc;
            }
        }

        private EncounterLabelGroup CreatePooledItem()
        {
            var label = MapLabelManager.Instance.AddEncounterLabelGroup();
            label.gameObject.SetActive(false);
            return label;
        }
        private void OnTakeFromPool(EncounterLabelGroup label) { label.gameObject.SetActive(true); }
        private void OnReturnedToPool(EncounterLabelGroup label) { label.gameObject.SetActive(false); }
        private void OnDestroyPoolObject(EncounterLabelGroup label) { Destroy(label.gameObject); }
    }
}