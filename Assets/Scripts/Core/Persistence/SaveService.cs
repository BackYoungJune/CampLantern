using System;
using System.IO;
using UnityEngine;

namespace CampLantern.Core.Persistence
{
    /// <summary>
    /// 로컬 JSON 저장/로드 — 백엔드 확정 전 임시 영구 저장 (tech-stack-decisions.md "영구 저장 백엔드" 항목).
    /// 나중에 서버 API로 교체할 때 이 클래스의 Load/Save 시그니처만 유지하면 호출부(PlayerState)는
    /// 그대로 두고 내부 구현만(HTTP 호출 등으로) 교체할 수 있도록 의도적으로 좁게 설계했다.
    /// </summary>
    public static class SaveService
    {
        private const string k_fileName = "player_save.json";

        private static string FilePath => Path.Combine(Application.persistentDataPath, k_fileName);

        /// <summary>저장 파일 존재 여부 — 최초 실행(신규 유저) 판별용.</summary>
        public static bool Exists() => File.Exists(FilePath);

        /// <summary>저장 파일이 없으면 빈 데이터를 반환한다 (최초 실행).</summary>
        public static PlayerSaveData Load()
        {
            if (!File.Exists(FilePath)) return new PlayerSaveData();

            try
            {
                string json = File.ReadAllText(FilePath);
                return JsonUtility.FromJson<PlayerSaveData>(json) ?? new PlayerSaveData();
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] 저장 파일 로드 실패 — 빈 데이터로 대체: {e.Message}");
                return new PlayerSaveData();
            }
        }

        public static void Save(PlayerSaveData data)
        {
            try
            {
                string json = JsonUtility.ToJson(data, prettyPrint: true);
                File.WriteAllText(FilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveService] 저장 실패: {e.Message}");
            }
        }
    }
}
