using UnityEngine;

// 복셀 조각 튀김 반응. 작은 큐브 몇 개를 맞은 방향으로 흩뿌린다. 충돌 없이 잠깐 날다 줄어들어 사라진다.
// !!) 보류: 조각 오브젝트 생성(CreatePrimitive·GetComponent·AddComponent). RenderMeshInstanced로 바꿀지 검토
public class ChunkBurstFeature : IHitReaction
{
    private const int Count = 6;
    private const float Size = 0.12f;
    private const float Speed = 4f;
    private const float Height = 0.5f;   // 조각이 튀어나오는 높이

    private static Material s_Material;
    private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int s_ColorId = Shader.PropertyToID("_Color");

    private readonly Transform m_Owner;
    private readonly Color m_Color;

    // ==================================================

    public ChunkBurstFeature(Transform owner, Color color)
    {
        m_Owner = owner;
        m_Color = color;
    }

    public void OnHit(in HitParams hit)
    {
        Vector3 origin = m_Owner.position + Vector3.up * Height;
        MaterialPropertyBlock block = new();
        block.SetColor(s_BaseColorId, m_Color);
        block.SetColor(s_ColorId, m_Color);

        for (int i = 0; i < Count; i++)
        {
            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Object.Destroy(chunk.GetComponent<Collider>());
            chunk.name = "Chunk";
            chunk.transform.SetPositionAndRotation(origin + Random.insideUnitSphere * 0.2f, Random.rotation);
            chunk.transform.localScale = Vector3.one * Size;

            MeshRenderer renderer = chunk.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetMaterial();
            renderer.SetPropertyBlock(block);

            Vector3 dir = (hit.Direction + Random.insideUnitSphere * 0.8f + Vector3.up).normalized;
            chunk.AddComponent<Chunk>().Init(dir * Speed * Random.Range(0.6f, 1.2f));
        }
    }

    private static Material GetMaterial()
    {
        if (s_Material == null)
            s_Material = HitMaterials.CreateUnlit("Chunk (Runtime)", Color.white);

        return s_Material;
    }

    // 조각 하나. 중력만 받고 수명이 다하면 줄어들며 사라진다.
    private class Chunk : MonoBehaviour
    {
        private const float Gravity = -18f;
        private const float LifeTime = 0.55f;

        private Vector3 m_Velocity;
        private Vector3 m_BaseScale;
        private float m_Age;

        public void Init(Vector3 velocity)
        {
            m_Velocity = velocity;
            m_BaseScale = transform.localScale;
        }

        private void Update()
        {
            m_Age += Time.deltaTime;
            if (m_Age >= LifeTime)
            {
                Destroy(gameObject);
                return;
            }

            m_Velocity.y += Gravity * Time.deltaTime;
            transform.position += m_Velocity * Time.deltaTime;
            transform.localScale = m_BaseScale * (1f - m_Age / LifeTime);
        }
    }
}
