using System.Collections.Generic;
using System.Threading.Tasks;
using CampLantern.Cooking;
using CampLantern.Core;
using CampLantern.Estate;
using CampLantern.Fishing;
using CampLantern.Hunting;
using CampLantern.Networking;
using CampLantern.Networking.Voice;
using Fusion;
using UnityEngine;

namespace CampLantern.Bootstrap
{
    /// <summary>
    /// P0 플레이 하네스 — 씬 부트스트랩 + 데스크톱 디버그 UI (IMGUI).
    /// Wallet/Inventory/EstateShop(순수 클래스)을 소유하고 각 시스템을 배선한다:
    /// FishCaught→Inventory, Cooked 표시, RewardGranted→보상 지급, EstateManager.Bind.
    /// VR 입력 어댑터가 생기면 이 하네스의 버튼이 하던 호출(Cast/Reel/Cook/Place...)을 그쪽으로 옮긴다.
    /// OnGUI는 개발용 임시 — 제품 UI 아님 (Quest 빌드 전 제거 대상, unity-mobile-performance.md).
    /// </summary>
    public class P0Harness : MonoBehaviour
    {
        [Header("씬 시스템 참조 (P0PlaySceneFactory가 배선)")]
        [SerializeField] private FishingRod m_rod;
        [SerializeField] private FishingSpot m_spot;
        [SerializeField] private CookingPot m_pot;
        [SerializeField] private EstateManager m_estateManager;
        [SerializeField] private SessionLauncher m_launcher;

        [Header("데이터/프리팹")]
        [Tooltip("상점 카탈로그 — Assets/Data/Estate의 EstateObjectDef들")]
        [SerializeField] private EstateObjectDef[] m_estateCatalog;
        [Tooltip("사냥감 네트워크 프리팹 — 세션 시작 시 마스터 클라이언트가 스폰")]
        [SerializeField] private NetworkObject m_huntTargetPrefab;

        [Header("테스트 편의값")]
        [Tooltip("시작 코인 — 상점 테스트 편의용 (경제 검증 시 0으로)")]
        [SerializeField] private int m_startingCoins = 100;
        [Tooltip("클릭당 사냥 타격량")]
        [SerializeField] private int m_hitDamage = 10;
        [Tooltip("영지 배치 시작 위치 — 이후 2m 간격 격자로 배치")]
        [SerializeField] private Vector3 m_placeOrigin = new Vector3(6f, 0f, 6f);
        [Tooltip("사냥감 스폰 위치")]
        [SerializeField] private Vector3 m_huntSpawnPos = new Vector3(-8f, 0f, 8f);

        private Wallet m_wallet;
        private Inventory m_inventory;
        private EstateShop m_shop;

        private VoiceController m_voice; // m_launcher와 같은 GO에서 캐싱
        private PlayerMute m_mute;

        private HuntTarget m_huntTarget; // 세션 중 스폰된 사냥감 — Update에서 발견 시 구독
        private HuntLedger m_huntLedger;

        private bool m_joining;
        private string m_lastLog = "-";
        private Vector2 m_scroll;

        private void Awake()
        {
            m_wallet    = new Wallet();
            m_inventory = new Inventory();
            m_shop      = new EstateShop(m_wallet, m_inventory);

            if (m_startingCoins > 0) m_wallet.Add(m_startingCoins);

            if (m_launcher != null)
            {
                m_voice = m_launcher.GetComponent<VoiceController>();
                m_mute  = m_launcher.GetComponent<PlayerMute>();
                m_launcher.SessionStarted -= OnSessionStarted;
                m_launcher.SessionStarted += OnSessionStarted;
            }

            if (m_rod != null)
            {
                m_rod.FishCaught -= OnFishCaught;
                m_rod.FishCaught += OnFishCaught;
            }
        }

        private void Start()
        {
            // Push 초기화 원칙 (rules/scripts.md) — 데이터 소유자인 하네스가 준비된 뒤 주입
            if (m_pot != null)
            {
                m_pot.Initialize(m_inventory); // 레시피/실패작은 씬에 세팅된 인스펙터 값 사용
                m_pot.Cooked -= OnCooked;
                m_pot.Cooked += OnCooked;
            }

            if (m_estateManager != null)
                m_estateManager.Bind(m_shop);
        }

        private void OnDestroy()
        {
            if (m_launcher != null) m_launcher.SessionStarted -= OnSessionStarted;
            if (m_rod != null) m_rod.FishCaught -= OnFishCaught;
            if (m_pot != null) m_pot.Cooked -= OnCooked;
            UnhookHuntTarget();
        }

        private void Update()
        {
            // 사냥감은 세션 중 네트워크 스폰이라 씬 참조로 미리 배선 불가 — 등장을 감지해 구독한다
            if (m_huntTarget == null && m_launcher != null && m_launcher.Runner != null)
            {
                var target = FindFirstObjectByType<HuntTarget>();
                if (target != null) HookHuntTarget(target);
            }
        }

        // ── 이벤트 배선 ──────────────────────────────────────────────

        private void OnFishCaught(FishDef fish)
        {
            m_inventory.Add(fish); // FishDef는 ItemDef 파생 — 그대로 인벤토리 투입
            m_lastLog = $"낚음: {fish.DisplayName}";
        }

        private void OnCooked(ItemDef result)
        {
            m_lastLog = $"조리 결과: {result.DisplayName}";
        }

        private void OnSessionStarted(NetworkRunner runner)
        {
            // 사냥감은 마스터 클라이언트만 스폰 — 전원이 스폰하면 중복 생성
            if (m_huntTargetPrefab != null && runner.IsSharedModeMasterClient)
                runner.Spawn(m_huntTargetPrefab, m_huntSpawnPos, Quaternion.identity);
        }

        private void HookHuntTarget(HuntTarget target)
        {
            m_huntTarget = target;
            m_huntLedger = target.GetComponent<HuntLedger>();
            if (m_huntLedger != null)
            {
                m_huntLedger.RewardGranted -= OnRewardGranted;
                m_huntLedger.RewardGranted += OnRewardGranted;
            }
        }

        private void UnhookHuntTarget()
        {
            if (m_huntLedger != null) m_huntLedger.RewardGranted -= OnRewardGranted;
            m_huntTarget = null;
            m_huntLedger = null;
        }

        private void OnRewardGranted(HuntTargetDef def)
        {
            if (def.RewardMaterials == null) return;
            foreach (ItemDef material in def.RewardMaterials)
                m_inventory.Add(material);
            m_lastLog = $"사냥 보상 지급: {def.DisplayName}";
        }

        private async Task JoinSessionAsync()
        {
            m_joining = true;
            try
            {
                await m_launcher.StartHuntZone("p0", destroyCancellationToken);
                m_lastLog = "세션 접속 완료";
            }
            catch (System.OperationCanceledException) { /* 파괴로 인한 취소 — 정상 */ }
            catch (System.Exception e)
            {
                m_lastLog = $"세션 접속 실패: {e.Message}";
                Debug.LogException(e, this);
            }
            finally
            {
                m_joining = false;
            }
        }

        // ── 디버그 UI (개발용 IMGUI) ─────────────────────────────────

        private void OnGUI()
        {
            m_scroll = GUILayout.BeginScrollView(m_scroll, GUILayout.Width(340), GUILayout.Height(Screen.height - 20));

            GUILayout.Label($"[Camp Lantern P0]  코인: {m_wallet.Coins}  |  {m_lastLog}");
            DrawFishing();
            DrawInventory();
            DrawCooking();
            DrawEstate();
            DrawNetwork();

            GUILayout.EndScrollView();
        }

        private void DrawFishing()
        {
            GUILayout.Space(8);
            GUILayout.Label($"── 낚시 ── 상태: {m_rod.State}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("캐스팅")) m_rod.Cast(m_spot);
            if (GUILayout.Button("챔질")) m_rod.Reel();
            GUILayout.EndHorizontal();
        }

        private void DrawInventory()
        {
            GUILayout.Space(8);
            GUILayout.Label("── 인벤토리 ── (투입=냄비, 판매=코인)");
            // 버튼 콜백 중 Inventory가 변해 Dictionary가 수정되므로 스냅샷 순회
            foreach (KeyValuePair<ItemDef, int> entry in new List<KeyValuePair<ItemDef, int>>(m_inventory.Items))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"{entry.Key.DisplayName} x{entry.Value}", GUILayout.Width(140));
                if (GUILayout.Button("투입", GUILayout.Width(60)))
                    m_pot.TryAddIngredient(entry.Key);
                if (GUILayout.Button($"판매 {entry.Key.SellPrice}c", GUILayout.Width(90)) &&
                    m_inventory.TryRemove(entry.Key))
                    m_wallet.Add(entry.Key.SellPrice);
                GUILayout.EndHorizontal();
            }
        }

        private void DrawCooking()
        {
            GUILayout.Space(8);
            var names = new List<string>();
            foreach (ItemDef ingredient in m_pot.Ingredients) names.Add(ingredient.DisplayName);
            GUILayout.Label($"── 요리 ── 냄비: [{string.Join(", ", names)}]");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("조리")) m_pot.Cook();
            if (GUILayout.Button("비우기")) m_pot.Clear();
            GUILayout.EndHorizontal();
        }

        private void DrawEstate()
        {
            GUILayout.Space(8);
            GUILayout.Label($"── 영지 ── 수용량: {m_estateManager.CapacityUsed}/{m_estateManager.CapacityMax}");
            foreach (EstateObjectDef def in m_estateCatalog)
            {
                if (def == null) continue;
                GUILayout.BeginHorizontal();
                string material = def.RequiredMaterial != null
                    ? $" + {def.RequiredMaterial.DisplayName} x{def.RequiredMaterialCount}" : "";
                GUILayout.Label($"{def.DisplayName} ({def.CoinCost}c{material})", GUILayout.Width(180));
                if (def.Rarity == Rarity.Epic)
                {
                    GUILayout.Label("이벤트 전용"); // 에픽은 상점 구매 불가 (estate-system.md)
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

            // 2m 간격 격자에 순서대로 배치 (P0 자유 배치 — 스냅은 P1)
            int index = m_estateManager.PlacedObjects.Count;
            Vector3 pos = m_placeOrigin + new Vector3((index % 4) * 2f, 0f, (index / 4) * 2f);
            PlacedObject placed = m_estateManager.Place(def, pos, Quaternion.identity);
            if (placed == null)
                m_shop.ReturnOwned(def); // CanPlace 통과 후 실패는 도달 불가 — 방어적 반환
            else
                m_lastLog = $"배치: {def.DisplayName}";
        }

        private void DrawNetwork()
        {
            GUILayout.Space(8);
            NetworkRunner runner = m_launcher != null ? m_launcher.Runner : null;

            if (runner == null)
            {
                GUILayout.Label("── 네트워크 ── 미접속");
                GUI.enabled = !m_joining && m_launcher != null;
                if (GUILayout.Button(m_joining ? "접속 중..." : "사냥터 접속 (hunt_zone_p0)"))
                    _ = JoinSessionAsync();
                GUI.enabled = true;
                return;
            }

            GUILayout.Label($"── 네트워크 ── 접속: {runner.SessionInfo.Name} ({runner.SessionInfo.PlayerCount}명)");

            // 음성
            if (m_voice != null && GUILayout.Button($"마이크 {(m_voice.MicEnabled ? "끄기" : "켜기")}"))
                m_voice.SetMicEnabled(!m_voice.MicEnabled);
            if (m_mute != null)
            {
                foreach (PlayerRef player in runner.ActivePlayers)
                {
                    if (player == runner.LocalPlayer) continue;
                    bool muted = m_mute.IsMuted(player);
                    if (GUILayout.Button($"P{player.PlayerId} 음소거 {(muted ? "해제" : "")}"))
                        m_mute.SetMuted(player, !muted);
                }
            }

            // 사냥
            if (m_huntTarget == null)
            {
                GUILayout.Label("사냥감 스폰 대기 중...");
                return;
            }

            GUILayout.Label($"사냥감 HP: {m_huntTarget.CurrentHealth}  진행중: {m_huntTarget.HuntActive}");
            GUILayout.BeginHorizontal();
            if (m_huntTarget.Object.HasStateAuthority)
            {
                if (GUILayout.Button("사냥 시작"))
                    m_lastLog = m_huntTarget.TryStartHunt() ? "사냥 시작!" : "시작 불가 (2인 미만)";
            }
            else
            {
                GUILayout.Label("(시작은 마스터만)", GUILayout.Width(110));
            }
            if (GUILayout.Button("타격"))
                m_huntTarget.ApplyHit(runner.LocalPlayer, m_hitDamage);
            if (m_huntLedger != null && GUILayout.Button("유인(기여)"))
                m_huntLedger.RecordContribution(runner.LocalPlayer, HuntLedger.ContributionKind.Lure);
            GUILayout.EndHorizontal();
        }
    }
}
