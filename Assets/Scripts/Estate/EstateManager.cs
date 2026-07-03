using System;
using System.Collections.Generic;
using CampLantern.Core;
using UnityEngine;

namespace CampLantern.Estate
{
    /// <summary>
    /// 영지 배치 목록·수용량 관리. 배치의 유일한 진입점 — 모든 배치는 Place를 통해서만 하며,
    /// Place는 내부에서 CanPlace(수용량 검사)를 반드시 통과해야 한다. 우회 경로 없음
    /// (캠프 수용량 = 렌더링·물리 비용 가중치 상한, domain/estate-system.md 성능 안전장치).
    /// P0: 배치 데이터 저장 없음 — 세션 메모리만. 자유 배치만 (스냅/되돌리기/방문 권한은 P1·소셜 MVP).
    /// </summary>
    public class EstateManager : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("캠프 수용량 상한 — P0 임시 고정값. 가중치(CapacityWeight) 합산 기준")]
        private int m_capacityMax = 20;

        private readonly List<PlacedObject> m_placedObjects = new List<PlacedObject>();
        private int m_capacityUsed;

        // 회수 시 Def를 되돌릴 보유 목록 소유자 — 씬 부트스트랩에서 Bind로 연결 (EstateShop은 순수 클래스)
        private EstateShop m_shop;

        /// <summary>현재 사용 중인 수용량 (배치된 오브젝트 CapacityWeight 합).</summary>
        public int CapacityUsed => m_capacityUsed;

        /// <summary>수용량 상한. 유저에게는 "남은 배치 가능량"으로 표시할 것 (domain/estate-system.md).</summary>
        public int CapacityMax => m_capacityMax;

        /// <summary>배치/회수로 수용량이 변할 때 발화. 구독자는 OnDestroy/OnDisable에서 반드시 해제할 것 (rules/scripts.md).</summary>
        public event Action CapacityChanged;

        public IReadOnlyList<PlacedObject> PlacedObjects => m_placedObjects;

        /// <summary>회수된 Def를 되돌릴 상점(보유 목록)을 연결한다 — 씬 부트스트랩에서 호출.</summary>
        public void Bind(EstateShop shop)
        {
            m_shop = shop;
        }

        /// <summary>
        /// 수용량 검사 — 배치 가능 여부. Place는 내부에서 이 검사를 반드시 수행하므로
        /// 호출자가 생략해도 수용량 초과 배치는 발생하지 않는다.
        /// </summary>
        public bool CanPlace(EstateObjectDef def)
        {
            if (def == null) return false;
            return m_capacityUsed + WeightOf(def) <= m_capacityMax;
        }

        /// <summary>
        /// 배치의 유일한 경로. 수용량 초과 시 null 반환 — 우회 경로를 만들지 않는다.
        /// 보유 목록 소비(EstateShop.TryConsumeOwned)는 호출측(배치 UI/어댑터)이 성공 확인 후 이 메서드를 호출한다.
        /// </summary>
        public PlacedObject Place(EstateObjectDef def, Vector3 pos, Quaternion rot)
        {
            if (!CanPlace(def)) return null;   // 수용량 검사 — 필수, 우회 금지

            GameObject go;
            if (def.Prefab != null)
            {
                go = Instantiate(def.Prefab, pos, rot);
            }
            else
            {
                // P0: 프리팹 미지정 Def도 테스트 가능하도록 빈 오브젝트로 배치
                go = new GameObject();
                go.transform.SetPositionAndRotation(pos, rot);
            }
            go.name = $"Placed_{def.Id}";

            var placed = go.GetComponent<PlacedObject>();
            if (placed == null)
                placed = go.AddComponent<PlacedObject>();
            placed.Initialize(def);

            m_placedObjects.Add(placed);
            m_capacityUsed += WeightOf(def);
            CapacityChanged?.Invoke();

            return placed;
        }

        /// <summary>
        /// 배치 오브젝트 회수. Def는 Inventory가 아니라 상점의 Def 보유 목록으로 되돌아간다.
        /// </summary>
        public void Remove(PlacedObject obj)
        {
            if (obj == null) return;
            if (!m_placedObjects.Remove(obj))
            {
                Debug.LogWarning($"[EstateManager] 관리 목록에 없는 오브젝트 회수 시도: {obj.name}");
                return;
            }

            m_capacityUsed -= WeightOf(obj.Def);
            if (m_capacityUsed < 0) m_capacityUsed = 0;   // 방어 — 음수 불가

            m_shop?.ReturnOwned(obj.Def);                 // 보유 목록으로 반환 (Inventory 아님)

            Destroy(obj.gameObject);
            CapacityChanged?.Invoke();
        }

        private static int WeightOf(EstateObjectDef def)
        {
            // 가중치 0/음수는 데이터 오류 — 성능 상한 안전장치가 무력화되지 않도록 최소 1로 보정
            if (def == null) return 0;
            if (def.CapacityWeight < 1)
            {
                Debug.LogError($"[EstateManager] CapacityWeight < 1 (데이터 오류): {def.Id} — 1로 보정");
                return 1;
            }
            return def.CapacityWeight;
        }
    }
}
