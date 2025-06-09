using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class AltimeterDisplay : MonoBehaviour
{
    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void Update()
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
        _text.text = String.Format("{0:F0}{1}", altitude, unit);
    }
}
