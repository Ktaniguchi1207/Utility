using System;
using UnityEngine;
using UnityEngine.UI;

public class TiledBorderImage : MaskableGraphic {
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
    [SerializeField] bool fillCenter = true;

    public Sprite SourceSprite {
        get => sourceSprite;
        set { sourceSprite = value; SetAllDirty(); }
    }

    public TileRegion HorizontalTile => horizontalTile;
    public TileRegion VerticalTile => verticalTile;

    public bool FillCenter {
        get => fillCenter;
        set { fillCenter = value; SetAllDirty(); }
    }

    public override Texture mainTexture =>
        sourceSprite != null ? sourceSprite.texture : base.mainTexture;

    float multipliedPixelsPerUnit {
        get {
            float ppu = 100f;
            if (sourceSprite != null) ppu = sourceSprite.pixelsPerUnit;
            float refPpu = 100f;
            if (canvas != null) refPpu = canvas.referencePixelsPerUnit;
            return ppu / refPpu;
        }
    }

    protected override void OnPopulateMesh(VertexHelper vh) {
        vh.Clear();

        if (sourceSprite == null) return;

        Texture2D tex = sourceSprite.texture;
        Rect sr = sourceSprite.textureRect;
        float mPpu = multipliedPixelsPerUnit;
        float texW = tex.width;
        float texH = tex.height;

        int sprW = (int)sr.width;
        int sprH = (int)sr.height;

        int hS = Mathf.Clamp(horizontalTile.start, 0, sprW);
        int hE = Mathf.Clamp(horizontalTile.end, hS, sprW);
        int vS = Mathf.Clamp(verticalTile.start, 0, sprH);
        int vE = Mathf.Clamp(verticalTile.end, vS, sprH);

        float[] uvX = {
            sr.x / texW,
            (sr.x + hS) / texW,
            (sr.x + hE) / texW,
            (sr.x + sprW) / texW
        };
        float[] uvY = {
            sr.y / texH,
            (sr.y + vS) / texH,
            (sr.y + vE) / texH,
            (sr.y + sprH) / texH
        };

        Rect rect = GetPixelAdjustedRect();

        float leftW = hS / mPpu;
        float midSrcW = (hE - hS) / mPpu;
        float rightW = (sprW - hE) / mPpu;
        float bottomH = vS / mPpu;
        float midSrcH = (vE - vS) / mPpu;
        float topH = (sprH - vE) / mPpu;

        float minW = leftW + rightW;
        float minH = bottomH + topH;
        float scaleW = minW > 0 && rect.width < minW ? rect.width / minW : 1f;
        float scaleH = minH > 0 && rect.height < minH ? rect.height / minH : 1f;

        leftW *= scaleW;
        rightW *= scaleW;
        midSrcW *= scaleW;
        bottomH *= scaleH;
        topH *= scaleH;
        midSrcH *= scaleH;

        float[] posX = { rect.x, rect.x + leftW, rect.xMax - rightW, rect.xMax };
        float[] posY = { rect.y, rect.y + bottomH, rect.yMax - topH, rect.yMax };

        Color32 col32 = color;

        const int MAX_QUADS = 16000;
        float effectiveMidSrcW = midSrcW;
        float effectiveMidSrcH = midSrcH;

        int estimatedQuads = EstimateTotalQuads(posX, posY, midSrcW, midSrcH, fillCenter);
        if (estimatedQuads > MAX_QUADS && estimatedQuads > 0) {
            float scale = Mathf.Sqrt((float)estimatedQuads / MAX_QUADS);
            if (effectiveMidSrcW > 0.0001f) effectiveMidSrcW *= scale;
            if (effectiveMidSrcH > 0.0001f) effectiveMidSrcH *= scale;
        }

        for (int row = 0 ; row < 3 ; row++) {
            for (int col = 0 ; col < 3 ; col++) {
                if (row == 1 && col == 1 && !fillCenter) continue;

                float regionW = posX[col + 1] - posX[col];
                float regionH = posY[row + 1] - posY[row];
                if (regionW <= 0.0001f || regionH <= 0.0001f) continue;

                bool tileX = col == 1 && effectiveMidSrcW > 0.0001f;
                bool tileY = row == 1 && effectiveMidSrcH > 0.0001f;

                if (!tileX && !tileY) {
                    AddQuad(vh, posX[col], posY[row], regionW, regionH,
                        uvX[col], uvY[row], uvX[col + 1], uvY[row + 1], col32);
                }
                else if (tileX && !tileY) {
                    EmitTiledX(vh, posX[col], posY[row], regionW, regionH, effectiveMidSrcW,
                        uvX[1], uvY[row], uvX[2], uvY[row + 1], col32);
                }
                else if (!tileX && tileY) {
                    EmitTiledY(vh, posX[col], posY[row], regionW, regionH, effectiveMidSrcH,
                        uvX[col], uvY[1], uvX[col + 1], uvY[2], col32);
                }
                else {
                    EmitTiledXY(vh, posX[col], posY[row], regionW, regionH,
                        effectiveMidSrcW, effectiveMidSrcH,
                        uvX[1], uvY[1], uvX[2], uvY[2], col32);
                }
            }
        }
    }
    int EstimateTotalQuads(float[] posX, float[] posY, float srcW, float srcH, bool center) {
        int total = 0;
        for (int row = 0 ; row < 3 ; row++) {
            for (int col = 0 ; col < 3 ; col++) {
                if (row == 1 && col == 1 && !center) continue;

                float rw = posX[col + 1] - posX[col];
                float rh = posY[row + 1] - posY[row];
                if (rw <= 0.0001f || rh <= 0.0001f) continue;

                bool tx = col == 1 && srcW > 0.0001f;
                bool ty = row == 1 && srcH > 0.0001f;

                if (!tx && !ty) total += 1;
                else if (tx && !ty) total += Mathf.CeilToInt(rw / srcW);
                else if (!tx && ty) total += Mathf.CeilToInt(rh / srcH);
                else total += Mathf.CeilToInt(rw / srcW) * Mathf.CeilToInt(rh / srcH);
            }
        }
        return total;
    }
    void AddQuad(VertexHelper vh, float x, float y, float w, float h,
        float uL, float vB, float uR, float vT, Color32 col) {
        int i = vh.currentVertCount;
        vh.AddVert(new Vector3(x, y), col, new Vector2(uL, vB));
        vh.AddVert(new Vector3(x + w, y), col, new Vector2(uR, vB));
        vh.AddVert(new Vector3(x + w, y + h), col, new Vector2(uR, vT));
        vh.AddVert(new Vector3(x, y + h), col, new Vector2(uL, vT));
        vh.AddTriangle(i, i + 2, i + 1);
        vh.AddTriangle(i, i + 3, i + 2);
    }

    void EmitTiledX(VertexHelper vh, float sx, float sy, float totalW, float h, float srcW,
        float uL, float vB, float uR, float vT, Color32 col) {
        float remaining = totalW;
        float cx = sx;
        while (remaining > 0.0001f) {
            float tw = Mathf.Min(remaining, srcW);
            AddQuad(vh, cx, sy, tw, h,
                uL, vB, Mathf.Lerp(uL, uR, tw / srcW), vT, col);
            cx += tw;
            remaining -= tw;
        }
    }

    void EmitTiledY(VertexHelper vh, float sx, float sy, float w, float totalH, float srcH,
        float uL, float vB, float uR, float vT, Color32 col) {
        float remaining = totalH;
        float cy = sy;
        while (remaining > 0.0001f) {
            float th = Mathf.Min(remaining, srcH);
            AddQuad(vh, sx, cy, w, th,
                uL, vB, uR, Mathf.Lerp(vB, vT, th / srcH), col);
            cy += th;
            remaining -= th;
        }
    }

    void EmitTiledXY(VertexHelper vh, float sx, float sy, float totalW, float totalH,
        float srcW, float srcH,
        float uL, float vB, float uR, float vT, Color32 col) {
        float ry = totalH;
        float cy = sy;
        while (ry > 0.0001f) {
            float th = Mathf.Min(ry, srcH);
            float fracY = th / srcH;
            float rx = totalW;
            float cx = sx;
            while (rx > 0.0001f) {
                float tw = Mathf.Min(rx, srcW);
                AddQuad(vh, cx, cy, tw, th,
                    uL, vB,
                    Mathf.Lerp(uL, uR, tw / srcW),
                    Mathf.Lerp(vB, vT, fracY), col);
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
        SetAllDirty();
    }

#if UNITY_EDITOR
    protected override void OnValidate() {
        base.OnValidate();
        SetAllDirty();
    }
#endif
}
