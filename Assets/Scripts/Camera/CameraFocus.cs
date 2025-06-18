using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFocus : SingletonBehaviour<CameraFocus>
{
    // TODO: only returns the active craft's location, change this once we have multiple crafts/celestial bodies

    /// <summary>
    /// location of camera focus, in the active celestial body space
    /// </summary>
    public Vector2d FocusPos { get => ActiveCraftController.Instance.craft.pos; }
}
