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

            var leftVal = Math.Max(steeringVal, 0);
            var rightVal = Math.Max(-steeringVal, 0);
            var left = $"<color=grey>{new string('-', 6 - leftVal)}</color><color=white>{new string('=', leftVal)}</color>";
            var right = $"<color=white>{new string('=', rightVal)}</color><color=grey>{new string('-', 6 - rightVal)}</color>";
            return $"STEER:[{left}<color=white>+</color>{right}]";
        }
    }
}
