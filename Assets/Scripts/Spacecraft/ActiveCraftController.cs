using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// provides user control to the active spacecraft
/// </summary>
[RequireComponent(typeof(Spacecraft))]
public class ActiveCraftController : MonoBehaviour
{
    public static ActiveCraftController Instance { get; private set; }

    private InputActions _inputActions;

    /// <summary>
    /// the active spacecraft
    /// </summary>
    public Spacecraft craft { get; private set; }

    /// <summary>
    /// active command plugin on spacecraft
    /// </summary>
    public Parts.CommandPlugin command { get; private set; }

    /// <summary>
    /// throttle change rate per second
    /// </summary>
    public float throttlingRate = 0.3f;

    private void Awake()
    {
        if (Instance == null || Instance == this) Instance = this;
        else Destroy(this);

        craft = GetComponent<Spacecraft>();

        craft.OnLoaded += () =>
        {
            // TODO: implement proper logic for active command plugin
            foreach (var part in craft.parts)
                if ((command = part.GetPlugin<Parts.CommandPlugin>()) != null) break;
        };

        _inputActions = new InputActions();
        _inputActions.Flight.Enable();

        _inputActions.Flight.ThrottleCut.performed += ctx => command.CutThrottle();
        _inputActions.Flight.ThrottleFull.performed += ctx => command.FullThrottle();

        _inputActions.Flight.AutoSteer.performed += ctx => command.IsAutoSteerEnabled = !command.IsAutoSteerEnabled;

    }

    private void Update()
    {
        if (command == null) return;
        command.SteeringInput = _inputActions.Flight.Steering.ReadValue<float>();
        command.ThrottleInput = _inputActions.Flight.Throttle.ReadValue<float>() * throttlingRate;
    }
}
