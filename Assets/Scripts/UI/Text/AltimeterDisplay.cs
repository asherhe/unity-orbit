using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AltimeterDisplay : TextDisplay
{
    protected override string GetText()
    {
        double altitude = ActiveCraftController.Instance.craft.altitude;
        double log10 = Math.Log10(altitude);
        String unit = "m";
        if (log10 >= 12.0)
        {
            altitude /= 1e9;
            unit = "Gm";
        }
        else if (log10 >= 9.0)
        {
            altitude /= 1e6;
            unit = "Mm";
        }
        else if (log10 >= 6.0)
        {
            altitude /= 1e3;
            unit = "km";
        }
        return String.Format("ALT:{0:F0}{1}", altitude, unit);
    }
}
