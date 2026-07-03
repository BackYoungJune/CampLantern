# Step 02: 코인 지갑·인벤토리

- **영역:** `core` (Assets/Scripts/Core/)
- **선행 단계:** step-01 완료 필요 (ItemDef)
- **후행 단계:** step-04(낚시 보상), step-05(재료 소모), step-06(코인 소모), step-08(사냥 보상)이 Wallet/Inventory를 사용

---

## 목적
코인 획득·소비와 아이템 보유를 담당하는 런타임 상태를 만든다. 낚시·요리·영지·사냥 네 영역이 전부 이 두 클래스를 통해서만 재화를 건드리게 해, 경제 로직이 한 곳에 모이게 한다.

---

## 에이전트 실행 지침

`/task-start`를 먼저 호출해 범위를 확정한 뒤, 아래 지시를 수행한다.
`.claude/domain/economy.md`를 먼저 읽는다.

### 생성/수정 파일
- `Assets/Scripts/Core/Wallet.cs` — 생성
- `Assets/Scripts/Core/Inventory.cs` — 생성

### 핵심 심볼
```csharp
// 플레인 C# 클래스 (MonoBehaviour 아님) — 소유자는 이후 GameManager 격 객체가 정한다
public class Wallet
{
    public int Coins { get; private set; }
    public event Action<int> CoinsChanged;   // UI 갱신용

    public void Add(int amount);
    public bool TrySpend(int amount);        // 부족하면 false, 차감 없음
}

public class Inventory
{
    public event Action Changed;

    public void Add(ItemDef item, int count = 1);
    public bool TryRemove(ItemDef item, int count = 1);  // 부족하면 false
    public int CountOf(ItemDef item);
    public IReadOnlyDictionary<ItemDef, int> Items { get; }
}
```

### 선행 산출물 의존성
- `ItemDef` — step-01에서 정의됨

### 제약
- 재화 변경은 반드시 이 클래스들의 메서드를 통해서만 — 외부에서 `Coins` 직접 세팅 불가 (private set).
- 이벤트 구독 규칙은 `.claude/rules/scripts.md` (구독 전 `-=`, OnDestroy 해제) — 이 클래스를 구독하는 쪽의 책임이지만 이벤트 시그니처는 여기서 확정.
- P0에서는 저장 없음 (백엔드 미정 — tech-stack-decisions.md). 세션 메모리 상태만.
- 소프트 캡(economy.md)은 P0 범위 밖 — 구현하지 않음. 필드 자리만 남기지도 말 것 (죽은 코드 금지).

### 완료 판정
- [ ] `Grep "class Wallet" Assets/Scripts/Core/` 확인
- [ ] Unity 컴파일 통과
- [ ] `TrySpend`가 잔액 부족 시 false 반환하고 잔액을 건드리지 않는 것을 코드 리뷰로 확인

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 영역 파일을 수정하지 않는다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다. 필요하면 아키텍트에게 보고하고 계획을 업데이트한다.
- UI를 만들지 않는다 (이벤트만 노출).
