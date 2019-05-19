using UnityEngine;

public class TerrainChunk
{
    private const float m_colliderGenerationDistanceThreshold = 5.0f;
    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;
    public Vector2 coord;

    private GameObject m_meshObject;
    private Vector2 m_sampleCenter;
    private Bounds m_bounds;

    private MeshRenderer m_meshRenderer;
    private MeshFilter m_meshFilter;
    private MeshCollider m_meshCollider;

    private LODInfo[] m_detailLevels;
    private readonly LODMesh[] m_lodMeshes;
    private readonly int m_colliderLODIndex;

    private HeightMap m_heightMap;
    private bool m_heightMapReceived;
    private int m_previousLODIndex = -1;
    private bool m_hasSetCollider;
    private readonly float m_maxViewDist;

    private readonly HeightMapSettings m_heightMapSettings;
    private MeshSettings m_meshSettings;
    private Transform m_viewer;

    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
    {
        this.coord = coord;
        m_detailLevels = detailLevels;
        m_colliderLODIndex = colliderLODIndex;
        m_heightMapSettings = heightMapSettings;
        m_meshSettings = meshSettings;
        m_viewer = viewer;

        m_sampleCenter = coord * meshSettings.MeshWorldSize / meshSettings.meshScale;
        Vector2 position = coord * meshSettings.MeshWorldSize;
        m_bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);

        m_meshObject = new GameObject("Terrain Chunk");
        m_meshRenderer = m_meshObject.AddComponent<MeshRenderer>();
        m_meshFilter = m_meshObject.AddComponent<MeshFilter>();
        m_meshCollider = m_meshObject.AddComponent<MeshCollider>();
        m_meshRenderer.material = material;

        m_meshObject.transform.position = new Vector3(position.x, 0, position.y);
        m_meshObject.transform.parent = parent;
        SetVisible(false);

        m_lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < detailLevels.Length; i++)
        {
            m_lodMeshes[i] = new LODMesh(detailLevels[i].lod);
            m_lodMeshes[i].UpdateCallback += UpdateTerrainChunk;
            if (i == colliderLODIndex)
            {
                m_lodMeshes[i].UpdateCallback += UpdateCollisionMesh;
            }
        }

        m_maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
    }

    public void Load()
    {
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(m_meshSettings.NumVertsPerLine, m_meshSettings.NumVertsPerLine, m_heightMapSettings, m_sampleCenter), OnHeightMapReceived);
    }

    private void OnHeightMapReceived(object heightMapObject)
    {
        m_heightMap = (HeightMap)heightMapObject;
        m_heightMapReceived = true;

        UpdateTerrainChunk();
    }

    private Vector2 ViewerPosition
    {
        get
        {
            return new Vector2(m_viewer.position.x, m_viewer.position.z);
        }
    }

    public void UpdateTerrainChunk()
    {
        if (m_heightMapReceived)
        {
            float viewerDistFromNearestEdge = Mathf.Sqrt(m_bounds.SqrDistance(ViewerPosition));

            bool wasVisible = IsVisible();
            bool visible = viewerDistFromNearestEdge <= m_maxViewDist;

            if (visible)
            {
                int lodIndex = 0;

                for (int i = 0; i < m_detailLevels.Length - 1; i++)
                {
                    if (viewerDistFromNearestEdge > m_detailLevels[i].visibleDistThreshold)
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != m_previousLODIndex)
                {
                    LODMesh lodMesh = m_lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        m_previousLODIndex = lodIndex;
                        m_meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(m_heightMap, m_meshSettings);
                    }
                }

            }

            if (wasVisible != visible)
            {
                SetVisible(visible);
                if (OnVisibilityChanged != null)
                {
                    OnVisibilityChanged(this, visible);
                }
            }
        }
    }

    public void UpdateCollisionMesh()
    {
        if (!m_hasSetCollider)
        {
            float sqrDistFromViewerToEdge = m_bounds.SqrDistance(ViewerPosition);

            if (sqrDistFromViewerToEdge < m_detailLevels[m_colliderLODIndex].SqrVisibleDistThreshold)
            {
                if (!m_lodMeshes[m_colliderLODIndex].hasRequestedMesh)
                {
                    m_lodMeshes[m_colliderLODIndex].RequestMesh(m_heightMap, m_meshSettings);
                }
            }

            if (sqrDistFromViewerToEdge < m_colliderGenerationDistanceThreshold * m_colliderGenerationDistanceThreshold)
            {
                if (m_lodMeshes[m_colliderLODIndex].hasMesh)
                {
                    m_meshCollider.sharedMesh = m_lodMeshes[m_colliderLODIndex].mesh;
                    m_hasSetCollider = true;
                }
            }
        }
    }

    public void SetVisible(bool visible)
    {
        m_meshObject.SetActive(visible);
    }

    public bool IsVisible()
    {
        return m_meshObject.activeSelf;
    }

    private class LODMesh
    {
        public Mesh mesh;
        public bool hasRequestedMesh;
        public bool hasMesh;
        private readonly int m_lod;
        public event System.Action UpdateCallback;

        public LODMesh(int lod)
        {
            m_lod = lod;
        }

        private void OnMeshDataReceived(object meshDataObject)
        {
            mesh = ((MeshData)meshDataObject).CreateMesh();
            hasMesh = true;

            UpdateCallback();
        }

        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            hasRequestedMesh = true;
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, m_lod), OnMeshDataReceived);
        }
    }
}