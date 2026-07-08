using UnityEngine;
using UnityEngine.SceneManagement;

namespace CampLantern.Bootstrap
{
    /// <summary>
    /// 로비 — 순수 메뉴/포탈 허브 (room-architecture.md). 공유 Room이 아니므로 네트워크 코드 없음.
    /// 각 공간 씬으로 SceneManager.LoadScene만 수행 — 매칭/샤딩은 해당 씬 진입 후 각자 처리한다.
    /// OnGUI는 개발용 임시 — 제품 UI 아님 (Quest 빌드 전 제거 대상, unity-mobile-performance.md).
    /// </summary>
    public class LobbyHarness : MonoBehaviour
    {
        [SerializeField] private string m_fishingSceneName = "FishingGround";
        [SerializeField] private string m_huntSceneName     = "HuntZone_A";
        [SerializeField] private string m_estateSceneName   = "EstateTemplate";

        private void OnGUI()
        {
            GUILayout.Label("[Camp Lantern] 로비 — 포탈 허브");
            GUILayout.Space(8);
            if (GUILayout.Button("낚시터로 이동")) SceneManager.LoadScene(m_fishingSceneName);
            if (GUILayout.Button("사냥터로 이동")) SceneManager.LoadScene(m_huntSceneName);
            if (GUILayout.Button("영지로 이동")) SceneManager.LoadScene(m_estateSceneName);
        }
    }
}
