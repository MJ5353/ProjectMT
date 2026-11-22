using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 누른 자리에서 버섯 아이콘이 튀어 퍼지며 사라지는 UI 연출. UIManager가 들고 매 프레임 조각을 움직인다(씬을 넘어 산다).
// 화면 아무 데나 누를 때가 아니라, 버튼이 UIManager.PlayClickBurst를 부를 때만 터진다(로비 하단 탭·[입장], 타이틀·로딩 화면의 배경 버튼)
// 조각은 UICommonImage를 ResourceManager 풀에서 직접 꺼내 띄울 자리(씬 UI 루트·오버레이) 맨 위에 띄우고, 끝나면 돌려준다. 수치는 DefineDef(ClickBurst*)

public sealed class UIClickBurst
{
    private struct Piece
    {
        public UICommonImage Item;
        public RectTransform Root;  // 띄운 자리. 씬 UI 루트가 떨어질 때 그 아래 조각만 돌려받는다
        public Vector3 Start;   // UI 루트 기준 누른 자리
        public Vector3 Offset;  // 끝날 때까지 퍼지는 거리
        
        public float Tilt;
        public float Spin;
        public float Time;
    }

    private readonly List<Sprite> m_ListIcon = new();
    private readonly List<Piece> m_ListPiece = new();
    private UICommonImage m_Prefab; // 조각 원본(UIComponentDef). 없으면 연출을 끈다

    // ------------------------------------------------

    public void Init()
    {
        if (GameDatabase.S_TryGet(UIComponentDef.UIComponentID, out UIComponentDef uiComponentDef))
            m_Prefab = uiComponentDef.Get<UICommonImage>(EUIComponentType.UICommonImage);
        else
            CLogger.Error("UIClickBurst", "UIComponentDef가 DB에 없습니다. UI 프리팹 표 갱신을 실행하세요.");

        foreach (MushroomDef mushroom in GameManager.Instance.Database.GetAll<MushroomDef>())
        {
            if (mushroom.Icon != null)
                m_ListIcon.Add(mushroom.Icon);
        }

        if (m_ListIcon.Count == 0)
            CLogger.Error("UIClickBurst", "아이콘이 있는 버섯 데이터가 없어 클릭 연출을 끕니다.");
    }

    public void Tick()
    {
        UpdatePieces(GameManager.Instance.Define);
    }

    // 지금 누른 자리(포인터 위치)에서 터뜨린다. root가 없으면 씬 UI 루트에 띄우고, 씬 전환 중에는 터뜨리지 않는다(로딩 화면이 덮는다)
    public void Play(RectTransform root)
    {
        if (m_ListIcon.Count == 0 || m_Prefab == null)
            return;

        if (root == null)
        {
            if (GameManager.Instance.IsTransitioning)
                return;

            root = UIManager.Instance.UIRoot;
        }

        Pop(GameManager.Instance.Define, InputManager.Instance.UI.Point.ReadValue<Vector2>(), root);
    }

    // root 아래 조각을 돌려받는다. null이면 전부. 씬과 함께 이미 지워진 조각은 건너뛴다(ResourceManager.S_Despawn이 null을 거른다)
    public void Clear(RectTransform root)
    {
        for (int i = m_ListPiece.Count - 1; i >= 0; --i)
        {
            Piece piece = m_ListPiece[i];
            if (root != null && piece.Root != root)
                continue;

            ResourceManager.S_Despawn(piece.Item);
            m_ListPiece.RemoveAt(i);
        }
    }

    // ---

    // !!) 팝업 등으로 게임 시간이 멈춰도 끝까지 재생하도록 unscaled 시간으로 돈다
    private void UpdatePieces(DefineDef define)
    {
        float deltaTime = Time.unscaledDeltaTime;
        for (int i = m_ListPiece.Count - 1; i >= 0; --i)
        {
            Piece piece = m_ListPiece[i];

            // 씬과 함께 지워졌으면 버린다
            if (piece.Item == null)
            {
                m_ListPiece.RemoveAt(i);
                continue;
            }

            // 띄운 자리가 꺼졌으면(로딩 화면이 닫힘) 풀로 돌려보낸다
            // !!) 목록에서만 빼면 조각이 꺼진 자리 아래에 반투명한 채 남아, 그 자리가 다시 켜질 때 보인다
            if (piece.Item.isActiveAndEnabled == false)
            {
                ResourceManager.S_Despawn(piece.Item);
                m_ListPiece.RemoveAt(i);
                continue;
            }

            piece.Time += deltaTime;
            float t = piece.Time / define.ClickBurstTime;
            
            if (t >= 1f)
            {
                ResourceManager.S_Despawn(piece.Item);
                m_ListPiece.RemoveAt(i);
                continue;
            }

            Image image = piece.Item.Image;
            RectTransform rt = image.rectTransform;
            rt.localPosition = piece.Start + piece.Offset * UITween.EaseOutCubic(t);
            rt.localRotation = Quaternion.Euler(0f, 0f, piece.Tilt + piece.Spin * t);
            rt.localScale = Vector3.one * Mathf.Lerp(1f, define.ClickBurstEndScale, t);
            image.color = new Color(1f, 1f, 1f, 1f - Mathf.InverseLerp(define.ClickBurstFadeStart, 1f, t));

            m_ListPiece[i] = piece;
        }
    }

    private void Pop(DefineDef define, Vector2 screenPosition, RectTransform root)
    {
        if (root == null || root.gameObject.activeInHierarchy == false)
            return;

        // 캔버스가 Screen Space Overlay라 카메라 없이 바꾼다(UIBuilder가 만든 캔버스)
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, null, out Vector2 start) == false)
            return;

        int count = Random.Range(define.ClickBurstCount.x, define.ClickBurstCount.y + 1);
        for (int i = 0; i < count; ++i)
        {
            UICommonImage item = ResourceManager.Instance.Spawn(m_Prefab, root);
            if (item == null)
                return;

            Image image = item.Image;
            image.sprite = m_ListIcon[Random.Range(0, m_ListIcon.Count)];
            image.color = Color.white;

            RectTransform rt = image.rectTransform;
            rt.SetAsLastSibling(); // 팝업보다 위에 그린다
            rt.sizeDelta = Vector2.one * Random.Range(define.ClickBurstSize.x, define.ClickBurstSize.y);
            rt.localPosition = start;

            float direction = Random.Range(0f, Mathf.PI * 2f);
            float tilt = Random.Range(-define.ClickBurstTilt, define.ClickBurstTilt);
            rt.localRotation = Quaternion.Euler(0f, 0f, tilt);

            m_ListPiece.Add(new Piece
            {
                Item = item,
                Root = root,
                Start = start,
                Offset = new Vector3(Mathf.Cos(direction), Mathf.Sin(direction), 0f) * Random.Range(define.ClickBurstDistance.x, define.ClickBurstDistance.y),
                Tilt = tilt,
                Spin = Random.Range(-define.ClickBurstSpin, define.ClickBurstSpin),
            });
        }
    }
}
