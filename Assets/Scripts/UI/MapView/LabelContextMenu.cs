using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    [RequireComponent(typeof(VerticalLayoutGroup))]
    public class LabelContextMenu : MonoBehaviour
    {
        [SerializeField]
        private GameObject _menuItemPrefab;
    }
}