using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class SteeringDisplay : TextDisplay
    {
        protected override string GetText()
        {
            var craft = ActiveCraftController.Instance.craft;
            var steeringVal = (int)Mathf.Round(craft.Control.SteeringControl * 6);
            var left = new string('=', Math.Max(-steeringVal, 0)).PadLeft(6, '-');
            var right = new string('=', Math.Max(steeringVal, 0)).PadRight(6, '-');
            return $"STEER:[{left}+{right}]";
        }
    }
}
