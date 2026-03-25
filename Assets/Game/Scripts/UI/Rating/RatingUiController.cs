using System.Collections;
using System.Collections.Generic;
using Game.Scripts.UI.Health;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.Rating
{
    public class RatingUiController : MonoBehaviour
    {
        private static readonly int State = Animator.StringToHash("State");
        
        [Header("Reference")] [SerializeField] 
        private Image _imageBack;

        [SerializeField] 
        private Image _imageFront;

        [SerializeField] 
        private Image _imageFill;

        [SerializeField] 
        private List<Image> _imageLightnings;
        
        [Space][SerializeField] 
        private Animator _animator;

        [Header("Settings")] [SerializeField] 
        private MyColorBlock _colorBlock;
        
        [SerializeField] 
        private ShakeSettings _shakeSettings;
        
        private Coroutine _shakeCoroutine;

        private void Awake()
        {
            SetBlockColors();
            _shakeSettings.SetNormalScale();
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
            while (time < _shakeSettings.TimeShake)
            {
                time += Time.deltaTime;
                _shakeSettings.ShakeTransform.localScale = Vector3.Lerp(_shakeSettings.NormalScale,
                    _shakeSettings.EndScale, _shakeSettings.GetEvaluate(time));
                yield return null;
            }
            _shakeSettings.ShakeTransform.localScale = Vector3.Lerp(_shakeSettings.NormalScale,
                _shakeSettings.EndScale, _shakeSettings.GetEvaluate(_shakeSettings.TimeShake));
        }

        private void SetBlockColors()
        {
            _imageBack.color = _colorBlock.OriginColor * _colorBlock.BackColor;
            _imageFill.color = _colorBlock.OriginColor * _colorBlock.NormalColor;
            _imageFront.color = _colorBlock.OriginColor * _colorBlock.NormalColor;
            
            _imageLightnings.ForEach(arg1 => arg1.color = _colorBlock.OriginColor * _colorBlock.NormalColor);
        }
    }
}