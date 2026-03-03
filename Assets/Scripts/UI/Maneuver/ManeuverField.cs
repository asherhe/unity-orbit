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
        private float _value = 0f;
        /// <summary>
        /// current value of field
        /// </summary>
        public float Value
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
        public float increment = 1f;

        /// <summary>
        /// value -> text formatter for generating text display
        /// </summary>
        public Func<float, string> formatter = val => val.ToString();

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

        protected void IncrementBy(float inc)
        {
            Value += inc;
        }

        private void IncreaseValue() => IncrementBy(increment);
        private void DecreaseValue() => IncrementBy(-increment);

        /// <summary>
        /// set the Value property without triggering an OnValueChanged event
        /// </summary>
        public void SetValueSilent(float value) { _value = value; }

        private void Update()
        {
            _displayText.text = formatter(Value);
        }
    }
}