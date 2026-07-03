# Step 03: P0 콘텐츠 데이터 에셋 생성 (/make-assets)

- **영역:** `core` (Assets/Scripts/Editor/ + Assets/Data/)
- **선행 단계:** step-01 완료 필요 (Def 타입 전부)
- **후행 단계:** step-04~06, 08이 이 에셋들을 Inspector에서 참조

---

## 목적
P0 플레이에 필요한 최소 콘텐츠 데이터를 `.asset`으로 생성한다. RULE-02(에셋 직접 편집 금지) 때문에 손으로 만들지 않고 Editor 스크립트가 생성하게 한다.

---

## 에이전트 실행 지침

`/task-start`를 먼저 호출한 뒤 **`/make-assets` 스킬을 사용**해 아래 데이터를 생성한다.

### 생성할 콘텐츠 (P0 최소 볼륨)
| 종류 | 수량 | 내용 |
|---|---|---|
| FishDef | 3종 | 일반 2, 희귀 1 — 대기시간·판정창 차등 |
| ItemDef (사냥 재료) | 2종 | 가죽, 뿔 등 — HuntTargetDef 보상용 |
| RecipeDef | 3종 | resource-loop.md "3종 요리". 각 결과물 SellPrice는 재료 합의 1.5~2배. 실패작 1종 공유 |
| ItemDef (요리 결과물/실패작) | 4종 | 완성 요리 3 + 실패작 1 |
| EstateObjectDef | 6종 | 카테고리 대표: 텐트, 랜턴, 캠핑 의자, 화분, 데크, (희귀)화롯불 — 희귀는 사냥 재료 요구 |
| HuntTargetDef | 1종 | RequiredParticipants = 2 (대형 사냥감) |

### 생성/수정 파일
- `Assets/Scripts/Editor/P0DataFactory.cs` — 생성 (메뉴 아이템으로 실행하는 일괄 생성 스크립트)
- 산출물: `Assets/Data/` 하위 `.asset` 파일들 (Unity가 생성)

### 선행 산출물 의존성
- step-01의 모든 Def 타입

### 제약
- RULE-01: `[InitializeOnLoad]` 금지 — `[MenuItem]` 방식으로만.
- RULE-02: `.asset`을 텍스트로 직접 쓰지 않는다. `ScriptableObject.CreateInstance` + `AssetDatabase.CreateAsset`만 사용.
- 이미 존재하는 에셋은 덮어쓰지 않고 스킵 (재실행 안전).
- 수치 밸런스는 프로토타입 임시값 — economy.md의 "한 세션에 오브젝트 1개" 기준으로 러프하게: 일반 어종 판매가 10코인, 일반 오브젝트 가격 100코인 스케일 권장.

### 완료 판정
- [ ] 메뉴 실행 후 `Assets/Data/` 하위에 19개 에셋 존재
- [ ] Unity 컴파일 통과
- [ ] 각 RecipeDef의 Ingredients가 실제 FishDef/ItemDef 에셋을 참조 (null 없음)

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 영역 파일을 수정하지 않는다.
- `.asset`/`.meta` 파일을 직접 편집하지 않는다 (RULE-02).
- 런타임 코드를 수정하지 않는다 — Editor 스크립트와 에셋 생성만.
