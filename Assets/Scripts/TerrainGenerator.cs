using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    private const float m_viewerMoveThresholdForChunkUpdate = 25.0f;
    private const float m_sqrViewerMoveThresholdForChunkUpdate = m_viewerMoveThresholdForChunkUpdate * m_viewerMoveThresholdForChunkUpdate;

    public int colliderLODIndex;
    public LODInfo[] detailLevels;

    public MeshSettings meshSettings;
    public HeightMapSettings heightMapSettings;
    public TextureSettings textureSettings;

    public Transform viewer;
    public Material mapMaterial;

    private Vector2 m_viewerPosition;
    private Vector2 m_viewerPositionOld;

    private float m_meshWorldSize;
    private int m_chunkVisibleInViewDist;
     
    private Dictionary<Vector2, TerrainChunk> m_terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    private List<TerrainChunk> m_visibleTerrainChunks = new List<TerrainChunk>();

    private void Start()
    {
        textureSettings.ApplyToMaterial(mapMaterial);
        textureSettings.UpdateMeshHeights(mapMaterial, heightMapSettings.MinHeight, heightMapSettings.MaxHeight);

        float maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
        m_meshWorldSize = meshSettings.MeshWorldSize;
        m_chunkVisibleInViewDist = Mathf.RoundToInt(maxViewDist / m_meshWorldSize);

        UpdateVisibleChunks();
    }

    private void Update()
    {
        m_viewerPosition = new Vector2(viewer.position.x, viewer.position.z);

        if (m_viewerPosition != m_viewerPositionOld)
        {
            foreach (TerrainChunk chunk in m_visibleTerrainChunks)
            {
                chunk.UpdateCollisionMesh();
            }
        }

        if ((m_viewerPositionOld - m_viewerPosition).sqrMagnitude > m_sqrViewerMoveThresholdForChunkUpdate)
        {
            m_viewerPositionOld = m_viewerPosition;
            UpdateVisibleChunks();
        }
    }

    private void UpdateVisibleChunks()
    {
        HashSet<Vector2> alreadyUpdatedChunkCoords = new HashSet<Vector2>();
        for (int i = m_visibleTerrainChunks.Count - 1; i >= 0; i--)
        {
            alreadyUpdatedChunkCoords.Add(m_visibleTerrainChunks[i].coord);
            m_visibleTerrainChunks[i].UpdateTerrainChunk();
        }

        int currentChunkCoordX = Mathf.RoundToInt(m_viewerPosition.x / m_meshWorldSize);
        int currentChunkCoordY = Mathf.RoundToInt(m_viewerPosition.y / m_meshWorldSize);

        for (int yOffset = -m_chunkVisibleInViewDist; yOffset <= m_chunkVisibleInViewDist; yOffset++)
        {
            for (int xOffset = -m_chunkVisibleInViewDist; xOffset <= m_chunkVisibleInViewDist; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (!alreadyUpdatedChunkCoords.Contains(viewedChunkCoord))
                {
                    if (m_terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                    {
                        m_terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                    }
                    else
                    {
                        TerrainChunk newChunk = new TerrainChunk(viewedChunkCoord, heightMapSettings, meshSettings, detailLevels, colliderLODIndex, transform, viewer, mapMaterial);
                        m_terrainChunkDictionary.Add(viewedChunkCoord, newChunk);
                        newChunk.OnVisibilityChanged += OnTerrainChunkVisibilityChanged;
                        newChunk.Load();
                    }
                }
            }
        }
    }

    private void OnTerrainChunkVisibilityChanged(TerrainChunk chunk, bool isVisible)
    {
        if (isVisible)
        {
            m_visibleTerrainChunks.Add(chunk);
        }
        else
        {
            m_visibleTerrainChunks.Remove(chunk);
        }
    }
}

[System.Serializable]
public struct LODInfo
{
    [Range(0, MeshSettings.numSupportedLODs - 1)]
    public int lod;
    public float visibleDistThreshold;

    public float SqrVisibleDistThreshold
    {
        get
        {
            return visibleDistThreshold * visibleDistThreshold;
        }
    }
}