# CLAUDE.md

이 파일은 Claude Code(claude.ai/code)가 이 레포지토리에서 작업할 때 참고하는 가이드이다.

## 프로젝트 개요

**Camp Lantern** — Unity 6000.3.10f1(Unity 6) 기반 **VR 코지 소셜 영지 게임** (Meta Quest 타겟). 유저가 캠핑 영지를 소유하고 낚시·요리·사냥으로 얻은 자원으로 영지를 꾸미며, 협동·방문·공유 진행도(Shared Ledger)로 소셜 플레이를 유도한다. 수익화는 프리미엄(1회 구매) + DLC.

GDD 원본: `.claude/domain/gdd/VR_코지_소셜_영지_게임_기획서.docx` (.docx라 직접 Read 불가). **시스템별 정리는 `.claude/domain/*.md`에 있으며 `.claude/INDEX.md` Level 2로 라우팅한다.** 실제 게임 코드는 아직 없음 (`Assets/Scripts/Test.cs` 플레이스홀더 상태) — P0 프로토타입부터 착수 예정.

## 불변 제약 (Inviolable Constraints)

프로젝트 루트의 [`RULES.md`](./RULES.md)에 시스템을 실제로 망가뜨리는 제약이 정리되어 있다 (에디터 멈춤, GUID 손상, 빌드 실패 등). 작업 시작 전 반드시 스캔할 것.

- **RULE-01**: Domain Reload 트리거 금지 (`[InitializeOnLoad]`, `autoReferenced: true`)
- **RULE-02**: Unity 에셋 파일 직접 편집 금지 (`.meta`/`.prefab`/`.unity`/`.asset`)
- **RULE-03**: 물리 API는 `FixedUpdate`에서만
- **RULE-04**: `ProjectSettings/`는 Claude가 직접 수정하지 않음

프로젝트 고유 시스템(저장 데이터, 플랫폼 분기 등)이 생기면 해당 시점에 새 RULE을 추가한다.

## 추가 규칙 및 컨벤션

프로젝트 지식은 `.claude/` 하위에 계층별로 정리되어 있다. 필요에 따라 참조할 것:

### `.claude/rules/` — 경로 기반 코딩 규칙 (자동 로드)
- `scripts.md` — 컴포넌트 캐싱, 비동기 CancellationToken, 이벤트 구독 해제, Awake 초기화 등 범용 스크립팅 컨벤션. 프로젝트 고유 컨벤션(싱글톤 체계, UI 프레임워크 등)이 정해지면 이 파일에 추가한다.

### `.claude/domain/` — 이 프로젝트 고유 설계 의도
시스템을 수정하기 **전에** 관련 기획서를 읽어 "왜 이렇게 설계됐는가"를 파악할 것. 현재는 GDD가 없어 비어 있음.

**GDD 라우팅은 `.claude/INDEX.md` Level 2를 통해 한다.** 특정 파일명을 직접 열지 말고, INDEX.md의 keywords와 작업 주제를 매칭해 해당 GDD만 선별 로드한다.
- `gdd/` — 기획서 도착 시 여기에 추가하고 INDEX.md Level 2에 등록.
- (도메인 파일은 필요 시 생성 — `domain/README.md` 참조)

### `.claude/knowledge/` — 범용 Unity/C# 레퍼런스
언어·엔진 베스트 프랙티스가 헷갈릴 때 참조:
- `RULES.md` — 21개 범용 코딩 원칙 (R1-R21)
- `csharp-dotnet.md` — 값 타입, 박싱, 이벤트, async, LINQ
- `unity-scripting-gotchas.md` — 직렬화 함정, 코루틴 주의점, IL2CPP, Unity 2022→6000 API 리네임
- `unity-mobile-performance.md` — 모바일/XR 성능 규칙 (프로파일링, GC, 배칭, UI 등)
- `debugging/` — 디버깅 원칙 10개 (가정 의심, 버그 분류, 증상 vs 원인, 버그 일지, 정적 분석, 단언, 이분 탐색 등)
- `qa/` — QA 원칙 10개

### `.claude/INDEX.md`
위 모든 계층에 대한 키워드 기반 라우팅. **GDD를 포함한 모든 도메인 문서는 이 파일을 통해 라우팅된다.** 어떤 파일을 열어야 할지 모를 때 참고.

## 빌드 커맨드

아직 커스텀 빌드 파이프라인이 없다. Unity 에디터의 기본 `File > Build Settings`를 사용한다. 빌드 자동화 스크립트가 추가되면 여기에 기록한다.

## 아키텍처

코드 레벨 아키텍처(매니저 구조, 데이터 모델)는 아직 미확정. 공간/네트워크 구조는 GDD 8장에서 확정됨:

- **4개 공간**: 로비(싱글) / 낚시터(드롭인 멀티) / 사냥터(존 분할) / 영지(`estate_{userID}`, 오프라인 방문 가능) — 상세는 `.claude/domain/room-architecture.md`
- **넷코드**: Photon Fusion 2 (서버 권한 모델 — 영지 주인 오프라인 요구사항 때문에 PUN 2 배제). 음성은 Photon Voice 2. 선택 근거는 `.claude/domain/tech-stack-decisions.md`
- **영구 저장 백엔드**: 미정 (자체 서버 방향, 추후 결정) — 영지 저장·Shared Ledger 복원 구현은 백엔드 확정 후 착수

코드 아키텍처가 정해지는 대로 이 섹션에 추가한다.

## 코드 컨벤션

- **한글 주석** 허용.
- 그 외 컨벤션(싱글톤 사용 여부, 비동기 라이브러리 선택, 이벤트 시스템 등)은 실제 코드가 쌓이면서 정해지는 대로 이 섹션과 `.claude/rules/scripts.md`에 기록한다.

## 주요 패키지

- **Meta XR SDK 슈트 — 전부 203.0.0으로 버전 통일** (`core`, `haptics`, `interaction.ovr`, `platform`). 새 Meta XR 패키지 추가 시 반드시 203.0.0에 맞출 것 — 슈트 버전이 섞이면 내부 API 호환이 깨진다.
- **Meta Avatars SDK 40.0.1** — EOF(End-of-Feature) 최종 버전. 버전 번호가 203.x 트레인과 다른 게 정상이며 업데이트하면 안 됨 (더 높은 버전이 존재하지 않음).
- **Photon Fusion 2 / Voice 2** — `.unitypackage`로 `Assets/Photon/`에 임포트됨 (UPM 아님). App ID는 `Fusion > Real Time Settings`의 `PhotonAppSettings` 에셋에 저장 (App Id Fusion / App Id Voice 두 필드).
- 나머지는 `Packages/manifest.json` 참고. Oculus 샘플 에셋은 `Assets/Oculus/`.

## 플랫폼 노트

- **타겟: Meta Quest** (GDD 확정 — 성능 기준, Quest 스토어 매출 벤치마크, 캠프 수용량 등 Quest 전제로 설계됨). PCVR 확장은 GDD 15-2에서 장기 검토 사항으로만 언급.
- 성능 목표: 90Hz 유지 (`.claude/rules/scripts.md`, `knowledge/unity-mobile-performance.md` 참고).
