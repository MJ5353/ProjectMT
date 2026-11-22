using UnityEngine;

// ==================================================

// 피격 연출용 머티리얼(번쩍, 조각). Resources/Materials 아래 에셋을 쓴다(경로는 CommonPath).
// 에셋은 MushTaker/초기 설정/누락 전투 에셋 복구에서 만든다.
// Resources에 있는 머티리얼은 빌드에 셰이더와 함께 들어간다

// 로드·캐시는 ResourceManager가 한다

public static class HitMaterials
{
    private const string MissingHint = "MushTaker/초기 설정/누락 전투 에셋 복구를 실행하세요.";

    public static Material Flash => ResourceManager.Instance.Load<Material>(CommonPath.HitFlashMaterial, MissingHint);
    public static Material Chunk => ResourceManager.Instance.Load<Material>(CommonPath.HitChunkMaterial, MissingHint);
}
