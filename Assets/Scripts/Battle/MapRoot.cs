using UnityEngine;

// 맵 프리팹 루트. 스폰 지점은 MapPrefabBuilder가 구울 때 채워서 런타임에 찾지 않는다
public sealed class MapRoot : MonoBehaviour
{
    [SerializeField] private Transform[] m_SpawnSlots = null; // [i] = SpawnSlot_{i+1}. 빠진 번호는 null

    public Transform[] SpawnSlots => m_SpawnSlots;

    // ------------------------------------------------

#if UNITY_EDITOR
    public void EditorSetSpawnSlots(Transform[] spawnSlots) => m_SpawnSlots = spawnSlots;
#endif
}
