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

        [SerializeField]
        private double _value = 0;
        /// <summary>
        /// current value of field
        /// </summary>
        public double Value
        {
            get => _value;
            set
            {
                if (value == _value) return;
                _value = value;
                OnValueChanged?.Invoke();
            }
        }
        /// <summary>
        /// amount by which the buttons change the value
        /// </summary>
        public double increment = 1.0;

        /// <summary>
        /// value -> text formatter for generating text display
        /// </summary>
        public Func<double, string> formatter = val => val.ToString();

        public event Action OnValueChanged;

        protected virtual void Awake() { }

        protected virtual void OnEnable()
        {
            _buttonIncrease.onClick.AddListener(IncreaseValue);
            _buttonDecrease.onClick.AddListener(DecreaseValue);
        }

        protected virtual void OnDisable()
        {
            _buttonIncrease.onClick.RemoveListener(IncreaseValue);
            _buttonDecrease.onClick.RemoveListener(DecreaseValue);
        }

        protected void IncrementBy(double inc)
        {
            Value += inc;
        }

        private void IncreaseValue() => IncrementBy(increment);
        private void DecreaseValue() => IncrementBy(-increment);

        /// <summary>
        /// set the Value property without triggering an OnValueChanged event
        /// </summary>
        public void SetValueSilent(double value) { _value = value; }

        private void Update()
        {
            _displayText.text = formatter(Value);
        }
    }
}