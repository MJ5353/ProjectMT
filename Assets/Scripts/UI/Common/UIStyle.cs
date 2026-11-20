using UnityEngine;

// UI 공용 색과 글자 크기. 복셀 숲 톤(이끼 초록 바탕, 크림 글자, 버섯 빨강 강조)
// !!) 갈무리11은 11의 배수 크기에서 가장 또렷하다. 글자 크기는 아래 상수만 쓴다
public static class UIStyle
{
    // 기준 해상도(가로 고정)
    public static readonly Vector2 RefResolution = new Vector2(1920, 1080);

    // font size ---

    public const int FontSmall = 22;
    public const int FontNormal = 33;
    public const int FontLarge = 44;
    public const int FontTitle = 66;
    public const int FontHuge = 88;

    // color ---

    public static readonly Color Background = Hex(0x1E2A22);
    public static readonly Color Panel = Hex(0x2C3B30);
    public static readonly Color PanelLight = Hex(0x3F5244);
    public static readonly Color Dim = Hex(0x000000, 0.6f);

    public static readonly Color Text = Hex(0xF3E9D2);
    public static readonly Color TextDim = Hex(0xA9B39C);

    public static readonly Color Accent = Hex(0xD9573B);
    public static readonly Color AccentDark = Hex(0x9E3A26);
    public static readonly Color Gold = Hex(0xF2C14E);

    public static readonly Color HpPlayer = Hex(0x7BC950);
    public static readonly Color HpBoss = Hex(0xD9573B);
    public static readonly Color GaugeTrail = Hex(0xFFF4D6, 0.85f);

    public static readonly Color Positive = Hex(0x7BC950);
    public static readonly Color Negative = Accent;

    // rarity ---

    public static readonly Color RarityCommon = Hex(0xA9B39C);
    public static readonly Color RarityRare = Hex(0x4FA3E0);
    public static readonly Color RarityEpic = Hex(0xB45EE0);

    // ==================================================

    public static Color Hex(int rgb, float alpha = 1f)
    {
        float r = ((rgb >> 16) & 0xFF) / 255f;
        float g = ((rgb >> 8) & 0xFF) / 255f;
        float b = (rgb & 0xFF) / 255f;

        return new Color(r, g, b, alpha);
    }

    public static Color GetRarityColor(ERarity rarity)
    {
        return rarity switch
        {
            ERarity.Epic => RarityEpic,
            ERarity.Rare => RarityRare,
            _ => RarityCommon,
        };
    }

    // 리치 텍스트 색 태그
    public static string ColorTag(Color color, string text)
    {
        return $"<color=#{ColorUtility.ToHtmlStringRGB(color)}>{text}</color>";
    }
}
