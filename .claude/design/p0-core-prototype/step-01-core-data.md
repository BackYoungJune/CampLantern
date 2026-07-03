# Step 01: Core 데이터 모델 정의 (ScriptableObject)

- **영역:** `core` (Assets/Scripts/Core/Data/)
- **선행 단계:** 없음 (첫 단계)
- **후행 단계:** step-02~06, 08이 여기서 정의한 Def 타입들을 사용

---

## 목적
낚시·요리·영지·사냥이 공유하는 게임 데이터의 형태를 확정한다. 모든 콘텐츠(어종, 재료, 레시피, 영지 오브젝트, 사냥감)는 ScriptableObject 정의로 통일하고, 이후 단계는 이 타입 시그니처에 의존한다. 여기가 흔들리면 전 단계가 흔들리므로 **시그니처를 가장 먼저 확정**한다.

---

## 에이전트 실행 지침

`/task-start`를 먼저 호출해 범위를 확정한 뒤, 아래 지시를 수행한다.
`.claude/domain/resource-loop.md`, `economy.md`, `estate-system.md`를 먼저 읽는다.

### 생성/수정 파일
- `Assets/Scripts/Core/Data/Rarity.cs` — 생성 (enum)
- `Assets/Scripts/Core/Data/ItemDef.cs` — 생성 (재료/요리 공통 기반)
- `Assets/Scripts/Core/Data/FishDef.cs` — 생성
- `Assets/Scripts/Core/Data/RecipeDef.cs` — 생성
- `Assets/Scripts/Core/Data/EstateObjectDef.cs` — 생성
- `Assets/Scripts/Core/Data/HuntTargetDef.cs` — 생성

### 핵심 심볼
```csharp
// 등급 3단계 — estate-system.md 7-3
public enum Rarity { Common, Rare, Epic }

// 판매 가능한 모든 것의 기반 (물고기·재료·요리 공통)
public class ItemDef : ScriptableObject
{
    public string Id;          // 안정 식별자 (에셋 이름과 별개)
    public string DisplayName;
    public Rarity Rarity;
    public int SellPrice;      // 코인 판매가 — economy.md 소스
}

public class FishDef : ItemDef
{
    public float BiteWindowSeconds;  // 챔질 타이밍 판정 창 (동물의숲 수준 단순화)
    public float MinWaitSeconds;
    public float MaxWaitSeconds;
}

public class RecipeDef : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public ItemDef[] Ingredients;   // 낚시/사냥 재료 풀 재사용 — 요리 전용 재료 금지 (resource-loop.md)
    public ItemDef Result;          // 완성 요리 (원물 대비 1.5~2배 SellPrice로 데이터 설정)
    public ItemDef FailResult;      // 실패작 (저가 판매용)
}

public class EstateObjectDef : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public Rarity Rarity;
    public int CoinCost;            // 일반 등급은 코인만 — economy.md
    public ItemDef RequiredMaterial;    // 희귀 이상만 사용, Common은 null
    public int RequiredMaterialCount;
    public int CapacityWeight;      // 캠프 수용량 가중치 — estate-system.md 성능 상한
    public GameObject Prefab;
}

public class HuntTargetDef : ScriptableObject
{
    public string Id;
    public string DisplayName;
    public int MaxHealth;
    public int RequiredParticipants;  // 대형 사냥감 = 2 (social-cooperation.md ②)
    public ItemDef[] RewardMaterials; // 코인이 아닌 고유 재료 — resource-loop.md 사냥 밸런스
}
```

### 선행 산출물 의존성
- 없음

### 제약
- RULE-02 준수: `.asset` 파일은 이 단계에서 만들지 않는다 (step-03에서 /make-assets로).
- `[CreateAssetMenu]`를 각 Def에 붙여 step-03의 Editor 스크립트가 생성할 수 있게 한다.
- 필드는 프로토타입이므로 public 필드 허용. 단 `Id`는 이후 저장/네트워크 동기화 키로 쓰이므로 이름 변경 금지.
- 네임스페이스는 `CampLantern.Core` 로 통일 (이후 단계 영역들도 `CampLantern.{Area}` 규칙).

### 완료 판정
- [ ] `Grep "class FishDef" Assets/Scripts/` 로 6개 타입 정의 확인
- [ ] Unity 컴파일 통과 (에러 0)
- [ ] 모든 Def에 `[CreateAssetMenu]` 존재

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 영역 파일을 수정하지 않는다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다. 필요하면 아키텍트에게 보고하고 계획을 업데이트한다.
- 로직(낚시 판정, 조합 검사 등)을 여기에 넣지 않는다 — 순수 데이터 정의만.
