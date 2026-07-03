using UnityEngine;

namespace CampLantern.Core
{
    /// <summary>
    /// 요리 레시피 정의. 요리는 독립 자원원이 아니라 낚시/사냥 재료의 부가가치화이므로
    /// 요리 전용 신규 재료를 참조하면 안 된다 (domain/resource-loop.md).
    /// </summary>
    [CreateAssetMenu(menuName = "CampLantern/Recipe", fileName = "Recipe_")]
    public class RecipeDef : ScriptableObject
    {
        [Tooltip("안정 식별자 — 변경 금지")]
        public string Id;

        public string DisplayName;

        [Tooltip("필요 재료 — 낚시/사냥 재료 풀 재사용 (순서 무관, 수량은 배열 중복으로 표현)")]
        public ItemDef[] Ingredients;

        [Tooltip("완성 요리 — SellPrice는 재료 합의 1.5~2배로 데이터 설정 (domain/economy.md)")]
        public ItemDef Result;

        [Tooltip("실패작 — 저가 판매용. 실패 시 Rare 이상 재료는 소모되지 않는다")]
        public ItemDef FailResult;
    }
}
