using UnityEditor;
using UnityEngine;

// Assets/Art/UI/Sprites 아래 도트 스프라이트의 가져오기 설정을 맞춘다.
// 도트가 번지지 않게 Point·무압축, 캔버스에서 4배로 보이게 PPU 25, UI_ 틀은 9-slice 테두리를 넣는다
public class UISpriteImporter : AssetPostprocessor
{
    private const string SpriteDir = "Assets/Art/UI/Sprites/";

    // 갈무리 11px 격자를 4배로 쓰는 것과 맞춘다(캔버스 기준 PPU 100 / 4)
    private const float PixelsPerUnit = 25f;

    // ==================================================

    private void OnPreprocessTexture()
    {
        if (assetPath.StartsWith(SpriteDir) == false)
            return;

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = PixelsPerUnit;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spriteBorder = GetBorder(System.IO.Path.GetFileNameWithoutExtension(assetPath));
    }

    // 9-slice 테두리(왼, 아래, 오른, 위). 틀이 아닌 아이콘은 0
    private static Vector4 GetBorder(string fileName)
    {
        switch (fileName)
        {
            case "UI_Panel":
                return new Vector4(3, 4, 3, 3);
            case "UI_Button":
                return new Vector4(4, 5, 4, 3);
            case "UI_Button_Pressed":
                return new Vector4(4, 3, 4, 5);
            case "UI_Slot":
                return new Vector4(3, 3, 3, 3);
            case "UI_GaugeFrame":
                return new Vector4(3, 3, 3, 3);
            case "UI_GaugeFill":
                return new Vector4(0, 2, 0, 0);
            case "UI_SpeechBubble":
                // 꼬리가 왼쪽 아래 모서리(7x6) 안에 있어 늘려도 찌그러지지 않는다
                return new Vector4(7, 6, 3, 3);
            default:
                return Vector4.zero;
        }
    }
}
