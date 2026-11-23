using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// ==================================================

// 피격 복셀 조각과 타격 마크(맞은 자리의 도트 별)를 한곳에서 움직이고 그린다. 조각마다 게임 오브젝트를 만들지 않는다.
// 색마다 미리 잡아 둔 배열을 재사용하고, Graphics.RenderMeshInstanced로 색 하나당 한 번에 그린다.
// EffectService가 들고 매 프레임 갱신하고, 전투 씬이 닫힐 때 정리한다.

public sealed class ChunkRenderer
{
    private const int MaxChunkPerColor = 256;   // 색 하나에 동시에 떠 있을 수 있는 조각 수. 넘치면 새 조각을 건너뛴다
    private const int BurstCount = 6;           // 한 번 맞을 때 튀는 조각 수
    private const float BurstSize = 0.12f;      // 조각 한 변 길이
    private const float Speed = 4f;             // 튀어 나가는 속도
    private const float BurstGravity = -18f;    // 떨어지는 속도
    private const float BurstLifeTime = 0.55f;  // 이 시간 동안 줄어들다 사라진다(초)

    private const float MarkPixel = 0.09f;      // 타격 마크 도트 한 칸 길이
    private const float MarkExpand = 6f;        // 도트가 중심에서 퍼지는 빠르기(중심 거리 배율, 초당)
    private const float MarkLifeTime = 0.2f;    // 타격 마크가 퍼지며 줄어들다 사라지는 시간(초)
    private const float MarkFront = 0.4f;       // 맞은 표면에 묻히지 않게 카메라 쪽으로 당기는 거리
    private const float PartMarkPixel = 0.14f;  // 부위 타격 마크 도트 한 칸 길이. 몸보다 크게
    private const float PartMarkLifeTime = 0.35f; // 부위 타격 마크가 남는 시간(초). 몸보다 길게

    private static readonly Color s_MarkCoreColor = Color.white;
    private static readonly Color s_MarkRayColor = new(1f, 0.85f, 0.2f);
    private static readonly Color s_PartMarkOuterColor = new(1f, 0.3f, 0.1f);

    // 타격 마크 칸 좌표(카메라 기준 오른쪽, 위). 가운데 십자는 흰색, 바깥 빛살 8줄기는 노란색
    private static readonly Vector2Int[] s_ArrMarkCore = { new(0, 0), new(1, 0), new(-1, 0), new(0, 1), new(0, -1) };
    private static readonly Vector2Int[] s_ArrMarkRay =
    {
        new(3, 0), new(4, 0), new(-3, 0), new(-4, 0), new(0, 3), new(0, 4), new(0, -3), new(0, -4),
        new(2, 2), new(3, 3), new(-2, 2), new(-3, 3), new(2, -2), new(3, -3), new(-2, -2), new(-3, -3),
    };
    // 부위 타격 마크에만 더하는 바깥 빨간 빛살
    private static readonly Vector2Int[] s_ArrPartMarkOuter =
    {
        new(6, 0), new(7, 0), new(-6, 0), new(-7, 0), new(0, 6), new(0, 7), new(0, -6), new(0, -7),
        new(5, 5), new(-5, 5), new(5, -5), new(-5, -5),
    };

    private static readonly int s_BaseColorID = Shader.PropertyToID("_BaseColor");
    private static readonly int s_ColorID = Shader.PropertyToID("_Color");
    private static readonly Bounds s_WorldBounds = new(Vector3.zero, Vector3.one * 10000f);

    private readonly Dictionary<Color, ChunkBatch> m_DicBatch = new();
    private Mesh m_Mesh;
    private Material m_Material;
    
    // 조각 하나
    private struct Chunk
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Velocity;
        public float Gravity;
        public float Size;
        public float LifeTime;
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
                Gravity = BurstGravity,
                Size = BurstSize,
                LifeTime = BurstLifeTime,
                Age = 0f,
            };
            batch.Count++;
        }
    }

    public void Mark(Vector3 point, bool isPart)
    {
        // 맞은 자리에 도트 별 모양 마크를 띄운다. 카메라를 향한 판에 칸을 찍고, 중심에서 퍼지며 줄어든다
        // 부위를 맞으면 도트가 크고 오래 남고, 바깥에 빨간 빛살이 더해진다

        Camera camera = CameraManager.Instance.Main;
        if (camera == null)
            return;

        Transform cameraTransform = camera.transform;
        Vector3 center = point - cameraTransform.forward * MarkFront;
        
        float pixel = isPart ? PartMarkPixel : MarkPixel;
        float lifeTime = isPart ? PartMarkLifeTime : MarkLifeTime;

        AddMarkPixels(s_ArrMarkCore, s_MarkCoreColor, center, cameraTransform, pixel, lifeTime);
        AddMarkPixels(s_ArrMarkRay, s_MarkRayColor, center, cameraTransform, pixel, lifeTime);
        
        if (isPart)
            AddMarkPixels(s_ArrPartMarkOuter, s_PartMarkOuterColor, center, cameraTransform, pixel, lifeTime);
    }

    // 히트스톱(timeScale 0) 중에는 deltaTime이 0이라 멈춘 채로 그려진다
    public void Tick(float deltaTime)
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

    // 조각만 비운다. 배치(색별 배열)·머티리얼·메시는 다음 판에 그대로 다시 쓴다
    public void Clear()
    {
        foreach (ChunkBatch batch in m_DicBatch.Values)
        {
            batch.Count = 0;
        }
    }

    private ChunkBatch GetBatch(Color color)
    {
        if (m_DicBatch.TryGetValue(color, out ChunkBatch batch))
            return batch;

        if (m_Material == null)
        {
            m_Mesh = Resources.GetBuiltinResource<Mesh>(CommonPath.BuiltinCube);
            m_Material = HitMaterials.Chunk;
        }

        batch = new ChunkBatch(m_Material, color);
        m_DicBatch.Add(color, batch);
        
        return batch;
    }

    private void AddMarkPixels(Vector2Int[] arrCell, Color color, Vector3 center, Transform cameraTransform, float pixel, float lifeTime)
    {
        ChunkBatch batch = GetBatch(color);
        foreach (Vector2Int cell in arrCell)
        {
            if (batch.Count >= MaxChunkPerColor)
                return;

            Vector3 offset = (cameraTransform.right * cell.x + cameraTransform.up * cell.y) * pixel;
            batch.ArrChunk[batch.Count] = new Chunk
            {
                Position = center + offset,
                Rotation = cameraTransform.rotation,   // 정육면체 한 면이 카메라를 향해 네모 도트로 보인다
                Velocity = offset * MarkExpand,
                Gravity = 0f,
                Size = pixel,
                LifeTime = lifeTime,
                Age = 0f,
            };
            
            batch.Count++;
        }
    }

    private static void UpdateBatch(ChunkBatch batch, float deltaTime)
    {
        Chunk[] arrChunk = batch.ArrChunk;
        for (int i = batch.Count - 1; i >= 0; i--)
        {
            Chunk chunk = arrChunk[i];
            chunk.Age += deltaTime;

            // 수명이 다하면 맨 뒤 조각을 이 자리로 옮겨 빈칸 없이 유지한다
            if (chunk.Age >= chunk.LifeTime)
            {
                batch.Count--;
                arrChunk[i] = arrChunk[batch.Count];
                continue;
            }

            chunk.Velocity.y += chunk.Gravity * deltaTime;
            chunk.Position += chunk.Velocity * deltaTime;
            arrChunk[i] = chunk;
        }

        for (int i = 0; i < batch.Count; i++)
        {
            Chunk chunk = arrChunk[i];
            float scale = chunk.Size * (1f - chunk.Age / chunk.LifeTime);
            batch.ArrMatrix[i] = Matrix4x4.TRS(chunk.Position, chunk.Rotation, Vector3.one * scale);
        }
    }
}
