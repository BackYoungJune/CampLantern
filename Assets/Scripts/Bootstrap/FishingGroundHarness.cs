using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CampLantern.Core;
using CampLantern.Fishing;
using CampLantern.Networking;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampLantern.Bootstrap
{
    /// <summary>
    /// 낚시터 — 싱글/멀티 선택, 상시 열림, 파티 구성 없이 드롭인 (room-architecture.md).
    /// P0는 실제 매칭/샤딩 없이 고정 샤드 이름("fishing_{m_shardId}")으로만 합류한다 —
    /// "정원 도달 시 새 인스턴스로 샤딩"은 매칭 백엔드가 필요해 P0 범위 밖.
    /// 인벤토리는 씬 전용 세션 메모리 — 영구 저장/씬 간 연속성은 백엔드 미정(tech-stack-decisions.md).
    /// </summary>
    public class FishingGroundHarness : MonoBehaviour
    {
        [SerializeField] private FishingRod m_rod;
        [SerializeField] private FishingSpot m_spot;
        [SerializeField] private SessionLauncher m_launcher;
        [SerializeField] private string m_lobbySceneName = "Lobby";
        [SerializeField] private string m_shardId = "shard0";

        private readonly Inventory m_inventory = new Inventory();
        private bool m_joining;
        private string m_lastLog = "-";

        private void Awake()
        {
            m_rod.FishCaught -= OnFishCaught;
            m_rod.FishCaught += OnFishCaught;
        }

        private void OnDestroy()
        {
            m_rod.FishCaught -= OnFishCaught;
        }

        private void OnFishCaught(FishDef fish)
        {
            m_inventory.Add(fish);
            m_lastLog = $"낚음: {fish.DisplayName}";
        }

        private async Task JoinAsync()
        {
            m_joining = true;
            try
            {
                await m_launcher.StartSession($"fishing_{m_shardId}", destroyCancellationToken);
                m_lastLog = "낚시터 접속 완료";
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
            GUILayout.Label($"[낚시터] {m_lastLog}");
            if (GUILayout.Button("로비로 복귀")) SceneManager.LoadScene(m_lobbySceneName);
            GUILayout.Space(8);

            var runner = m_launcher.Runner;
            if (runner == null)
            {
                GUI.enabled = !m_joining;
                if (GUILayout.Button(m_joining ? "접속 중..." : "낚시터 접속 (드롭인)"))
                    _ = JoinAsync();
                GUI.enabled = true;
            }
            else
            {
                GUILayout.Label($"접속: {runner.SessionInfo.Name} ({runner.SessionInfo.PlayerCount}명) — 고정 샤드, 실제 매칭/샤딩 TBD");
            }

            GUILayout.Space(8);
            GUILayout.Label($"── 낚시 ── 상태: {m_rod.State}");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("캐스팅")) m_rod.Cast(m_spot);
            if (GUILayout.Button("챔질")) m_rod.Reel();
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            GUILayout.Label("── 인벤토리 ──");
            foreach (KeyValuePair<ItemDef, int> entry in new List<KeyValuePair<ItemDef, int>>(m_inventory.Items))
                GUILayout.Label($"{entry.Key.DisplayName} x{entry.Value}");
        }
    }
}
