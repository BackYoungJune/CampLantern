#if UNITY_EDITOR
using System.Threading;
using CampLantern.Networking;
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
    }
}
#endif
