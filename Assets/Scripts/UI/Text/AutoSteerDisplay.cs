using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace UI
{
    public class AutoSteerDisplay : TextDisplay
    {
        protected override string GetText()
        {
            var command = ActiveCraftController.Instance.command;
            var isAutoSteer = command != null && command.IsAutoSteerEnabled;
            var color = isAutoSteer ? "green" : "red";
            return $"<color={color}>AUTO\n{(isAutoSteer ? "ON" : "OFF")}</color>";
        }
    }
}
