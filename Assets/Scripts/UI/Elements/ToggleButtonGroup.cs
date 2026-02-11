using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    /// <summary>
    /// a group of toggle buttons, but only one can be active at any time.
    /// all toggle buttons should be a direct child of the GameObject this is attached to.
    /// </summary>
    public class ToggleButtonGroup : MonoBehaviour
    {
        /// <summary>
        /// all toggle buttons
        /// </summary>
        public List<SpriteToggleButton> buttons = new();
        /// <summary>
        /// currently active button
        /// </summary>
        public SpriteToggleButton activeButton { get; private set; } = null;

        /// <summary>
        /// invoked when the button that is currently active changes
        /// </summary>
        public event Action OnActiveSwitched;

        private void Awake()
        {
            foreach (Transform child in transform)
            {
                SpriteToggleButton button;
                if (child.TryGetComponent<SpriteToggleButton>(out button))
                {
                    buttons.Add(button);
                    button.OnToggled += () => ToggleButton(button);
                }
            }
        }

        /// <summary>
        /// toggle a single button in this group, enforcing the one-active-button rule
        /// </summary>
        private void ToggleButton(SpriteToggleButton button)
        {
            if (button.IsActive)
            {
                if (activeButton == button) return;
                if (activeButton != null) activeButton.IsActive = false;
                activeButton = button;
                OnActiveSwitched?.Invoke();
            }
            else
            {
                if (activeButton == button)
                {
                    activeButton = null;
                    OnActiveSwitched?.Invoke();
                }
            }
        }
    }
}