using TMPro;
using UnityEngine;

namespace CampLantern.UI
{
    /// <summary>
    /// VR 월드스페이스 UI 패널의 기본 컨테이너. 월드스페이스 Canvas + 배경 + 제목 + 콘텐츠 영역을 묶고,
    /// Meta Interaction의 PointableCanvas(레이/포크 → UGUI 포인터 변환)를 이미 배선한 상태로 만들어진다.
    /// 나머지 게임 UI(상점·인벤토리·낚시 HUD 등)는 이 패널을 복제/확장해 그 위에 짓는다.
    ///
    /// 프리팹은 Tools > Make Assets > VR UI (Create All) 로 생성한다(VRUIFactory).
    ///
    /// 씬 배치: 월드스페이스라 위치를 씬/소유자가 정한다. 팔로우/빌보드가 필요하면 m_faceCamera를 켠다.
    /// 초기 표시 여부는 소유 Manager의 Awake에서 SetVisible로 제어한다(컴포넌트 자신의 Awake에서
    /// SetActive(false) 금지 — rules/scripts.md).
    /// </summary>
    public class VRUIPanel : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Canvas m_canvas;
        [SerializeField] private RectTransform m_content;   // 버튼 등 자식 UI를 담는 영역
        [SerializeField] private TextMeshProUGUI m_title;

        [Header("Behaviour")]
        [Tooltip("켜면 매 프레임 카메라(중앙 눈)를 바라보도록 회전한다. 고정 패널이면 꺼둔다.")]
        [SerializeField] private bool m_faceCamera = false;

        /// <summary>버튼 등을 부모로 붙일 콘텐츠 영역.</summary>
        public RectTransform Content => m_content;

        /// <summary>제목 텍스트. 소유자가 준비된 시점에 Push한다(rules/scripts.md).</summary>
        public void SetTitle(string text)
        {
            if (m_title != null) m_title.text = text;
        }

        /// <summary>패널 표시/숨김.</summary>
        public void SetVisible(bool visible) => gameObject.SetActive(visible);

        private void Awake()
        {
            if (m_canvas == null) m_canvas = GetComponent<Canvas>();
        }

        private void Update()
        {
            if (!m_faceCamera) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            // 카메라를 등지지 않고 마주 보도록: 패널 앞면(+Z)이 카메라를 향하게 회전
            Vector3 toCam = transform.position - cam.transform.position;
            toCam.y = 0f;                       // 수평 회전만 — 위/아래로 기울지 않게
            if (toCam.sqrMagnitude > 0.0001f)
                transform.rotation = Quaternion.LookRotation(toCam);
        }
    }
}
