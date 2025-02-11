using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class AltimeterSpeedometerDisplay : MonoBehaviour
{
    public TMP_Text speedometerText;
    public TMP_Text altimeterText;

    private void Update()
    {
        speedometerText.text = ActiveCraftController.Instance.craft.vel.magnitude.ToString("F2") + " m/s";
        altimeterText.text = ActiveCraftController.Instance.craft.altitude.ToString("F0") + " m";
    }
}
