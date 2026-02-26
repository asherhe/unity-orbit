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
        private LinkedList<EncounterLabelGroup> _labelPool = new();
        /// <summary>
        /// currently active labels in the pool
        /// </summary>
        private LinkedList<EncounterLabelGroup> _activeLabels = new();

        private void Awake()
        {
            _labelPool = new();

            TargetingSystem.WhenInstantiated(() => { TargetingSystem.Instance.OnEncounterUpdate += UpdateLabels; });
        }

        private void UpdateLabels()
        {
            foreach (var label in _activeLabels) DeactivateItem(label);
            _activeLabels.Clear();

            var node = GetFirstItem();
            foreach (var enc in TargetingSystem.Instance.Encounters)
            {
                var label = node.Value;
                ActivateItem(label);
                _activeLabels.AddLast(label);
                label.Encounter = enc;

                node = GetNextItem(node);
            }
        }

        private EncounterLabelGroup CreatePooledItem()
        {
            var label = MapLabelManager.Instance.AddEncounterLabelGroup();
            label.name = $"Encounter {_labelPool.Count}";
            label.gameObject.SetActive(false);
            return label;
        }
        private LinkedListNode<EncounterLabelGroup> GetFirstItem()
        {
            if (_labelPool.Count == 0) return _labelPool.AddLast(CreatePooledItem());
            else return _labelPool.First;
        }
        private LinkedListNode<EncounterLabelGroup> GetNextItem(LinkedListNode<EncounterLabelGroup> node)
        {
            if (node.Next != null) return node.Next;
            return _labelPool.AddLast(CreatePooledItem());
        }
        private void ActivateItem(EncounterLabelGroup label) { label.gameObject.SetActive(true); }
        private void DeactivateItem(EncounterLabelGroup label) { label.gameObject.SetActive(false); }
    }
}