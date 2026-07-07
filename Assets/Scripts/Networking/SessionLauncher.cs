using System;
using System.Threading;
using System.Threading.Tasks;
using Fusion;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampLantern.Networking
{
    /// <summary>
    /// Fusion 2 세션 진입/종료 관리. P0 범위는 사냥터 존 하나뿐이다 (step-07).
    /// 로비/낚시터/영지 Room은 P0 범위 밖 — 여기에 추가하지 않는다 (room-architecture.md).
    /// NetworkRunner는 씬 배치 대신 이 컴포넌트가 같은 GameObject에 AddComponent로 부착한다 (RULE-02).
    /// </summary>
    public class SessionLauncher : MonoBehaviour
    {
        /// <summary>현재 실행 중인 러너. 세션 없으면 null.</summary>
        public NetworkRunner Runner { get; private set; }

        /// <summary>세션 시작 성공 시 발화. 구독자는 OnDestroy/OnDisable에서 반드시 해제할 것 (rules/scripts.md).</summary>
        public event Action<NetworkRunner> SessionStarted;

        /// <summary>
        /// Shared Mode로 사냥터 존 Room에 접속한다. roomName 규칙: hunt_zone_{zoneId}.
        /// 같은 zoneId로 접속한 클라이언트는 같은 Room에 합류한다 — P0는 매칭/샤딩 없이 고정 zoneId만 사용.
        /// </summary>
        public async Task StartHuntZone(string zoneId, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(zoneId))
                throw new ArgumentException("zoneId가 비어 있음", nameof(zoneId));
            if (Runner != null)
            {
                Debug.LogWarning("[SessionLauncher] 이미 세션이 실행 중 — 중복 시작 무시");
                return;
            }

            var runner = gameObject.AddComponent<NetworkRunner>();
            runner.ProvideInput = true; // step-08 협동 사냥 입력용 — Shared Mode에서는 로컬 플레이어 입력 제공 필수

            // StartGame이 SceneManager 없이도 기본값을 만들어주지만, FusionBootstrap과 동일하게 명시 부착
            var sceneManager = runner.GetComponent<INetworkSceneManager>()
                               ?? (INetworkSceneManager)runner.gameObject.AddComponent<NetworkSceneManagerDefault>();

            var args = new StartGameArgs
            {
                GameMode                 = GameMode.Shared, // 데디케이티드/호스트 모드는 P0 불필요 (step-07 제약)
                SessionName              = $"hunt_zone_{zoneId}",
                SceneManager             = sceneManager,
                StartGameCancellationToken = ct,
            };

            // 멀티 피어 모드(에디터 더미 테스트)일 때만 시작 씬 지정 — 싱글 피어 경로는 기존 그대로
            NetworkSceneInfo? arenaScene = TryGetMultiPeerArenaScene();
            if (arenaScene.HasValue) args.Scene = arenaScene.Value;

            var result = await runner.StartGame(args);

            if (!result.Ok)
            {
                Destroy(runner);
                throw new InvalidOperationException(
                    $"[SessionLauncher] 세션 시작 실패: {result.ShutdownReason} — {result.ErrorMessage}");
            }

            ct.ThrowIfCancellationRequested();

            Runner = runner;
            SessionStarted?.Invoke(Runner);
        }

        /// <summary>세션을 종료하고 러너를 제거한다. 세션이 없으면 아무것도 하지 않는다.</summary>
        public async Task Shutdown()
        {
            if (Runner == null) return;

            var runner = Runner;
            Runner = null;

            // destroyGameObject: false 필수 — true(기본값)면 SessionLauncher가 붙은 GameObject째 파괴된다
            await runner.Shutdown(destroyGameObject: false);
            if (runner != null) Destroy(runner);
        }

        private void OnDestroy()
        {
            // 파괴 시 연결 누수 방지 — await 불가 지점이므로 동기 킥오프만
            if (Runner != null && Runner.IsRunning)
                Runner.Shutdown(destroyGameObject: false);
        }

        /// <summary>
        /// 멀티 피어 모드(에디터 더미 테스트)에서는 StartGameArgs.Scene 지정이 필수 —
        /// 없으면 씬 준비 상태가 완료되지 않아 스폰 큐가 영영 처리되지 않는다 (경고가 아니라 실질 고장).
        /// 빈 아레나 씬(P0NetArena)을 피어별로 로드시킨다. 싱글 피어에서는 null 반환 (기존 동작 유지).
        /// 더미 피어(P0Harness)도 같은 헬퍼를 사용한다.
        /// </summary>
        internal static NetworkSceneInfo? TryGetMultiPeerArenaScene()
        {
            if (NetworkProjectConfig.Global.PeerMode != NetworkProjectConfig.PeerModes.Multiple)
                return null;

            int buildIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/P0NetArena.unity");
            if (buildIndex < 0)
            {
                Debug.LogWarning("[SessionLauncher] 멀티 피어 모드인데 P0NetArena 씬이 Build Settings에 없음 — " +
                                 "Tools > Make Assets > P0 Net Arena Scene 실행 필요. 스폰이 처리되지 않을 수 있음");
                return null;
            }

            var sceneInfo = new NetworkSceneInfo();
            sceneInfo.AddSceneRef(SceneRef.FromIndex(buildIndex), LoadSceneMode.Additive);
            return sceneInfo;
        }
    }
}
