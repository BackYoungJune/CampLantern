# Step 04: 낚시 상태머신

- **영역:** `fishing` (Assets/Scripts/Fishing/)
- **선행 단계:** step-02 완료 필요 (Wallet/Inventory), step-03 권장 (테스트용 FishDef 에셋)
- **후행 단계:** 없음 (P0에서 낚시는 종단 기능)

---

## 목적
동물의숲 수준으로 단순화된 낚시 루프(캐스팅→대기→입질→챔질→획득)를 만든다. resource-loop.md 원칙대로 정교한 라인/장력 물리는 넣지 않고, 타이밍 판정 하나로 승부한다. 획득한 물고기는 Inventory로 들어간다.

---

## 에이전트 실행 지침

`/task-start`를 먼저 호출해 범위를 확정한 뒤, 아래 지시를 수행한다.
`.claude/domain/resource-loop.md` 낚시 섹션과 `.claude/rules/scripts.md`를 먼저 읽는다.

### 생성/수정 파일
- `Assets/Scripts/Fishing/FishingRod.cs` — 생성 (상태머신 본체, MonoBehaviour)
- `Assets/Scripts/Fishing/FishingSpot.cs` — 생성 (낚시 가능 구역 + 어종 테이블)

### 핵심 심볼
```csharp
public enum FishingState { Idle, Casting, Waiting, Biting, Caught, Missed }

public class FishingRod : MonoBehaviour
{
    public FishingState State { get; private set; }
    public event Action<FishDef> FishCaught;
    public event Action StateChanged;

    public void Cast(FishingSpot spot);   // Idle→Casting→Waiting
    public void Reel();                   // Biting 중이면 판정 → Caught/Missed
}

public class FishingSpot : MonoBehaviour
{
    [SerializeField] private FishDef[] m_fishTable;
    public FishDef PickRandomFish();
}
```

### 선행 산출물 의존성
- `FishDef` — step-01
- `Inventory` — step-02 (Caught 시 물고기 추가는 FishCaught 이벤트를 구독하는 쪽에서 — 프로토타입에선 임시 GameManager 또는 테스트 컴포넌트 허용)

### 제약
- 대기/입질 타이밍은 코루틴 사용 (scripts.md — 프레임 단위 대기는 코루틴).
- VR 입력(컨트롤러 스윙/버튼)은 P0에서 단순화 — `Cast()`/`Reel()`을 public으로 열어두고 입력 바인딩은 얇은 어댑터로 분리. Meta Interaction SDK 연동은 씬 구성 시점에.
- 입질 판정 창은 `FishDef.BiteWindowSeconds` 사용 — 하드코딩 금지.
- `Update()`는 가볍게 (90Hz — scripts.md). GetComponent 캐싱 필수.
- 물리 사용 시 FixedUpdate에서만 (RULE-03). 단, P0 낚시는 물리 없이 타이밍만으로 구현 권장.

### 완료 판정
- [ ] `Grep "class FishingRod" Assets/Scripts/Fishing/` 확인
- [ ] Unity 컴파일 통과
- [ ] 에디터 플레이에서 Cast→(대기)→Biting 로그→Reel→Caught 흐름이 동작 (임시 테스트 키 바인딩 허용)

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 영역 파일을 수정하지 않는다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다. 필요하면 아키텍트에게 보고하고 계획을 업데이트한다.
- 도감/수족관/레어도 연출(9-1 후반)은 P1 — 구현하지 않는다.
