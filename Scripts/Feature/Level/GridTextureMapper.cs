using UnityEngine;
using System.Collections.Generic;

public class GridTextureMapper : MonoBehaviour
{
    [Header("Grid Settings")]
    public int gridWidth = 120;
    public int gridHeight = 120;
    public float planeSize = 120f;

    [System.Serializable]
    public struct TileArea
    {
        public string areaName;
        public Vector2Int[] points;
        public Texture2D texture;
    }

    [Header("Tile Layers")]
    public List<TileArea> tileAreas;

    [ContextMenu("Generate Tiles")]
    public void GenerateCustomTiles()
    {
        ClearExistingTiles();
        if (tileAreas == null) return;

        for (int i = 0; i < tileAreas.Count; i++)
        {
            if (tileAreas[i].points != null && tileAreas[i].points.Length == 4)
            {
                CreateTileMesh(tileAreas[i], i);
            }
        }
    }

    void CreateTileMesh(TileArea area, int index)
    {
        string objName = string.IsNullOrEmpty(area.areaName) ? $"Layer_{index}" : $"{area.areaName}_Layer";
        GameObject tileObj = new GameObject(objName);
        tileObj.transform.SetParent(this.transform);

        // Z-Fighting 방지를 위한 미세 고도 오프셋 적용
        tileObj.transform.localPosition = new Vector3(0, 0.0001f * (index + 1), 0);
        tileObj.transform.localRotation = Quaternion.identity;
        tileObj.transform.localScale = Vector3.one;

        MeshFilter mf = tileObj.AddComponent<MeshFilter>();
        MeshRenderer mr = tileObj.AddComponent<MeshRenderer>();
        Mesh mesh = new Mesh();

        Vector3[] vertices = new Vector3[4];
        Vector2[] uv = new Vector2[4];

        for (int i = 0; i < 4; i++)
        {
            vertices[i] = GridToLocalPos(area.points[i]);

            // 텍스처 왜곡 방지를 위해 절대 좌표 기반 UV 매핑 적용
            float u = (float)(area.points[i].x - 1) / gridWidth;
            float v = 1.0f - ((float)(area.points[i].y - 1) / gridHeight);

            uv[i] = new Vector2(u, v);
        }

        mesh.vertices = vertices;
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = uv;
        mesh.RecalculateNormals();

        mf.mesh = mesh;

        Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));

        // 동일 고도에서의 렌더링 깜빡임(Z-Fighting)을 방지하기 위한 렌더 큐 명시적 할당
        mat.renderQueue = 2000 + (index + 1);

        if (area.texture != null)
        {
            // 경계선 텍스처 번짐 방지를 위한 랩핑 모드 Clamp 설정
            area.texture.wrapMode = TextureWrapMode.Clamp;
            mat.mainTexture = area.texture;
            mat.enableInstancing = true;
        }
        mr.material = mat;
    }

    [ContextMenu("Clear Tiles")]
    public void ClearExistingTiles()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in transform)
        {
            if (child.name.Contains("_Layer"))
            {
                children.Add(child.gameObject);
            }
        }

        foreach (var child in children)
        {
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    // 그리드 인덱스를 유니티 월드(로컬) 좌표계로 변환
    Vector3 GridToLocalPos(Vector2Int gridPoint)
    {
        float xPos = (gridPoint.x - 1) * (planeSize / gridWidth) - (planeSize / 2f);
        float zPos = (planeSize / 2f) - (gridPoint.y - 1) * (planeSize / gridHeight);
        return new Vector3(xPos, 0, zPos);
    }
}