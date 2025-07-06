using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Text))]
public class TextDisplay : MonoBehaviour
{
    private TMP_Text _text;

    private void Awake()
    {
        _text = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        _text.text = GetText();
    }

    /// <summary>
    /// determines the text to display. override this to set custom text
    /// </summary>
    /// <returns></returns>
    protected virtual string GetText()
    {
        return "display string";
    }
}
