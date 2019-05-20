using UnityEngine;

/// <summary>
/// Displays a preview of what map will be generated.
/// </summary>
public class MapPreview : MonoBehaviour
{
    /// <summary>
    /// Used to display the noise and falloff maps.
    /// </summary>
    public Renderer textureRenderer;
    /// <summary>
    /// Stores the map mesh.
    /// </summary>
    public MeshFilter meshFilter;
    /// <summary>
    /// Used to display the map mesh.
    /// </summary>
    public MeshRenderer meshRenderer;

    /// <summary>
    /// Determines what kind of map is being drawn.
    /// </summary>
    public enum DrawMode
    {
        NoiseMap,
        Mesh,
        FalloffMap
    };
    /// <summary>
    /// The method by which the map will be drawn.
    /// </summary>
    public DrawMode drawMode;

    /// <summary>
    /// Alters the mesh of the map.
    /// </summary>
    public MeshSettings meshSettings;
    /// <summary>
    /// Changes the height and noise of the mesh.
    /// </summary>
    public HeightMapSettings heightMapSettings;
    /// <summary>
    /// Adds texture layers to the mesh.
    /// </summary>
    public TextureSettings textureSettings;
    /// <summary>
    /// The material the terrain is being stored in.
    /// </summary>
    public Material terrainMaterial;
    /// <summary>
    /// The level of detail of the preview map.
    /// </summary>
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int editorPreviewLOD;
    /// <summary>
    /// Determines if the preview should update whenever a value is changed.
    /// </summary>
    public bool autoUpdate;

    /// <summary>
    /// Draws the preview map in the scene.
    /// </summary>
    public void DrawMapInEditor()
    {
        // applies the texture settings to the material
        textureSettings.ApplyToMaterial(terrainMaterial);
        textureSettings.UpdateMeshHeights(terrainMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);
        // generates a height map from the mesh, height map and noise settings at location (0, 0)
        HeightMap heightMap = HeightMapGenerator.GenerateHeightMap(meshSettings.NumVertsPerLine, meshSettings.NumVertsPerLine, heightMapSettings, Vector2.zero);

        // draws a noise map on a texture
        if (drawMode == DrawMode.NoiseMap)
        {
            DrawTexture(TextureGenerator.TextureFromHeightMap(heightMap));
        }
        // generates a mesh from the height map
        else if (drawMode == DrawMode.Mesh)
        {
            DrawMesh(MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, editorPreviewLOD));
        }
        // draws a falloff map on a texture
        else if (drawMode == DrawMode.FalloffMap)
        {
            // passes in a height map generated from the falloff generator
            DrawTexture(TextureGenerator.TextureFromHeightMap(new HeightMap(FalloffGenerator.GenerateFalloffMap(meshSettings.NumVertsPerLine), 0, 1)));
        }
    }

    /// <summary>
    /// Applies the texture to the renderer.
    /// </summary>
    /// <param name="texture">The texture being applied.</param>
    public void DrawTexture(Texture2D texture)
    {
        textureRenderer.sharedMaterial.mainTexture = texture;
        textureRenderer.transform.localScale = new Vector3(texture.width, 1, texture.height) / 10.0f;

        // turns on the texture renderer and turns off the mesh
        textureRenderer.gameObject.SetActive(true);
        meshFilter.gameObject.SetActive(false);
    }

    /// <summary>
    /// Creates and stores a mesh from the mesh data.
    /// </summary>
    /// <param name="meshData">The data that will be displayed by the mesh.</param>
    public void DrawMesh(MeshData meshData)
    {
        // creates a mesh from the mesh data and displays
        meshFilter.sharedMesh = meshData.CreateMesh();

        textureRenderer.gameObject.SetActive(false);
        meshFilter.gameObject.SetActive(true);
    }

    /// <summary>
    /// Updates the preview map only if the applictaion is not playing.
    /// </summary>
    private void OnValuesUpdated()
    {
        // checks if the applictaion is playing before redrawing the map
        if (!Application.isPlaying)
        {
            DrawMapInEditor();
        }
    }

    /// <summary>
    /// Applies the updated values to the material.
    /// </summary>
    private void OnTextureValuesUpdated()
    {
        textureSettings.ApplyToMaterial(terrainMaterial);
    }

    /// <summary>
    /// Called whenever a value is changed in the inspector.
    /// </summary>
    private void OnValidate()
    {
        // updates mesh settings
        if (meshSettings != null)
        {
            meshSettings.OnValuesUpdated -= OnValuesUpdated;
            meshSettings.OnValuesUpdated += OnValuesUpdated;
        }
        // updates height map and noise settings
        if (heightMapSettings != null)
        {
            heightMapSettings.OnValuesUpdated -= OnValuesUpdated;
            heightMapSettings.OnValuesUpdated += OnValuesUpdated;
        }
        // updates texture settings
        if (textureSettings != null)
        {
            textureSettings.OnValuesUpdated -= OnTextureValuesUpdated;
            textureSettings.OnValuesUpdated += OnTextureValuesUpdated;
        }
    }
}