using Fusion;
using Meta.XR.MultiplayerBlocks.Fusion;
using UnityEngine;

namespace CampLantern.Networking.Avatar
{
    /// <summary>
    /// Fusion 세션에 Meta Avatars(네트워크 아바타)를 연결한다.
    /// SessionLauncher가 세션을 시작하면 플레이어당 아바타(FusionAvatarSdk28Plus 프리팹)를 1개 스폰한다.
    ///
    /// 왜 Meta의 AvatarSpawnerFusion을 쓰지 않는가:
    ///   AvatarSpawnerFusion은 FusionBBEvents.OnSceneLoadDone(Meta Building Blocks 표준 부트스트랩 이벤트)로
    ///   러너를 얻는데, 이 프로젝트는 커스텀 SessionLauncher가 런타임에 러너를 AddComponent하므로 그 이벤트가
    ///   발화하지 않는다 → 아바타가 영영 스폰되지 않는다. 그래서 VoiceController와 동일하게
    ///   SessionLauncher.SessionStarted에 직접 얹어 runner.Spawn 한다 (tech-stack-decisions.md
    ///   "런타임 AddComponent된 러너는 자동 콜백 수집에서 빠진다" 패턴과 같은 맥락).
    ///
    /// 씬 선행 조건 (AvatarSetupFactory가 배선):
    ///   - OVRCameraRig (VRPlayerRig) — 로컬 아바타가 헤드/핸드를 따라가는 소스. AvatarBehaviourFusion이
    ///     OVRManager.instance.GetComponentInChildren&lt;OVRCameraRig&gt;()로 찾는다.
    ///   - OvrAvatarManager (AvatarSdkManagerStyle2Meta) — 아바타 에셋 로딩 매니저. 씬에 1개 필수.
    ///   - SampleInputManager (OvrAvatarInputManager) — 로컬 바디 트래킹 입력. AvatarEntity가 씬에서 찾는다.
    ///
    /// P0 범위: OculusId=0으로 스폰 → Meta 계정 entitlement 없이 프리셋(테스트) 아바타를 쓴다.
    ///   실제 유저 아바타는 Platform SDK entitlement(OvrAvatarEntitlement.SetAccessToken)가 필요 — 후속 작업.
    /// </summary>
    [RequireComponent(typeof(SessionLauncher))]
    public class AvatarController : MonoBehaviour
    {
        // FusionAvatarSdk28Plus.prefab (NetworkObject + NetworkTransform + AvatarBehaviourFusion + AvatarEntity).
        // AvatarSetupFactory가 패키지 프리팹 참조를 씬에서 할당한다.
        [SerializeField] private NetworkObject m_avatarPrefab;

        // 프리셋(테스트) 아바타 개수 — LocalAvatarIndex 랜덤 범위. 샘플 zip 기본 6종(AvatarSpawnerFusion과 동일).
        [SerializeField] private int m_presetAvatarCount = 6;

        private SessionLauncher m_launcher;
        private NetworkObject m_spawnedAvatar; // 스폰한 로컬 아바타 — 세션 종료 폴링으로 정리 판단

        private void Awake()
        {
            m_launcher = GetComponent<SessionLauncher>();

            // 중복 구독 방지 (rules/scripts.md)
            m_launcher.SessionStarted -= OnSessionStarted;
            m_launcher.SessionStarted += OnSessionStarted;
        }

        private void OnDestroy()
        {
            if (m_launcher != null) m_launcher.SessionStarted -= OnSessionStarted;
        }

        private void Update()
        {
            // SessionLauncher에 세션 종료 이벤트가 없어(step-07 시그니처 유지) Runner 소멸을 폴링으로 감지.
            // 스폰한 아바타는 러너 종료와 함께 자동 despawn되므로 참조만 비운다.
            if (m_spawnedAvatar != null && m_launcher.Runner == null)
                m_spawnedAvatar = null;
        }

        private void OnSessionStarted(NetworkRunner runner)
        {
            if (m_spawnedAvatar != null)
            {
                Debug.LogWarning("[AvatarController] 이미 아바타가 스폰됨 — 중복 세션 시작 무시");
                return;
            }

            if (m_avatarPrefab == null)
            {
                Debug.LogError("[AvatarController] m_avatarPrefab 미할당 — FusionAvatarSdk28Plus.prefab을 할당하세요 " +
                               "(AvatarSetupFactory 또는 Inspector). 아바타 없이 진행");
                return;
            }

            // Shared Mode — 스폰한 로컬 플레이어가 State/Input Authority.
            // 로컬 아바타는 OVRCameraRig를 따라가고(AvatarBehaviourFusion.HasInputAuthority),
            // 원격 피어에는 스트리밍된 포즈로 재생된다.
            m_spawnedAvatar = runner.Spawn(
                m_avatarPrefab,
                Vector3.zero,
                Quaternion.identity,
                runner.LocalPlayer, // inputAuthority = 로컬 플레이어
                onBeforeSpawned: (_, obj) =>
                {
                    var behaviour = obj.GetComponent<AvatarBehaviourFusion>();
                    if (behaviour != null)
                    {
                        // 프리셋 아바타 인덱스만 지정(외형 다양성). OculusId는 0 유지 → entitlement 불필요한 테스트 아바타.
                        behaviour.LocalAvatarIndex = Random.Range(0, Mathf.Max(1, m_presetAvatarCount));
                    }
                    else
                    {
                        Debug.LogWarning("[AvatarController] 프리팹에 AvatarBehaviourFusion이 없음 — FusionAvatarSdk28Plus가 맞는지 확인");
                    }
                });
        }
    }
}
