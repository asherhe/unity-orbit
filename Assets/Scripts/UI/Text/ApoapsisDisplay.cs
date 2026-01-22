using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class Apoapsis : TextDisplay
    {
        protected override string GetText()
        {
            var craft = ActiveCraftController.Instance.craft;
            double apoapsis = craft.orbit.apoapsis - craft.body.radius;
            return $"Ap:{AddMetricPrefix(apoapsis)}m";
        }
    }
}
