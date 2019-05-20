using UnityEngine;

/// <summary>
/// Creates a mesh from a height map.
/// </summary>
public static class MeshGenerator
{
    /// <summary>
    /// Generates mesh data from a height map with customisable settings.
    /// </summary>
    /// <param name="heightMap">The height map values.</param>
    /// <param name="meshSettings">Mesh settings to vary the mesh.</param>
    /// <param name="levelOfDetail">The level of detail of the mesh.</param>
    /// <returns>Collections of vertices and triangles that make up the mesh.</returns>
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        // the amount of vertices skipped each line
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        // the number of vertices in each line
        int numVertsPerLine = meshSettings.NumVertsPerLine;

        // the top left coordinate of the mesh
        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2.0f;
        // initialises a mesh's vertices and triangles
        MeshData meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.useFlatShading);

        // the index of each vertex
        int[,] vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
        // the index of the current vertex
        int meshVertexIndex = 0;
        // the index of the current vertex not in the mesh bounds
        int outOfMeshVertexIndex = -1;
        // for every index of the index map, determine if the vertex is in or out of the mesh
        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                // determines if the vertex is out of the mesh
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                // determines if the vertex is skipped
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 &&
                    y > 2 && y < numVertsPerLine - 3 &&
                    ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                // adds an out of mesh vertex index to the map
                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                // adds an in mesh vertex index to the map if it is not skipped
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        // adds the vertex positions to the mesh data
        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                // determines if the vertex is skipped
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 &&
                    y > 2 && y < numVertsPerLine - 3 &&
                    ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                // checks if the vertex is not skipped
                if (!isSkippedVertex)
                {
                    // determines if the vertex is out of the mesh
                    bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    // determines if the vertex is on the edge
                    bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
                    // determines if the vertex is counted
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    // determines if the vertex is being connected to
                    bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    // the index of the vertex in the mesh
                    int vertexIndex = vertexIndicesMap[x, y];
                    // calculates the vertex's position as a percentage between main vertices
                    Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);
                    // gets the 2D position of the vertex
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.MeshWorldSize;
                    // gets the height value of the vertex from the height map
                    float height = heightMap[x, y];

                    // checks if the vertex is being connected to along the edge
                    if (isEdgeConnectionVertex)
                    {
                        // determines if the connection is along the left or right side
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        // gets the distances to the main vertices
                        int distToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipIncrement;
                        int distToMainVertexB = skipIncrement - distToMainVertexA;
                        // gets the distance as a percentage between the two
                        float distPercentFromAToB = distToMainVertexA / (float)skipIncrement;

                        // gets the height of the two vertices
                        float heightOfMainVertexA = heightMap[(isVertical) ? x : x - distToMainVertexA, (isVertical) ? y - distToMainVertexA : y];
                        float heightOfMainVertexB = heightMap[(isVertical) ? x : x + distToMainVertexB, (isVertical) ? y + distToMainVertexB : y];

                        // averages out the height
                        height = heightOfMainVertexA * (1 - distPercentFromAToB) + heightOfMainVertexB * distPercentFromAToB;
                    }

                    // adds the vertex to the mesh
                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    // determines if a triangle should be made based on if it is not on an edge
                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));
                    // checks if a triangle should be made
                    if (createTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;

                        // gets the 4 vertices of a tile of the mesh
                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];
                        // adds 2 triangles to make up a tile
                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
        }

        // calculates the normals of the mesh
        meshData.ProcessMesh();

        return meshData;
    }
}

/// <summary>
/// Contains the different properties of a mesh.
/// </summary>
public class MeshData
{
    /// <summary>
    /// Collection of the mesh's vertices.
    /// </summary>
    private Vector3[] m_vertices;
    /// <summary>
    /// Collection of the mesh's triangles.
    /// </summary>
    private int[] m_triangles;
    /// <summary>
    /// Collection of the texture coordinates of each vertex.
    /// </summary>
    private Vector2[] m_uvs;
    /// <summary>
    /// Calculated normals for each vertex.
    /// </summary>
    private Vector3[] m_bakedNormals;
    /// <summary>
    /// Vertices that are part of the generated mesh but are not in the mesh size.
    /// </summary>
    private readonly Vector3[] m_outOfMeshVertices;
    /// <summary>
    /// Triangles that are part of the generated mesh but are not in the mesh size.
    /// </summary>
    private int[] m_outOfMeshTriangles;
    /// <summary>
    /// The index of the current triangle in the container.
    /// </summary>
    private int m_triangleIndex;
    /// <summary>
    /// The index of the current triangle outside of the mesh triangle container.
    /// </summary>
    private int m_outOfMeshTriangleIndex;
    /// <summary>
    /// Determines if flatshading is being used when rendering the mesh.
    /// </summary>
    private readonly bool m_useFlatShading;

    /// <summary>
    /// Initialises the mesh data.
    /// </summary>
    /// <param name="numVertsPerLine">The number of vertices in each line of a 2D mesh.</param>
    /// <param name="skipIncrement">The amount of vertices skipped based on the level of detail.</param>
    /// <param name="useFlatShading">Determines if faltshading is to be used.</param>
    public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading)
    {
        m_useFlatShading = useFlatShading;

        // number of vertices along the edge of the mesh
        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
        // number of vertices that will be connected to along the edge
        int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        // number of vertices in each line that is not being skipped
        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
        // total number of main vertices
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        m_vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        // creates a uv per vertex
        m_uvs = new Vector2[m_vertices.Length];

        // number of triangles that will be connected to along the edge
        int numMeshEgdeTriangles = 8 * (numVertsPerLine - 4);
        // total number of triangles
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        m_triangles = new int[(numMeshEgdeTriangles + numMainTriangles) * 3];

        // initialises the out of mesh containers based on the amount of objects that will be connected to
        m_outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
        m_outOfMeshTriangles = new int[24 * (numVertsPerLine - 2)];
    }

    /// <summary>
    /// Adds a vertex with its uv to the mesh.
    /// </summary>
    /// <param name="vertexPosition">The position of the added vertex.</param>
    /// <param name="uv">The uv of the vertex.</param>
    /// <param name="vertexIndex">The index the vertex is being added at.</param>
    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        // checks if it is an out of mesh vertex
        if (vertexIndex < 0)
        {
            m_outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
        }
        // adds the vertex position and uv to the containers
        else
        {
            m_vertices[vertexIndex] = vertexPosition;
            m_uvs[vertexIndex] = uv;
        }
    }

    /// <summary>
    /// Adds a triangle to the mesh.
    /// </summary>
    /// <param name="a">First vertex of the triangle.</param>
    /// <param name="b">Second vertex of the triangle.</param>
    /// <param name="c">Third vertex of the triangle.</param>
    public void AddTriangle(int a, int b, int c)
    {
        // checks if any of the vertices are out of the mesh
        if (a < 0 || b < 0 || c < 0)
        {
            // adds each vertex to the container, one after the other
            m_outOfMeshTriangles[m_outOfMeshTriangleIndex] = a;
            m_outOfMeshTriangles[m_outOfMeshTriangleIndex + 1] = b;
            m_outOfMeshTriangles[m_outOfMeshTriangleIndex + 2] = c;
            // increments the index count for each vertex added
            m_outOfMeshTriangleIndex += 3;
        }
        else
        {
            // adds each vertex to the container, one after the other
            m_triangles[m_triangleIndex] = a;
            m_triangles[m_triangleIndex + 1] = b;
            m_triangles[m_triangleIndex + 2] = c;
            // increments the index count for each vertex added
            m_triangleIndex += 3;
        }
    }

    /// <summary>
    /// Calculates the normal at each vertex.
    /// </summary>
    /// <returns>Baked normals.</returns>
    private Vector3[] CalculateNormals()
    {
        // contains every vertex normal
        Vector3[] vertexNormals = new Vector3[m_vertices.Length];
        // the amount of triangles
        int triangleCount = m_triangles.Length / 3;
        // calculates and adds the surface normal of each triangle to the vertex normals
        for (int i = 0; i < triangleCount; i++)
        {
            // gets the triangle index
            int normalTriangleIndex = i * 3;
            // gets each vertex of the triangle
            int vertexIndexA = m_triangles[normalTriangleIndex];
            int vertexIndexB = m_triangles[normalTriangleIndex + 1];
            int vertexIndexC = m_triangles[normalTriangleIndex + 2];

            // calculates the surface normal of the triangle
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            // adds the normal to each vertex's normal
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        // the amount of triangles along the edge of the mesh
        int borderTriangleCount = m_outOfMeshTriangles.Length / 3;
        // calculates and adds the surface normal of each triangle to the normals of the vertices that are inside the mesh
        for (int i = 0; i < borderTriangleCount; i++)
        {
            // gets the triangle index
            int normalTriangleIndex = i * 3;
            // gets each vertex of the triangle
            int vertexIndexA = m_outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = m_outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = m_outOfMeshTriangles[normalTriangleIndex + 2];

            // calculates the surface normal of the triangle
            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            // adds the normal to each vertex's normal if the vertex is inside the mesh
            if (vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if (vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if (vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }
        }

        // normalises all the normals
        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    /// <summary>
    /// Calculates the surface normal of the triangle.
    /// </summary>
    /// <param name="indexA">The index of the first vertex of the triangle.</param>
    /// <param name="indexB">The index of the second vertex of the triangle.</param>
    /// <param name="indexC">The index of the third vertex of the triangle.</param>
    /// <returns>The normal of the triangle.</returns>
    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        // gets the vertex at the index
        Vector3 pointA = (indexA < 0) ? m_outOfMeshVertices[-indexA - 1] : m_vertices[indexA];
        Vector3 pointB = (indexB < 0) ? m_outOfMeshVertices[-indexB - 1] : m_vertices[indexB];
        Vector3 pointC = (indexC < 0) ? m_outOfMeshVertices[-indexC - 1] : m_vertices[indexC];

        // calculates the perpendicular vector that sides AB and AC have in common
        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    /// <summary>
    /// Calculates the normals of the mesh based on whether flatshading is used or not.
    /// </summary>
    public void ProcessMesh()
    {
        // checks if the normals should be calculated using flatshading
        if (m_useFlatShading)
        {
            FlatShading();
        }
        else
        {
            // calculates the normals of each triangle normally
            BakeNormals();
        }
    }

    /// <summary>
    /// Gets the calculated normals in a thread safe way.
    /// </summary>
    private void BakeNormals()
    {
        // m_bakeNormals will not be used in a threaded method
        m_bakedNormals = CalculateNormals();
    }

    /// <summary>
    /// Calculates the normals using flatshading.
    /// </summary>
    private void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[m_triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[m_triangles.Length];

        // gives each triangle their own set of vertices so that blending does not occur between normals
        for (int i = 0; i < m_triangles.Length; i++)
        {
            flatShadedVertices[i] = m_vertices[m_triangles[i]];
            flatShadedUvs[i] = m_uvs[m_triangles[i]];
            m_triangles[i] = i;
        }

        m_vertices = flatShadedVertices;
        m_uvs = flatShadedUvs;
    }

    /// <summary>
    /// Creates a mesh based off of the mesh data.
    /// </summary>
    /// <returns>A mesh based on the mesh data.</returns>
    public Mesh CreateMesh()
    {
        // sets the vertices, triangles and uvs of the mesh
        Mesh mesh = new Mesh
        {
            vertices = m_vertices,
            triangles = m_triangles,
            uv = m_uvs
        };

        // calculates the normal of each vertex without blending
        if (m_useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            // sets the normals to the blended normals
            mesh.normals = m_bakedNormals;
        }

        return mesh;
    }
}