using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtils {
    // the modulus operator instead of the remainder operator
    // by default % calculates the remainder, which is not what we want to see
    public static int Mod(int a, int b) {
        return a - b * (int)Mathf.Floor((float)a / b);
    }
}