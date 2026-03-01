using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    /// <summary>
    /// numerical field in maneuver planner
    /// </summary>
    public class ManeuverField : MonoBehaviour
    {
        [SerializeField]
        private TMP_Text _displayText;
        [SerializeField]
        private Button _buttonIncrease, _buttonDecrease;

        /// <summary>
        /// current value of field
        /// </summary>
        public float value = 0f;
        /// <summary>
        /// amount by which the buttons change the value
        /// </summary>
        public float increment = 1f;

        /// <summary>
        /// value -> text formatter for generating text display
        /// </summary>
        public Func<float, string> formatter = val => val.ToString();

        public event Action OnValueChanged;

        private void OnEnable()
        {
            _buttonIncrease.onClick.AddListener(ValueIncreased);
            _buttonDecrease.onClick.AddListener(ValueDecreased);
        }

        private void OnDisable()
        {
            _buttonIncrease.onClick.RemoveListener(ValueIncreased);
            _buttonDecrease.onClick.RemoveListener(ValueDecreased);
        }

        private void ValueIncreased()
        {
            value += increment;
            OnValueChanged?.Invoke();
        }

        private void ValueDecreased()
        {
            value -= increment;
            OnValueChanged?.Invoke();
        }

        private void Update()
        {
            _displayText.text = formatter(value);
        }
    }
}