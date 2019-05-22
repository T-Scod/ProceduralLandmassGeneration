using UnityEngine;

/// <summary>
/// Generates a texture from a height map.
/// </summary>
public static class TextureGenerator
{
    /// <summary>
    /// Creates a texture from the colour map.
    /// </summary>
    /// <param name="colourMap">The values of the map at each coord.</param>
    /// <param name="width">Width of the texture.</param>
    /// <param name="height">Height of the texture.</param>
    /// <returns></returns>
    public static Texture2D TextureFromColourMap(Color[] colourMap, int width, int height)
    {
        // creates a texture of the given textures
        Texture2D texture = new Texture2D(width, height)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        // sets the pixels of the colour map to the values from the colour map
        texture.SetPixels(colourMap);
        // applies and returns the texture
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Creates a black and white colour map of the height map.
    /// </summary>
    /// <param name="heightMap">The height map being sampled.</param>
    /// <returns></returns>
    public static Texture2D TextureFromHeightMap(HeightMap heightMap)
    {
        // dimensions of the map
        int width = heightMap.values.GetLength(0);
        int height = heightMap.values.GetLength(1);

        // stores the colour representing the height from the map
        Color[] colourMap = new Color[width * height];
        // calculates the colour equivalent of the height values for each coordinate in the map
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // interpolates the value as a precentage between the height map's maximum and minimum
                colourMap[y * width + x] = Color.Lerp(Color.black, Color.white, Mathf.InverseLerp(heightMap.minValue, heightMap.maxValue, heightMap.values[x, y]));
            }
        }

        // creates a texture from the colour map
        return TextureFromColourMap(colourMap, width, height);
    }
}