using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Scripts.UI.SettingsCanvas
{
    public class CanvasSettingsController : MonoBehaviour
    {
        private static readonly int HideID = Animator.StringToHash("Hide");

        [Header("Reference")] 
        [SerializeField] private Animator _animator;
        [SerializeField] private InputActionReference _inputAction;

        private Coroutine _hideCoroutine;
        public bool CanOpenMenu = true;
        
        private void Awake()
        {
            _hideCoroutine = null;
            CanOpenMenu = true;
            gameObject.SetActive(false);

            if (_inputAction)
            {
                _inputAction.action.performed += InputPerformed;
            }
        }

        private void OnDestroy()
        {
            if (_inputAction)
            {
                _inputAction.action.performed -= InputPerformed;
            }
        }

        public void Hide()
        {
            if (_hideCoroutine != null || !CanOpenMenu)
            {
               return;
            }

            _hideCoroutine = StartCoroutine(HideIE());
        }

        private IEnumerator HideIE()
        {
            _animator.SetTrigger(HideID);
            yield return null;
            yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);
            gameObject.SetActive(false);
            _hideCoroutine = null;
        }

        private void InputPerformed(InputAction.CallbackContext context)
        {
            if (!CanOpenMenu) return;
            
            if (gameObject.activeInHierarchy)
            {
                Hide();
            }
            else
            {
                gameObject.SetActive(true);
            }
        }
    }
}
