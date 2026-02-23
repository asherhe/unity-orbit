using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class EncounterLabel : PatchPOILabel
    {
        protected override Vector2d GetPosition()
        {
            return new Vector2d(double.NaN, double.NaN);
        }
        protected override string GetLabelText()
        {
            throw new System.NotImplementedException("EncounterLabel is not implemented");
        }
    }
}