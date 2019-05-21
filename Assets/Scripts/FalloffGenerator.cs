using UnityEngine;

/// <summary>
/// Creates a falloff map which is highly concentrated in the center and decreases towards the edges.
/// </summary>
public static class FalloffGenerator
{
    /// <summary>
    /// Generates a square falloff map of the size.
    /// </summary>
    /// <param name="size">The side length of the map.</param>
    /// <returns></returns>
    public static float[,] GenerateFalloffMap(int size)
    {
        // falloff map of the given size
        float[,] map = new float[size, size];

        for (int i = 0; i < size; i++)
        {
            for (int j = 0; j < size; j++)
            {
                // gets the sample coordinate
                float x = i / (float)size * 2 - 1;
                float y = j / (float)size * 2 - 1;

                // ensures that the value is positive
                float value = Mathf.Max(Mathf.Abs(x), Mathf.Abs(y));
                // generates a map height based on the value along a curve
                map[i, j] = Evaluate(value);
            }
        }

        return map;
    }

    /// <summary>
    /// Calculates a height value from the given value.
    /// </summary>
    /// <param name="value">The value being evaluated.</param>
    /// <returns></returns>
    private static float Evaluate(float value)
    {
        // these are custom values for producing a larger concentration in the center when passed into the curve equation
        float a = 3;
        float b = 2.2f;
        // x^a / x^a + (b - b * x)^a
        return Mathf.Pow(value, a) / (Mathf.Pow(value, a) + Mathf.Pow(b - b * value, a));
    }
}