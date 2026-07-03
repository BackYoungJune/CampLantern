# P0 코어 프로토타입

## 한 줄 요약
낚시→요리→영지 배치→코인 경제의 15~30분 루프가 끊김 없이 순환하고, 2인이 협동 사냥에서 동일 목표·보상을 공유하는 최소 프로토타입.

## 원문
`.claude/domain/mvp-scope.md` P0 코어 프로토타입 행:
> 단순 낚시, 3종 요리, 소형 영지 배치, 코인 경제, 2인 협동 사냥, 기본 음성·음소거
> 완료 판단: 15~30분 루프가 끊김 없이 순환하고 2인이 동일 목표·보상을 공유

시스템 상세는 `.claude/domain/` 참조: resource-loop.md(낚시·요리·사냥), estate-system.md(배치), economy.md(코인), social-cooperation.md(Shared Ledger), room-architecture.md(Room 구조).

## 아키텍처 결정
- **데이터는 ScriptableObject 정의(Def) + 런타임 상태 분리**: 어종/레시피/영지 오브젝트를 SO로 정의 — RULE-02(에셋 직접 편집 금지) 준수를 위해 `.asset` 생성은 `/make-assets`(Editor 스크립트)로만 한다.
- **P0는 로컬 우선, 네트워크는 사냥만**: 낚시·요리·영지 배치는 싱글 로직으로 먼저 완성. Fusion NetworkBehaviour는 협동 사냥(Shared Ledger)에만 적용 — P0 완료 판단이 "2인이 동일 목표·보상 공유"이므로 사냥이 네트워크 검증 지점.
- **Shared Ledger는 P0에서 최소판**: State Authority가 관리하는 진행도·기여 셋만. 재접속 스냅샷 복원은 백엔드 미정(tech-stack-decisions.md)이라 P0 범위 제외.
- **폴더 구조 신설 제안**: `Assets/Scripts/` 하위에 `Core/`, `Fishing/`, `Cooking/`, `Estate/`, `Networking/`, `Hunting/` 신설. 현재 `Test.cs`뿐이라 충돌 없음. 각 폴더는 해당 영역 첫 단계에서 파일과 함께 생성 (asmdef는 만들지 않음 — RULE-01 회피, P0에서는 Assembly-CSharp 단일 어셈블리 유지).
- **씬 구성은 코드 이후**: MonoBehaviour 완성 후 씬 배치는 에디터에서 수동 또는 `/make-assets` Editor 스크립트로. `.unity` 직접 편집 금지(RULE-02).

## 선행 조건 (착수 전 해결 필요)
- ⚠️ **Photon Voice 2가 프로젝트에 없음** — `Assets/Photon/`에 Fusion만 확인됨. step-09(음성) 착수 전 Voice 2 (Fusion 통합판) `.unitypackage` 임포트 필요. step-01~08은 Voice 없이 진행 가능.

## 터치 영역
| 영역 | 경로 | 역할 |
|---|---|---|
| core | Assets/Scripts/Core/ | 데이터 정의(SO), 코인 지갑, 인벤토리 |
| fishing | Assets/Scripts/Fishing/ | 낚시 상태머신 (캐스팅→대기→입질→릴링) |
| cooking | Assets/Scripts/Cooking/ | 3종 레시피 조합·실패작 |
| estate | Assets/Scripts/Estate/ | 오브젝트 구매·배치·수용량 |
| networking | Assets/Scripts/Networking/ | Fusion 세션 진입 (사냥터 존 Room) |
| hunting | Assets/Scripts/Hunting/ | 2인 협동 사냥 + Shared Ledger 최소판 |
| voice | Assets/Scripts/Networking/Voice/ | 근접 음성·음소거 (Voice 2 설치 후) |

## 의존성 그래프
```
Core/Data (Def SO들)
  ├→ Core/Economy (Wallet, Inventory)
  │    ├→ Fishing  (포획 보상 지급)
  │    ├→ Cooking  (재료 소모→요리 생성)
  │    └→ Estate   (코인 소모→오브젝트 배치)
  └→ Hunting (사냥감 Def 참조)
Networking/Session (Fusion 접속)
  ├→ Hunting (NetworkBehaviour 러너 필요)
  └→ Voice   (Voice 2 설치 선행)
```

## 단계
1. [step-01-core-data.md](step-01-core-data.md) — Core: ScriptableObject 데이터 모델 정의
2. [step-02-core-economy.md](step-02-core-economy.md) — Core: 코인 지갑·인벤토리
3. [step-03-core-assets.md](step-03-core-assets.md) — Core: P0 콘텐츠 데이터 에셋 생성 (/make-assets)
4. [step-04-fishing-loop.md](step-04-fishing-loop.md) — Fishing: 낚시 상태머신
5. [step-05-cooking-recipes.md](step-05-cooking-recipes.md) — Cooking: 3종 레시피 조합
6. [step-06-estate-placement.md](step-06-estate-placement.md) — Estate: 구매·배치·수용량
7. [step-07-networking-session.md](step-07-networking-session.md) — Networking: Fusion 세션 진입
8. [step-08-hunting-coop.md](step-08-hunting-coop.md) — Hunting: 2인 협동 사냥 + Shared Ledger
9. [step-09-voice-mute.md](step-09-voice-mute.md) — Voice: 근접 음성·음소거 (⚠️ Voice 2 설치 선행)

## 병렬 실행 가능성
- step-01 → step-02 → (step-03, step-04, step-05, step-06 **4개 병렬 가능** — 서로 다른 영역, 서로 독립)
- step-07은 step-01 완료 후 언제든 병렬 가능 (Core/Economy 불필요)
- step-08은 step-02 + step-07 완료 후
- step-09는 step-07 완료 + **Photon Voice 2 임포트** 후

## P0 전체 완료 판정 (mvp-scope.md 기준)
- 낚시→요리→배치→코인 루프가 에디터 플레이에서 순환
- 2인(에디터+빌드 또는 ParrelSync 등) 협동 사냥에서 두 클라이언트의 진행도·보상 동일
- 음소거 토글 동작
