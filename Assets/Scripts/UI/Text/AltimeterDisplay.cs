using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AltimeterDisplay : TextDisplay
{
    protected override string GetText()
    {
        double altitude = ActiveCraftController.Instance.craft.Altitude;
        return $"ALT:{AddMetricPrefix(altitude)}m";
    }
}
