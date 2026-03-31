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
        
        private void Awake()
        {
            _hideCoroutine = null;
            gameObject.SetActive(false);
        }

        public void Hide()
        {
            if (_hideCoroutine != null)
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
    }
}
