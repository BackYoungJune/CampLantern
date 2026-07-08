#if UNITY_EDITOR
using System.Threading;
using CampLantern.Bootstrap;
using CampLantern.Core;
using CampLantern.Estate;
using CampLantern.Networking;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampLantern.EditorTools
{
    /// <summary>Room Scenes(로비/낚시터/사냥터/영지) Play Mode 접속 스모크 테스트 — 브릿지 자동화용.</summary>
    public static class RoomScenesPlayTestMenu
    {
        /// <summary>영지에서 구매→배치→저장까지 수행 (라운드트립 검증 1단계).</summary>
        [MenuItem("Tools/P0 Play Test/Persistence - Purchase And Save")]
        public static void PersistencePurchaseAndSave()
        {
            var harness = Object.FindFirstObjectByType<EstateHarness>();
            var estateManager = Object.FindFirstObjectByType<EstateManager>();
            if (harness == null || estateManager == null) { Debug.LogError("[P0PlayTest] EstateHarness/EstateManager 없음 — EstateTemplate 씬에서 Play 필요"); return; }

            var registry = Resources.Load<ContentRegistry>("ContentRegistry");
            if (!registry.TryGetEstateObject("estate_tent", out EstateObjectDef def))
            {
                Debug.LogError("[P0PlayTest] estate_tent Def를 레지스트리에서 못 찾음");
                return;
            }

            var state = harness.State;
            state.Wallet.Add(1000); // 구매 보장용 여분 코인
            bool purchased = state.Shop.TryPurchase(def);
            bool consumed  = purchased && state.Shop.TryConsumeOwned(def);
            var placed = consumed ? estateManager.Place(def, new Vector3(9f, 0f, 9f), Quaternion.identity) : null;

            state.Save(estateManager);

            Debug.LogWarning($"[P0PlayTest] 구매:{purchased} 배치:{placed != null} 저장 전 코인:{state.Wallet.Coins} " +
                              $"배치수:{estateManager.PlacedObjects.Count}");
        }

        /// <summary>씬을 다시 로드해(Awake부터 재실행) 저장된 값이 복원되는지 확인 (라운드트립 검증 2단계).</summary>
        [MenuItem("Tools/P0 Play Test/Persistence - Reload Scene")]
        public static void PersistenceReloadScene()
        {
            SceneManager.LoadScene("EstateTemplate");
        }

        /// <summary>리로드 후 상태 보고 (라운드트립 검증 3단계).</summary>
        [MenuItem("Tools/P0 Play Test/Persistence - Report After Reload")]
        public static void PersistenceReportAfterReload()
        {
            var harness = Object.FindFirstObjectByType<EstateHarness>();
            var estateManager = Object.FindFirstObjectByType<EstateManager>();
            if (harness == null || estateManager == null) { Debug.LogError("[P0PlayTest] EstateHarness/EstateManager 없음"); return; }

            Debug.LogWarning($"[P0PlayTest] 리로드 후 — 코인:{harness.State.Wallet.Coins} " +
                              $"배치수:{estateManager.PlacedObjects.Count}");
        }

        [MenuItem("Tools/P0 Play Test/Join Fishing Ground")]
        public static async void JoinFishingGround()
        {
            var launcher = Object.FindFirstObjectByType<SessionLauncher>();
            if (launcher == null) { Debug.LogError("[P0PlayTest] SessionLauncher 없음"); return; }
            try
            {
                await launcher.StartSession("fishing_shard0", CancellationToken.None);
                Debug.LogWarning($"[P0PlayTest] 낚시터 접속 성공: {launcher.Runner.SessionInfo.Name} ({launcher.Runner.SessionInfo.PlayerCount}명)");
            }
            catch (System.Exception e) { Debug.LogError($"[P0PlayTest] 낚시터 접속 실패: {e.Message}"); }
        }

        [MenuItem("Tools/P0 Play Test/Join Hunt Zone A")]
        public static async void JoinHuntZoneA()
        {
            var launcher = Object.FindFirstObjectByType<SessionLauncher>();
            if (launcher == null) { Debug.LogError("[P0PlayTest] SessionLauncher 없음"); return; }
            try
            {
                await launcher.StartHuntZone("a", CancellationToken.None);
                Debug.LogWarning($"[P0PlayTest] 사냥터_존A 접속 성공: {launcher.Runner.SessionInfo.Name} ({launcher.Runner.SessionInfo.PlayerCount}명)");
            }
            catch (System.Exception e) { Debug.LogError($"[P0PlayTest] 사냥터_존A 접속 실패: {e.Message}"); }
        }

        [MenuItem("Tools/P0 Play Test/Join Estate")]
        public static async void JoinEstate()
        {
            var launcher = Object.FindFirstObjectByType<SessionLauncher>();
            if (launcher == null) { Debug.LogError("[P0PlayTest] SessionLauncher 없음"); return; }
            try
            {
                await launcher.StartSession($"estate_{SystemInfo.deviceUniqueIdentifier}", CancellationToken.None);
                Debug.LogWarning($"[P0PlayTest] 영지 접속 성공: {launcher.Runner.SessionInfo.Name} ({launcher.Runner.SessionInfo.PlayerCount}명)");
            }
            catch (System.Exception e) { Debug.LogError($"[P0PlayTest] 영지 접속 실패: {e.Message}"); }
        }
    }
}
#endif
