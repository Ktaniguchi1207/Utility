using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TiledBorderImage))]
public class TiledBorderImageEditor : Editor {
    SerializedProperty sourceSpriteProp;
    SerializedProperty horizontalTileProp;
    SerializedProperty verticalTileProp;
    SerializedProperty fillCenterProp;
    SerializedProperty colorProp;
    SerializedProperty raycastTargetProp;
    SerializedProperty maskableProp;

    bool showPreview = true;
    int draggingHandle = -1;

    const float PREVIEW_SIZE = 256f;
    const float HANDLE_WIDTH = 8f;

    static readonly Color handleColorH = new Color(1f, 0.3f, 0.3f, 0.7f);
    static readonly Color handleColorV = new Color(0.3f, 0.5f, 1f, 0.7f);
    static readonly Color handleColorHHover = new Color(1f, 0.5f, 0.5f, 0.9f);
    static readonly Color handleColorVHover = new Color(0.5f, 0.7f, 1f, 0.9f);
    static readonly Color regionColorH = new Color(1f, 0.3f, 0.3f, 0.06f);
    static readonly Color regionColorV = new Color(0.3f, 0.5f, 1f, 0.06f);

    static readonly string[] regionLabels = {
        "Fixed", "Tile Y", "Fixed",
        "Tile X", "Tile XY", "Tile X",
        "Fixed", "Tile Y", "Fixed"
    };

    static readonly Color[] regionLabelColors = {
        Color.gray, handleColorV, Color.gray,
        handleColorH, Color.green, handleColorH,
        Color.gray, handleColorV, Color.gray
    };

    void OnEnable() {
        sourceSpriteProp = serializedObject.FindProperty("sourceSprite");
        horizontalTileProp = serializedObject.FindProperty("horizontalTile");
        verticalTileProp = serializedObject.FindProperty("verticalTile");
        fillCenterProp = serializedObject.FindProperty("fillCenter");
        colorProp = serializedObject.FindProperty("m_Color");
        raycastTargetProp = serializedObject.FindProperty("m_RaycastTarget");
        maskableProp = serializedObject.FindProperty("m_Maskable");
    }

    public override void OnInspectorGUI() {
        serializedObject.Update();

        EditorGUILayout.PropertyField(sourceSpriteProp);
        EditorGUILayout.Space(4);

        EditorGUILayout.PropertyField(fillCenterProp);
        EditorGUILayout.PropertyField(colorProp);
        EditorGUILayout.PropertyField(raycastTargetProp);
        EditorGUILayout.PropertyField(maskableProp);
        EditorGUILayout.Space(8);

        DrawTileRegionFields();
        EditorGUILayout.Space(8);

        showPreview = EditorGUILayout.Foldout(showPreview, "Region Preview (Drag Handles)", true);
        if (showPreview)
            DrawInteractivePreview();

        EditorGUILayout.Space(4);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Auto Detect (25%/75%)")) {
            Undo.RecordObject(target, "Auto Detect Regions");
            ((TiledBorderImage)target).AutoDetectRegions();
            serializedObject.Update();
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }

    void DrawTileRegionFields() {
        EditorGUILayout.LabelField("Horizontal Tile Region (px)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUI.color = handleColorH;
        EditorGUILayout.LabelField("■", GUILayout.Width(14));
        GUI.color = Color.white;
        EditorGUILayout.PropertyField(
            horizontalTileProp.FindPropertyRelative("start"), new GUIContent("Start"));
        EditorGUILayout.PropertyField(
            horizontalTileProp.FindPropertyRelative("end"), new GUIContent("End"));
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        EditorGUILayout.LabelField("Vertical Tile Region (px)", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        GUI.color = handleColorV;
        EditorGUILayout.LabelField("■", GUILayout.Width(14));
        GUI.color = Color.white;
        EditorGUILayout.PropertyField(
            verticalTileProp.FindPropertyRelative("start"), new GUIContent("Start"));
        EditorGUILayout.PropertyField(
            verticalTileProp.FindPropertyRelative("end"), new GUIContent("End"));
        EditorGUILayout.EndHorizontal();
    }

    void DrawInteractivePreview() {
        TiledBorderImage renderer = (TiledBorderImage)target;
        Sprite sprite = renderer.SourceSprite;
        if (sprite == null) {
            EditorGUILayout.HelpBox("Spriteを設定するとプレビューが表示されます", MessageType.Info);
            return;
        }

        Rect sr = sprite.textureRect;
        int sprW = (int)sr.width;
        int sprH = (int)sr.height;

        float aspect = (float)sprW / sprH;
        float previewW, previewH;
        if (aspect >= 1f) {
            previewW = PREVIEW_SIZE;
            previewH = PREVIEW_SIZE / aspect;
        }
        else {
            previewH = PREVIEW_SIZE;
            previewW = PREVIEW_SIZE * aspect;
        }

        Rect previewRect = GUILayoutUtility.GetRect(previewW + 40, previewH + 40);
        previewRect.x += 20;
        previewRect.y += 10;
        previewRect.width = previewW;
        previewRect.height = previewH;

        Texture2D tex = sprite.texture;
        Rect uvRect = new Rect(sr.x / tex.width, sr.y / tex.height,
            sr.width / tex.width, sr.height / tex.height);
        GUI.DrawTextureWithTexCoords(previewRect, tex, uvRect);

        float scaleX = previewW / sprW;
        float scaleY = previewH / sprH;

        int hS = renderer.HorizontalTile.start;
        int hE = renderer.HorizontalTile.end;
        int vS = renderer.VerticalTile.start;
        int vE = renderer.VerticalTile.end;

        float hStartX = previewRect.x + hS * scaleX;
        float hEndX = previewRect.x + hE * scaleX;
        float vStartY = previewRect.yMax - vS * scaleY;
        float vEndY = previewRect.yMax - vE * scaleY;

        EditorGUI.DrawRect(new Rect(hStartX, previewRect.y, hEndX - hStartX, previewH), regionColorH);
        EditorGUI.DrawRect(new Rect(previewRect.x, vEndY, previewW, vStartY - vEndY), regionColorV);

        Rect handleHStart = new Rect(hStartX - HANDLE_WIDTH / 2, previewRect.y, HANDLE_WIDTH, previewH);
        Rect handleHEnd = new Rect(hEndX - HANDLE_WIDTH / 2, previewRect.y, HANDLE_WIDTH, previewH);
        Rect handleVStart = new Rect(previewRect.x, vStartY - HANDLE_WIDTH / 2, previewW, HANDLE_WIDTH);
        Rect handleVEnd = new Rect(previewRect.x, vEndY - HANDLE_WIDTH / 2, previewW, HANDLE_WIDTH);

        Event e = Event.current;
        int hoveredHandle = GetHoveredHandle(e.mousePosition, handleHStart, handleHEnd, handleVStart, handleVEnd);

        EditorGUI.DrawRect(handleHStart, hoveredHandle == 0 || draggingHandle == 0 ? handleColorHHover : handleColorH);
        EditorGUI.DrawRect(handleHEnd, hoveredHandle == 1 || draggingHandle == 1 ? handleColorHHover : handleColorH);
        EditorGUI.DrawRect(handleVStart, hoveredHandle == 2 || draggingHandle == 2 ? handleColorVHover : handleColorV);
        EditorGUI.DrawRect(handleVEnd, hoveredHandle == 3 || draggingHandle == 3 ? handleColorVHover : handleColorV);

        if (hoveredHandle >= 0)
            EditorGUIUtility.AddCursorRect(
                hoveredHandle < 2 ? (hoveredHandle == 0 ? handleHStart : handleHEnd) : (hoveredHandle == 2 ? handleVStart : handleVEnd),
                hoveredHandle < 2 ? MouseCursor.ResizeHorizontal : MouseCursor.ResizeVertical);

        if (e.type == EventType.MouseDown && e.button == 0 && hoveredHandle >= 0) {
            draggingHandle = hoveredHandle;
            e.Use();
        }
        else if (e.type == EventType.MouseDrag && draggingHandle >= 0) {
            Undo.RecordObject(target, "Adjust Tile Region");

            if (draggingHandle == 0) {
                int px = Mathf.Clamp(Mathf.RoundToInt((e.mousePosition.x - previewRect.x) / scaleX), 0, hE);
                renderer.HorizontalTile.start = px;
            }
            else if (draggingHandle == 1) {
                int px = Mathf.Clamp(Mathf.RoundToInt((e.mousePosition.x - previewRect.x) / scaleX), hS, sprW);
                renderer.HorizontalTile.end = px;
            }
            else if (draggingHandle == 2) {
                int px = Mathf.Clamp(Mathf.RoundToInt((previewRect.yMax - e.mousePosition.y) / scaleY), 0, vE);
                renderer.VerticalTile.start = px;
            }
            else if (draggingHandle == 3) {
                int px = Mathf.Clamp(Mathf.RoundToInt((previewRect.yMax - e.mousePosition.y) / scaleY), vS, sprH);
                renderer.VerticalTile.end = px;
            }

            serializedObject.Update();
            renderer.SetAllDirty();
            e.Use();
            Repaint();
        }
        else if (e.type == EventType.MouseUp && draggingHandle >= 0) {
            draggingHandle = -1;
            e.Use();
        }

        DrawRegionLabels(previewRect, hStartX, hEndX, vStartY, vEndY, scaleX, scaleY);
        DrawDimensionLabels(previewRect, hS, hE, vS, vE, sprW, sprH, scaleX, scaleY);

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox(
            "赤ハンドル = 横方向タイル範囲（ドラッグで調整）\n" +
            "青ハンドル = 縦方向タイル範囲（ドラッグで調整）\n\n" +
            "四隅: 固定（タイルなし）\n" +
            "上辺・下辺: X方向にタイル\n" +
            "左辺・右辺: Y方向にタイル\n" +
            "中央: XY両方にタイル（Fill Center有効時）",
            MessageType.None);
    }

    void DrawRegionLabels(Rect pr, float hStartX, float hEndX, float vStartY, float vEndY,
        float scaleX, float scaleY) {
        GUIStyle style = new GUIStyle(EditorStyles.miniLabel) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 9,
            fontStyle = FontStyle.Bold
        };

        float[] colCenters = {
            (pr.x + hStartX) / 2f,
            (hStartX + hEndX) / 2f,
            (hEndX + pr.xMax) / 2f
        };
        float[] rowCenters = {
            (vStartY + pr.yMax) / 2f,
            (vEndY + vStartY) / 2f,
            (pr.y + vEndY) / 2f
        };

        for (int row = 0 ; row < 3 ; row++) {
            for (int col = 0 ; col < 3 ; col++) {
                int idx = row * 3 + col;
                style.normal.textColor = regionLabelColors[idx];
                Rect labelRect = new Rect(colCenters[col] - 24, rowCenters[row] - 7, 48, 14);
                GUI.Label(labelRect, regionLabels[idx], style);
            }
        }
    }

    void DrawDimensionLabels(Rect pr, int hS, int hE, int vS, int vE,
        int sprW, int sprH, float scaleX, float scaleY) {
        GUIStyle dimStyle = new GUIStyle(EditorStyles.miniLabel) {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 8
        };

        float hStartX = pr.x + hS * scaleX;
        float hEndX = pr.x + hE * scaleX;
        float vStartY = pr.yMax - vS * scaleY;
        float vEndY = pr.yMax - vE * scaleY;

        dimStyle.normal.textColor = new Color(1f, 0.4f, 0.4f, 1f);
        GUI.Label(new Rect(hStartX - 15, pr.yMax + 2, 30, 12), hS.ToString(), dimStyle);
        GUI.Label(new Rect(hEndX - 15, pr.yMax + 2, 30, 12), hE.ToString(), dimStyle);

        dimStyle.normal.textColor = new Color(0.4f, 0.6f, 1f, 1f);
        GUI.Label(new Rect(pr.xMax + 2, vStartY - 6, 30, 12), vS.ToString(), dimStyle);
        GUI.Label(new Rect(pr.xMax + 2, vEndY - 6, 30, 12), vE.ToString(), dimStyle);
    }

    int GetHoveredHandle(Vector2 mouse, Rect h0, Rect h1, Rect v0, Rect v1) {
        Rect expanded;
        expanded = Expand(h0, 4); if (expanded.Contains(mouse)) return 0;
        expanded = Expand(h1, 4); if (expanded.Contains(mouse)) return 1;
        expanded = Expand(v0, 4); if (expanded.Contains(mouse)) return 2;
        expanded = Expand(v1, 4); if (expanded.Contains(mouse)) return 3;
        return -1;
    }

    Rect Expand(Rect r, float px) {
        return new Rect(r.x - px, r.y - px, r.width + px * 2, r.height + px * 2);
    }
}
