#if UNITY_EDITOR
using Fusion;
using Photon.Voice.Fusion;
using Photon.Voice.Unity;
using UnityEditor;
using UnityEngine;

namespace CampLantern.EditorTools
{
    /// <summary>
    /// VoicePlayer 프리팹 생성기 (step-09).
    /// 플레이어당 1개 스폰되는 음성 아바타 — NetworkObject + VoiceNetworkObject + Speaker + AudioSource(3D).
    /// RULE-02: .prefab 텍스트 직접 작성 금지 — PrefabUtility.SaveAsPrefabAsset만 사용.
    /// 저장 시 Fusion 임포트 후처리가 NetworkObject를 자동 베이킹한다.
    /// </summary>
    public static class VoicePlayerFactory
    {
        private const string k_folder = "Assets/Prefabs";
        private const string k_path   = k_folder + "/VoicePlayer.prefab";

        [MenuItem("Tools/Make Assets/Voice Player Prefab")]
        public static void Create() => CreateInternal(force: false);

        [MenuItem("Tools/Make Assets/Voice Player Prefab (Force Recreate)")]
        public static void ForceRecreate() => CreateInternal(force: true);

        private static void CreateInternal(bool force)
        {
            if (!force && AssetDatabase.LoadAssetAtPath<GameObject>(k_path) != null)
            {
                Debug.LogWarning($"[MakeAssets] 이미 존재합니다. Force Recreate 메뉴를 사용하세요: {k_path}");
                return;
            }

            if (!AssetDatabase.IsValidFolder(k_folder))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var root = new GameObject("VoicePlayer");
            try
            {
                var networkObject = root.AddComponent<NetworkObject>();
                // Shared Mode에서 스폰 주인이 나가면 오브젝트도 제거 — 고아 스피커 방지
                networkObject.Flags |= NetworkObjectFlags.DestroyWhenStateAuthorityLeaves;

                root.AddComponent<VoiceNetworkObject>();

                // Speaker의 RequireComponent가 AudioSource를 먼저 요구하므로 명시 추가 후 3D 설정
                var audioSource = root.AddComponent<AudioSource>();
                audioSource.playOnAwake  = false; // Speaker가 스트림 링크 시점에 직접 재생
                audioSource.spatialBlend = 1f;    // 근접 감쇠는 3D 오디오 설정으로만 — 커스텀 감쇠 금지 (step-09 제약)
                audioSource.rolloffMode  = AudioRolloffMode.Logarithmic;
                audioSource.minDistance  = 1f;    // 1m 이내 원음량 — 근접 대화 스케일
                audioSource.maxDistance  = 15f;   // 캠프 반경 밖에서는 사실상 안 들림

                root.AddComponent<Speaker>();

                PrefabUtility.SaveAsPrefabAsset(root, k_path);
                Debug.Log($"[MakeAssets] VoicePlayer 생성 완료: {k_path}");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
            AssetDatabase.Refresh();
        }
    }
}
#endif
