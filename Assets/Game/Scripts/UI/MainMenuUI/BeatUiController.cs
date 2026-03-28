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
        [SerializeField] private TimingSettings _timingSettings;
        [SerializeField] private Animator _animator;

        private Coroutine _startCoroutine;
        
        private void Start()
        {
            if (_startCoroutine != null)
            {
                StopCoroutine(_startCoroutine);
                _startCoroutine = null;
            }

            _startCoroutine = StartCoroutine(StartIE());
        }

        private void OnDestroy()
        {
            if (_startCoroutine == null) return;
            
            StopCoroutine(_startCoroutine);
            _startCoroutine = null;
        }

        private IEnumerator StartIE()
        {
            float timePast = 0;
            int index = 0;
            while (true)
            {
                timePast += Time.deltaTime;
                if (_timingSettings.TimingValues[index].TimeStart <= timePast)
                {
                    index++;
                    _animator.SetTrigger(Beat);
                }

                if (index >= _timingSettings.TimingValues.Count)
                {
                    index = 0;
                    timePast = 0;
                }
                yield return null;
            }
        }
    }
}
