using UnityEngine;

namespace CampLantern.Player
{
    /// <summary>
    /// 각 공간 씬에 하나 배치하는 스폰 위치 마커. 영속 플레이어(<see cref="PersistentPlayer"/>)가
    /// 씬 로드 시 이 지점의 위치·회전으로 이동한다.
    ///
    /// 씬에 여러 개 있으면 첫 번째(FindFirstObjectByType 결과)를 사용한다. 스폰포인트가 없는 씬은
    /// 영속 플레이어가 직전 위치를 유지한다 (예: 스폰 규칙이 아직 없는 임시 씬).
    /// 자체 비주얼/콜라이더는 없다 — 순수 위치 마커.
    /// </summary>
    public class PlayerSpawnPoint : MonoBehaviour
    {
    }
}
