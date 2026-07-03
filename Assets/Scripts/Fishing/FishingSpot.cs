using CampLantern.Core;
using UnityEngine;

namespace CampLantern.Fishing
{
    /// <summary>
    /// 낚시 가능 구역. 이 구역에서 잡을 수 있는 어종 테이블을 들고 있으며,
    /// FishingRod.Cast() 시점에 어종 하나를 추첨해 준다.
    /// P0에서는 균등 추첨만 — 레어도별 가중치/연출은 P1 (step-04 금지 사항).
    /// </summary>
    public class FishingSpot : MonoBehaviour
    {
        [Tooltip("이 구역에서 잡히는 어종 목록 (Editor 스크립트로 생성한 FishDef 에셋 참조)")]
        [SerializeField] private FishDef[] m_fishTable;

        /// <summary>
        /// 어종 테이블에서 무작위로 하나를 뽑는다. 테이블이 비어 있으면 null.
        /// </summary>
        public FishDef PickRandomFish()
        {
            if (m_fishTable == null || m_fishTable.Length == 0)
            {
                Debug.LogWarning($"[FishingSpot] {name}: 어종 테이블이 비어 있음", this);
                return null;
            }

            return m_fishTable[Random.Range(0, m_fishTable.Length)];
        }
    }
}
