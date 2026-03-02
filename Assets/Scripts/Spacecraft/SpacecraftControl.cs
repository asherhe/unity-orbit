using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Spacecraft))]
public class SpacecraftControl : MonoBehaviour
{
    [SerializeField]
    private float _throttle = 0.0f;
    /// <summary>
    /// spacecraft throttle, as a fraction of max throttle (between 0.0 and 1.0)
    /// </summary>
    public float Throttle
    {
        get => _throttle;
        set { _throttle = Mathf.Clamp01(value); }
    }

    [SerializeField]
    private float _steeringControl = 0.0f;
    /// <summary>
    /// input for spacecraft steering (between -1.0 and 1.0), positive is counterclockwise steering
    /// </summary>
    public float SteeringControl
    {
        get => _steeringControl;
        set { _steeringControl = Mathf.Clamp(value, -1.0f, 1.0f); }
    }
}
