
// ==================================================

// ProjectSettings/TagManager의 레이어 이름. 이름을 바꾸면 여기도 같이 바꾼다

public static class GameLayers
{
    public const string Actor = "Actor";       // 플레이어·몬스터 루트(CharacterController)
    public const string Terrain = "Terrain";   // 맵 지형(Map_Terrain)
    public const string Player = "Player";     // 플레이어 몸 렌더러만(루트는 Actor). 가려지면 실루엣으로 그린다(렌더러 기능 PlayerSilhouette)
    public const string Occluder = "Occluder"; // 보스 몸 렌더러만. 이 뒤에 가려진 플레이어만 실루엣으로 그린다(렌더러 기능 BossOccluder)
}
