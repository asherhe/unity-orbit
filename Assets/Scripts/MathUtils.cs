using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MathUtils {
    // the c++ modulo instead of the c# modulo
    // actually more like remainder
    public static int mod(int a, int b) {
        return a - b * (int)Mathf.Floor((float)a / b);
    }
}