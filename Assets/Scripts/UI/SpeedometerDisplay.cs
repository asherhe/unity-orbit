using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class SpeedometerDisplay : MonoBehaviour
{
    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        double speed = ActiveCraftController.Instance.craft.Vel.magnitude;
        _text.text = String.Format("{0:F1}<sprite name=\"mps\">", speed);
    }
}
