# Step 08: 2인 협동 사냥 + Shared Ledger 최소판

- **영역:** `hunting` (Assets/Scripts/Hunting/)
- **선행 단계:** step-02 (Wallet/Inventory), step-07 (SessionLauncher) 완료 필요
- **후행 단계:** 없음 (P0 마지막 게임플레이 단계)

---

## 목적
대형 사냥감 하나를 2인이 함께 잡고 **두 클라이언트가 동일한 진행도·보상을 받는** 최소 협동 루프. P0 완료 판단("2인이 동일 목표·보상을 공유")의 핵심 검증 지점이며, social-cooperation.md의 Shared Ledger 규칙 중 P0에 필요한 것만 구현한다: 자동 활성화, 복수 행동 기여 인정, 공유 보상.

---

## 에이전트 실행 지침

`/task-start`를 먼저 호출해 범위를 확정한 뒤, 아래 지시를 수행한다.
`.claude/domain/social-cooperation.md`를 **반드시** 먼저 읽는다 — 이 단계는 설계 의도 위반이 가장 나기 쉬운 곳.

### 생성/수정 파일
- `Assets/Scripts/Hunting/HuntTarget.cs` — 생성 (NetworkBehaviour — 사냥감 체력·상태)
- `Assets/Scripts/Hunting/HuntLedger.cs` — 생성 (NetworkBehaviour — 진행도·기여·보상 분배)

### 핵심 심볼
```csharp
public class HuntTarget : NetworkBehaviour
{
    [Networked] public int CurrentHealth { get; set; }
    public HuntTargetDef Def;   // step-03 에셋 참조

    public void ApplyHit(PlayerRef contributor, int damage);  // State Authority에서만 차감
    public event Action<HuntTarget> Defeated;
}

public class HuntLedger : NetworkBehaviour
{
    // 기여 인정 행동 — 피해량 단일 기준 금지 (social-cooperation.md)
    public enum ContributionKind { Hit, Lure, Assist }

    [Networked, Capacity(8)] public NetworkDictionary<PlayerRef, int> Contributions { get; }

    public void RecordContribution(PlayerRef player, ContributionKind kind);
    public bool IsParticipant(PlayerRef player);   // 유효 행동 1회 이상 = 참여
    // Defeated 시: 모든 참여자에게 동일한 RewardMaterials 지급 이벤트 발생
    public event Action<HuntTargetDef> RewardGranted;   // 로컬 플레이어가 참여자일 때만 발화
}
```

### 선행 산출물 의존성
- `HuntTargetDef`, `ItemDef` — step-01
- `Inventory` — step-02 (RewardGranted 구독 측에서 지급)
- `SessionLauncher` — step-07

### 제약
- **핵심 보상은 참여자 전원 동일** — 기여도 차등은 P0에서 아예 구현하지 않는다 (보너스 차등은 P1).
- 참여 판정은 `ContributionKind` 아무거나 1회 이상 — Hit만 인정하는 코드를 짜면 설계 위반.
- 체력 차감·기여 기록은 State Authority 권한에서만 — 클라이언트 각자 계산 금지 (진행도 불일치가 GDD 13장 최우선 리스크).
- `RequiredParticipants`(=2) 미달이면 사냥 시작 불가 — social-cooperation.md ② "2인 이상 협동 시에만 포획 가능".
- 물리 사용 시 FixedUpdate에서만 (RULE-03). P0 사냥감 AI는 제자리+체력바 수준으로 단순화 허용 (이동 AI는 P1).
- 재접속 복원은 구현하지 않는다 (백엔드 미정).

### 완료 판정
- [ ] `Grep "class HuntLedger" Assets/Scripts/Hunting/` 확인
- [ ] Unity 컴파일 통과
- [ ] 2클라이언트 테스트: 한쪽만 공격해도 다른 쪽이 Lure/Assist 1회 수행 시 **양쪽 모두** 동일 보상 수령
- [ ] 1인 상태에서 사냥 시작 불가 확인

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 영역 파일을 수정하지 않는다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다. 필요하면 아키텍트에게 보고하고 계획을 업데이트한다.
- 피해량 순위·경쟁 UI를 만들지 않는다 (경쟁적 박탈감 방지 원칙).
