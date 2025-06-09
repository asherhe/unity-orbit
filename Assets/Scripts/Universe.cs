using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// contains information about the in-game universe
/// </summary>
public class Universe : MonoBehaviour
{
    public static Universe Instance { get; private set; }

    /// <summary>
    /// universal gravitational constant, in <c>m^3/(kg s^2)</c>
    /// </summary>
    public double G = 6.67430e-11;

    /// <summary>
    /// universal time
    /// </summary>
    public double UT = 0.0;

    /// <summary>
    /// time warp speed multiplier
    /// </summary>
    public double timewarpScale = 1.0;

    /// <summary>
    /// fixed delta time adjusted for time warp
    /// </summary>
    public double fixedDeltaTime { get => Time.fixedDeltaTime * timewarpScale; }

    private InputActions _inputActions;

    private void Awake()
    {
        // TODO: i'm not too sure how this will work with scene switching
        if (Instance == null || Instance == this) Instance = this;
        else Destroy(this);

        _inputActions = new InputActions();
        _inputActions.Warp.Enable();

        _inputActions.Warp.WarpIncrease.performed += WarpIncrease;
        _inputActions.Warp.WarpDecrease.performed += WarpDecrease;
        _inputActions.Warp.WarpCancel.performed += WarpCancel;
    }

    private void WarpIncrease(InputAction.CallbackContext context) { if (ActiveCraftController.Instance.craft.Throttle == 0.0) timewarpScale *= 10.0; }
    private void WarpDecrease(InputAction.CallbackContext context) { timewarpScale = Math.Max(1.0, timewarpScale * 0.1); }
    private void WarpCancel(InputAction.CallbackContext context) { timewarpScale = 1.0; }


    private void FixedUpdate()
    {
        UT += fixedDeltaTime;
    }
}
