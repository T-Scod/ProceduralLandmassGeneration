using UnityEngine;

/// <summary>
/// Contains a chunk of mesh.
/// </summary>
public class TerrainChunk
{
    /// <summary>
    /// Callback that determines if the chunk's visibility was changed.
    /// </summary>
    public event System.Action<TerrainChunk, bool> OnVisibilityChanged;
    /// <summary>
    /// Used to check if a terrain chunk has been looped over.
    /// </summary>
    public Vector2 coord;

    /// <summary>
    /// The distance that colliders generate within.
    /// </summary>
    private const float m_colliderGenerationDistThreshold = 5.0f;
    /// <summary>
    /// The terrain chunk object.
    /// </summary>
    private GameObject m_meshObject;
    /// <summary>
    /// The center of the terrain.
    /// </summary>
    private Vector2 m_sampleCenter;
    /// <summary>
    /// Box around the terrain from the position of the viewer.
    /// </summary>
    private Bounds m_bounds;
    /// <summary>
    /// Used to display the mesh.
    /// </summary>
    private MeshRenderer m_meshRenderer;
    /// <summary>
    /// Stores the generated mesh.
    /// </summary>
    private MeshFilter m_meshFilter;
    /// <summary>
    /// Stores the generated mesh collider.
    /// </summary>
    private MeshCollider m_meshCollider;
    /// <summary>
    /// Collection of the different used detail levels.
    /// </summary>
    private LODInfo[] m_detailLevels;
    /// <summary>
    /// Collection of different level of detail meshes.
    /// </summary>
    private readonly LODMesh[] m_lodMeshes;
    /// <summary>
    /// The level of detail index for colliders.
    /// </summary>
    private readonly int m_colliderLODIndex;
    /// <summary>
    /// Stores the generated height map to then use to generate a mesh.
    /// </summary>
    private HeightMap m_heightMap;
    /// <summary>
    /// Determines if a height map was received.
    /// </summary>
    private bool m_heightMapReceived;
    /// <summary>
    /// The previous level of detail index.
    /// </summary>
    private int m_previousLODIndex = -1;
    /// <summary>
    /// Determines if a collider has been set to the mesh.
    /// </summary>
    private bool m_hasSetCollider;
    /// <summary>
    /// The maximum distance the viewer can see.
    /// </summary>
    private readonly float m_maxViewDist;
    /// <summary>
    /// Settings to vary the generated heights of the terrain.
    /// </summary>
    private readonly HeightMapSettings m_heightMapSettings;
    /// <summary>
    /// Setteings to vary the generated mesh.
    /// </summary>
    private MeshSettings m_meshSettings;
    /// <summary>
    /// The object that the terrain is being built around.
    /// </summary>
    private Transform m_viewer;

    /// <summary>
    /// Initialises the terrain chunk.
    /// </summary>
    /// <param name="coord">The position of the mesh the viewer is on.</param>
    /// <param name="heightMapSettings">Settings of the height map being used to generate the mesh chunk.</param>
    /// <param name="meshSettings">Settings of the mesh being generated.</param>
    /// <param name="detailLevels">The different possible levels of detail.</param>
    /// <param name="colliderLODIndex">The level of detail index the </param>
    /// <param name="parent"></param>
    /// <param name="viewer"></param>
    /// <param name="material"></param>
    public TerrainChunk(Vector2 coord, HeightMapSettings heightMapSettings, MeshSettings meshSettings, LODInfo[] detailLevels, int colliderLODIndex, Transform parent, Transform viewer, Material material)
    {
        // assigns the values from the constructor to the object
        this.coord = coord;
        m_detailLevels = detailLevels;
        m_colliderLODIndex = colliderLODIndex;
        m_heightMapSettings = heightMapSettings;
        m_meshSettings = meshSettings;
        m_viewer = viewer;

        // calculates the center of the mesh
        m_sampleCenter = coord * meshSettings.MeshWorldSize / meshSettings.meshScale;
        // the position of the coordinate on the mesh
        Vector2 position = coord * meshSettings.MeshWorldSize;
        // gets the bounds around the position
        m_bounds = new Bounds(position, Vector2.one * meshSettings.MeshWorldSize);

        // adds an object for the mesh that has a renderer, filter and collider component
        m_meshObject = new GameObject("Terrain Chunk");
        m_meshRenderer = m_meshObject.AddComponent<MeshRenderer>();
        m_meshFilter = m_meshObject.AddComponent<MeshFilter>();
        m_meshCollider = m_meshObject.AddComponent<MeshCollider>();
        // sets the material of the object
        m_meshRenderer.material = material;

        // creates the mesh from the coord position in 2D
        m_meshObject.transform.position = new Vector3(position.x, 0, position.y);
        m_meshObject.transform.parent = parent;
        SetVisible(false);

        // sets the level of detail and colliders of the mesh by subscribing the update callbacks
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

        // sets the max viewing distance to the largest visible distance threshold
        m_maxViewDist = detailLevels[detailLevels.Length - 1].visibleDistThreshold;
    }

    /// <summary>
    /// Generates a height map on a separate thread.
    /// </summary>
    public void Load()
    {
        // passes the function into the thread using the lambda method
        ThreadedDataRequester.RequestData(() => HeightMapGenerator.GenerateHeightMap(m_meshSettings.NumVertsPerLine, m_meshSettings.NumVertsPerLine, m_heightMapSettings, m_sampleCenter), OnHeightMapReceived);
    }

    /// <summary>
    /// Updates a terrain chunk when a new height map is received.
    /// </summary>
    /// <param name="heightMapObject">The height map being received.</param>
    private void OnHeightMapReceived(object heightMapObject)
    {
        // casts the object as a height map
        m_heightMap = (HeightMap)heightMapObject;
        m_heightMapReceived = true;

        // updates the terrain chunk
        UpdateTerrainChunk();
    }

    /// <summary>
    /// Accesses the view position in 2D.
    /// </summary>
    private Vector2 ViewerPosition
    {
        get
        {
            return new Vector2(m_viewer.position.x, m_viewer.position.z);
        }
    }

    /// <summary>
    /// Updates the terrain chunk.
    /// </summary>
    public void UpdateTerrainChunk()
    {
        // checks if a height map was received
        if (m_heightMapReceived)
        {
            // gets the distance to the closest edge
            float viewerDistFromNearestEdge = Mathf.Sqrt(m_bounds.SqrDistance(ViewerPosition));

            // gets the visibility of the mesh
            bool wasVisible = IsVisible();
            // gets if the chunk is visible
            bool visible = viewerDistFromNearestEdge <= m_maxViewDist;

            // checks if the chunk is visible
            if (visible)
            {
                // the level of detail of the mesh
                int lodIndex = 0;

                // checks the distance to each visible distance threshold to determine what the level of detail is
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
                    // sets the mesh to the calculated level of detail
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

            // checks if the visibility was changed
            if (wasVisible != visible)
            {
                // changes the visibility of the mesh
                SetVisible(visible);
                if (OnVisibilityChanged != null)
                {
                    OnVisibilityChanged(this, visible);
                }
            }
        }
    }

    /// <summary>
    /// Updates the collider mesh.
    /// </summary>
    public void UpdateCollisionMesh()
    {
        // checks if a collider has been set
        if (!m_hasSetCollider)
        {
            // gets the square distance between the viewer and the edge
            float sqrDistFromViewerToEdge = m_bounds.SqrDistance(ViewerPosition);

            // checks if the distance to the edge is within the visible distance
            if (sqrDistFromViewerToEdge < m_detailLevels[m_colliderLODIndex].SqrVisibleDistThreshold)
            {
                // checks if a mesh has not been requested
                if (!m_lodMeshes[m_colliderLODIndex].hasRequestedMesh)
                {
                    // generates a mesh on the thread
                    m_lodMeshes[m_colliderLODIndex].RequestMesh(m_heightMap, m_meshSettings);
                }
            }

            // checks if the distance to the collider is within the generation distance threshold
            if (sqrDistFromViewerToEdge < m_colliderGenerationDistThreshold * m_colliderGenerationDistThreshold)
            {
                // checks if there is already a mesh
                if (m_lodMeshes[m_colliderLODIndex].hasMesh)
                {
                    // sets the collider to the current mesh
                    m_meshCollider.sharedMesh = m_lodMeshes[m_colliderLODIndex].mesh;
                    m_hasSetCollider = true;
                }
            }
        }
    }

    /// <summary>
    /// Sets the visibility of the mesh object.
    /// </summary>
    /// <param name="visible">The visibility of the mesh object.</param>
    public void SetVisible(bool visible)
    {
        m_meshObject.SetActive(visible);
    }

    /// <summary>
    /// Gets the visibility of the mesh object.
    /// </summary>
    /// <returns>Gets the active self variable from the mesh object.</returns>
    public bool IsVisible()
    {
        return m_meshObject.activeSelf;
    }

    /// <summary>
    /// Creates a mesh of the specified level of detail.
    /// </summary>
    private class LODMesh
    {
        /// <summary>
        /// Stores the mesh.
        /// </summary>
        public Mesh mesh;
        /// <summary>
        /// Determines if the thread is requesting a mesh.
        /// </summary>
        public bool hasRequestedMesh;
        /// <summary>
        /// Determines if there is a generated mesh.
        /// </summary>
        public bool hasMesh;
        /// <summary>
        /// Used to subscribe the mesh update callbacks.
        /// </summary>
        public event System.Action UpdateCallback;

        /// <summary>
        /// The level of detail of the mesh.
        /// </summary>
        private readonly int m_lod;

        /// <summary>
        /// Sets the level of detail.
        /// </summary>
        /// <param name="lod">The level of detail.</param>
        public LODMesh(int lod)
        {
            m_lod = lod;
        }

        /// <summary>
        /// Creates a mesh from the mesh data received.
        /// </summary>
        /// <param name="meshDataObject">The mesh data.</param>
        private void OnMeshDataReceived(object meshDataObject)
        {
            // casts the object as mesh data because this method is always used with mesh data
            mesh = ((MeshData)meshDataObject).CreateMesh();
            hasMesh = true;

            // updates the chunks and colliders
            UpdateCallback();
        }

        /// <summary>
        /// Generates the terrain mesh on a separate thread.
        /// </summary>
        /// <param name="heightMap">The height map being turned into a mesh.</param>
        /// <param name="meshSettings">The settings of the mesh being generated.</param>
        public void RequestMesh(HeightMap heightMap, MeshSettings meshSettings)
        {
            // prevents the data being requested again before the thread is done
            hasRequestedMesh = true;
            // uses a lambda function to pass a funciton to the thread
            ThreadedDataRequester.RequestData(() => MeshGenerator.GenerateTerrainMesh(heightMap.values, meshSettings, m_lod), OnMeshDataReceived);
        }
    }
}