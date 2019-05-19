using System.Collections;
using UnityEngine;

/// <summary>
/// Settings for generating different variations of meshes.
/// </summary>
[CreateAssetMenu()]
public class MeshSettings : UpdatableData
{
    /// <summary>
    /// The number of supported levels of detail.
    /// </summary>
    public const int numSupportedLODs = 5;
    /// <summary>
    /// The number of supported chunk sizes.
    /// </summary>
    public const int numSupportedChunkSizes = 9;
    /// <summary>
    /// The number of supported flatshaded chunk sizes.
    /// </summary>
    public const int numSupportedFlatshadedChunkSizes = 3;
    /// <summary>
    /// The supported chunk sizes that are compatible with the supported levels of detail.
    /// </summary>
    public static readonly int[] supportedChunkSizes = { 48, 72, 96, 120, 144, 168, 192, 216, 240 };

    /// <summary>
    /// The scale of the mesh.
    /// </summary>
    public float meshScale = 2.5f;
    /// <summary>
    /// Determines if the mesh will be flatshaded
    /// </summary>
    public bool useFlatShading;

    /// <summary>
    /// The index of the chunk size in the supported chunk sizes array.
    /// </summary>
    [Range(0, numSupportedChunkSizes - 1)]
    public int chunkSizeIndex;
    /// <summary>
    /// The index of the flatshaded chunk size in the supported chunk sizes array.
    /// </summary>
    [Range(0, numSupportedFlatshadedChunkSizes - 1)]
    public int flatshadedChunkSizeIndex;

    /// <summary>
    /// Number of vertices per line of mesh rendered at LOD = 0.
    /// Includes the 4 extra verts that are excluded from final mesh, but used for calculating normals.
    /// </summary>
    public int NumVertsPerLine
    {
        get
        {
            return supportedChunkSizes[(useFlatShading) ? flatshadedChunkSizeIndex : chunkSizeIndex] + 5;
        }
    }
    /// <summary>
    /// The size of the mesh.
    /// </summary>
    public float MeshWorldSize
    {
        get
        {
            return (NumVertsPerLine - 3) * meshScale;
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Ensures that settings are clamped to certain values.
    /// </summary>
    protected override void OnValidate()
    {
        if (meshScale <= 0.0f)
        {
            meshScale = 0.01f;
        }
        base.OnValidate();
    }
#endif
}