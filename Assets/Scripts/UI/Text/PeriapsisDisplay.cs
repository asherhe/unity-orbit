using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class PeriapsisDisplay : TextDisplay
    {
        protected override string GetText()
        {
            var craft = ActiveCraftController.Instance.craft;
            double periapsis = craft.orbit.periapsis - craft.body.radius;
            return $"Pe:{AddMetricPrefix(periapsis)}m";
        }
    }
}
