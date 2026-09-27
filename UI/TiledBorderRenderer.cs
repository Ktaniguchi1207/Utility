using System;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class TiledBorderRenderer : MonoBehaviour {
    [Serializable]
    public class TileRegion {
        [Min(0)] public int start;
        [Min(0)] public int end;

        public TileRegion(int start, int end) {
            this.start = start;
            this.end = end;
        }

        public int Length => Mathf.Max(0, end - start);
    }

    [SerializeField] Sprite sourceSprite;

    [Header("Tile Regions (px)")]
    [SerializeField] TileRegion horizontalTile = new TileRegion(32, 96);
    [SerializeField] TileRegion verticalTile = new TileRegion(32, 96);

    [Header("Display")]
    [SerializeField] Vector2 size = new Vector2(3f, 3f);
    [SerializeField] bool fillCenter;
    [SerializeField] Color color = Color.white;

    [Header("Sorting")]
    [SerializeField] int sortingOrder;
    [SerializeField] string sortingLayerName = "Default";

    static readonly Dictionary<Texture2D, Material> sharedMaterialCache = new();

    MeshFilter cachedMeshFilter;
    MeshRenderer cachedMeshRenderer;
    Mesh generatedMesh;
    MaterialPropertyBlock cachedPropertyBlock;

    readonly List<Vector3> verts = new(64);
    readonly List<Vector2> uvs = new(64);
    readonly List<int> tris = new(96);
    readonly List<Color> colors = new(64);

#if UNITY_EDITOR
    Sprite lastSprite;
    int lastHStart, lastHEnd, lastVStart, lastVEnd;
    Vector2 lastSize;
    bool lastFillCenter;
    Color lastColor;
    int lastSortingOrder;
    string lastSortingLayerName;
#endif

    public Sprite SourceSprite {
        get => sourceSprite;
        set { sourceSprite = value; RebuildMesh(); }
    }

    public TileRegion HorizontalTile => horizontalTile;
    public TileRegion VerticalTile => verticalTile;

    public Vector2 Size {
        get => size;
        set { size = value; RebuildMesh(); }
    }

    void OnEnable() {
        cachedMeshFilter = GetComponent<MeshFilter>();
        cachedMeshRenderer = GetComponent<MeshRenderer>();
        RebuildMesh();
    }

    void OnDisable() {
        if (generatedMesh != null) {
            if (Application.isPlaying) Destroy(generatedMesh);
            else DestroyImmediate(generatedMesh);
        }
        if (cachedMeshFilter != null)
            cachedMeshFilter.sharedMesh = null;
    }

#if UNITY_EDITOR
    void Update() {
        if (!Application.isPlaying && NeedsRebuild())
            RebuildMesh();
    }

    bool NeedsRebuild() {
        return sourceSprite != lastSprite
            || size != lastSize
            || fillCenter != lastFillCenter
            || color != lastColor
            || sortingOrder != lastSortingOrder
            || sortingLayerName != lastSortingLayerName
            || horizontalTile.start != lastHStart
            || horizontalTile.end != lastHEnd
            || verticalTile.start != lastVStart
            || verticalTile.end != lastVEnd;
    }

    void CacheState() {
        lastSprite = sourceSprite;
        lastSize = size;
        lastFillCenter = fillCenter;
        lastColor = color;
        lastSortingOrder = sortingOrder;
        lastSortingLayerName = sortingLayerName;
        lastHStart = horizontalTile.start;
        lastHEnd = horizontalTile.end;
        lastVStart = verticalTile.start;
        lastVEnd = verticalTile.end;
    }
#endif

    public void RebuildMesh() {
#if UNITY_EDITOR
        CacheState();
#endif

        if (cachedMeshFilter == null) cachedMeshFilter = GetComponent<MeshFilter>();
        if (cachedMeshRenderer == null) cachedMeshRenderer = GetComponent<MeshRenderer>();

        if (sourceSprite == null) {
            if (generatedMesh != null) generatedMesh.Clear();
            return;
        }

        if (generatedMesh == null)
            generatedMesh = new Mesh { name = "TiledBorder" };
        generatedMesh.Clear();

        Texture2D tex = sourceSprite.texture;
        Rect sr = sourceSprite.textureRect;
        float ppu = sourceSprite.pixelsPerUnit;
        float texW = tex.width;
        float texH = tex.height;

        int sprW = (int)sr.width;
        int sprH = (int)sr.height;

        int hS = Mathf.Clamp(horizontalTile.start, 0, sprW);
        int hE = Mathf.Clamp(horizontalTile.end, hS, sprW);
        int vS = Mathf.Clamp(verticalTile.start, 0, sprH);
        int vE = Mathf.Clamp(verticalTile.end, vS, sprH);

        float[] uvX =
        {
            sr.x / texW,
            (sr.x + hS) / texW,
            (sr.x + hE) / texW,
            (sr.x + sprW) / texW
        };
        float[] uvY =
        {
            sr.y / texH,
            (sr.y + vS) / texH,
            (sr.y + vE) / texH,
            (sr.y + sprH) / texH
        };

        float leftW = hS / ppu;
        float midSrcW = (hE - hS) / ppu;
        float rightW = (sprW - hE) / ppu;
        float bottomH = vS / ppu;
        float midSrcH = (vE - vS) / ppu;
        float topH = (sprH - vE) / ppu;

        float minW = leftW + rightW;
        float minH = bottomH + topH;
        float clampedW = Mathf.Max(size.x, minW + 0.001f);
        float clampedH = Mathf.Max(size.y, minH + 0.001f);
        float halfW = clampedW / 2f;
        float halfH = clampedH / 2f;

        float[] posX = { -halfW, -halfW + leftW, halfW - rightW, halfW };
        float[] posY = { -halfH, -halfH + bottomH, halfH - topH, halfH };

        verts.Clear();
        uvs.Clear();
        tris.Clear();
        colors.Clear();

        for (int row = 0 ; row < 3 ; row++) {
            for (int col = 0 ; col < 3 ; col++) {
                if (row == 1 && col == 1 && !fillCenter) continue;

                float regionW = posX[col + 1] - posX[col];
                float regionH = posY[row + 1] - posY[row];
                if (regionW <= 0.0001f || regionH <= 0.0001f) continue;

                bool tileX = col == 1 && midSrcW > 0.0001f;
                bool tileY = row == 1 && midSrcH > 0.0001f;

                if (!tileX && !tileY) {
                    AddQuad(posX[col], posY[row], regionW, regionH,
                        uvX[col], uvY[row], uvX[col + 1], uvY[row + 1]);
                }
                else if (tileX && !tileY) {
                    EmitTiledX(posX[col], posY[row], regionW, regionH, midSrcW,
                        uvX[1], uvY[row], uvX[2], uvY[row + 1]);
                }
                else if (!tileX && tileY) {
                    EmitTiledY(posX[col], posY[row], regionW, regionH, midSrcH,
                        uvX[col], uvY[1], uvX[col + 1], uvY[2]);
                }
                else {
                    EmitTiledXY(posX[col], posY[row], regionW, regionH,
                        midSrcW, midSrcH,
                        uvX[1], uvY[1], uvX[2], uvY[2]);
                }
            }
        }

        generatedMesh.SetVertices(verts);
        generatedMesh.SetUVs(0, uvs);
        generatedMesh.SetTriangles(tris, 0);
        generatedMesh.SetColors(colors);
        generatedMesh.RecalculateBounds();

        cachedMeshFilter.sharedMesh = generatedMesh;
        EnsureMaterial();
        ApplySorting();
    }

    void EnsureMaterial() {
        Texture2D tex = sourceSprite.texture;

        if (cachedMeshRenderer.sharedMaterial == null ||
            cachedMeshRenderer.sharedMaterial.mainTexture != tex) {
            if (!sharedMaterialCache.TryGetValue(tex, out Material mat)) {
                mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = tex };
                sharedMaterialCache[tex] = mat;
            }
            cachedMeshRenderer.sharedMaterial = mat;
        }

        if (cachedPropertyBlock == null)
            cachedPropertyBlock = new MaterialPropertyBlock();
        cachedMeshRenderer.GetPropertyBlock(cachedPropertyBlock);
        cachedPropertyBlock.SetColor("_Color", color);
        cachedMeshRenderer.SetPropertyBlock(cachedPropertyBlock);
    }

    void ApplySorting() {
        cachedMeshRenderer.sortingOrder = sortingOrder;
        cachedMeshRenderer.sortingLayerName = sortingLayerName;
    }

    void AddQuad(float x, float y, float w, float h,
        float uL, float vB, float uR, float vT) {
        int i = verts.Count;
        verts.Add(new Vector3(x, y, 0));
        verts.Add(new Vector3(x + w, y, 0));
        verts.Add(new Vector3(x + w, y + h, 0));
        verts.Add(new Vector3(x, y + h, 0));
        uvs.Add(new Vector2(uL, vB));
        uvs.Add(new Vector2(uR, vB));
        uvs.Add(new Vector2(uR, vT));
        uvs.Add(new Vector2(uL, vT));
        tris.Add(i); tris.Add(i + 2); tris.Add(i + 1);
        tris.Add(i); tris.Add(i + 3); tris.Add(i + 2);
        colors.Add(color); colors.Add(color); colors.Add(color); colors.Add(color);
    }

    void EmitTiledX(float sx, float sy, float totalW, float h, float srcW,
        float uL, float vB, float uR, float vT) {
        float remaining = totalW;
        float cx = sx;
        while (remaining > 0.0001f) {
            float tw = Mathf.Min(remaining, srcW);
            AddQuad(cx, sy, tw, h,
                uL, vB, Mathf.Lerp(uL, uR, tw / srcW), vT);
            cx += tw;
            remaining -= tw;
        }
    }

    void EmitTiledY(float sx, float sy, float w, float totalH, float srcH,
        float uL, float vB, float uR, float vT) {
        float remaining = totalH;
        float cy = sy;
        while (remaining > 0.0001f) {
            float th = Mathf.Min(remaining, srcH);
            AddQuad(sx, cy, w, th,
                uL, vB, uR, Mathf.Lerp(vB, vT, th / srcH));
            cy += th;
            remaining -= th;
        }
    }

    void EmitTiledXY(float sx, float sy, float totalW, float totalH, float srcW, float srcH,
        float uL, float vB, float uR, float vT) {
        float ry = totalH;
        float cy = sy;
        while (ry > 0.0001f) {
            float th = Mathf.Min(ry, srcH);
            float fracY = th / srcH;
            float rx = totalW;
            float cx = sx;
            while (rx > 0.0001f) {
                float tw = Mathf.Min(rx, srcW);
                AddQuad(cx, cy, tw, th,
                    uL, vB,
                    Mathf.Lerp(uL, uR, tw / srcW),
                    Mathf.Lerp(vB, vT, fracY));
                cx += tw;
                rx -= tw;
            }
            cy += th;
            ry -= th;
        }
    }

    public void AutoDetectRegions() {
        if (sourceSprite == null) return;
        int w = (int)sourceSprite.textureRect.width;
        int h = (int)sourceSprite.textureRect.height;
        horizontalTile.start = Mathf.RoundToInt(w * 0.25f);
        horizontalTile.end = Mathf.RoundToInt(w * 0.75f);
        verticalTile.start = Mathf.RoundToInt(h * 0.25f);
        verticalTile.end = Mathf.RoundToInt(h * 0.75f);
    }
}
