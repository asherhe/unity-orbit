using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class SpeedometerDisplay : TextDisplay
    {
        protected override string GetText()
        {
            double speed = ActiveCraftController.Instance.craft.Velocity.Magnitude;
            return $"VEL:{FormatSpeed(speed)}";
        }
    }
}
