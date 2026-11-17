using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

// Pixel art import settings for everything in the HUD sprite folder,
// applied automatically whenever a sprite there is imported.
public class HudSpriteImporter : AssetPostprocessor
{
    public const string Folder = "Assets/Sprites/UI/HUD";

    void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(Folder + "/") == false) { return; }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        // 9-slice borders (left, bottom, right, top) for the stretchable sprites
        string name = Path.GetFileNameWithoutExtension(assetPath);
        if (name == "panel")
        {
            importer.spriteBorder = new Vector4(18f, 24f, 18f, 18f);
        }
        else if (name == "bar_frame")
        {
            importer.spriteBorder = new Vector4(6f, 9f, 6f, 6f);
        }
    }
}
