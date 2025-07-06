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

    public SpacecraftControl control { get; private set; }

    /// <summary>
    /// throttle change rate per second
    /// </summary>
    public float throttlingRate = 0.3f;

    private void Awake()
    {
        if (Instance == null || Instance == this) Instance = this;
        else Destroy(this);

        craft = GetComponent<Spacecraft>();

        _inputActions = new InputActions();
        _inputActions.Flight.Enable();

        _inputActions.Flight.ThrottleCut.performed += CutThrottle;
        _inputActions.Flight.ThrottleFull.performed += FullThrottle;
    }

    private void Start()
    {
        // added in Spacecraft.Awake(), so we get it in Start() and not Awake()
        control = GetComponent<SpacecraftControl>();
    }

    public void CutThrottle(InputAction.CallbackContext context) => control.Throttle = 0.0f;
    public void FullThrottle(InputAction.CallbackContext context) => control.Throttle = 1.0f;

    private void Update()
    {
        control.SteeringControl = _inputActions.Flight.Steering.ReadValue<float>();

        float throttleControl = _inputActions.Flight.Throttle.ReadValue<float>();
        control.Throttle += throttleControl * throttlingRate * Time.deltaTime;
    }
}
