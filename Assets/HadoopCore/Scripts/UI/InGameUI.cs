using DG.Tweening;
using HadoopCore.Scripts.Manager;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HadoopCore.Scripts.UI {
    public class InGameUI : MonoBehaviour {
        [SerializeField] private Button pauseBtn;
        [SerializeField] private RectTransform leftMoveBtn;
        [SerializeField] private RectTransform rightMoveBtn;

        [Header("按钮动画")]
        [SerializeField] private float pressScale = 1.2f;
        [SerializeField] private float animDuration = 0.1f;

        // 当前各方向是否被按住
        private bool _leftPressed;
        private bool _rightPressed;

        private Player _player;

        private void Start() {
            pauseBtn.onClick.AddListener(OnPauseBtnClicked);
            pauseBtn.onClick.AddListener(() => AudioManager.Instance.PlayBtnSfx());

            // 找到场景中的 Player
            _player = FindObjectOfType<Player>();

            // 为移动按钮添加按压缩放动画 + 移动驱动
            if (leftMoveBtn != null) {
                AddPressAnimation(leftMoveBtn);
                AddMoveInput(leftMoveBtn, Vector2.left);
            }
            if (rightMoveBtn != null) {
                AddPressAnimation(rightMoveBtn);
                AddMoveInput(rightMoveBtn, Vector2.right);
            }
        }

        private void Update() {
            if (_player == null) return;

            // 每帧持续驱动 UI 移动输入，确保 FixedUpdate 里的合并输入始终有效
            Vector2 input = Vector2.zero;
            if (_leftPressed) input += Vector2.left;
            if (_rightPressed) input += Vector2.right;
            _player.SetUIMoveInput(input);
        }

        private void OnPauseBtnClicked() {
            LevelEventCenter.TriggerGamePaused();
        }

        private void AddPressAnimation(RectTransform target) {
            var trigger = target.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) {
                trigger = target.gameObject.AddComponent<EventTrigger>();
            }

            // PointerDown → 放大
            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener(_ => {
                target.DOKill();
                target.DOScale(pressScale, animDuration).SetEase(Ease.OutBack).SetUpdate(true);
            });
            trigger.triggers.Add(pointerDown);

            // PointerUp → 恢复
            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener(_ => {
                target.DOKill();
                target.DOScale(1f, animDuration).SetEase(Ease.InBack).SetUpdate(true);
            });
            trigger.triggers.Add(pointerUp);
        }

        /// <summary>
        /// 为移动按钮绑定 PointerDown/PointerUp 来设置按压状态标志
        /// </summary>
        private void AddMoveInput(RectTransform target, Vector2 direction) {
            var trigger = target.gameObject.GetComponent<EventTrigger>();
            if (trigger == null) {
                trigger = target.gameObject.AddComponent<EventTrigger>();
            }

            bool isLeft = direction == Vector2.left;

            // PointerDown → 标记按住
            var pointerDown = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            pointerDown.callback.AddListener(_ => {
                if (isLeft) _leftPressed = true;
                else _rightPressed = true;
            });
            trigger.triggers.Add(pointerDown);

            // PointerUp → 取消标记
            var pointerUp = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            pointerUp.callback.AddListener(_ => {
                if (isLeft) _leftPressed = false;
                else _rightPressed = false;
            });
            trigger.triggers.Add(pointerUp);

            // PointerExit → 手指滑出按钮区域也要取消（防止卡住）
            var pointerExit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            pointerExit.callback.AddListener(_ => {
                if (isLeft) _leftPressed = false;
                else _rightPressed = false;
                // 同步恢复按钮缩放
                target.DOKill();
                target.DOScale(1f, animDuration).SetEase(Ease.InBack).SetUpdate(true);
            });
            trigger.triggers.Add(pointerExit);
        }
    }
}
