using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// maintains one InputActions instance that is shared across the entire scene
/// </summary>
public class InputReader : SingletonBehaviour<InputReader>
{
    public InputActions Actions { get; private set; }

    protected override void Awake()
    {
        Actions = new InputActions();

        base.Awake();
    }
}
