#if UNITY_EDITOR
using System.Threading;
using System.Threading.Tasks;
using CampLantern.Bootstrap;
using CampLantern.Hunting;
using CampLantern.Networking;
using Fusion;
using UnityEditor;
using UnityEngine;

namespace CampLantern.EditorTools
{
    /// <summary>
    /// Play Mode 스모크 테스트 트리거 — 하네스 버튼을 사람이 누르지 않고도
    /// 브릿지(ExecuteMenuItem)로 세션 접속을 구동해 콘솔 로그로 합불을 판정한다 (/verify 용).
    /// </summary>
    public static class P0PlayTestMenu
    {
        /// <summary>Meta Building Blocks 어셈블리가 Fusion 위버로 처리됐는지 확인 — 위빙 누락이면 러너 초기화가 통째로 실패한다.</summary>
        [MenuItem("Tools/P0 Play Test/Check Meta Weaving")]
        public static void CheckMetaWeaving()
        {
            var type = System.Type.GetType(
                "Meta.XR.MultiplayerBlocks.Colocation.Fusion.FusionAnchor, Meta.XR.MultiplayerBlocks.Fusion");
            if (type == null)
            {
                Debug.LogError("[P0PlayTest] FusionAnchor 타입 로드 실패 — 어셈블리 이름 확인 필요");
                return;
            }

            bool weaved = System.Attribute.IsDefined(type, typeof(global::Fusion.NetworkStructWeavedAttribute));
            if (weaved)
                // Warning 레벨 사용 — 브릿지 콘솔 조회(unity-mcp)가 Info 로그를 반환하지 않아 판정 신호로 씀
                Debug.LogWarning("[P0PlayTest] Meta.XR.MultiplayerBlocks.Fusion 위빙 확인 — OK");
            else
                Debug.LogError("[P0PlayTest] 위빙 안 됨 — NetworkProjectConfig 반영 후 재컴파일 필요");
        }

        [MenuItem("Tools/P0 Play Test/Join Hunt Zone")]
        public static async void JoinHuntZone()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("[P0PlayTest] Play Mode에서만 실행 가능");
                return;
            }

            var launcher = Object.FindFirstObjectByType<SessionLauncher>();
            if (launcher == null)
            {
                Debug.LogError("[P0PlayTest] 씬에 SessionLauncher 없음");
                return;
            }

            try
            {
                await launcher.StartHuntZone("p0", CancellationToken.None);
                // Warning 레벨 사용 — 브릿지 콘솔 조회(unity-mcp)가 Info 로그를 반환하지 않아 판정 신호로 씀
                Debug.LogWarning($"[P0PlayTest] 세션 접속 성공: {launcher.Runner.SessionInfo.Name} " +
                                 $"({launcher.Runner.SessionInfo.PlayerCount}명)");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[P0PlayTest] 세션 접속 실패: {e.Message}");
            }
        }

        /// <summary>
        /// step-08 검증 시나리오 자동 실행 — 더미 피어로 2인 상태를 만들고
        /// ① 1인 사냥 시작 차단 ② 협동 게이트 통과 ③ 더미 기여(RPC) ④ 처치 ⑤ 참여자 판정·보상 발화를 확인한다.
        /// 결과는 LogWarning/LogError로 출력 (브릿지 콘솔 조회가 Info를 반환하지 않음).
        /// </summary>
        [MenuItem("Tools/P0 Play Test/Run Coop Hunt Test")]
        public static async void RunCoopHuntTest()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogError("[P0PlayTest] Play Mode에서만 실행 가능");
                return;
            }

            var launcher = Object.FindFirstObjectByType<SessionLauncher>();
            var harness  = Object.FindFirstObjectByType<P0Harness>();
            if (launcher == null || launcher.Runner == null || harness == null)
            {
                Debug.LogError("[P0PlayTest] 세션 미접속 또는 하네스 없음 — Join Hunt Zone 먼저 실행");
                return;
            }
            NetworkRunner mainRunner = launcher.Runner;

            // 본체 쪽 사냥감 확보
            HuntTarget mainTarget = await WaitFor(() => harness.FindHuntTarget(mainRunner), 10f);
            if (mainTarget == null)
            {
                Debug.LogError("[P0PlayTest] 본체 사냥감 미발견 — 마스터 클라이언트가 아닌가?");
                return;
            }

            // ① 1인 상태면 시작 차단 확인 (더미 접속 전에만 가능)
            if (mainRunner.SessionInfo.PlayerCount < 2 && !mainTarget.HuntActive)
            {
                bool blocked = !mainTarget.TryStartHunt();
                Debug.LogWarning($"[P0PlayTest] ① 1인 사냥 시작 차단: {(blocked ? "OK" : "FAIL — 1인인데 시작됨")}");
            }

            // 더미 피어 확보
            if (harness.DummyRunner == null)
            {
                harness.AddDummyPeer();
                await WaitFor(() => harness.DummyRunner, 15f);
            }
            NetworkRunner dummyRunner = harness.DummyRunner;
            if (dummyRunner == null)
            {
                Debug.LogError("[P0PlayTest] 더미 접속 실패");
                return;
            }

            // PlayerCount 반영 + 더미 쪽 사냥감 복제 대기
            for (int i = 0; i < 100 && mainRunner.SessionInfo.PlayerCount < 2; i++) await Task.Delay(100);
            HuntTarget dummyTarget = await WaitFor(() => harness.FindHuntTarget(dummyRunner), 10f);
            Debug.LogWarning($"[P0PlayTest] ② PlayerCount = {mainRunner.SessionInfo.PlayerCount}, 더미 복제 사냥감: {(dummyTarget != null ? "OK" : "미발견")}");
            if (dummyTarget == null) return;

            var mainLedger  = mainTarget.GetComponent<HuntLedger>();
            var dummyLedger = dummyTarget.GetComponent<HuntLedger>();
            bool rewardFired = false;
            System.Action<CampLantern.Core.HuntTargetDef> onReward = _ => rewardFired = true;
            mainLedger.RewardGranted += onReward;
            try
            {
                // ② 2인 상태 사냥 시작
                if (!mainTarget.HuntActive && !mainTarget.TryStartHunt())
                {
                    Debug.LogError("[P0PlayTest] 2인인데 TryStartHunt 실패");
                    return;
                }

                // ③ 더미 기여 — 더미 러너 인스턴스에서 더미 PlayerRef로 (실제 RPC 경로)
                dummyLedger.RecordContribution(dummyRunner.LocalPlayer, HuntLedger.ContributionKind.Lure);
                await Task.Delay(500); // RPC 왕복 여유

                // ④ 본체 타격으로 처치
                for (int guard = 0; guard < 100 && mainTarget.CurrentHealth > 0; guard++)
                {
                    mainTarget.ApplyHit(mainRunner.LocalPlayer, 10);
                    await Task.Delay(50);
                }
                await Task.Delay(1000); // ChangeDetector(Render) + 보상 발화 여유

                // ⑤ 판정
                bool mainParticipant  = mainLedger.IsParticipant(mainRunner.LocalPlayer);
                bool dummyParticipant = mainLedger.IsParticipant(dummyRunner.LocalPlayer);
                bool pass = mainTarget.CurrentHealth <= 0 && mainParticipant && dummyParticipant && rewardFired;
                string detail = $"HP:{mainTarget.CurrentHealth} 본체참여:{mainParticipant} 더미참여(RPC):{dummyParticipant} 본체보상발화:{rewardFired}";
                if (pass)
                    Debug.LogWarning($"[P0PlayTest] 협동 사냥 테스트 PASS — {detail}");
                else
                    Debug.LogError($"[P0PlayTest] 협동 사냥 테스트 FAIL — {detail}");
            }
            finally
            {
                mainLedger.RewardGranted -= onReward;
            }
        }

        /// <summary>
        /// 같은 사냥감을 재사냥해도 보상이 다시 지급되는지 확인 — RunCoopHuntTest로 1차 처치 완료 후 실행.
        /// m_defeatedFired/Contributions가 새 사이클에서 리셋되는지 검증 (2026-07-08 버그 수정 회귀 테스트).
        /// </summary>
        [MenuItem("Tools/P0 Play Test/Run Second Hunt Round Test")]
        public static async void RunSecondHuntRoundTest()
        {
            var launcher = Object.FindFirstObjectByType<SessionLauncher>();
            var harness  = Object.FindFirstObjectByType<P0Harness>();
            if (launcher == null || launcher.Runner == null || harness == null || harness.DummyRunner == null)
            {
                Debug.LogError("[P0PlayTest] 세션/더미 없음 — Run Coop Hunt Test 먼저 실행해 1차 처치 완료 상태여야 함");
                return;
            }

            NetworkRunner mainRunner = launcher.Runner;
            NetworkRunner dummyRunner = harness.DummyRunner;
            HuntTarget mainTarget = harness.FindHuntTarget(mainRunner);
            HuntTarget dummyTarget = harness.FindHuntTarget(dummyRunner);
            if (mainTarget == null || dummyTarget == null || mainTarget.CurrentHealth > 0)
            {
                Debug.LogError("[P0PlayTest] 사냥감이 처치 상태(HP<=0)가 아님 — 1차 처치를 먼저 완료해야 함");
                return;
            }

            var registry = Resources.Load<CampLantern.Core.ContentRegistry>("ContentRegistry");
            registry.TryGetItem("item_deer_hide", out CampLantern.Core.ItemDef hideDef);
            int hideBefore = harness.State.Inventory.CountOf(hideDef);

            var mainLedger  = mainTarget.GetComponent<HuntLedger>();
            var dummyLedger = dummyTarget.GetComponent<HuntLedger>();
            bool rewardFired = false;
            System.Action<CampLantern.Core.HuntTargetDef> onReward = _ => rewardFired = true;
            mainLedger.RewardGranted += onReward;
            try
            {
                bool started = mainTarget.TryStartHunt();
                dummyLedger.RecordContribution(dummyRunner.LocalPlayer, HuntLedger.ContributionKind.Lure);
                await Task.Delay(500);

                for (int guard = 0; guard < 100 && mainTarget.CurrentHealth > 0; guard++)
                {
                    mainTarget.ApplyHit(mainRunner.LocalPlayer, 10);
                    await Task.Delay(50);
                }
                await Task.Delay(1000);

                int hideAfter = harness.State.Inventory.CountOf(hideDef);
                bool pass = started && rewardFired && hideAfter == hideBefore + 1;
                string detail = $"재사냥시작:{started} 보상재발화:{rewardFired} 가죽:{hideBefore}->{hideAfter}";
                if (pass)
                    Debug.LogWarning($"[P0PlayTest] 2차 사냥 회귀 테스트 PASS — {detail}");
                else
                    Debug.LogError($"[P0PlayTest] 2차 사냥 회귀 테스트 FAIL — {detail}");
            }
            finally
            {
                mainLedger.RewardGranted -= onReward;
            }
        }

        /// <summary>진단용 — 현재 인벤토리의 사슴 가죽/뿔 수량 즉시 보고.</summary>
        [MenuItem("Tools/P0 Play Test/Report Hide Count")]
        public static void ReportHideCount()
        {
            var harness = Object.FindFirstObjectByType<P0Harness>();
            var registry = Resources.Load<CampLantern.Core.ContentRegistry>("ContentRegistry");
            if (harness == null || registry == null) { Debug.LogError("[P0PlayTest] 하네스/레지스트리 없음"); return; }

            registry.TryGetItem("item_deer_hide", out CampLantern.Core.ItemDef hideDef);
            registry.TryGetItem("item_deer_antler", out CampLantern.Core.ItemDef antlerDef);
            Debug.LogWarning($"[P0PlayTest] 현재 인벤토리 — 가죽:{harness.State.Inventory.CountOf(hideDef)} " +
                              $"뿔:{harness.State.Inventory.CountOf(antlerDef)}");
        }

        /// <summary>더미 피어 제거 트리거 — VoiceNetworkObject Despawned 경로 재현용.</summary>
        [MenuItem("Tools/P0 Play Test/Remove Dummy Peer")]
        public static void RemoveDummyPeer()
        {
            var harness = Object.FindFirstObjectByType<P0Harness>();
            if (harness == null) { Debug.LogError("[P0PlayTest] 하네스 없음"); return; }
            harness.RemoveDummyPeer();
            Debug.LogWarning("[P0PlayTest] 더미 제거 요청 전송");
        }

        /// <summary>러너/씬/스폰 상태 스냅샷 — 멀티 피어 문제 진단용.</summary>
        [MenuItem("Tools/P0 Play Test/Report Net State")]
        public static void ReportNetState()
        {
            var sb = new System.Text.StringBuilder("[P0PlayTest] NetState\n");
            foreach (NetworkRunner runner in NetworkRunner.Instances)
            {
                var targets = new System.Collections.Generic.List<HuntTarget>();
                runner.GetAllBehaviours(targets);
                var voices = new System.Collections.Generic.List<Photon.Voice.Fusion.VoiceNetworkObject>();
                runner.GetAllBehaviours(voices);
                sb.AppendLine($"runner:{runner.name} state:{runner.State} master:{runner.IsSharedModeMasterClient} " +
                              $"local:{runner.LocalPlayer} players:{(runner.SessionInfo.IsValid ? runner.SessionInfo.PlayerCount : -1)} " +
                              $"hunt:{targets.Count} voice:{voices.Count}");
            }
            for (int i = 0; i < UnityEngine.SceneManagement.SceneManager.sceneCount; i++)
            {
                var scene = UnityEngine.SceneManagement.SceneManager.GetSceneAt(i);
                sb.AppendLine($"scene[{i}]:{scene.name} roots:{scene.rootCount}");
            }
            Debug.LogWarning(sb.ToString());
        }

        // 조건이 참이 될 때까지 폴링 (Play Mode 비동기 대기용)
        private static async Task<T> WaitFor<T>(System.Func<T> getter, float timeoutSeconds) where T : class
        {
            int tries = Mathf.CeilToInt(timeoutSeconds * 10f);
            for (int i = 0; i < tries; i++)
            {
                T value = getter();
                if (value != null) return value;
                await Task.Delay(100);
            }
            return getter();
        }
    }
}
#endif
