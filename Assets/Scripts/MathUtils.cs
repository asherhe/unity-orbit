using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtils {
    /// <summary>
    /// the modulus operator instead of the remainder operator.
    /// by default % calculates the remainder, which is not often desirable for negative numbers.
    /// unlike the remainder, the modulus will always match the sign of <c>a</c>, which is useful for things like array indexing
    /// </summary>
    public static int Mod(int a, int b) {
        return a - b * (int)Mathf.Floor((float)a / b);
    }
}