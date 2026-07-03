using UnityEngine;

namespace CampLantern.Core
{
    /// <summary>
    /// 어종 정의. 정교한 라인/장력 물리 없이 챔질 타이밍 판정만으로 승부한다
    /// — 동물의숲 수준 단순화 (domain/resource-loop.md 낚시 섹션).
    /// </summary>
    [CreateAssetMenu(menuName = "CampLantern/Fish", fileName = "Fish_")]
    public class FishDef : ItemDef
    {
        [Tooltip("입질 후 챔질(Reel) 판정이 성공하는 시간 창(초)")]
        public float BiteWindowSeconds = 1f;

        [Tooltip("캐스팅 후 입질까지 최소 대기(초)")]
        public float MinWaitSeconds = 2f;

        [Tooltip("캐스팅 후 입질까지 최대 대기(초)")]
        public float MaxWaitSeconds = 8f;
    }
}
