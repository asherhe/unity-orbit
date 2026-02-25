using System;
using System.Collections;
using System.Collections.Generic;
using UI;
using UnityEngine;

public class TimewarpControls : MonoBehaviour
{
    private InputActions _inputActions;

    [SerializeField]
    private double[] timeWarpFactors = { 1, 2, 5, 10, 20, 50, 100, 300, 1e3, 3e3, 1e4, 3e4, 1e5, 3e5, 1e6, 3e6, 1e7, 3e7, 1e8 };

    /// <summary>
    /// above this timescale, no craft control is allowed
    /// </summary>
    public double maxControlWarp = 20.0;

    private int _warpIndex = 0;
    public int WarpIndex
    {
        get => _warpIndex;
        set
        {
            _warpIndex = Math.Clamp(value, 0, timeWarpFactors.Length - 1);
            OnWarpChanged?.Invoke();
        }
    }

    /// <summary>
    /// time warp speed multiplier
    /// </summary>
    public double TimewarpScale { get => timeWarpFactors[WarpIndex]; }

    /// <summary>
    /// invoked when the warp level changes
    /// </summary>
    public event Action OnWarpChanged;

    private void Awake()
    {
        _inputActions = new InputActions();
        _inputActions.Warp.Enable();

        _inputActions.Warp.WarpIncrease.performed += ctx => WarpIncrease();
        _inputActions.Warp.WarpDecrease.performed += ctx => WarpDecrease();
        _inputActions.Warp.WarpCancel.performed += ctx => WarpCancel();

        OnWarpChanged += () => AnnouncementDisplay.Instance.Announce($"Time Warp: x{Universe.Instance.Timewarp.TimewarpScale}");
    }

    public void WarpIncrease()
    {
        WarpIndex++;
        if (ActiveCraftController.Instance.command.HasControlInput && TimewarpScale > maxControlWarp)
        {
            WarpIndex--;
            AnnouncementDisplay.Instance.Announce("Cannot timewarp while throttle is open!");
        }
    }
    public void WarpDecrease()
    {
        WarpIndex--;
    }
    public void WarpCancel()
    {
        WarpIndex = 0;
    }
}
