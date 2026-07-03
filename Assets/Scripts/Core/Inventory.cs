using System;
using System.Collections.Generic;

namespace CampLantern.Core
{
    /// <summary>
    /// 아이템 보유 목록. 모든 아이템 증감은 반드시 이 클래스를 통한다.
    /// P0에서는 저장 없음 — 세션 메모리 상태만 (백엔드 미정, domain/tech-stack-decisions.md).
    /// </summary>
    public class Inventory
    {
        private readonly Dictionary<ItemDef, int> m_items = new Dictionary<ItemDef, int>();

        /// <summary>보유 목록 변경 시 발화. 구독자는 OnDestroy/OnDisable에서 반드시 해제할 것 (rules/scripts.md).</summary>
        public event Action Changed;

        public IReadOnlyDictionary<ItemDef, int> Items => m_items;

        public void Add(ItemDef item, int count = 1)
        {
            if (item == null || count <= 0) return;

            m_items.TryGetValue(item, out int current);
            m_items[item] = current + count;
            Changed?.Invoke();
        }

        /// <summary>보유 수량이 충분하면 차감 후 true. 부족하면 목록을 건드리지 않고 false.</summary>
        public bool TryRemove(ItemDef item, int count = 1)
        {
            if (item == null || count <= 0) return false;
            if (!m_items.TryGetValue(item, out int current) || current < count) return false;

            int remaining = current - count;
            if (remaining > 0)
                m_items[item] = remaining;
            else
                m_items.Remove(item);

            Changed?.Invoke();
            return true;
        }

        public int CountOf(ItemDef item)
        {
            if (item == null) return 0;
            return m_items.TryGetValue(item, out int count) ? count : 0;
        }
    }
}
