using UnityEngine;

// ==================================================

// 피격 연출용 머티리얼(번쩍, 조각). Resources/Materials 아래 에셋을 쓴다.
// 에셋은 에디터 메뉴(MushTaker/Battle/프리팹 다시 만들기)가 URP Unlit 흰색으로 만든다.
// Resources에 있는 머티리얼은 빌드에 셰이더와 함께 들어간다

public static class HitMaterials
{
    public const string ResourceDir = "Materials/";
    public const string FlashName = "HitFlash";
    public const string ChunkName = "HitChunk";

    private static Material s_Flash;
    private static Material s_Chunk;

    public static Material Flash => Load(ref s_Flash, FlashName);
    public static Material Chunk => Load(ref s_Chunk, ChunkName);

    // ==================================================

    private static Material Load(ref Material cache, string materialName)
    {
        if (cache != null)
            return cache;

        cache = Resources.Load<Material>(ResourceDir + materialName);
        if (cache == null)
            CLogger.Error("HitMaterials", $"Resources/{ResourceDir}{materialName} 머티리얼이 없습니다. 메뉴 MushTaker/Battle/프리팹 다시 만들기를 실행하세요.");

        return cache;
    }
}
