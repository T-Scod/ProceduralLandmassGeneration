using UnityEngine;

/// <summary>
/// Creates a height map.
/// </summary>
public static class HeightMapGenerator
{
    /// <summary>
    /// Generates a height map based on the specified settings from a noise map.
    /// </summary>
    /// <param name="width">The width of the height map.</param>
    /// <param name="height">The length of the height map.</param>
    /// <param name="settings">Height settings to specify a height map.</param>
    /// <param name="sampleCenter">The center of the height map.</param>
    /// <returns>The generated height map.</returns>
    public static HeightMap GenerateHeightMap(int width, int height, HeightMapSettings settings, Vector2 sampleCenter)
    {
        // generates a noise map
        float[,] values = Noise.GenerateNoiseMap(width, height, settings.noiseSettings, sampleCenter);

        // gets an animation curve from the settings
        AnimationCurve threadSafeHeightCurve = new AnimationCurve(settings.heightCurve.keys);

        // used to pass the min and max height values into the height map
        float minValue = float.MaxValue;
        float maxValue = float.MinValue;

        // scales each coordinate in the map based on the settings
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                // scales the value based on the height curve and height multiplier
                values[i, j] *= threadSafeHeightCurve.Evaluate(values[i, j]) * settings.heightMultiplier;

                // stores the min and max values
                if (values[i, j] > maxValue)
                {
                    maxValue = values[i, j];
                }
                if (values[i, j] < minValue)
                {
                    minValue = values[i, j];
                }
            }
        }

        // creates and returns a height map from the values
        return new HeightMap(values, minValue, maxValue);
    }
}

/// <summary>
/// Stores the height map values as well as the min and max heights.
/// </summary>
public struct HeightMap
{
    /// <summary>
    /// Height values of the height map.
    /// </summary>
    public readonly float[,] values;
    /// <summary>
    /// Minimum height value.
    /// </summary>
    public readonly float minValue;
    /// <summary>
    /// Maximum height value.
    /// </summary>
    public readonly float maxValue;

    /// <summary>
    /// Initialises the variables.
    /// </summary>
    /// <param name="values">Height values.</param>
    /// <param name="minValue">Minimum height.</param>
    /// <param name="maxValue">Maximum height.</param>
    public HeightMap(float[,] values, float minValue, float maxValue)
    {
        this.values = values;
        this.minValue = minValue;
        this.maxValue = maxValue;
    }
}