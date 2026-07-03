# Step 09: 근접 음성·음소거 (⚠️ 선행 조건 있음)

- **영역:** `voice` (Assets/Scripts/Networking/Voice/)
- **선행 단계:** step-07 완료 + **Photon Voice 2 (Fusion 통합판) 임포트 — 현재 프로젝트에 없음 (2026-07-03 확인)**
- **후행 단계:** 없음 (P0 마지막 단계)

---

## 목적
사냥터 세션에서 근접 음성 대화와 원터치 음소거를 제공한다. GDD 6-4 안전장치의 P0 최소판 — 음소거만 (차단·신고·개인 공간 버블은 P0 소셜 MVP 이후).

---

## 에이전트 실행 지침

**착수 전 확인:** `Assets/Photon/PhotonVoice/` 폴더 존재 여부를 먼저 확인한다. 없으면 사용자에게 Photon Voice 2 (Voice for Fusion) `.unitypackage` 임포트를 요청하고 중단한다. Voice App ID는 `PhotonAppSettings`에 이미 입력되어 있음.

`/task-start`를 먼저 호출해 범위를 확정한 뒤, 아래 지시를 수행한다.

### 생성/수정 파일
- `Assets/Scripts/Networking/Voice/VoiceController.cs` — 생성 (Voice 연결·로컬 마이크)
- `Assets/Scripts/Networking/Voice/PlayerMute.cs` — 생성 (상대별 음소거 토글)

### 핵심 심볼
```csharp
public class VoiceController : MonoBehaviour
{
    public bool MicEnabled { get; }
    public void SetMicEnabled(bool enabled);   // 자기 마이크 on/off
}

public class PlayerMute : MonoBehaviour
{
    public bool IsMuted(PlayerRef player);
    public void SetMuted(PlayerRef player, bool muted);  // 원터치 음소거 — 상대 스피커 로컬 차단
}
```

### 선행 산출물 의존성
- `SessionLauncher` — step-07 (같은 세션에 Voice 연결)
- Photon Voice 2 패키지 (FusionVoiceClient 등) — **외부 선행 조건**

### 제약
- 근접 감쇠는 Voice의 3D 오디오 설정(AudioSource spatial) 사용 — 커스텀 감쇠 로직 금지 (P0 단순화).
- 음소거는 로컬 처리(상대 스피커 mute) — 서버 개입 불필요.
- 파티 무전 채널은 P2 (mvp-scope.md) — 채널 개념을 넣지 않는다.
- 마이크 권한 요청은 Quest 매니페스트 설정 필요할 수 있음 — ProjectSettings 변경이 필요하면 직접 수정하지 말고 사용자에게 안내 (RULE-04).

### 완료 판정
- [ ] `Grep "class VoiceController" Assets/Scripts/Networking/Voice/` 확인
- [ ] Unity 컴파일 통과
- [ ] 2클라이언트: 음성 송수신 동작, 음소거 토글 시 해당 상대만 안 들림

---

## 금지 사항
- 이 단계의 범위를 벗어난 다른 영역 파일을 수정하지 않는다.
- 인터페이스 시그니처를 임의로 바꾸지 않는다. 필요하면 아키텍트에게 보고하고 계획을 업데이트한다.
- Voice 패키지가 없는 상태에서 컴파일 안 되는 코드를 커밋하지 않는다 — 선행 조건 미충족이면 중단이 정답.
