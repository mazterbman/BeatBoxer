using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.ScriptableObject;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.UI.MainMenuUI
{
    public class BeatUiManager : MonoBehaviour
    {
        public static BeatUiManager Instance { get; private set; }

        [Header("Reference")]
        [SerializeField] private TrackSettings _trackSettings;
        [SerializeField] private AudioSource _sourceBack;

        [Header("Settings")] 
        [SerializeField] [Range(0, 2)] private float _timeLerpChangeAudio = 0.25f;

        public TrackSettings TrackSettings => _trackSettings;
        public AudioSource AudioSource => _sourceBack;

        public UnityAction<TrackSettings> OnTimingChanged;

        private Coroutine _changeTimingsCoroutine;
        
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(Instance.gameObject);
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _sourceBack.Stop();
            _sourceBack.clip = _trackSettings.AudioClip;
            _sourceBack.Play();
        }

        public void ChangeTimings(TrackSettings trackSettings)
        {
            if (trackSettings == _trackSettings)
            {
                Debug.Log($"Already have {trackSettings}");
                return;
            }
            
            if (_changeTimingsCoroutine != null)
            {
                StopCoroutine(_changeTimingsCoroutine);
                _changeTimingsCoroutine = null;
            }

            Debug.Log($"Was changed from {_trackSettings.name} on {trackSettings.name}");
            _trackSettings = trackSettings;
            _changeTimingsCoroutine = StartCoroutine(ChangeTimingsIE());
        }

        private IEnumerator ChangeTimingsIE()
        {
            float timePast = (_timeLerpChangeAudio - _timeLerpChangeAudio * _sourceBack.volume);
            while (timePast < _timeLerpChangeAudio)
            {
                timePast += Time.deltaTime;
                _sourceBack.volume = Mathf.Lerp(1, 0, timePast / _timeLerpChangeAudio);
                yield return null;
            }
            
            _sourceBack.volume = 0;
            timePast = 0;
            _sourceBack.Stop();
            _sourceBack.clip = _trackSettings.AudioClip;
            _sourceBack.Play();
            OnTimingChanged?.Invoke(_trackSettings);
            yield return null;
            
            while (timePast < _timeLerpChangeAudio)
            {
                timePast += Time.deltaTime;
                _sourceBack.volume = Mathf.Lerp(0, 1, timePast / _timeLerpChangeAudio);
                yield return null;
            }
            _sourceBack.volume = 1;
        }
    }
}
