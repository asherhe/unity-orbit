using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CameraFocus : SingletonBehaviour<CameraFocus>
{
    /// <summary>
    /// the object that the camera is focused on
    /// TODO: dynamically change focused object
    /// </summary>
    //public IOrbitingObject Focus { get => ActiveCraftController.Instance.craft; }
    public IOrbitingObject Focus { get => CelestialBodyManager.Instance.celestialBodies["Sun"]; }

    /// <summary>
    /// get the current location of an orbit, relative to the location of the focused object.
    /// </summary>
    /// <returns></returns>
    public Vector2d GetRelativePosition(IOrbitingObject obj)
    {
        if (Focus == obj) return Vector2d.zero;

        // get lowest common ancestor of the two orbits
        IOrbitingObject cur;
        LinkedList<IOrbitingObject> focusPath = new(), objPath = new();
        focusPath.AddFirst(cur = Focus);
        while (cur.orbit != null)
        {
            cur = cur.orbit.body;
            focusPath.AddFirst(cur);
        }
        objPath.AddFirst(cur = obj);
        while (cur.orbit != null)
        {
            cur = cur.orbit.body;
            objPath.AddFirst(cur);
        }
        // look for the first difference
        IOrbitingObject common = focusPath.First.Value;
        LinkedListNode<IOrbitingObject> focusNode = focusPath.First, objNode = objPath.First;
        while (focusNode.Value == objNode.Value)
        {
            common = focusNode.Value;
            if ((focusNode = focusNode.Next) == null || (objNode = objNode.Next) == null) break;
        }

        // position of the two objects in common space
        Vector2d focusPos = Vector2d.zero, objPos = Vector2d.zero;
        focusNode = focusPath.Last;
        while (focusNode.Value != common)
        {
            focusPos += focusNode.Value.orbit.GetPosition();
            focusNode = focusNode.Previous;
        }
        objNode = objPath.Last;
        while (objNode.Value != common)
        {
            objPos += objNode.Value.orbit.GetPosition();
            objNode = objNode.Previous;
        }

        return objPos - focusPos;
    }
}
