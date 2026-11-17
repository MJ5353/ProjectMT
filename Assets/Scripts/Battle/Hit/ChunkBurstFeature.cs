using UnityEngine;

// 복셀 조각 튀김 반응. 작은 큐브 몇 개를 맞은 방향으로 흩뿌린다. 충돌 없이 잠깐 날다 줄어들어 사라진다.
public class ChunkBurstFeature : MonoBehaviour, IHitReaction
{
    [SerializeField] private int m_Count = 6;
    [SerializeField] private Color m_Color = new(0.85f, 0.75f, 0.6f);
    [SerializeField] private float m_Size = 0.12f;
    [SerializeField] private float m_Speed = 4f;
    [SerializeField] private float m_Height = 0.5f;

    private static Material s_Material;
    private static readonly int s_BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int s_ColorId = Shader.PropertyToID("_Color");

    public int Order => 40;

    public void OnHit(in HitParams hit)
    {
        Vector3 origin = transform.position + Vector3.up * m_Height;
        MaterialPropertyBlock block = new();
        block.SetColor(s_BaseColorId, m_Color);
        block.SetColor(s_ColorId, m_Color);

        for (int i = 0; i < m_Count; i++)
        {
            GameObject chunk = GameObject.CreatePrimitive(PrimitiveType.Cube);
            Destroy(chunk.GetComponent<Collider>());
            chunk.name = "Chunk";
            chunk.transform.SetPositionAndRotation(origin + Random.insideUnitSphere * 0.2f, Random.rotation);
            chunk.transform.localScale = Vector3.one * m_Size;

            MeshRenderer renderer = chunk.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = GetMaterial();
            renderer.SetPropertyBlock(block);

            Vector3 dir = (hit.Direction + Random.insideUnitSphere * 0.8f + Vector3.up).normalized;
            chunk.AddComponent<Chunk>().Init(dir * m_Speed * Random.Range(0.6f, 1.2f));
        }
    }

    private static Material GetMaterial()
    {
        if (s_Material != null)
            return s_Material;

        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");
        s_Material = new Material(shader) { name = "Chunk (Runtime)", enableInstancing = true };
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
