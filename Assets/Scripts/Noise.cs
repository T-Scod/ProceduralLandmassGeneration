using System.Collections;
using UnityEngine;

/// <summary>
/// A randomly generated float value for each coordinate of a map.
/// </summary>
public static class Noise
{
    /// <summary>
    /// Determines if the noise height will be local or global
    /// </summary>
    public enum NormaliseMode
    {
        Local,
        Global
    };

    /// <summary>
    /// Generates a noise map using Unity's Perlin noise method.
    /// </summary>
    /// <param name="mapWidth">The width of the noise map.</param>
    /// <param name="mapHeight">The height of the noise map.</param>
    /// <param name="settings">These noise settings are used to customise attributes of the noise generated.</param>
    /// <param name="sampleCenter">The center of the noise map.</param>
    /// <returns></returns>
    public static float[,] GenerateNoiseMap(int mapWidth, int mapHeight, NoiseSettings settings, Vector2 sampleCenter)
    {
        // 2D float array of the specified map dimensions to store the noise values at specific coordinates
        float[,] noiseMap = new float[mapWidth, mapHeight];

        // pseudo random number generator with the specified seed from the noise settings
        System.Random prng = new System.Random(settings.seed);
        // used to offset each layer of the noise
        Vector2[] octaveOffset = new Vector2[settings.octaves];

        // the maximum possible height of the noise
        float maxPossibleHeight = 0;
        // the height of the noise
        float amplitude = 1;
        // the width of the noise
        float frequency = 1;

        // gets a random coordinate offset for each octave of the noise and gets the sum of the amplitudes
        for (int i = 0; i < settings.octaves; i++)
        {
            // gets the random offset from the center of the map
            float offsetX = prng.Next(-100000, 100000) + settings.offset.x + sampleCenter.x;
            float offsetY = prng.Next(-100000, 100000) - settings.offset.y - sampleCenter.y;
            octaveOffset[i] = new Vector2(offsetX, offsetY);

            // increments the maximum height of the noise by the amplitude of the octave
            maxPossibleHeight += amplitude;
            // scales the amplitude down for the next octave
            amplitude *= settings.persistence;
        }

        // used to localise the height of the noise
        float maxLocalNoiseHeight = float.MinValue;
        float minLocalNoiseHeight = float.MaxValue;

        // the extents of the noise map
        float halfWidth = mapWidth / 2.0f;
        float halfHeight = mapHeight / 2.0f;

        // calculates the noise value for each coordinate in the map
        for (int y = 0; y < mapHeight; y++)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                // resets the accumulation of amplitude and frequency for each coordinate
                amplitude = 1;
                frequency = 1;
                // stores the noise height at this coordinate
                float noiseHeight = 0;

                // accumulates a noise height for each octave
                for (int i = 0; i < settings.octaves; i++)
                {
                    // calaculates the coordinate that the Perlin noise will be sampled from at this octave
                    float sampleX = (x - halfWidth + octaveOffset[i].x) / settings.scale * frequency;
                    float sampleY = (y - halfHeight + octaveOffset[i].y) / settings.scale * frequency;

                    // gets a Perlin value from the sample
                    float perlinValue = Mathf.PerlinNoise(sampleX, sampleY) * 2 - 1;
                    // increments the noise height by the value scaled by the amplitude
                    noiseHeight += perlinValue * amplitude;

                    // scales down the amplitude per octave
                    amplitude *= settings.persistence;
                    // scales up the frequency per octave
                    frequency *= settings.lacunarity;
                }

                // records the min and max noise height
                if (noiseHeight > maxLocalNoiseHeight)
                {
                    maxLocalNoiseHeight = noiseHeight;
                }
                if (noiseHeight < minLocalNoiseHeight)
                {
                    minLocalNoiseHeight = noiseHeight;
                }
                // adds the noise height to the map
                noiseMap[x, y] = noiseHeight;

                // gets an estimated global height
                if (settings.normaliseMode == NormaliseMode.Global)
                {
                    float normalisedHeight = (noiseMap[x, y] + 1) / (maxPossibleHeight / 0.9f);
                    noiseMap[x, y] = Mathf.Clamp(normalisedHeight, 0, int.MaxValue);
                }
            }
        }

        // localises each noise height if the normalise mode is local
        if (settings.normaliseMode == NormaliseMode.Local)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    // provides the noise height as a percentage between min and max local noise height
                    noiseMap[x, y] = Mathf.InverseLerp(minLocalNoiseHeight, maxLocalNoiseHeight, noiseMap[x, y]);
                }
            }
        }

        return noiseMap;
    }
}

/// <summary>
/// Different attributes that can alter the noise height.
/// </summary>
[System.Serializable]
public class NoiseSettings
{
    /// <summary>
    /// Determines if the noise height will be local or global.
    /// </summary>
    public Noise.NormaliseMode normaliseMode;
    /// <summary>
    /// The scale of the noise map.
    /// </summary>
    public float scale = 50;

    /// <summary>
    /// The amount of layers of noise that will be accumulated.
    /// </summary>
    [Range(1, 20)]
    public int octaves = 6;
    /// <summary>
    /// The amount of amplitude that remains after each octave.
    /// </summary>
    [Range(0, 1)]
    public float persistence = 0.6f;
    /// <summary>
    /// The decrease in noise width after each octave.
    /// </summary>
    [Range(1, 15)]
    public float lacunarity = 2.0f;

    /// <summary>
    /// Used to get a specified random generation.
    /// </summary>
    public int seed;
    /// <summary>
    /// Coordinate offset of the noise.
    /// </summary>
    public Vector2 offset;

    /// <summary>
    /// Ensures that whenever a value is changed in the inspector that it is between certain values
    /// </summary>
    public void ValidateValues()
    {
        scale = Mathf.Max(scale, 0.01f);
        octaves = Mathf.Max(octaves, 1);
        persistence = Mathf.Clamp01(persistence);
        lacunarity = Mathf.Max(lacunarity, 1);
    }
}