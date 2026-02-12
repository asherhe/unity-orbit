using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtils
{
    /// <summary>
    /// the modulus operator instead of the remainder operator.
    /// by default % calculates the remainder, which is not often desirable for negative numbers.
    /// unlike the remainder, the modulus will always match the sign of <c>a</c>, which is useful for things like array indexing
    /// </summary>
    public static int Mod(int a, int b)
        => a - b * (int)Mathf.Floor((float)a / b);
    public static double Mod(double a, double b)
        => a - b * (int)Math.Floor(a / b);

    /// <summary>
    /// normalize an angle to [-PI, PI)
    /// </summary>
    public static float NormalizeAngle(float a)
        => Mathf.Repeat(a + Mathf.PI, 2 * Mathf.PI) - Mathf.PI;
}
