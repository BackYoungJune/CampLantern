#if UNITY_EDITOR
using System.Threading;
using System.Threading.Tasks;
using CampLantern.Bootstrap;
using CampLantern.Core.Persistence;
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

        /// <summary>
        /// QA — 요리 성공/실패 + 영지 수용량 경계를 자동 검증한다.
        /// 성공: 붕어 2마리 투입 → 생선구이 획득, 재료 소모 확인.
        /// 실패: 붕어 1마리(레시피 불일치) 투입 → 실패작(탄 음식) 획득 + Common 재료 소모 확인.
        /// 수용량: CapacityMax까지 배치 반복 → 초과분은 CanPlace가 차단하는지 확인.
        /// </summary>
        [MenuItem("Tools/P0 Play Test/Run Cooking And Estate QA")]
        public static async void RunCookingAndEstateQA()
        {
            var harness = Object.FindFirstObjectByType<P0Harness>();
            var pot = Object.FindFirstObjectByType<CampLantern.Cooking.CookingPot>();
            var estateManager = Object.FindFirstObjectByType<CampLantern.Estate.EstateManager>();
            var registry = Resources.Load<CampLantern.Core.ContentRegistry>("ContentRegistry");
            if (harness == null || pot == null || estateManager == null || registry == null)
            {
                Debug.LogError("[P0PlayTest] 하네스/CookingPot/EstateManager/레지스트리 없음");
                return;
            }

            registry.TryGetItem("fish_crucian", out CampLantern.Core.ItemDef crucian);
            registry.TryGetItem("item_grilled_fish", out CampLantern.Core.ItemDef grilledFish);
            registry.TryGetItem("item_burnt_food", out CampLantern.Core.ItemDef burntFood);
            if (crucian == null || grilledFish == null || burntFood == null)
            {
                Debug.LogError("[P0PlayTest] 요리 QA용 아이템 Id를 레지스트리에서 못 찾음 (fish_crucian/item_grilled_fish/item_burnt_food 확인)");
                return;
            }

            var inv = harness.State.Inventory;

            // ── 요리 성공 ──
            inv.Add(crucian, 2);
            pot.TryAddIngredient(crucian);
            pot.TryAddIngredient(crucian);
            int grilledBefore = inv.CountOf(grilledFish);
            pot.Cook();
            await Task.Delay(100);
            bool cookSuccessPass = inv.CountOf(grilledFish) == grilledBefore + 1 && inv.CountOf(crucian) == 0;
            Debug.LogWarning($"[P0PlayTest] 요리 성공 케이스: {(cookSuccessPass ? "PASS" : "FAIL")} " +
                              $"(생선구이:{inv.CountOf(grilledFish)}, 남은붕어:{inv.CountOf(crucian)})");

            // ── 요리 실패 (붕어 1개만 투입 — 레시피 불일치) ──
            inv.Add(crucian, 1);
            pot.TryAddIngredient(crucian);
            int burntBefore = inv.CountOf(burntFood);
            pot.Cook();
            await Task.Delay(100);
            bool cookFailPass = inv.CountOf(burntFood) == burntBefore + 1 && inv.CountOf(crucian) == 0;
            Debug.LogWarning($"[P0PlayTest] 요리 실패 케이스: {(cookFailPass ? "PASS" : "FAIL")} " +
                              $"(탄음식:{inv.CountOf(burntFood)}, 남은붕어:{inv.CountOf(crucian)})");

            // ── 영지 수용량 경계 ──
            registry.TryGetEstateObject("estate_lantern", out CampLantern.Core.EstateObjectDef lanternDef);
            if (lanternDef == null)
            {
                Debug.LogError("[P0PlayTest] estate_lantern Def 없음");
                return;
            }

            int capacityMax = estateManager.CapacityMax;
            int weight = lanternDef.CapacityWeight;
            int placedCount = 0;
            harness.State.Wallet.Add(100000);
            while (estateManager.CanPlace(lanternDef))
            {
                harness.State.Shop.TryPurchase(lanternDef);
                harness.State.Shop.TryConsumeOwned(lanternDef);
                var p = estateManager.Place(lanternDef, new Vector3(20f + placedCount, 0f, 20f), Quaternion.identity);
                if (p == null) break; // 안전장치 — 무한루프 방지
                placedCount++;
                if (placedCount > 100) { Debug.LogError("[P0PlayTest] 배치 루프 100회 초과 — 중단"); break; }
            }
            bool overCapacityBlocked = !estateManager.CanPlace(lanternDef);
            int expectedMaxCount = capacityMax / weight;
            bool capacityPass = overCapacityBlocked && estateManager.CapacityUsed <= capacityMax &&
                                 placedCount == expectedMaxCount;
            Debug.LogWarning($"[P0PlayTest] 영지 수용량 경계: {(capacityPass ? "PASS" : "FAIL")} " +
                              $"(배치수:{placedCount}, 기대치:{expectedMaxCount}, 사용량:{estateManager.CapacityUsed}/{capacityMax}, 초과차단:{overCapacityBlocked})");
        }

        /// <summary>
        /// QA — 저장된 배치가 축소된 수용량을 초과할 때 데이터 유실 없이 보유 목록으로 반환되는지 검증
        /// (2026-07-08 수정: EstateHarness/P0Harness 배치 복원 루프의 Place() 반환값 무시 버그 회귀 테스트).
        /// 씬 리로드로는 인스펙터 기본값(20)이 되살아나 재현이 안 되므로, 실행 중인 EstateManager의
        /// CapacityMax를 리플렉션으로 임시 축소한 뒤 복원 루프와 동일한 로직을 직접 수행해 검증한다.
        /// </summary>
        [MenuItem("Tools/P0 Play Test/Run Capacity Restore QA")]
        public static void RunCapacityRestoreQA()
        {
            var harness = Object.FindFirstObjectByType<P0Harness>();
            var estateManager = Object.FindFirstObjectByType<CampLantern.Estate.EstateManager>();
            var registry = Resources.Load<CampLantern.Core.ContentRegistry>("ContentRegistry");
            if (harness == null || estateManager == null || registry == null)
            {
                Debug.LogError("[P0PlayTest] 하네스/EstateManager/레지스트리 없음");
                return;
            }
            if (!registry.TryGetEstateObject("estate_lantern", out CampLantern.Core.EstateObjectDef lanternDef))
            {
                Debug.LogError("[P0PlayTest] estate_lantern Def 없음");
                return;
            }

            var capacityField = typeof(CampLantern.Estate.EstateManager)
                .GetField("m_capacityMax", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            int originalCapacity = estateManager.CapacityMax;

            try
            {
                capacityField.SetValue(estateManager, 2); // 3개를 복원 시도하면 1개는 초과되도록 축소

                int ownedBefore = harness.State.Shop.CountOwned(lanternDef);
                var fakeSaved = new[]
                {
                    new PlacedObjectSave { DefId = "estate_lantern", Position = new Vector3(30f, 0f, 0f), Rotation = Quaternion.identity },
                    new PlacedObjectSave { DefId = "estate_lantern", Position = new Vector3(31f, 0f, 0f), Rotation = Quaternion.identity },
                    new PlacedObjectSave { DefId = "estate_lantern", Position = new Vector3(32f, 0f, 0f), Rotation = Quaternion.identity },
                };

                int placedCount = 0, returnedCount = 0;
                foreach (PlacedObjectSave saved in fakeSaved)
                {
                    if (!registry.TryGetEstateObject(saved.DefId, out CampLantern.Core.EstateObjectDef def)) continue;
                    if (estateManager.Place(def, saved.Position, saved.Rotation) == null)
                    {
                        harness.State.Shop.ReturnOwned(def);
                        returnedCount++;
                    }
                    else
                    {
                        placedCount++;
                    }
                }

                int ownedAfter = harness.State.Shop.CountOwned(lanternDef);
                bool pass = placedCount == 2 && returnedCount == 1 && ownedAfter == ownedBefore + 1;
                Debug.LogWarning($"[P0PlayTest] 수용량 초과 복원 QA: {(pass ? "PASS" : "FAIL")} " +
                                  $"(배치:{placedCount} 반환:{returnedCount} 보유목록:{ownedBefore}->{ownedAfter})");
            }
            finally
            {
                capacityField.SetValue(estateManager, originalCapacity); // 원복
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
