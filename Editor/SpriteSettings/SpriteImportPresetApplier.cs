using UnityEditor;
using UnityEngine;

/// <summary>SpriteImportPresetの内容をTextureImporterへ書き込む処理</summary>
public static class SpriteImportPresetApplier {
    /// <summary>
    /// 複数プリセットを順番に1つのimporterへ適用し、最後に1回だけ再インポートする。
    /// 後のプリセットほど優先(同じ項目は上書きされる)
    /// </summary>
    public static void ApplyToImporter(TextureImporter importer, SpriteImportPreset[] presets) {
        if (importer == null || presets == null || presets.Length == 0) return;

        foreach (var preset in presets) {
            if (preset == null) continue;
            ApplyFields(importer, preset);
        }

        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }

    static void ApplyFields(TextureImporter importer, SpriteImportPreset preset) {
        importer.textureType = preset.textureType;
        importer.spriteImportMode = preset.spriteMode;
        importer.spritePixelsPerUnit = preset.pixelsPerUnit;

        // Mesh Type/Extrude Edges/Pivot/Generate Physics ShapeはTextureImporterに直接のプロパティが無く、
        // TextureImporterSettings経由でしか読み書きできない
        var spriteSettings = new TextureImporterSettings();
        importer.ReadTextureSettings(spriteSettings);
        spriteSettings.spriteMeshType = preset.meshType;
        spriteSettings.spriteExtrude = preset.extrudeEdges;
        spriteSettings.spriteAlignment = (int)preset.pivot;
        if (preset.pivot == SpriteAlignment.Custom) spriteSettings.spritePivot = preset.customPivot;
        spriteSettings.spriteGenerateFallbackPhysicsShape = preset.generatePhysicsShape;
        importer.SetTextureSettings(spriteSettings);

        importer.sRGBTexture = preset.sRGBTexture;
        importer.alphaSource = preset.alphaSource;
        importer.alphaIsTransparency = preset.alphaIsTransparency;
        importer.isReadable = preset.readWriteEnabled;
        importer.mipmapEnabled = preset.generateMipMaps;

        importer.wrapMode = preset.wrapMode;
        importer.filterMode = preset.filterMode;
        importer.anisoLevel = preset.anisoLevel;

        var platformSettings = importer.GetDefaultPlatformTextureSettings();
        platformSettings.maxTextureSize = preset.maxSize;
        platformSettings.resizeAlgorithm = preset.resizeAlgorithm;
        platformSettings.format = preset.format;
        platformSettings.textureCompression = preset.compression;
        platformSettings.crunchedCompression = preset.useCrunchCompression;
        importer.SetPlatformTextureSettings(platformSettings);
    }
}
