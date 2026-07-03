using System;
using System.Collections.Generic;
using CampLantern.Core;

namespace CampLantern.Estate
{
    /// <summary>
    /// 영지 오브젝트 구매 검증 (순수 클래스 — MonoBehaviour 아님).
    /// 이중 재화 규칙 (domain/economy.md):
    ///   - 일반(Common): 코인만으로 구매.
    ///   - 희귀(Rare) 이상: 코인 + 지정 재료 둘 다 필요. 재료는 코인으로 대체 불가.
    ///   - 에픽(Epic): 상점 구매 불가 — 시즌 이벤트/커뮤니티 보상 전용 (domain/estate-system.md).
    /// 구매 성공 시 Def 보유 목록에 추가. 배치 시 소비하고, 회수(EstateManager.Remove) 시 돌아온다.
    /// </summary>
    public class EstateShop
    {
        private readonly Wallet    m_wallet;
        private readonly Inventory m_inventory;

        // Def 보유 목록 — 구매했지만 아직 배치하지 않은 오브젝트 (P0: 세션 메모리만, 저장 없음)
        private readonly Dictionary<EstateObjectDef, int> m_ownedDefs = new Dictionary<EstateObjectDef, int>();

        /// <summary>보유 목록 변경 시 발화. 구독자는 OnDestroy/OnDisable에서 반드시 해제할 것 (rules/scripts.md).</summary>
        public event Action OwnedChanged;

        public IReadOnlyDictionary<EstateObjectDef, int> OwnedDefs => m_ownedDefs;

        public EstateShop(Wallet wallet, Inventory inventory)
        {
            m_wallet    = wallet ?? throw new ArgumentNullException(nameof(wallet));
            m_inventory = inventory ?? throw new ArgumentNullException(nameof(inventory));
        }

        /// <summary>
        /// 구매 시도. 원자성 보장: 코인·재료를 모두 사전 검증한 뒤에만 차감한다 —
        /// 하나라도 부족하면 아무것도 차감하지 않는다 (코인만 차감되는 경로 없음).
        /// </summary>
        public bool TryPurchase(EstateObjectDef def)
        {
            if (def == null) return false;

            // 에픽은 상점에서 코인으로 직접 구매 불가 (domain/estate-system.md)
            if (def.Rarity == Rarity.Epic) return false;

            bool needsMaterial = def.Rarity >= Rarity.Rare;
            if (needsMaterial && (def.RequiredMaterial == null || def.RequiredMaterialCount <= 0))
            {
                // 희귀 이상인데 지정 재료가 없는 정의는 데이터 오류 — 이중 재화 규칙 위반이므로 판매 거부
                UnityEngine.Debug.LogError($"[EstateShop] 희귀 이상 오브젝트에 지정 재료 미설정: {def.Id}");
                return false;
            }

            // ── 1단계: 사전 검증 (아무것도 차감하지 않음) ──
            if (m_wallet.Coins < def.CoinCost) return false;
            if (needsMaterial && m_inventory.CountOf(def.RequiredMaterial) < def.RequiredMaterialCount) return false;

            // ── 2단계: 차감 (사전 검증을 통과했으므로 실패 없이 진행) ──
            if (def.CoinCost > 0 && !m_wallet.TrySpend(def.CoinCost))
                return false; // 사전 검증상 도달 불가 — 방어 코드

            if (needsMaterial && !m_inventory.TryRemove(def.RequiredMaterial, def.RequiredMaterialCount))
            {
                // 사전 검증상 도달 불가 — 만약 도달하면 코인 롤백으로 원자성 유지
                m_wallet.Add(def.CoinCost);
                return false;
            }

            AddOwned(def);
            return true;
        }

        /// <summary>보유 수량 조회.</summary>
        public int CountOwned(EstateObjectDef def)
        {
            if (def == null) return 0;
            return m_ownedDefs.TryGetValue(def, out int count) ? count : 0;
        }

        /// <summary>배치 시 보유 목록에서 1개 소비. 미보유면 false — 배치 UI는 성공 후에만 EstateManager.Place를 호출한다.</summary>
        public bool TryConsumeOwned(EstateObjectDef def)
        {
            if (def == null) return false;
            if (!m_ownedDefs.TryGetValue(def, out int count) || count <= 0) return false;

            int remaining = count - 1;
            if (remaining > 0)
                m_ownedDefs[def] = remaining;
            else
                m_ownedDefs.Remove(def);

            OwnedChanged?.Invoke();
            return true;
        }

        /// <summary>회수된 오브젝트를 보유 목록으로 되돌린다 — EstateManager.Remove에서 호출 (Inventory 반환 아님).</summary>
        public void ReturnOwned(EstateObjectDef def)
        {
            if (def == null) return;
            AddOwned(def);
        }

        private void AddOwned(EstateObjectDef def)
        {
            m_ownedDefs.TryGetValue(def, out int current);
            m_ownedDefs[def] = current + 1;
            OwnedChanged?.Invoke();
        }
    }
}
