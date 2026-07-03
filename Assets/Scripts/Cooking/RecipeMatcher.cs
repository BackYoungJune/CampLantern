using System.Collections.Generic;
using CampLantern.Core;

namespace CampLantern.Cooking
{
    /// <summary>
    /// 재료 조합 → 레시피 매칭 순수 로직 (MonoBehaviour 아님 — 테스트 가능).
    /// 순서 무관·수량 일치 비교. LINQ 없이 카운트 딕셔너리로 비교해 GC를 회피한다
    /// (knowledge/unity-mobile-performance.md).
    /// </summary>
    public class RecipeMatcher
    {
        /// <summary>레시피 하나의 재료 구성을 미리 카운트해 둔 캐시.</summary>
        private struct RecipeEntry
        {
            public RecipeDef Recipe;
            public Dictionary<ItemDef, int> Counts;
            public int Total;
        }

        private readonly List<RecipeEntry> m_entries = new List<RecipeEntry>();

        // Match 호출마다 새 딕셔너리를 만들지 않도록 재사용 (GC 회피)
        private readonly Dictionary<ItemDef, int> m_queryCounts = new Dictionary<ItemDef, int>();

        public RecipeMatcher(IReadOnlyList<RecipeDef> recipes)
        {
            if (recipes == null) return;

            for (int i = 0; i < recipes.Count; i++)
            {
                RecipeDef recipe = recipes[i];
                if (recipe == null || recipe.Ingredients == null || recipe.Ingredients.Length == 0)
                    continue;

                var counts = new Dictionary<ItemDef, int>();
                int total = 0;
                for (int j = 0; j < recipe.Ingredients.Length; j++)
                {
                    ItemDef ingredient = recipe.Ingredients[j];
                    if (ingredient == null) continue;

                    counts.TryGetValue(ingredient, out int current);
                    counts[ingredient] = current + 1;
                    total++;
                }

                if (total == 0) continue;

                m_entries.Add(new RecipeEntry
                {
                    Recipe = recipe,
                    Counts = counts,
                    Total  = total,
                });
            }
        }

        /// <summary>
        /// 재료 조합이 레시피와 일치하면 해당 레시피, 아니면 null.
        /// 순서 무관, 수량까지 정확히 일치해야 성공 (여분 재료가 있으면 실패).
        /// </summary>
        public RecipeDef Match(IReadOnlyList<ItemDef> ingredients)
        {
            if (ingredients == null || ingredients.Count == 0) return null;

            // 투입 재료 카운트 집계 (재사용 딕셔너리)
            m_queryCounts.Clear();
            int queryTotal = 0;
            for (int i = 0; i < ingredients.Count; i++)
            {
                ItemDef ingredient = ingredients[i];
                if (ingredient == null) continue;

                m_queryCounts.TryGetValue(ingredient, out int current);
                m_queryCounts[ingredient] = current + 1;
                queryTotal++;
            }

            if (queryTotal == 0) return null;

            for (int i = 0; i < m_entries.Count; i++)
            {
                RecipeEntry entry = m_entries[i];

                // 총 수량이 다르면 비교 불필요
                if (entry.Total != queryTotal) continue;

                // 총 수량이 같으므로, 레시피 쪽 모든 항목의 수량이 일치하면 완전 일치
                bool matched = true;
                foreach (KeyValuePair<ItemDef, int> pair in entry.Counts)
                {
                    if (!m_queryCounts.TryGetValue(pair.Key, out int queryCount) || queryCount != pair.Value)
                    {
                        matched = false;
                        break;
                    }
                }

                if (matched) return entry.Recipe;
            }

            return null;
        }
    }
}
