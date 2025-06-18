using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// contains information about the in-game universe
/// </summary>
public class Universe : SingletonBehaviour<Universe>
{
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

    protected override void Awake()
    {
        base.Awake();

        _inputActions = new InputActions();
        _inputActions.Warp.Enable();

        _inputActions.Warp.WarpIncrease.performed += WarpIncrease;
        _inputActions.Warp.WarpDecrease.performed += WarpDecrease;
        _inputActions.Warp.WarpCancel.performed += WarpCancel;
    }

    // TODO: i dont want to rely on ActiveCraftcontroller, so either we allow timewarp wtih throttle to a certain speed (what i hope we can do),
    // or we add an interface to restrict timewarp and then use it in ActiveCraftController
    // however this works fine for now so we'll keep it
    // honestly i should also separate the time warp controller to a new component while i'm at it
    private void WarpIncrease(InputAction.CallbackContext context) { if (ActiveCraftController.Instance.control.Throttle == 0.0) timewarpScale *= 10.0; }
    private void WarpDecrease(InputAction.CallbackContext context) { timewarpScale = Math.Max(1.0, timewarpScale * 0.1); }
    private void WarpCancel(InputAction.CallbackContext context) { timewarpScale = 1.0; }


    private void FixedUpdate()
    {
        UT += fixedDeltaTime;
    }
}
