using System.Collections;
using Game.Scripts.UI.MainMenuUI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.GamePlay
{
    public class MenuController : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private InputActionReference _openAction;
        [SerializeField] private ButtonMenuController _closeButton;
        [SerializeField] private GamePlayController _gamePlayController;

        [Space] 
        [SerializeField] private GameObject _menuObject;
        [SerializeField] private Animator _countDownAnimator;
        
        [Space]
        [SerializeField] private PlayerInput _playerInput;
        
        private InputActionMap _uiMap;
        private InputActionMap _playerMap;

        private Coroutine _closeCoroutine;

        public bool CanOpenMenu = true;

        private void Awake()
        {
            CanOpenMenu = true;
            _uiMap = _playerInput.actions.FindActionMap("UI");
            _playerMap = _playerInput.actions.FindActionMap("Player");
            
            _openAction.action.performed += Open;
            _closeButton.OnClkEvent.AddListener(Close);
            
            
            gameObject.SetActive(false);
            _menuObject.SetActive(false);
            _countDownAnimator.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            _openAction.action.performed -= Open;
            _closeButton.OnClkEvent.RemoveListener(Close);
        }

        private void Open(InputAction.CallbackContext context)
        {
            if (!CanOpenMenu) return;
            
            gameObject.SetActive(true);
            _menuObject.SetActive(true);
            _countDownAnimator.gameObject.SetActive(false);
            
            _gamePlayController.PauseGame();
        }

        private void Close()
        {
            if (_countDownAnimator.gameObject.activeInHierarchy || !CanOpenMenu) return;
            
            if (_closeCoroutine != null)
            {
                StopCoroutine(_closeCoroutine);
                _closeCoroutine = null;
            }
            
            gameObject.SetActive(true);
            _menuObject.SetActive(false);
            _countDownAnimator.gameObject.SetActive(true);
            _closeCoroutine = StartCoroutine(CloseIE());
        }

        private IEnumerator CloseIE()
        {
            yield return null;
            yield return new WaitForSeconds(_countDownAnimator.GetCurrentAnimatorStateInfo(0).length);
            
            _gamePlayController.UnPauseGame();
            _countDownAnimator.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }
    }
}
