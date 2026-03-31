using System;
using System.Collections;
using Game.Scripts.ScriptableObject;
using UnityEngine;

namespace Game.Scripts.UI.MainMenuUI
{
    public class BeatUiController : MonoBehaviour
    {
        private static readonly int Beat = Animator.StringToHash("Beat");

        [Header("Reference")] 
        [SerializeField] private Animator _animator;

        private BeatUiManager _manager;
        private TrackSettings _trackSettings;
        private AudioSource _audioSource;
        
        private Coroutine _startCoroutine;
        private bool _needReset = false;
        
        private void Start()
        {
            _manager = BeatUiManager.Instance;
            _manager.OnTimingChanged += OnTimingChange;
            
            _audioSource = _manager.AudioSource;
            _trackSettings = _manager.TrackSettings;
            
            RestartPlay();
        }

        private void OnDisable()
        {
            if (_startCoroutine != null)
            {
                StopCoroutine(_startCoroutine);
                _startCoroutine = null;
            }
        }

        private void OnEnable()
        {
            if (!_needReset) return;

            RestartPlay();
        }

        private void OnDestroy()
        {
            if (_manager)
            {
                _manager.OnTimingChanged -= OnTimingChange;
            }
            
            if (_startCoroutine == null) return;
            
            StopCoroutine(_startCoroutine);
            _startCoroutine = null;
            _needReset = false;
        }

        private void OnTimingChange(TrackSettings trackSettings)
        {
            _trackSettings = trackSettings;
            RestartPlay();
        }

        private void RestartPlay()
        {
            if (_startCoroutine != null)
            {
                StopCoroutine(_startCoroutine);
                _startCoroutine = null;
            }

            if (gameObject.activeInHierarchy)
            {
                Play();
            }
            else
            {
                _needReset = true;
            }
        }

        private void Play()
        {
            _needReset = false;
            _startCoroutine = StartCoroutine(StartIE());
        }
        
        private IEnumerator StartIE()
        {
            float timePast = _audioSource ? _audioSource.time : 0;
            int index = 0;
            while (true)
            {
                timePast += Time.deltaTime;
                if (_trackSettings.TimingValues[index].TimeStart <= timePast)
                {
                    index++;
                    _animator.SetTrigger(Beat);
                }

                if (index >= _trackSettings.TimingValues.Count)
                {
                    index = 0;
                    timePast = 0;
                }
                yield return null;
            }
        }
    }
}
