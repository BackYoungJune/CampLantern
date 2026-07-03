using UnityEngine;

namespace CampLantern.Core
{
    /// <summary>
    /// 사냥감 정의. 사냥 보상은 코인이 아닌 고유 재료 — 존재 이유가 코인 효율이 아니라
    /// 재료 접근성이다 (domain/resource-loop.md 사냥 밸런스 원칙).
    /// </summary>
    [CreateAssetMenu(menuName = "CampLantern/HuntTarget", fileName = "Hunt_")]
    public class HuntTargetDef : ScriptableObject
    {
        [Tooltip("안정 식별자 — 변경 금지")]
        public string Id;

        public string DisplayName;

        public int MaxHealth = 100;

        [Tooltip("대형 사냥감은 2 — 2인 이상 협동 시에만 포획 가능 (domain/social-cooperation.md ②)")]
        public int RequiredParticipants = 2;

        [Tooltip("참여자 전원에게 동일 지급되는 고유 재료 (Shared Ledger 공유 보상)")]
        public ItemDef[] RewardMaterials;
    }
}
