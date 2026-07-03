# Step 07: Fusion 세션 진입

- **영역:** `networking` (Assets/Scripts/Networking/)
- **선행 단계:** step-01 완료 필요 (네임스페이스 규칙만 공유 — Core 로직 의존 없음). step-02~06과 병렬 가능
- **후행 단계:** step-08(협동 사냥)이 NetworkRunner를 사용, step-09(음성)가 세션에 Voice 연결

---

## 목적
Photon Fusion 2로 2인이 같은 Room(사냥터 존 1개)에 접속하는 최소 세션 관리. room-architecture.md의 4공간 구조 중 P0는 **사냥터 존 하나만** 네트워크로 구현한다 — P0 완료 판단이 "2인 협동"이므로 이것만 있으면 된다.

---

## 에이전트 실행 지침

`/task-start`를 먼저 호출해 범위를 확정한 뒤, 아래 지시를 수행한다.
`.claude/domain/room-architecture.md`, `tech-stack-decisions.md`를 먼저 읽는다.
Fusion 2 API는 `Assets/Photon/Fusion/` 소스와 공식 문서(doc.photonengine.com/fusion) 기준 — PUN 2 API와 혼동 금지.

### 생성/수정 파일
- `Assets/Scripts/Networking/SessionLauncher.cs` — 생성 (NetworkRunner 시작/종료)

### 핵심 심볼
```csharp
public class SessionLauncher : MonoBehaviour
{
    public NetworkRunner Runner { get; private set; }
    public event Action<NetworkRunner> SessionStarted;

    // Shared Mode로 사냥터 존 Room 접속 (roomName 규칙: hunt_zone_{zoneId})
    public async Task StartHuntZone(string zoneId, CancellationToken ct);
    public async Task Shutdown();
}
```

### 선행 산출물 의존성
- 없음 (Fusion 패키지만)

### 제약
- Fusion **Shared Mode** 사용 — P0에서 데디케이티드/호스트 모드 불필요, Shared가 이후 "서버 관리형 영지"(room-architecture.md 8-4)와도 결이 맞음. 모드 변경이 필요하다고 판단되면 아키텍트에게 보고.
- async 메서드는 CancellationToken 파라미터 필수 (scripts.md).
- App ID는 코드에 넣지 않는다 — `PhotonAppSettings` 에셋에 이미 설정됨.
- NetworkRunner 프리팹/씬 배치가 필요하면 코드에서 `AddComponent` 방식 우선 (RULE-02 — 씬 직접 편집 회피).
- 매칭/샤딩/로비는 P0 범위 밖 — 고정 zoneId 문자열로 같은 Room 합류만.

### 완료 판정
- [ ] `Grep "class SessionLauncher" Assets/Scripts/Networking/` 확인
- [ ] Unity 컴파일 통과
- [ ] 에디터 2개 인스턴스(또는 에디터+빌드)에서 같은 zoneId로 접속 시 `Runner.SessionInfo.PlayerCount == 2` 확인

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 영역 파일을 수정하지 않는다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다. 필요하면 아키텍트에게 보고하고 계획을 업데이트한다.
- 로비/낚시터/영지 Room은 만들지 않는다 — 사냥터 존 하나만.
