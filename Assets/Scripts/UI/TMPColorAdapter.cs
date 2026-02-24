using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace UI.Colorable
{
    [RequireComponent(typeof(TMP_Text))]
    public class TMPColorAdapter : ColorableUIObject
    {
        public TMP_Text text { get; private set; }

        public override Color Color
        {
            get => text.color;
            set => text.color = value;
        }

        private void Awake()
        {
            text = GetComponent<TMP_Text>();
        }
    }
}