using System.Collections;
using UnityEngine;

public static class MeshGenerator
{
    public static MeshData GenerateTerrainMesh(float[,] heightMap, MeshSettings meshSettings, int levelOfDetail)
    {
        int skipIncrement = (levelOfDetail == 0) ? 1 : levelOfDetail * 2;
        int numVertsPerLine = meshSettings.NumVertsPerLine;

        Vector2 topLeft = new Vector2(-1, 1) * meshSettings.MeshWorldSize / 2.0f;

        MeshData meshData = new MeshData(numVertsPerLine, skipIncrement, meshSettings.useFlatShading);

        int[,] vertexIndicesMap = new int[numVertsPerLine, numVertsPerLine];
        int meshVertexIndex = 0;
        int outOfMeshVertexIndex = -1;

        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {
                bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 &&
                    y > 2 && y < numVertsPerLine - 3 &&
                    ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (isOutOfMeshVertex)
                {
                    vertexIndicesMap[x, y] = outOfMeshVertexIndex;
                    outOfMeshVertexIndex--;
                }
                else if (!isSkippedVertex)
                {
                    vertexIndicesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < numVertsPerLine; y++)
        {
            for (int x = 0; x < numVertsPerLine; x++)
            {                
                bool isSkippedVertex = x > 2 && x < numVertsPerLine - 3 &&
                    y > 2 && y < numVertsPerLine - 3 &&
                    ((x - 2) % skipIncrement != 0 || (y - 2) % skipIncrement != 0);
                if (!isSkippedVertex)
                {
                    bool isOutOfMeshVertex = y == 0 || y == numVertsPerLine - 1 || x == 0 || x == numVertsPerLine - 1;
                    bool isMeshEdgeVertex = (y == 1 || y == numVertsPerLine - 2 || x == 1 || x == numVertsPerLine - 2) && !isOutOfMeshVertex;
                    bool isMainVertex = (x - 2) % skipIncrement == 0 && (y - 2) % skipIncrement == 0 && !isOutOfMeshVertex && !isMeshEdgeVertex;
                    bool isEdgeConnectionVertex = (y == 2 || y == numVertsPerLine - 3 || x == 2 || x == numVertsPerLine - 3) && !isOutOfMeshVertex && !isMeshEdgeVertex && !isMainVertex;

                    int vertexIndex = vertexIndicesMap[x, y];
                    Vector2 percent = new Vector2(x - 1, y - 1) / (numVertsPerLine - 3);
                    Vector2 vertexPosition2D = topLeft + new Vector2(percent.x, -percent.y) * meshSettings.MeshWorldSize;
                    float height = heightMap[x, y];

                    if (isEdgeConnectionVertex)
                    {
                        bool isVertical = x == 2 || x == numVertsPerLine - 3;
                        int distToMainVertexA = ((isVertical) ? y - 2 : x - 2) % skipIncrement;
                        int distToMainVertexB = skipIncrement - distToMainVertexA;
                        float distPercentFromAToB = distToMainVertexA / (float)skipIncrement;

                        float heightOfMainVertexA = heightMap[(isVertical) ? x : x - distToMainVertexA, (isVertical) ? y - distToMainVertexA : y];
                        float heightOfMainVertexB = heightMap[(isVertical) ? x : x + distToMainVertexB, (isVertical) ? y + distToMainVertexB : y];

                        height = heightOfMainVertexA * (1 - distPercentFromAToB) + heightOfMainVertexB * distPercentFromAToB;
                    }

                    meshData.AddVertex(new Vector3(vertexPosition2D.x, height, vertexPosition2D.y), percent, vertexIndex);

                    bool createTriangle = x < numVertsPerLine - 1 && y < numVertsPerLine - 1 && (!isEdgeConnectionVertex || (x != 2 && y != 2));

                    if (createTriangle)
                    {
                        int currentIncrement = (isMainVertex && x != numVertsPerLine - 3 && y != numVertsPerLine - 3) ? skipIncrement : 1;

                        int a = vertexIndicesMap[x, y];
                        int b = vertexIndicesMap[x + currentIncrement, y];
                        int c = vertexIndicesMap[x, y + currentIncrement];
                        int d = vertexIndicesMap[x + currentIncrement, y + currentIncrement];
                        meshData.AddTriangle(a, d, c);
                        meshData.AddTriangle(d, a, b);
                    }
                }
            }
        }

        meshData.ProcessMesh();

        return meshData;
    }
}

public class MeshData
{
    private Vector3[] m_vertices;
    private int[] m_triangles;
    private Vector2[] m_uvs;
    private Vector3[] m_bakedNormals;

    private readonly Vector3[] m_outOfMeshVertices;
    private int[] m_outOfMeshTriangles;

    private int m_triangleIndex;
    private int m_outOfMeshTriangleIndex;

    private readonly bool m_useFlatShading;

    public MeshData(int numVertsPerLine, int skipIncrement, bool useFlatShading)
    {
        m_useFlatShading = useFlatShading;

        int numMeshEdgeVertices = (numVertsPerLine - 2) * 4 - 4;
        int numEdgeConnectionVertices = (skipIncrement - 1) * (numVertsPerLine - 5) / skipIncrement * 4;
        int numMainVerticesPerLine = (numVertsPerLine - 5) / skipIncrement + 1;
        int numMainVertices = numMainVerticesPerLine * numMainVerticesPerLine;

        m_vertices = new Vector3[numMeshEdgeVertices + numEdgeConnectionVertices + numMainVertices];
        m_uvs = new Vector2[m_vertices.Length];

        int numMeshEgdeTriangles = 8 * (numVertsPerLine - 4);
        int numMainTriangles = (numMainVerticesPerLine - 1) * (numMainVerticesPerLine - 1) * 2;
        m_triangles = new int[(numMeshEgdeTriangles + numMainTriangles) * 3];

        m_outOfMeshVertices = new Vector3[numVertsPerLine * 4 - 4];
        m_outOfMeshTriangles = new int[24 * (numVertsPerLine - 2)];
    }

    public void AddVertex(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if (vertexIndex < 0)
        {
            m_outOfMeshVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            m_vertices[vertexIndex] = vertexPosition;
            m_uvs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if (a < 0 || b < 0 || c < 0)
        {
            m_outOfMeshTriangles[m_outOfMeshTriangleIndex] = a;
            m_outOfMeshTriangles[m_outOfMeshTriangleIndex + 1] = b;
            m_outOfMeshTriangles[m_outOfMeshTriangleIndex + 2] = c;
            m_outOfMeshTriangleIndex += 3;
        }
        else
        {
            m_triangles[m_triangleIndex] = a;
            m_triangles[m_triangleIndex + 1] = b;
            m_triangles[m_triangleIndex + 2] = c;
            m_triangleIndex += 3;
        }
    }

    private Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[m_vertices.Length];
        int triangleCount = m_triangles.Length / 3;
        for (int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = m_triangles[normalTriangleIndex];
            int vertexIndexB = m_triangles[normalTriangleIndex + 1];
            int vertexIndexC = m_triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = m_outOfMeshTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = m_outOfMeshTriangles[normalTriangleIndex];
            int vertexIndexB = m_outOfMeshTriangles[normalTriangleIndex + 1];
            int vertexIndexC = m_outOfMeshTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndices(vertexIndexA, vertexIndexB, vertexIndexC);
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

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    private Vector3 SurfaceNormalFromIndices(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? m_outOfMeshVertices[-indexA - 1] : m_vertices[indexA];
        Vector3 pointB = (indexB < 0) ? m_outOfMeshVertices[-indexB - 1] : m_vertices[indexB];
        Vector3 pointC = (indexC < 0) ? m_outOfMeshVertices[-indexC - 1] : m_vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;
        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void ProcessMesh()
    {
        if (m_useFlatShading)
        {
            FlatShading();
        }
        else
        {
            BakeNormals();
        }
    }

    private void BakeNormals()
    {
        m_bakedNormals = CalculateNormals();
    }

    private void FlatShading()
    {
        Vector3[] flatShadedVertices = new Vector3[m_triangles.Length];
        Vector2[] flatShadedUvs = new Vector2[m_triangles.Length];

        for (int i = 0; i < m_triangles.Length; i++)
        {
            flatShadedVertices[i] = m_vertices[m_triangles[i]];
            flatShadedUvs[i] = m_uvs[m_triangles[i]];
            m_triangles[i] = i;
        }

        m_vertices = flatShadedVertices;
        m_uvs = flatShadedUvs;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh
        {
            vertices = m_vertices,
            triangles = m_triangles,
            uv = m_uvs
        };

        if (m_useFlatShading)
        {
            mesh.RecalculateNormals();
        }
        else
        {
            mesh.normals = m_bakedNormals;
        }

        return mesh;
    }
}