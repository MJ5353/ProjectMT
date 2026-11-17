using UnityEngine;

namespace MushTaker.Data
{
    public enum Rarity { Common, Rare, Epic }

    /// <summary>
    /// 버섯 정의. 성질(분해·기생·포자 등)과 전투 효과는 장르가 정해진 뒤 필드를 추가한다.
    /// </summary>
    [CreateAssetMenu(menuName = "MushTaker/Data/Mushroom", fileName = "Mushroom_")]
    public class MushroomDef : DataDef
    {
        [SerializeField] Rarity rarity;
        [SerializeField] Color tint = Color.white;

        public Rarity Rarity => rarity;
        public Color Tint => tint;
    }
}
