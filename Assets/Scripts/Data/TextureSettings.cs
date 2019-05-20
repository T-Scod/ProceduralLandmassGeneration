using UnityEngine;
using System.Linq;

/// <summary>
/// Settings for the textures passed onto the mesh.
/// </summary>
[CreateAssetMenu()]
public class TextureSettings : UpdatableData
{
    /// <summary>
    /// Size of the texture.
    /// </summary>
    private const int m_textureSize = 512;
    /// <summary>
    /// Format of the texture.
    /// </summary>
    private const TextureFormat m_textureFormat = TextureFormat.RGB565;

    /// <summary>
    /// The different layers of textures.
    /// </summary>
    public Layer[] layers;

    /// <summary>
    /// Minimum height of the mesh
    /// </summary>
    private float m_savedMinHeight;
    /// <summary>
    /// Maximum height of the mesh
    /// </summary>
    private float m_savedMaxHeight;

    /// <summary>
    /// Applies the texture settings to the material.
    /// </summary>
    /// <param name="material">The material the settings are being set on.</param>
    public void ApplyToMaterial(Material material)
    {
        // passes in the amount of layers
        material.SetInt("layerCount", layers.Length);
        // using the Linq method, each property of the layers array is separated into its own array and passed into the material
        material.SetColorArray("baseColours", layers.Select(x => x.tint).ToArray());
        material.SetFloatArray("baseStartHeights", layers.Select(x => x.startHeight).ToArray());
        material.SetFloatArray("baseBlends", layers.Select(x => x.blendStrength).ToArray());
        material.SetFloatArray("baseColourStrengths", layers.Select(x => x.tintStrength).ToArray());
        material.SetFloatArray("baseTextureScales", layers.Select(x => x.textureScale).ToArray());
        // gets a Texture2DArray from a Texture2D[] so that the material will accept it
        Texture2DArray textureArray = GenerateTextureArray(layers.Select(x => x.texture).ToArray());
        material.SetTexture("baseTextures", textureArray);

        // passes the min and max saved heights into the material
        UpdateMeshHeights(material, m_savedMinHeight, m_savedMaxHeight);
    }

    /// <summary>
    /// Passes the min and max heights into the material.
    /// </summary>
    /// <param name="material">The material the values are being passed into.</param>
    /// <param name="minHeight">Minimum height.</param>
    /// <param name="maxHeight">Maximum height.</param>
    public void UpdateMeshHeights(Material material, float minHeight, float maxHeight)
    {
        // saves the heights in the settings
        m_savedMinHeight = minHeight;
        m_savedMaxHeight = maxHeight;
        // passes on the values
        material.SetFloat("minHeight", minHeight);
        material.SetFloat("maxHeight", maxHeight);
    }

    /// <summary>
    /// Generates a Texture2DArray from a Texture2D[].
    /// </summary>
    /// <param name="textures">The array of textures that is being turned into a texture array.</param>
    /// <returns>A texture array from the array of textures.</returns>
    private Texture2DArray GenerateTextureArray(Texture2D[] textures)
    {
        // creates a new texture array based on the texture settings
        Texture2DArray textureArray = new Texture2DArray(m_textureSize, m_textureSize, textures.Length, m_textureFormat, true);
        // the texture array gets the pixels from each texture
        for (int i = 0; i < textures.Length; i++)
        {
            textureArray.SetPixels(textures[i].GetPixels(), i);
        }
        
        textureArray.Apply();
        return textureArray;
    }

    /// <summary>
    /// The settings that each layer of texture has.
    /// </summary>
    [System.Serializable]
    public class Layer
    {
        /// <summary>
        /// The texture of the layer.
        /// </summary>
        public Texture2D texture;
        /// <summary>
        /// Tints the texture.
        /// </summary>
        public Color tint;
        /// <summary>
        /// The amount the tint affects the texture.
        /// </summary>
        [Range(0, 1)]
        public float tintStrength;
        /// <summary>
        /// The height the texture starts from.
        /// </summary>
        [Range(0, 1)]
        public float startHeight;
        /// <summary>
        /// The amount the layer blends into other layers.
        /// </summary>
        [Range(0, 1)]
        public float blendStrength;
        /// <summary>
        /// The scale of the texture.
        /// </summary>
        public float textureScale;
    }

#if UNITY_EDITOR
    /// <summary>
    /// Ensures that settings are clamped to certain values.
    /// </summary>
    protected override void OnValidate()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i].textureScale <= 0.0f)
            {
                layers[i].textureScale = 0.01f;
            }
        }
        base.OnValidate();
    }
#endif
}