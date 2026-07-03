using CampLantern.Core;
using UnityEngine;

namespace CampLantern.Estate
{
    /// <summary>
    /// 영지에 배치된 개별 오브젝트. 생성은 반드시 EstateManager.Place를 통해서만 한다
    /// (수용량 검사 우회 금지 — domain/estate-system.md 성능 상한).
    /// VR 집기(Grab) 어댑터는 MoveTo를 호출해 이동시킨다 — 씬 구성 시점에 연동.
    /// </summary>
    public class PlacedObject : MonoBehaviour
    {
        public EstateObjectDef Def { get; private set; }

        private Rigidbody m_rigidbody;   // 컴포넌트 캐싱 (rules/scripts.md)

        // 물리 API는 FixedUpdate에서만 호출 (RULE-03) — MoveTo 요청을 버퍼링했다가 적용
        private bool       m_hasPendingMove;
        private Vector3    m_pendingPosition;
        private Quaternion m_pendingRotation;

        private void Awake()
        {
            // 배치 오브젝트는 장식물 — 물리 시뮬레이션 대상이 아니므로 kinematic으로 확정.
            // Inspector/Prefab 세팅에 의존하지 않고 코드로 초기값 설정 (rules/scripts.md).
            m_rigidbody = GetComponent<Rigidbody>();
            if (m_rigidbody != null)
            {
                m_rigidbody.isKinematic = true;
                m_rigidbody.useGravity  = false;
            }
        }

        /// <summary>EstateManager.Place 전용 초기화. 재초기화 금지.</summary>
        public void Initialize(EstateObjectDef def)
        {
            if (Def != null)
            {
                Debug.LogWarning($"[PlacedObject] 이미 초기화됨 — 재초기화 무시: {name}");
                return;
            }
            Def = def;
        }

        /// <summary>
        /// 배치 오브젝트 이동. Rigidbody가 있으면 다음 FixedUpdate에서 MovePosition으로 적용하고(RULE-03),
        /// 없으면 Transform을 직접 이동한다.
        /// </summary>
        public void MoveTo(Vector3 pos, Quaternion rot)
        {
            if (m_rigidbody != null)
            {
                m_pendingPosition = pos;
                m_pendingRotation = rot;
                m_hasPendingMove  = true;
            }
            else
            {
                transform.SetPositionAndRotation(pos, rot);
            }
        }

        private void FixedUpdate()
        {
            // RULE-03: Rigidbody 이동은 FixedUpdate에서만
            if (!m_hasPendingMove || m_rigidbody == null) return;

            m_rigidbody.MovePosition(m_pendingPosition);
            m_rigidbody.MoveRotation(m_pendingRotation);
            m_hasPendingMove = false;
        }
    }
}
