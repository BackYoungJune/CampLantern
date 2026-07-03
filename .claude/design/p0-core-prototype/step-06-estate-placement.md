# Step 06: 영지 구매·배치·수용량

- **영역:** `estate` (Assets/Scripts/Estate/)
- **선행 단계:** step-02 완료 필요 (Wallet/Inventory), step-03 권장 (EstateObjectDef 에셋)
- **후행 단계:** 없음 (P0에서 영지는 로컬 전용 — 방문/네트워크는 P0 소셜 MVP 별도 설계)

---

## 목적
코인으로 오브젝트를 사서 영지에 놓고, 옮기고, 회수하는 최소 배치 시스템. estate-system.md의 캠프 수용량(가중치 상한)을 P0부터 넣는다 — 나중에 붙이면 기존 배치가 위반 상태가 되는 안전장치라 처음부터 있어야 한다.

---

## 에이전트 실행 지침

`/task-start`를 먼저 호출해 범위를 확정한 뒤, 아래 지시를 수행한다.
`.claude/domain/estate-system.md`와 `economy.md`를 먼저 읽는다.

### 생성/수정 파일
- `Assets/Scripts/Estate/EstateManager.cs` — 생성 (배치 목록·수용량 관리)
- `Assets/Scripts/Estate/PlacedObject.cs` — 생성 (배치된 개별 오브젝트)
- `Assets/Scripts/Estate/EstateShop.cs` — 생성 (구매 검증)

### 핵심 심볼
```csharp
public class EstateManager : MonoBehaviour
{
    public int CapacityUsed { get; }
    public int CapacityMax { get; }          // P0 임시 고정값 (SerializeField)
    public event Action CapacityChanged;

    public bool CanPlace(EstateObjectDef def);        // 수용량 검사
    public PlacedObject Place(EstateObjectDef def, Vector3 pos, Quaternion rot);
    public void Remove(PlacedObject obj);             // 회수 → Inventory 반환 아님, Def 보유 목록으로
}

public class PlacedObject : MonoBehaviour
{
    public EstateObjectDef Def { get; }
    public void MoveTo(Vector3 pos, Quaternion rot);
}

public class EstateShop
{
    public EstateShop(Wallet wallet, Inventory inventory);
    // 코인(+희귀는 재료) 검증·차감 후 보유 목록에 추가 — economy.md 이중 재화
    public bool TryPurchase(EstateObjectDef def);
}
```

### 선행 산출물 의존성
- `EstateObjectDef`, `Rarity` — step-01
- `Wallet`, `Inventory` — step-02

### 제약
- **수용량 검사는 Place 이전에 필수** — `CanPlace` 우회 경로를 만들지 않는다 (estate-system.md 성능 상한은 안전장치).
- 희귀 등급 구매는 코인 + `RequiredMaterial` 재료 둘 다 검증 (economy.md 이중 재화). 재료는 코인으로 대체 불가.
- 배치는 P0에서 자유 배치만 — 스냅/복제/되돌리기/충돌 미리보기는 P1 (편집 피로 지표 검증 후).
- 배치 데이터 저장 없음 (백엔드 미정) — 세션 메모리만.
- Rigidbody/Collider 초기값은 Awake에서 코드로 설정 (scripts.md).
- VR 집기(Grab) 연동은 씬 구성 시점에 — `Place`/`MoveTo`를 public으로 열어 어댑터가 호출.

### 완료 판정
- [ ] `Grep "class EstateManager" Assets/Scripts/Estate/` 확인
- [ ] Unity 컴파일 통과
- [ ] 에디터 플레이: 구매(코인 차감)→배치(수용량 증가)→수용량 초과 시 CanPlace false 확인

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 영역 파일을 수정하지 않는다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다. 필요하면 아키텍트에게 보고하고 계획을 업데이트한다.
- 방문/권한(읽기 전용·공동 편집자) 로직을 넣지 않는다 — P0 소셜 MVP 범위.
