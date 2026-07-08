using System.Collections.Generic;
using UnityEngine;

namespace CampLantern.Core
{
    /// <summary>
    /// Id 문자열 → ItemDef/EstateObjectDef 에셋 조회 레지스트리. 저장 파일(JSON)에는 Id만 기록되므로
    /// 로드 시 실제 에셋 참조를 이걸로 복원한다 (ScriptableObject는 JSON 직렬화 대상이 아님).
    /// Assets/Resources/ContentRegistry.asset에 위치 — 씬 배선 없이 Resources.Load로 어디서든 접근하기 위함.
    /// 생성/갱신은 Editor 팩토리(ContentRegistryFactory, Tools > Make Assets > Content Registry)가
    /// Assets/Data를 스캔해서 자동으로 한다 — 새 콘텐츠 데이터 추가 후 재실행 필요.
    /// </summary>
    public class ContentRegistry : ScriptableObject
    {
        [SerializeField] private ItemDef[] m_items;
        [SerializeField] private EstateObjectDef[] m_estateObjects;

        private Dictionary<string, ItemDef> m_itemLookup;
        private Dictionary<string, EstateObjectDef> m_estateLookup;

        public bool TryGetItem(string id, out ItemDef def)
        {
            EnsureLookup();
            return m_itemLookup.TryGetValue(id, out def);
        }

        public bool TryGetEstateObject(string id, out EstateObjectDef def)
        {
            EnsureLookup();
            return m_estateLookup.TryGetValue(id, out def);
        }

        private void EnsureLookup()
        {
            if (m_itemLookup != null) return;

            m_itemLookup = new Dictionary<string, ItemDef>();
            if (m_items != null)
            {
                foreach (ItemDef item in m_items)
                {
                    if (item == null || string.IsNullOrEmpty(item.Id)) continue;
                    m_itemLookup[item.Id] = item;
                }
            }

            m_estateLookup = new Dictionary<string, EstateObjectDef>();
            if (m_estateObjects != null)
            {
                foreach (EstateObjectDef def in m_estateObjects)
                {
                    if (def == null || string.IsNullOrEmpty(def.Id)) continue;
                    m_estateLookup[def.Id] = def;
                }
            }
        }
    }
}
