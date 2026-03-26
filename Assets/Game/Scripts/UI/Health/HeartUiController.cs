using System;
using System.Collections;
using Game.Scripts.ScriptableObject;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Health
{
    public class HeartUiController : MonoBehaviour
    {
        private static readonly int State = Animator.StringToHash("State");

        [Header("Reference")] [SerializeField] 
        private Image _imageBack;

        [SerializeField] 
        private Image _imageFront;

        [SerializeField] 
        private Image _imageFill;

        [SerializeField] 
        private Image _imageLightUp;

        [SerializeField] 
        private Image _imageLightDown;
        
        [Space][SerializeField] 
        private Animator _animator;

        [Header("Settings")] [SerializeField] 
        private MyColorBlock _colorBlock;

        [SerializeField] 
        private ShakeSettings _shakeSettings;

        private Coroutine _shakeCoroutine;
        private ShakeSettings _loadedShakeSettings;

        private void Awake()
        {
            SetBlockColors();
            _loadedShakeSettings = _shakeSettings.Clone();
            _loadedShakeSettings.ShakeTransform = transform;
            _loadedShakeSettings.SetNormalScale();
        }

        private void OnValidate()
        {
            SetBlockColors();
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
        
        public void Hide()
        {
            _animator.SetInteger(State, 2);
        }

        public void Show()
        {
            _animator.SetInteger(State, 1);
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

        private void SetBlockColors()
        {
            _imageBack.color = _colorBlock.OriginColor * _colorBlock.BackColor;
            _imageFill.color = _colorBlock.OriginColor * _colorBlock.NormalColor;
            _imageFront.color = _colorBlock.OriginColor * _colorBlock.NormalColor;
            
            _imageLightDown.color = _colorBlock.OriginColor * _colorBlock.NormalColor;
            _imageLightUp.color = _colorBlock.OriginColor * _colorBlock.NormalColor;
        }
    }

    [Serializable]
    public struct MyColorBlock
    {
        public Color OriginColor;
        public Color NormalColor;
        public Color BackColor;
    }
}
