using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// ==================================================

// 피격 복셀 조각을 한곳에서 움직이고 그린다. 조각마다 게임 오브젝트를 만들지 않는다.
// 색마다 미리 잡아 둔 배열을 재사용하고, Graphics.RenderMeshInstanced로 색 하나당 한 번에 그린다.
// GameManager가 매 프레임 갱신하고 전투 씬이 닫힐 때 정리한다.

public sealed class ChunkBurstService : ServiceBase
{
    private const int MaxChunkPerColor = 256;   // 색 하나에 동시에 떠 있을 수 있는 조각 수. 넘치면 새 조각을 건너뛴다
    private const int BurstCount = 6;           // 한 번 맞을 때 튀는 조각 수
    private const float Size = 0.12f;           // 조각 한 변 길이
    private const float Speed = 4f;             // 튀어 나가는 속도
    private const float Gravity = -18f;         // 떨어지는 속도
    private const float LifeTime = 0.55f;       // 이 시간 동안 줄어들다 사라진다(초)

    private static readonly int s_BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int s_ColorID = Shader.PropertyToID("_Color");
    private static readonly Bounds s_WorldBounds = new(Vector3.zero, Vector3.one * 10000f);

    // 조각 하나
    private struct Chunk
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public float Age;
    }

    // 같은 색 조각 묶음. 살아 있는 조각은 항상 앞쪽 [0, Count)에 모아 둔다
    private class ChunkBatch
    {
        public readonly Chunk[] ArrChunk = new Chunk[MaxChunkPerColor];
        public readonly Matrix4x4[] ArrMatrix = new Matrix4x4[MaxChunkPerColor];
        public readonly RenderParams RenderParams;
        public int Count;

        public ChunkBatch(Material material, Color color)
        {
            MaterialPropertyBlock block = new();
            block.SetColor(s_BaseColorID, color);
            block.SetColor(s_ColorID, color);

            RenderParams = new RenderParams(material)
            {
                matProps = block,
                worldBounds = s_WorldBounds,
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
            };
        }
    }

    private readonly Dictionary<Color, ChunkBatch> m_DicBatch = new();

    private Mesh m_Mesh;
    private Material m_Material;

    // ------------------------------------------------

    // origin에서 direction 쪽으로 조각을 흩뿌린다
    public void Burst(Vector3 origin, Vector3 direction, Color color)
    {
        ChunkBatch batch = GetBatch(color);
        for (int i = 0; i < BurstCount; i++)
        {
            if (batch.Count >= MaxChunkPerColor)
                return;

            Vector3 dir = (direction + Random.insideUnitSphere * 0.8f + Vector3.up).normalized;
            batch.ArrChunk[batch.Count] = new Chunk
            {
                Position = origin + Random.insideUnitSphere * 0.2f,
                Rotation = Random.rotation,
                Velocity = dir * (Speed * Random.Range(0.6f, 1.2f)),
                Age = 0f,
            };
            batch.Count++;
        }
    }

    // 히트스톱(timeScale 0) 중에는 deltaTime이 0이라 멈춘 채로 그려진다
    public override void Tick(float deltaTime)
    {
        foreach (ChunkBatch batch in m_DicBatch.Values)
        {
            if (batch.Count == 0)
                continue;

            UpdateBatch(batch, deltaTime);
            if (batch.Count > 0)
                Graphics.RenderMeshInstanced(batch.RenderParams, m_Mesh, 0, batch.ArrMatrix, batch.Count);
        }
    }

    public override void Clear()
    {
        m_DicBatch.Clear();
        m_Material = null;   // 에셋(Resources)이라 지우지 않는다
        m_Mesh = null;
    }

    private ChunkBatch GetBatch(Color color)
    {
        if (m_DicBatch.TryGetValue(color, out ChunkBatch batch))
            return batch;

        if (m_Material == null)
        {
            m_Mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            m_Material = HitMaterials.Chunk;
        }

        batch = new ChunkBatch(m_Material, color);
        m_DicBatch.Add(color, batch);
        return batch;
    }

    private static void UpdateBatch(ChunkBatch batch, float deltaTime)
    {
        Chunk[] arrChunk = batch.ArrChunk;
        for (int i = batch.Count - 1; i >= 0; i--)
        {
            Chunk chunk = arrChunk[i];
            chunk.Age += deltaTime;

            // 수명이 다하면 맨 뒤 조각을 이 자리로 옮겨 빈칸 없이 유지한다
            if (chunk.Age >= LifeTime)
            {
                batch.Count--;
                arrChunk[i] = arrChunk[batch.Count];
                continue;
            }

            chunk.Velocity.y += Gravity * deltaTime;
            chunk.Position += chunk.Velocity * deltaTime;
            arrChunk[i] = chunk;
        }

        for (int i = 0; i < batch.Count; i++)
        {
            Chunk chunk = arrChunk[i];
            float scale = Size * (1f - chunk.Age / LifeTime);
            batch.ArrMatrix[i] = Matrix4x4.TRS(chunk.Position, chunk.Rotation, Vector3.one * scale);
        }
    }
}
