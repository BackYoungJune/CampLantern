using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CampLantern.UI
{
    /// <summary>
    /// VR 월드스페이스 UI의 기본 버튼. UGUI Button + TMP 라벨을 감싸 타입 있는 API(라벨·활성·클릭)를 제공한다.
    /// 레이/포크 인터랙션은 Button의 배경 Image가 RaycastTarget이면 Meta PointableCanvas가 그대로 UGUI
    /// 포인터 이벤트로 넘겨주므로, 이 컴포넌트는 데스크톱/VR 구분 없이 동일하게 동작한다.
    ///
    /// 프리팹은 Tools > Make Assets > VR UI (Create All) 로 생성한다(VRUIFactory).
    /// UI 초기화 순서 원칙(rules/scripts.md): 스스로 외부 상태를 조회하지 않는다 — 소유자가 SetLabel로 Push한다.
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class VRUIButton : MonoBehaviour
    {
        [SerializeField] private Button m_button;
        [SerializeField] private TextMeshProUGUI m_label;

        /// <summary>버튼이 눌렸을 때. 소유 Manager/Controller가 구독한다.</summary>
        public event Action Clicked;

        /// <summary>표시 라벨. 소유자가 준비된 시점에 Push한다.</summary>
        public void SetLabel(string text)
        {
            if (m_label != null) m_label.text = text;
        }

        /// <summary>클릭 가능 여부(비활성 시 톤다운 + 입력 차단은 Button이 처리).</summary>
        public void SetInteractable(bool value)
        {
            if (m_button != null) m_button.interactable = value;
        }

        private void Awake()
        {
            // 코드로 참조 확정 — Inspector 누락에도 항상 올바른 상태(rules/scripts.md)
            if (m_button == null) m_button = GetComponent<Button>();
            if (m_label == null)  m_label  = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private void OnEnable()
        {
            if (m_button == null) return;
            // 중복 구독 방지 후 등록(rules/scripts.md)
            m_button.onClick.RemoveListener(HandleClick);
            m_button.onClick.AddListener(HandleClick);
        }

        private void OnDisable()
        {
            if (m_button != null) m_button.onClick.RemoveListener(HandleClick);
        }

        private void HandleClick() => Clicked?.Invoke();
    }
}
