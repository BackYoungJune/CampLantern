using System;
using CampLantern.Core;
using Fusion;
using UnityEngine;

namespace CampLantern.Hunting
{
    /// <summary>
    /// Shared Ledger P0 최소판 — 자동 활성화, 복수 행동 기여 인정, 공유 보상 (domain/social-cooperation.md).
    /// 기여 기록은 State Authority에서만 쓴다. 기여도 차등 보상은 P0에서 구현하지 않는다 (보너스 차등은 P1).
    /// HuntTarget과 같은 GameObject에 붙는다.
    /// </summary>
    public class HuntLedger : NetworkBehaviour
    {
        /// <summary>
        /// 기여 인정 행동 — 피해량(Hit) 단일 기준 금지. 어떤 종류든 1회 이상이면 참여자다.
        /// </summary>
        public enum ContributionKind { Hit, Lure, Assist }

        /// <summary>플레이어별 유효 행동 누적 횟수. 종류 불문 1 이상 = 참여.</summary>
        [Networked, Capacity(8)]
        public NetworkDictionary<PlayerRef, int> Contributions { get; }

        /// <summary>
        /// 처치 시 로컬 플레이어가 참여자일 때만 발화 — 참여자 전원이 각자 클라이언트에서 동일 보상을 받는다.
        /// 구독 측(Inventory 보유자)이 Def.RewardMaterials를 지급한다. OnDestroy/OnDisable에서 반드시 해제할 것.
        /// </summary>
        public event Action<HuntTargetDef> RewardGranted;

        private HuntTarget m_target;

        public override void Spawned()
        {
            m_target = GetComponent<HuntTarget>();
            if (m_target != null)
            {
                m_target.Defeated -= OnTargetDefeated;
                m_target.Defeated += OnTargetDefeated;
            }
        }

        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (m_target != null)
                m_target.Defeated -= OnTargetDefeated;
        }

        /// <summary>
        /// 기여 기록. 비권한 클라이언트에서 호출하면 State Authority로 RPC 전달된다.
        /// 파티 진행 중 자동 호출되는 구조 — 개인이 따로 활성화할 필요 없음 (Shared Ledger 자동 활성화 규칙).
        /// </summary>
        public void RecordContribution(PlayerRef player, ContributionKind kind)
        {
            if (Object.HasStateAuthority)
                RecordAuthoritative(player);
            else
                RPC_RecordContribution(player, (int)kind);
        }

        /// <summary>유효 행동 1회 이상 = 참여. 종류는 따지지 않는다.</summary>
        public bool IsParticipant(PlayerRef player)
        {
            return Contributions.TryGet(player, out var count) && count > 0;
        }

        /// <summary>
        /// 새 사냥 사이클 시작 시 이전 기여 기록을 지운다 — 같은 사냥감을 반복 사냥할 때
        /// 이전 라운드 참여자가 이번 라운드에 기여 없이도 보상받는 것을 방지한다.
        /// HuntTarget.TryStartHunt에서만 호출(State Authority 컨텍스트).
        /// </summary>
        public void ResetForNewHunt()
        {
            if (!Object.HasStateAuthority) return;
            Contributions.Clear();
        }

        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_RecordContribution(PlayerRef player, int kind)
        {
            RecordAuthoritative(player);
        }

        private void RecordAuthoritative(PlayerRef player)
        {
            Contributions.TryGet(player, out var count);
            Contributions.Set(player, count + 1);
        }

        private void OnTargetDefeated(HuntTarget target)
        {
            if (target.Def == null) return;

            // 핵심 보상은 참여자 전원 동일 — 순위/차등 없음 (경쟁적 박탈감 방지 원칙)
            if (IsParticipant(Runner.LocalPlayer))
                RewardGranted?.Invoke(target.Def);
        }
    }
}
