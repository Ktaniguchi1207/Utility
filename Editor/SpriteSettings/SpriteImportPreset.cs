using UnityEditor;
using UnityEngine;

/// <summary>
/// Texture2D(Sprite)のImport Settingsを一括適用するためのプリセット。
/// Tools/Sprite Settings で作成・編集し、Inspector右クリックの「Apply Settings」から
/// 選択中のテクスチャに適用する
/// </summary>
public class SpriteImportPreset : ScriptableObject {
    public TextureImporterType textureType = TextureImporterType.Sprite;

    [Header("Sprite")]
    public SpriteImportMode spriteMode = SpriteImportMode.Single;
    public float pixelsPerUnit = 100f;
    public SpriteMeshType meshType = SpriteMeshType.Tight;
    [Range(0, 32)] public uint extrudeEdges = 1;
    public SpriteAlignment pivot = SpriteAlignment.Center;
    public Vector2 customPivot = new Vector2(0.5f, 0.5f);
    public bool generatePhysicsShape = false;

    [Header("Advanced")]
    public bool sRGBTexture = true;
    public TextureImporterAlphaSource alphaSource = TextureImporterAlphaSource.FromInput;
    public bool alphaIsTransparency = true;
    public bool readWriteEnabled = false;
    public bool generateMipMaps = false;

    [Header("Filter")]
    public TextureWrapMode wrapMode = TextureWrapMode.Clamp;
    public FilterMode filterMode = FilterMode.Point;
    [Range(0, 16)] public int anisoLevel = 1;

    [Header("Default Platform")]
    public int maxSize = 2048;
    public TextureResizeAlgorithm resizeAlgorithm = TextureResizeAlgorithm.Mitchell;
    public TextureImporterFormat format = TextureImporterFormat.Automatic;
    public TextureImporterCompression compression = TextureImporterCompression.Compressed;
    public bool useCrunchCompression = false;
}
