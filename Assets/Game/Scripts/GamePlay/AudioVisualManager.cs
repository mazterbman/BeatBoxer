using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.Global;
using Game.Scripts.ScriptableObject;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.GamePlay
{
    public class AudioVisualManager : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private List<AudioVisualizer> _visualizers;
        
        [Header("Settings")]
        [SerializeField] private ShakeSettings _shakeSettings;
        
        private ShakeSettings _loadedShakeSettings;
        private Coroutine _shakeCoroutine;
        private List<Image> _allBars = new List<Image>();

        private void Awake()
        {
            _visualizers.ForEach(arg1 =>
            {
                foreach (var bar in arg1.bars)
                {
                    if (bar.TryGetComponent(out Image image))
                    {
                        _allBars.Add(image);   
                    }
                }
            });

            _loadedShakeSettings = _shakeSettings.Clone();
            _loadedShakeSettings.ShakeTransform = transform;
            _loadedShakeSettings.SetNormalScale();
        }

        public void Visual(Color color)
        {
            _allBars.ForEach(arg1 => arg1.color = color);
        }
        
        public void Shake()
        {
            if (_shakeCoroutine != null)
            {
                StopCoroutine(_shakeCoroutine);
                _shakeCoroutine = null;
            }

            _shakeCoroutine = StartCoroutine(ShakeIE());
        }
        
        private IEnumerator ShakeIE()
        {
            float time = 0;
            while (time < _loadedShakeSettings.TimeShake)
            {
                time += Time.deltaTime;
                _loadedShakeSettings.ShakeTransform.localScale = Vector3.Lerp(_loadedShakeSettings.NormalScale,
                    _loadedShakeSettings.EndScale, _loadedShakeSettings.GetEvaluate(time));
                yield return null;
            }
            _loadedShakeSettings.ShakeTransform.localScale = Vector3.Lerp(_loadedShakeSettings.NormalScale,
                _loadedShakeSettings.EndScale, _loadedShakeSettings.GetEvaluate(_loadedShakeSettings.TimeShake));
        }
    }
}
