using UnityEngine;

// ==================================================

// UI 칸·줄처럼 RectTransform 아래로 꺼내 쓰는 PoolableMono. ResourceManager.Spawn(prefab, parent)로 꺼낸다.
// 다시 꺼낼 때 연출이 바꿔 둔 크기·회전만 되돌린다. 위치·크기는 목록의 LayoutGroup이 정하므로 건드리지 않는다

public abstract class PoolableMonoUI : PoolableMono
{
    public RectTransform RectTransform => transform as RectTransform;

    // ------------------------------------------------

    // virtual ---

    public override void OnPoolSpawn()
    {
        Transform tr = transform;
        tr.localScale = Vector3.one;
        tr.localRotation = Quaternion.identity;
    }
}
