using Orbit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFocus : SingletonBehaviour<CameraFocus>
{
    /// <summary>
    /// the object that the camera is focused on
    /// TODO: dynamically change focused object
    /// </summary>
    public OrbitingObject Focus { get => ActiveCraftController.Instance.craft; }

    /// <summary>
    /// get the current location of an orbit, relative to the location of the focused object.
    /// </summary>
    /// <returns></returns>
    public Vector2d GetRelativePosition(OrbitingObject obj)
    {
        if (Focus == obj) return Vector2d.zero;

        // get lowest common ancestor of the two orbits
        OrbitingObject cur;
        LinkedList<OrbitingObject> focusPath = new(), objPath = new();
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
        OrbitingObject common = focusPath.First.Value;
        LinkedListNode<OrbitingObject> focusNode = focusPath.First, objNode = objPath.First;
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
            focusPos += new UniversalPropagator(focusNode.Value.orbit).GetPosition(Universe.Instance.UT);
            focusNode = focusNode.Previous;
        }
        objNode = objPath.Last;
        while (objNode.Value != common)
        {
            objPos += new UniversalPropagator(objNode.Value.orbit).GetPosition(Universe.Instance.UT);
            objNode = objNode.Previous;
        }

        return objPos - focusPos;
    }
}
