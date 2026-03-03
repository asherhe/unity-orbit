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

    public Vector2d() { x = 0; y = 0; }
    public Vector2d(Vector2d v) { x = v.x; y = v.y; }
    public Vector2d(double x, double y) { this.x = x; this.y = y; }

    /* operations on vectors */

    public static bool operator ==(Vector2d v, Vector2d w)
    {
        if (v is null || w is null) return v is null && w is null;
        return v.x == w.x && v.y == w.y;
    }
    public static bool operator !=(Vector2d v, Vector2d w) => !(v == w);
    public override bool Equals(object obj) => Equals(obj as Vector2d);
    public override int GetHashCode() => HashCode.Combine(x, y);

    public static Vector2d operator +(Vector2d v, Vector2d w) => new(v.x + w.x, v.y + w.y);
    public static Vector2d operator -(Vector2d v, Vector2d w) => new(v.x - w.x, v.y - w.y);
    public static Vector2d operator -(Vector2d v) => new(-v.x, -v.y);
    public static Vector2d operator *(Vector2d v, double k) => new(v.x * k, v.y * k);
    public static Vector2d operator *(double k, Vector2d v) => new(v.x * k, v.y * k);
    public static Vector2d operator /(Vector2d v, double k) => new(v.x / k, v.y / k);

    /// <summary>
    /// dot product between two vectors
    /// </summary>
    public static double Dot(Vector2d v, Vector2d w) => v.x * w.x + v.y * w.y;

    /// <summary>
    /// cross product between two vectors
    /// </summary>
    /// <returns>"z-component" of the cross product</returns>
    public static double Cross(Vector2d v, Vector2d w) => v.x * w.y - v.y * w.x;
    /// <summary>
    /// cross product between a vector and a z-component
    /// </summary>
    public static Vector2d Cross(Vector2d v, double w) => new(v.y * w, -v.x * w);
    /// <summary>
    /// cross product between a z-component and a vector
    /// </summary>
    public static Vector2d Cross(double w, Vector2d v) => new(-v.y * w, v.x * w);

    /// <summary>
    /// square of magnitude
    /// </summary>
    public double Magnitude2 { get => x * x + y * y; }
    public double Magnitude { get => Math.Sqrt(Magnitude2); }
    public Vector2d Normalized { get => this / Magnitude; }

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

    /// <summary>
    /// angle of rotation from v to w
    /// </summary>
    /// <returns>radian angle from v to w, guarenteed to be in (-PI, PI]</returns>
    public static double Angle(Vector2d v, Vector2d w)
    {
        double angv = Math.Atan2(v.y, v.x), angw = Math.Atan2(w.y, w.x);
        var dang = MathUtils.Mod(angw - angv, 2 * Math.PI);
        if (dang > Math.PI) dang -= 2 * Math.PI;
        return dang;
    }


    public static Vector2d zero => new(0.0, 0.0);
    public static Vector2d up => new(0.0, 1.0);
    public static Vector2d down => new(0.0, -1.0);
    public static Vector2d left => new(-1.0, 0.0);
    public static Vector2d right => new(1.0, 0.0);

    public override string ToString() => $"({x}, {y})";
    public static implicit operator Vector2(Vector2d v) { return new Vector2((float)v.x, (float)v.y); }
    public static implicit operator Vector3(Vector2d v) { return new Vector3((float)v.x, (float)v.y, 0.0f); }
    public static implicit operator Vector4(Vector2d v) { return new Vector4((float)v.x, (float)v.y, 0.0f, 0.0f); }
    public static explicit operator Vector2d(Vector2 v) { return new Vector2d(v.x, v.y); }
    public static explicit operator Vector2d(Vector3 v) { return new Vector2d(v.x, v.y); }
    public static explicit operator Vector2d(Vector4 v) { return new Vector2d(v.x, v.y); }
}
