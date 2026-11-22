using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// ==================================================

// 누른 자리에서 버섯 아이콘이 튀어 퍼지며 사라지는 UI 연출. EffectService가 들고(UseClickBurst를 켠 씬만) 매 프레임 조각을 움직인다.
// 화면 아무 데나 누를 때가 아니라, 버튼이 EffectService.PlayClickBurst를 부를 때만 터진다(로비 하단 탭·[입장], 타이틀·로딩 화면의 배경 버튼)
// 조각은 UICommonImage를 UIReuseService로 꺼내 UI 루트 맨 위(팝업보다 위)에 띄우고, 끝나면 돌려준다. 수치는 DefineDef(ClickBurst*)

public sealed class UIClickBurst
{
    private struct Piece
    {
        public UICommonImage Item;
        public Vector3 Start;   // UI 루트 기준 누른 자리
        public Vector3 Offset;  // 끝날 때까지 퍼지는 거리
        
        public float Tilt;
        public float Spin;
        public float Time;
    }

    private readonly List<Sprite> m_ListIcon = new();
    private readonly List<Piece> m_ListPiece = new();

    // ------------------------------------------------

    public void Init()
    {
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
        if (m_ListIcon.Count == 0)
            return;

        if (root == null)
        {
            if (GameManager.Instance.IsTransitioning)
                return;

            root = RootManager.Instance.UIRoot;
        }

        Pop(GameManager.Instance.Define, InputManager.Instance.UI.Point.ReadValue<Vector2>(), root);
    }

    // 남은 조각은 씬이 바뀌기 직전 UIReuseService가 한꺼번에 돌려받는다
    public void Clear()
    {
        m_ListPiece.Clear();
        m_ListIcon.Clear();
    }

    // ---

    // !!) 팝업 등으로 게임 시간이 멈춰도 끝까지 재생하도록 unscaled 시간으로 돈다
    private void UpdatePieces(DefineDef define)
    {
        float deltaTime = Time.unscaledDeltaTime;
        for (int i = m_ListPiece.Count - 1; i >= 0; --i)
        {
            Piece piece = m_ListPiece[i];

            // 씬 전환 때 UIReuseService가 먼저 돌려받았으면 버린다
            if (piece.Item == null || piece.Item.isActiveAndEnabled == false)
            {
                m_ListPiece.RemoveAt(i);
                continue;
            }

            piece.Time += deltaTime;
            float t = piece.Time / define.ClickBurstTime;
            
            if (t >= 1f)
            {
                GameManager.Instance.UIReuseS.Despawn(piece.Item);
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
        UIReuseService uiReuseS = GameManager.Instance.UIReuseS;
        
        if (root == null || uiReuseS == null || root.gameObject.activeInHierarchy == false)
            return;

        // 캔버스가 Screen Space Overlay라 카메라 없이 바꾼다(UIBuilder가 만든 캔버스)
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(root, screenPosition, null, out Vector2 start) == false)
            return;

        int count = Random.Range(define.ClickBurstCount.x, define.ClickBurstCount.y + 1);
        for (int i = 0; i < count; ++i)
        {
            UICommonImage item = uiReuseS.Spawn<UICommonImage>(EUIComponentType.UICommonImage, root);
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
                Start = start,
                Offset = new Vector3(Mathf.Cos(direction), Mathf.Sin(direction), 0f) * Random.Range(define.ClickBurstDistance.x, define.ClickBurstDistance.y),
                Tilt = tilt,
                Spin = Random.Range(-define.ClickBurstSpin, define.ClickBurstSpin),
            });
        }
    }
}
