using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// similar to <c>Vector2</c>, but uses a double internally.
/// this is used for orbital calculations, which require higher precision
/// </summary>
[Serializable]
public class Vector2d
{
    public double x, y;

    public Vector2d()
    {
        x = 0; y = 0;
    }

    public Vector2d(double x, double y)
    {
        this.x = x; this.y = y;
    }

    /* operations on vectors */

    public static Vector2d operator +(Vector2d v, Vector2d w) => new Vector2d(v.x + w.x, v.y + w.y);
    public static Vector2d operator -(Vector2d v, Vector2d w) => new Vector2d(v.x - w.x, v.y - w.y);
    public static Vector2d operator -(Vector2d v) => new Vector2d(-v.x, -v.y);
    public static Vector2d operator *(Vector2d v, double k) => new Vector2d(v.x * k, v.y * k);
    public static Vector2d operator *(double k, Vector2d v) => new Vector2d(v.x * k, v.y * k);
    public static Vector2d operator /(Vector2d v, double k) => new Vector2d(v.x / k, v.y / k);

    /// <summary>
    /// cross product between two vectors
    /// </summary>
    /// <returns>"z-component" of the cross product</returns>
    public static double Cross(Vector2d v, Vector2d w) => v.x * w.y - v.y * w.x;
    /// <summary>
    /// cross product between a vector and a z-component
    /// </summary>
    public static Vector2d Cross(Vector2d v, double w) => new Vector2d(v.y * w, -v.x * w);
    /// <summary>
    /// cross product between a z-component and a vector
    /// </summary>
    public static Vector2d Cross(double w, Vector2d v) => new Vector2d(-v.y * w, v.x * w);

    public double magnitude { get => Math.Sqrt(x * x + y * y); }
    public Vector2d normalized { get => this / magnitude; }

    /// <summary>
    /// rotates a vector counterclockwise
    /// </summary>
    /// <param name="angle">angle, in radians, to rotate</param>
    public Vector2d Rotate(double angle)
    {
        double s = Math.Sin(angle), c = Math.Cos(angle);
        return new Vector2d(
            x * c - y * s,
            x * s + y * c
        );
    }


    public static Vector2d zero => new Vector2d(0.0, 0.0);
    public static Vector2d up => new Vector2d(0.0, 1.0);
    public static Vector2d down => new Vector2d(0.0, -1.0);
    public static Vector2d left => new Vector2d(-1.0, 0.0);
    public static Vector2d right => new Vector2d(1.0, 0.0);

    public override string ToString() => "(" + x + ", " + y + ")";
    public static implicit operator Vector2(Vector2d v) { return new Vector2((float)v.x, (float)v.y); }
    public static implicit operator Vector3(Vector2d v) { return new Vector3((float)v.x, (float)v.y, 0.0f); }
}
