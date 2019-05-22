using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Generates a chunks of terrain.
/// </summary>
public class TerrainGenerator : MonoBehaviour
{
    /// <summary>
    /// The index of the collider's level of detail.
    /// </summary>
    public int colliderLODIndex;
    /// <summary>
    /// Contains all the levels of detail for certain view distances.
    /// </summary>
    public LODInfo[] detailLevels;
    /// <summary>
    /// Settings for the mesh of the terrain.
    /// </summary>
    public MeshSettings meshSettings;
    /// <summary>
    /// Settings for the heights of the mesh.
    /// </summary>
    public HeightMapSettings heightMapSettings;
    /// <summary>
    /// Settings for the material applied to the map.
    /// </summary>
    public TextureSettings textureSettings;
    /// <summary>
    /// Reference to the location of the viewer.
    /// </summary>
    public Transform viewer;
    /// <summary>
    /// The material of the map.
    /// </summary>
    public Material mapMaterial;

    /// <summary>
    /// The distance that needs to be moved before the chunks update.
    /// </summary>
    private const float m_viewerMoveThresholdForChunkUpdate = 25.0f;
    /// <summary>
    /// The squared threshold for update.
    /// Used for simplified calculations.
    /// </summary>
    private const float m_sqrViewerMoveThresholdForChunkUpdate = m_viewerMoveThresholdForChunkUpdate * m_viewerMoveThresholdForChunkUpdate;
    /// <summary>
    /// Current position of the viewer.
    /// </summary>
    private Vector2 m_viewerPosition;
    /// <summary>
    /// The previous position of the viewer.
    /// </summary>
    private Vector2 m_viewerPositionOld;
    /// <summary>
    /// Size of the world.
    /// </summary>
    private float m_meshWorldSize;
    /// <summary>
    /// The distance required for chunks to be visible.
    /// </summary>
    private int m_chunkVisibleInViewDist;    
    /// <summary>
    /// Contains all chunks.
    /// </summary>
    private Dictionary<Vector2, TerrainChunk> m_terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    /// <summary>
    /// List of all the visible chunks.
    /// </summary>
    private List<TerrainChunk> m_visibleTerrainChunks = new List<TerrainChunk>();

    /// <summary>
    /// Applies the texture settings to the map material and initialises chunks.
    /// </summary>
    private void Start()
    {
        // applies the texture settings to the material and passes the min and max height to the shader
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

        // calculates the max viewing distance from the visibile distance threshold of the last level of detail info
        float maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        // gets the mesh world size from the settings
        m_meshWorldSize = meshSettings.MeshWorldSize;
        // calculates what the view distance is for visible chunks
        m_chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist / m_meshWorldSize);

        // updates the visibility of the chunks, must occur in start because it is only called when the viewer has moved
        UpdateVisibleChunks();
    }

    /// <summary>
    /// Updates mesh colliders and visibility of chunks if the viewer has moved.
    /// </summary>
    private void Update()
    {
        // gets the viewer position in 2D
        m_viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        // checks if the viewer has moved
        if (m_viewerPosition != m_viewerPositionOld)
        {
            // updates colliders
            foreach (TerrainChunk chunk in m_visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        // checks if the viewer movement is large enough to warrant a recalculation of visible chunks
        if ((m_viewerPositionOld - m_viewerPosition).sqrMagnitude > m_sqrViewerMoveThresholdForChunkUpdate)
        {
            // stores the current position for the next frame
            m_viewerPositionOld = m_viewerPosition;
            UpdateVisibleChunks();
        }
    }

    /// <summary>
    /// Updates existing chunks and creates new ones.
    /// </summary>
    private void UpdateVisibleChunks()
    {
        // used to store chunk positions that have already been looked at
        // HashSet was used because it is easy to check if it contains an item
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        // iterates through the visible chunks from the back to prevent null references if a chunk is removed
        for (int i = m_visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            // adds the chunk to the updated list before 
            alreadyUpdatedChunkCoords.Add(m_visibleTerrainChunks[i].coord);
            // updates the terrain chunk.
            m_visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        // coordinates of the chunk the viewer is currently on
        int currentChunkCoordX = Mathf.RoundToInt(m_viewerPosition.x / m_meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(m_viewerPosition.y / m_meshWorldSize);

        // iterates through chunks on all sides of the viewer, meaning ± the distance from both x and y offsets
        for (int yOffset = -m_chunkVisibleInViewDist; yOffset <= m_chunkVisibleInViewDist; yOffset++)
        {
            for (int xOffset = -m_chunkVisibleInViewDist; xOffset <= m_chunkVisibleInViewDist; xOffset++)
            {
                // gets the coordinates of the chunk being iterated over
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                // checks if the coord has not already been updated
                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    // checks if the chunk exists but has not been updated
                    if (m_terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        // updates
                        m_terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        // creates a new chunk at the coordinate
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                        // adds it to the dictionary
                        m_terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        // subscribesthe callback that adds or removes the chunk from a visibility list and passes on the change info to the chunk
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        // generates a height map for the chunk
                        newChunk.Load();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Adds the chunk to the list of visible chunks if it is visible.
    /// </summary>
    /// <param name="chunk">The chunk that is being assesed.</param>
    /// <param name="isVisible">The visibility of the chunk.</param>
    private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        // adds chunk if visible
        if (isVisible)
        {
            m_visibleTerrainChunks.Add(chunk);
        }
        // removes if invisible
        else
        {
            m_visibleTerrainChunks.Remove(chunk);
        }
    }
}

/// <summary>
/// Used to set the level of detail for certain distances.
/// </summary>
[System.Serializable]
public struct LODInfo
{
    /// <summary>
    /// Level of detail of the distance.
    /// </summary>
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    /// <summary>
    /// Distance that the level of detail occurs at.
    /// </summary>
    public float visibleDistThreshold;

    /// <summary>
    /// The squared visible distance threshold for reduced calculations.
    /// </summary>
    public float SqrVisibleDistThreshold
    {
        get
        {
            return visibleDistThreshold * visibleDistThreshold;
        }
    }
}