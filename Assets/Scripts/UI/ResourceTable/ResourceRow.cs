using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ResourceRow : MonoBehaviour
{
    [SerializeField]
    private TMP_Text _nameText;
    [SerializeField]
    private TMP_Text _amountText;

    private string _name;
    public string Name
    {
        get => _name;
        set
        {
            _name = value;
            _nameText.text = _name;
        }
    }

    private double _amount;
    public double Amount
    {
        get => _amount;
        set
        {
            _amount = value;
            GenerateAmountText();
        }
    }

    private double _maxAmount;
    public double MaxAmount
    {
        get => _maxAmount;
        set
        {
            _maxAmount = value;
            GenerateAmountText();
        }
    }

    private void GenerateAmountText()
    {
        int decimalDigits = 0;
        if (MaxAmount < 100.0)
            decimalDigits = 2 - (int)Math.Floor(Math.Log10(MaxAmount)); // 3 digits of precision

        _amountText.text = $"{Amount.ToString("F" + decimalDigits)}/{MaxAmount.ToString("F" + decimalDigits)}";
    }
}
