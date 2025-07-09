using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpeedometerDisplay : TextDisplay
{
    protected override string GetText()
    {
        double speed = ActiveCraftController.Instance.craft.Vel.Magnitude;
        return String.Format("VEL:{0:F1}<sprite name=\"mps\">", speed);
    }
}
