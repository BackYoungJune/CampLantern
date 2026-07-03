using System;
using System.Collections;
using CampLantern.Core;
using UnityEngine;

namespace CampLantern.Fishing
{
    /// <summary>
    /// 낚시 상태 (P0 — 캐스팅→대기→입질→챔질→획득/실패).
    /// </summary>
    public enum FishingState
    {
        Idle,     // 대기 (캐스팅 가능)
        Casting,  // 캐스팅 모션 중
        Waiting,  // 찌 내려간 뒤 입질 대기
        Biting,   // 입질 중 — BiteWindowSeconds 안에 Reel() 성공 시 Caught
        Caught,   // 획득 성공 (잠시 후 Idle 복귀)
        Missed,   // 챔질 실패/이탈 (잠시 후 Idle 복귀)
    }

    /// <summary>
    /// 낚시 루프 상태머신 본체. 동물의숲 수준으로 단순화 — 라인/장력 물리 없이
    /// 코루틴 타이밍 + Reel() 판정 하나로 승부한다 (domain/resource-loop.md 낚시 섹션).
    /// 물리 API 미사용이므로 RULE-03 해당 없음.
    ///
    /// VR 입력(컨트롤러 스윙/버튼)은 P0에서 단순화 — Cast()/Reel()을 public으로 열어두고
    /// 입력 바인딩은 얇은 어댑터(별도 컴포넌트)에서 호출한다.
    /// 획득 물고기의 Inventory 추가는 FishCaught 이벤트를 구독하는 쪽(임시 GameManager 등) 책임.
    /// </summary>
    public class FishingRod : MonoBehaviour
    {
        [Tooltip("캐스팅 모션에 걸리는 시간(초) — Casting→Waiting 전이 지연")]
        [SerializeField] private float m_castSeconds = 0.8f;

        [Tooltip("Caught/Missed 결과 표시 후 Idle로 복귀하기까지의 시간(초)")]
        [SerializeField] private float m_resultResetSeconds = 1f;

        /// <summary>현재 상태.</summary>
        public FishingState State { get; private set; }

        /// <summary>챔질 성공으로 물고기를 획득했을 때 (Inventory 추가는 구독자 책임).</summary>
        public event Action<FishDef> FishCaught;

        /// <summary>State가 바뀔 때마다 발생 (UI/사운드/찌 연출 훅).</summary>
        public event Action StateChanged;

        // 현재 캐스팅에서 추첨된 어종 — 대기/입질 타이밍은 전부 이 FishDef 필드에서 온다 (하드코딩 금지).
        private FishDef m_currentFish;
        private Coroutine m_loopRoutine;

        private void Awake()
        {
            // 초기 상태는 코드로 확정 (scripts.md — Inspector 의존 금지)
            State = FishingState.Idle;
        }

        private void OnDisable()
        {
            // 비활성화 시 코루틴이 끊기므로 상태도 함께 정리해 재활성화 시 꼬임 방지
            StopLoop();
            m_currentFish = null;
            if (State != FishingState.Idle)
                SetState(FishingState.Idle);
        }

        /// <summary>
        /// 캐스팅 시작. Idle에서만 유효 — Idle→Casting→Waiting→(입질 대기)→Biting.
        /// 입질까지의 대기 시간과 판정 창은 추첨된 FishDef 필드를 사용한다.
        /// </summary>
        public void Cast(FishingSpot spot)
        {
            if (State != FishingState.Idle)
                return;

            if (spot == null)
            {
                Debug.LogWarning("[FishingRod] Cast: spot이 null", this);
                return;
            }

            m_currentFish = spot.PickRandomFish();
            if (m_currentFish == null)
                return; // 어종 테이블이 비어 있음 — FishingSpot이 경고 로그 출력

            SetState(FishingState.Casting);
            m_loopRoutine = StartCoroutine(FishingLoop());
        }

        /// <summary>
        /// 챔질/릴링. Biting 중이면 Caught(획득), Casting/Waiting 중이면 성급한 챔질로 Missed.
        /// 그 외 상태에서는 무시.
        /// </summary>
        public void Reel()
        {
            switch (State)
            {
                case FishingState.Biting:
                    StopLoop();
                    var fish = m_currentFish;
                    m_currentFish = null;
                    SetState(FishingState.Caught);
                    FishCaught?.Invoke(fish);
                    m_loopRoutine = StartCoroutine(ResetAfterResult());
                    break;

                case FishingState.Casting:
                case FishingState.Waiting:
                    // 입질 전에 걷어올림 — 캐스팅 취소 취급
                    Miss();
                    break;
            }
        }

        // 캐스팅→대기→입질→(판정 창 초과 시) Missed 까지의 타이밍 루프.
        // 프레임 단위 대기는 코루틴 사용 (scripts.md).
        private IEnumerator FishingLoop()
        {
            // Casting → Waiting
            yield return new WaitForSeconds(m_castSeconds);
            SetState(FishingState.Waiting);

            // 입질까지 대기 — FishDef의 Min/MaxWaitSeconds 사용
            float wait = UnityEngine.Random.Range(m_currentFish.MinWaitSeconds, m_currentFish.MaxWaitSeconds);
            yield return new WaitForSeconds(wait);
            SetState(FishingState.Biting);

            // 판정 창 — FishDef.BiteWindowSeconds 안에 Reel()이 없으면 놓침
            yield return new WaitForSeconds(m_currentFish.BiteWindowSeconds);
            Miss();
        }

        private IEnumerator ResetAfterResult()
        {
            yield return new WaitForSeconds(m_resultResetSeconds);
            m_loopRoutine = null;
            SetState(FishingState.Idle);
        }

        private void Miss()
        {
            StopLoop();
            m_currentFish = null;
            SetState(FishingState.Missed);
            m_loopRoutine = StartCoroutine(ResetAfterResult());
        }

        private void StopLoop()
        {
            if (m_loopRoutine != null)
            {
                StopCoroutine(m_loopRoutine);
                m_loopRoutine = null;
            }
        }

        private void SetState(FishingState next)
        {
            if (State == next)
                return;

            State = next;
            StateChanged?.Invoke();
        }
    }
}
