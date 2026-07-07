# 기술 스택 결정 (GDD 미기재 — 세션 결정 사항)

## 한 줄 요약
GDD 8장은 "Photon 기반 Room" 이라고만 명시하고 구체 SDK·버전·백엔드는 정하지 않았다. 이 문서는 실제로 확정한 값과 그 이유를 기록한다.

## 핵심 타입 / 진입점
- 아직 코드 없음. `Packages/manifest.json`에 패키지 의존성만 존재.

## 결정 사항

| 항목 | 결정 | 이유 |
|---|---|---|
| 넷코드 | Photon **Fusion 2** (PUN 2 아님) | GDD 8-4 "마스터 클라이언트를 주인으로 고정할 수 없음, 서버 관리형 방식 필요" — Fusion의 서버 권한(host authoritative) 모델이 이 요구와 직접 맞음. PUN 2는 마스터클라이언트 기반이라 8-5 "배치 데이터는 서버 권한" 요구를 만족하려면 추가 설계가 필요했음 |
| 음성 | Photon **Voice 2** (Voice for Fusion 통합판) | 6-4 근접 음성 + 파티 무전 요구. Fusion과 별도 App ID로 관리 |
| 아바타 | Meta Avatars SDK **v40.0.1 (EOF 고정)** | Meta가 Avatars SDK를 End-of-Feature 처리해 v40.0.1이 마지막 릴리스. 신규 기능·API 추가는 없지만 백엔드 서비스는 계속 운영되고 기존 통합은 정상 동작 — 대체 SDK가 없어 그대로 채택. **버전이 다른 Meta XR 패키지(203.x 트레인)와 번호 체계가 다르다는 점**이 낚시 포인트 (Asset Store 검색에서 안 보일 수 있음, Meta Downloads 페이지에서 직접 받아야 함) |
| Meta XR SDK 버전 | 기존 haptics/interaction.ovr/platform과 동일한 **203.0.0**으로 신규 패키지(Core) 버전 통일 | Meta XR SDK는 슈트 단위로 버전이 같이 올라가는 구조 — 버전 섞으면 슈트 내부 API 호환 깨짐 |
| 영지 배치 데이터 영구 저장 백엔드 | **미정 (자체 서버, 추후 결정)** | Photon Room은 실시간 세션일 뿐 영구 DB가 아님. 8장 "재화·오브젝트 배치 데이터는 Room 생존 여부와 무관하게 서버 DB에 영구 저장" 요구를 만족할 별도 백엔드가 필요하지만 아직 팀 결정 전 |

## 시스템 간 관계
- Fusion 2의 서버 권한 모델은 [[room-architecture]] 8-4/8-5 (영지 권한, 협동 이벤트 진행도)의 전제 조건이다. Fusion 없이는 "먼저 입장한 유저 또는 서버 관리형" 구조를 구현할 방법이 마땅치 않음.
- Avatars SDK가 EOF라는 것은, 향후 아바타 커스터마이징 기능 확장 시 Meta가 새 API를 추가해주지 않는다는 뜻 — 커스터마이징 요구가 늘면 자체 아바타 시스템 전환을 재검토해야 할 수 있음.
- 영구 저장 백엔드 미정 상태이므로, [[room-architecture]]의 "영지 오프라인 방문" 기능은 백엔드가 정해지기 전까지 실제 구현 착수 불가 (스냅샷을 어디서 읽어올지가 정해져야 함).

## 기획 의도 / 역사적 맥락
- GDD가 "Photon 기반"까지만 정하고 SDK 세부 선택을 비워둔 건 기획 문서가 기술 스택보다 시스템 요구사항 중심으로 쓰였기 때문. 8-4의 "마스터 클라이언트 고정 불가" 요구사항 한 줄이 사실상 PUN 2를 배제하고 Fusion을 선택하게 만든 결정적 근거였음.

## Photon Voice 2 임포트 레이아웃 (2026-07-06 확정 — 표준 임포트와 다름)

Voice 2.63(Asset Store)은 Realtime **4** 기반인데 Fusion 2.1은 Realtime **5** 소스(`Assets/Photon/PhotonRealtime`, asmdef `Photon.Realtime`)를 쓴다. Voice를 그대로 임포트하면 같은 폴더에 RT4가 덮어써져 **asmdef 중복 + RT5 파일 손상**으로 컴파일이 깨진다. 공식 문서(voice-for-fusion)는 Fusion 2.1 이전 기준이라 이 충돌을 다루지 않음. 이 프로젝트의 확정 레이아웃:

- **RT4 전체를 `Assets/Photon/PhotonVoice/PhotonRealtime/Code/`로 이전** (asmdef `PhotonRealtime`). RT5는 원위치 유지. 두 어셈블리는 이름이 달라 공존 가능.
- 구 lib `Photon3Unity3D.dll`(네임스페이스 `ExitGames.Client.Photon`)과 신 lib `PhotonClient.dll`(`Photon.Client`)은 네임스페이스가 달라 공존 가능 — **둘 다 필요, 둘 다 삭제 금지**.
- PUN 2/PhotonChat/Voice의 PUN 통합/비-Fusion 데모는 공식 가이드대로 임포트 제외(삭제)됨.
- `FusionVoiceClient.cs`·`PrefabSpawner.cs`에 Fusion 2.1 호환 수정 있음 (OnReliableDataReceived 시그니처, FusionAppSettings 필드 리플렉션 복사, UseFusionAuthValues 비활성). **Voice 패키지를 업데이트/재임포트하면 이 수정과 폴더 이전이 전부 되돌아가므로 이 섹션 절차를 다시 적용해야 한다.**

### Fusion 위빙 함정 — Meta XR Building Blocks (2026-07-07 발견·해결)
- Meta XR Core SDK는 Fusion 존재를 감지하면 `Meta.XR.MultiplayerBlocks.Fusion` 어셈블리(NetworkBehaviour 포함)를 자동 컴파일한다. 이 어셈블리가 `NetworkProjectConfig.fusion`의 `AssembliesToWeave`에 없으면 **러너 초기화(NetworkTypesMeta)가 통째로 실패해 모든 세션 접속이 Error**가 된다 (`FusionAnchor has no attribute NetworkStructWeavedAttribute`). 목록에 추가돼 있음 — 지우지 말 것.
- `AssembliesToWeave` 변경은 일반 재컴파일로는 반영 안 된다 (Bee가 ILPostProcessor 결과 캐시 재사용). **`CompilationPipeline.RequestScriptCompilation(RequestScriptCompilationOptions.CleanBuildCache)`** 로 캐시까지 버려야 위버가 다시 돈다. Meta XR/Fusion 패키지 업데이트 후 같은 에러가 재발하면 이 절차부터.
- Photon 대시보드 App ID는 **타입이 있다** — Voice 접속엔 반드시 "Voice" 타입 앱의 ID여야 한다. 다른 타입(Fusion 등) ID를 App Id Voice에 넣으면 룸 참가에서 `Unsupported Plugin (32752)` 서버 에러. (2026-07-07 실제 발생 — Voice 타입 앱 새로 생성해 해결.)

### Voice 런타임 통합 구조 (2026-07-07, step-09)
- `UseFusionAuthValues`가 비활성(위 수술)이라 **Voice 룸 액터번호↔Fusion PlayerRef 매핑이 불가능**하다.
  플레이어 식별이 필요한 음성 기능(음소거, P2 파티 무전)은 반드시 플레이어당 스폰되는
  `VoiceNetworkObject`(VoicePlayer.prefab)의 `Object.StateAuthority`를 경유할 것.
- NetworkRunner가 런타임 AddComponent라(SessionLauncher) FusionVoiceClient도 세션 시작 **후**
  같은 GO에 부착한다 — 자동 콜백 수집에서 빠지므로 `runner.AddCallbacks(voiceClient)` 필수.

## 숨은 규칙 / 암묵지
- Meta Avatars SDK를 Asset Store에서 검색해도 안 뜨는 게 정상이다 (EOF라 검색 노출이 약함). `developers.meta.com/horizon/downloads/package/meta-avatars-sdk/`에서 직접 받아야 한다.
- Fusion App ID와 Voice App ID는 **같은 `PhotonAppSettings` 에셋의 다른 필드**(App Id Fusion / App Id Voice)에 들어간다 — 별도 설정 파일이 아니다.

## 수정 시 주의
- 백엔드가 확정되면 이 문서의 "영구 저장 백엔드" 행을 갱신하고, `domain/room-architecture.md`의 오프라인 방문 섹션에도 반영해야 한다.
- Meta가 Avatars SDK를 공식 후속 SDK로 대체 발표하면(현재 시점 미확인) 이 문서와 마이그레이션 필요 여부를 같이 검토.
