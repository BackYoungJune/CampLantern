#if UNITY_EDITOR
using System.Threading;
using CampLantern.Bootstrap;
using CampLantern.Networking;
using UnityEditor;
using UnityEngine;

namespace CampLantern.EditorTools
{
    /// <summary>Room Scenes(로비/낚시터/사냥터/영지) Play Mode 접속 스모크 테스트 — 브릿지 자동화용.</summary>
    public static class RoomScenesPlayTestMenu
    {
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
