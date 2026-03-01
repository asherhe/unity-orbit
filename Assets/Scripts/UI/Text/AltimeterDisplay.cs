using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class AltimeterDisplay : TextDisplay
    {
        protected override string GetText()
        {
            double altitude = ActiveCraftController.Instance.craft.Altitude;
            return $"ALT:{FormatDistance(altitude)}";
        }
    }
}
