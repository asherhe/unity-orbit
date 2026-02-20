using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class SOITransitionLabel : PatchPOILabel
    {
        protected override Vector2d GetPosition()
        {
            if (Patch.HasTransition) return Patch.NextTransition.State.pos;
            else return new Vector2d(double.NaN, double.NaN);
        }
        protected override string GetLabelText()
        {
            var transition = Patch.NextTransition;
            if (transition == null) return "";
            if (transition == Patch.soiEscape) return $"{Patch.patchOrbit.body.bodyName} Escape";
            if (transition == Patch.soiIntercept) return $"{Patch.soiIntercept.nextCaptureBody.bodyName} Capture";
            return "Orbit transition";
        }
    }
}