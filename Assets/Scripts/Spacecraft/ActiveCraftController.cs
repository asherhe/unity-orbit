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
    /// how fast throttle increases/decreases, as a fraction of full throttle per second
    /// </summary>
    public float throttleRate = 0.3f;

    private void Awake()
    {
        if (Instance == null || Instance == this)  Instance = this;
        else Destroy(this);

        craft = GetComponent<Spacecraft>();

        _inputActions = new InputActions();
        _inputActions.Flight.Enable();

        _inputActions.Flight.ThrottleCut.performed += CutThrottle;
        _inputActions.Flight.ThrottleFull.performed += FullThrottle;
    }

    private void CutThrottle(InputAction.CallbackContext context) => craft.throttle = 0.0f;
    private void FullThrottle(InputAction.CallbackContext context) => craft.throttle = 1.0f;

    private void Update()
    {
        craft.steeringControl = _inputActions.Flight.Steering.ReadValue<float>();

        float throttleControl = _inputActions.Flight.Throttle.ReadValue<float>();
        craft.throttle += throttleControl * throttleRate * Time.deltaTime;
    }
}
