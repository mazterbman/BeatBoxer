using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Scripts.UI.LoadingCanvas
{
    public class LoadingManager : MonoBehaviour
    {
        private static readonly int Hide = Animator.StringToHash("Hide");
        private static readonly int Show = Animator.StringToHash("Show");
        public static LoadingManager Instance { get; private set; }
        
        [Header("Reference")] 
        [SerializeField] private Animator _animator;
        [SerializeField] private CanvasGroup _canvasGroup;

        private Coroutine _coroutineHide;
        private Coroutine _coroutineShow;
        private int _indexScene;
        private bool _isHide = false;
        
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            _isHide = false;
           HideAnim();
        }

        public void LoadSceneAsync(int indexScene)
        {
            if (indexScene >= SceneManager.sceneCountInBuildSettings || indexScene < 0)
            {
                return;
            }

            _indexScene = indexScene;
            ShowAnim();
        }

        private void ShowAnim()
        {
            if (!_isHide) return;
            
            _isHide = false;
            if (_coroutineShow != null)
            {
                StopCoroutine(_coroutineShow);
                _coroutineShow = null;
            }
            
            if (_coroutineHide != null)
            {
                StopCoroutine(_coroutineHide);
                _coroutineHide = null;
            }

            gameObject.SetActive(true);
            _coroutineShow = StartCoroutine(ShowIE());
        }
        
        private void HideAnim()
        {
            if (_isHide) return;
            
            _isHide = true;

            if (_coroutineShow != null)
            {
                StopCoroutine(_coroutineShow);
                _coroutineShow = null;
            }
            
            if (_coroutineHide != null)
            {
                StopCoroutine(_coroutineHide);
                _coroutineHide = null;
            }

            gameObject.SetActive(true);
            _coroutineHide = StartCoroutine(HideIE());
        }

        private IEnumerator HideIE()
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _animator.SetTrigger(Hide);
            
            yield return null;
            yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);
            
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
            gameObject.SetActive(false);
        }
        
        private IEnumerator ShowIE()
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            _animator.SetTrigger(Show);
            
            yield return null;
            yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);
            yield return null;

             AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(_indexScene);
             // Wait until the asynchronous scene fully loads
             while (!asyncLoad.isDone)
             {
                 yield return null;
             }
             
             yield return null;
             HideAnim();
        }
    }
}
