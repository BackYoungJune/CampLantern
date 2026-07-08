using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampLantern.Cooking;
using CampLantern.Core;
using CampLantern.Estate;
using CampLantern.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampLantern.Bootstrap
{
    /// <summary>
    /// 영지 — 개인 소유 공간, roomName = estate_{ownerId} (room-architecture.md).
    /// 주인 오프라인이어도 방문 가능해야 하는데, 유저 인증·영구 저장 백엔드가 아직 미정이라
    /// (tech-stack-decisions.md) P0에서는 기기별 임시 식별자로 자기 영지에만 접속하는 템플릿이다.
    /// 오프라인 방문·읽기 전용 공유는 백엔드 확정 후 착수.
    /// </summary>
    public class EstateHarness : MonoBehaviour
    {
        [SerializeField] private EstateManager m_estateManager;
        [SerializeField] private CookingPot m_pot;
        [SerializeField] private SessionLauncher m_launcher;
        [SerializeField] private EstateObjectDef[] m_estateCatalog;
        [SerializeField] private string m_lobbySceneName = "Lobby";
        [SerializeField] private int m_startingCoins = 100;
        [SerializeField] private Vector3 m_placeOrigin = new Vector3(3f, 0f, 3f);

        private Wallet m_wallet;
        private Inventory m_inventory;
        private EstateShop m_shop;

        private bool m_joining;
        private string m_lastLog = "-";

        private void Awake()
        {
            m_wallet    = new Wallet();
            m_inventory = new Inventory();
            m_shop      = new EstateShop(m_wallet, m_inventory);
            if (m_startingCoins > 0) m_wallet.Add(m_startingCoins);
        }

        private void Start()
        {
            m_pot.Initialize(m_inventory);
            m_pot.Cooked -= OnCooked;
            m_pot.Cooked += OnCooked;

            m_estateManager.Bind(m_shop);
        }

        private void OnDestroy()
        {
            m_pot.Cooked -= OnCooked;
        }

        private void OnCooked(ItemDef result)
        {
            m_lastLog = $"조리 결과: {result.DisplayName}";
        }

        // 영지 소유자 식별 — 인증 백엔드 확정 전 임시값 (SystemInfo.deviceUniqueIdentifier).
        // 실제 유저 계정 도입 시 이 부분을 로그인 결과로 교체할 것.
        private static string OwnerId => SystemInfo.deviceUniqueIdentifier;

        private async Task JoinAsync()
        {
            m_joining = true;
            try
            {
                await m_launcher.StartSession($"estate_{OwnerId}", destroyCancellationToken);
                m_lastLog = "영지 접속 완료";
            }
            catch (OperationCanceledException) { /* 파괴로 인한 취소 — 정상 */ }
            catch (Exception e)
            {
                m_lastLog = $"접속 실패: {e.Message}";
                Debug.LogException(e, this);
            }
            finally
            {
                m_joining = false;
            }
        }

        private void OnGUI()
        {
            GUILayout.Label($"[영지] 코인: {m_wallet.Coins}  |  {m_lastLog}");
            if (GUILayout.Button("로비로 복귀")) SceneManager.LoadScene(m_lobbySceneName);
            GUILayout.Space(8);

            var runner = m_launcher.Runner;
            if (runner == null)
            {
                GUI.enabled = !m_joining;
                if (GUILayout.Button(m_joining ? "접속 중..." : "내 영지 접속"))
                    _ = JoinAsync();
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label($"접속: {runner.SessionInfo.Name} ({runner.SessionInfo.PlayerCount}명) — 오프라인 방문은 백엔드 TBD");
            }

            GUILayout.Space(8);
            GUILayout.Label("── 인벤토리 ── (투입=냄비, 판매=코인)");
            foreach (KeyValuePair<ItemDef, int> entry in new List<KeyValuePair<ItemDef, int>>(m_inventory.Items))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{entry.Key.DisplayName} x{entry.Value}", GUILayout.Width(140));
                if (GUILayout.Button("투입", GUILayout.Width(60))) m_pot.TryAddIngredient(entry.Key);
                if (GUILayout.Button($"판매 {entry.Key.SellPrice}c", GUILayout.Width(90)) &&
                    m_inventory.TryRemove(entry.Key))
                    m_wallet.Add(entry.Key.SellPrice);
                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8);
            var names = new List<string>();
            foreach (ItemDef ingredient in m_pot.Ingredients) names.Add(ingredient.DisplayName);
            GUILayout.Label($"── 요리 ── 냄비: [{string.Join(", ", names)}]");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("조리")) m_pot.Cook();
            if (GUILayout.Button("비우기")) m_pot.Clear();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label($"── 영지 배치 ── 수용량: {m_estateManager.CapacityUsed}/{m_estateManager.CapacityMax}");
            foreach (EstateObjectDef def in m_estateCatalog)
            {
                if (def == null) continue;
                GUILayout.BeginHorizontal();
                string material = def.RequiredMaterial != null
                    ? $" + {def.RequiredMaterial.DisplayName} x{def.RequiredMaterialCount}" : "";
                GUILayout.Label($"{def.DisplayName} ({def.CoinCost}c{material})", GUILayout.Width(180));
                if (def.Rarity == Rarity.Epic)
                {
                    GUILayout.Label("이벤트 전용");
                }
                else
                {
                    if (GUILayout.Button("구매", GUILayout.Width(50)))
                        m_lastLog = m_shop.TryPurchase(def) ? $"구매: {def.DisplayName}" : "구매 실패 (재화 부족)";
                    int owned = m_shop.CountOwned(def);
                    if (owned > 0 && GUILayout.Button($"배치({owned})", GUILayout.Width(70)))
                        TryPlace(def);
                }
                GUILayout.EndHorizontal();
            }

            if (m_estateManager.PlacedObjects.Count > 0 && GUILayout.Button("마지막 배치물 회수"))
                m_estateManager.Remove(m_estateManager.PlacedObjects[m_estateManager.PlacedObjects.Count - 1]);
        }

        private void TryPlace(EstateObjectDef def)
        {
            if (!m_estateManager.CanPlace(def))
            {
                m_lastLog = "배치 실패 — 수용량 초과";
                return;
            }
            if (!m_shop.TryConsumeOwned(def)) return;

            int index = m_estateManager.PlacedObjects.Count;
            Vector3 pos = m_placeOrigin + new Vector3((index % 4) * 2f, 0f, (index / 4) * 2f);
            PlacedObject placed = m_estateManager.Place(def, pos, Quaternion.identity);
            if (placed == null) m_shop.ReturnOwned(def);
            else m_lastLog = $"배치: {def.DisplayName}";
        }
    }
}
