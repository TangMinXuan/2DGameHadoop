using HadoopCore.Scripts.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace HadoopCore.Scripts.UI {
    public class InGameUI : MonoBehaviour {
        [SerializeField] private Button pauseBtn;
        
        private void Start() {
            pauseBtn.onClick.AddListener(OnPauseBtnClicked);
        }

        private void OnPauseBtnClicked() {
            LevelEventCenter.TriggerGamePaused();
        }
    }
}