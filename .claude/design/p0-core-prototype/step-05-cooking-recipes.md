# Step 05: 3종 레시피 조합

- **영역:** `cooking` (Assets/Scripts/Cooking/)
- **선행 단계:** step-02 완료 필요 (Inventory), step-03 권장 (RecipeDef 에셋)
- **후행 단계:** 없음 (P0에서 요리는 종단 기능)

---

## 목적
재료를 넣고 조리하면 레시피 매칭 결과(성공 요리 또는 실패작)가 나오는 최소 조합 시스템. resource-loop.md 원칙대로 레시피는 사전 공개하지 않고, 실패해도 희귀 재료는 소모되지 않는다.

---

## 에이전트 실행 지침

`/task-start`를 먼저 호출해 범위를 확정한 뒤, 아래 지시를 수행한다.
`.claude/domain/resource-loop.md` 요리 섹션을 먼저 읽는다.

### 생성/수정 파일
- `Assets/Scripts/Cooking/CookingPot.cs` — 생성 (재료 투입·조리 실행)
- `Assets/Scripts/Cooking/RecipeMatcher.cs` — 생성 (순수 로직, MonoBehaviour 아님)

### 핵심 심볼
```csharp
// 순수 로직 — 테스트 가능하게 MonoBehaviour와 분리
public class RecipeMatcher
{
    public RecipeMatcher(IReadOnlyList<RecipeDef> recipes);
    // 재료 조합이 레시피와 일치하면 해당 레시피, 아니면 null (순서 무관, 수량 일치)
    public RecipeDef Match(IReadOnlyList<ItemDef> ingredients);
}

public class CookingPot : MonoBehaviour
{
    public event Action<ItemDef> Cooked;   // 성공 요리 또는 실패작

    public bool TryAddIngredient(ItemDef item);  // Inventory에서 꺼내 투입
    public void Cook();                          // 매칭 → 성공/실패 결과 생성
    public void Clear();                         // 투입 재료 반환
}
```

### 선행 산출물 의존성
- `ItemDef`, `RecipeDef` — step-01
- `Inventory` — step-02

### 제약
- **실패 시 Rare 이상 재료는 Inventory로 반환**, Common만 소모 (resource-loop.md 실패 피드백). 실패작 1개 지급.
- 성공 시 투입 재료 전부 소모, `RecipeDef.Result` 지급.
- 30~60초 수동 조리 인터랙션(자르기/섞기)은 P0에서 생략 — `Cook()` 즉시 판정. VR 인터랙션은 P1 몫.
- 힌트 시스템·숙련도는 P1 — 구현하지 않는다.
- RecipeMatcher는 LINQ 남용 없이 (mobile-performance — GC 회피). 조합 비교는 정렬 후 비교 또는 카운트 딕셔너리.

### 완료 판정
- [ ] `Grep "class RecipeMatcher" Assets/Scripts/Cooking/` 확인
- [ ] Unity 컴파일 통과
- [ ] 에디터 플레이: 올바른 재료 → Result, 틀린 재료 → FailResult + Rare 재료 반환 확인

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 영역 파일을 수정하지 않는다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다. 필요하면 아키텍트에게 보고하고 계획을 업데이트한다.
- 요리 전용 신규 재료를 만들지 않는다 (기존 재료 풀 재사용 원칙).
