using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class ThrottleDisplay : TextDisplay
    {
        protected override string GetText()
        {
            var craft = ActiveCraftController.Instance.craft;
            var thrustVal = (int)Mathf.Round(craft.Control.Throttle * 10);
            var bar = new string('=', thrustVal).PadRight(10);
            return $"THROTTLE:[{bar}]";
        }
    }
}
