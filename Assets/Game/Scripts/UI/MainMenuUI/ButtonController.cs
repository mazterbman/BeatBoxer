using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.UI.MainMenuUI
{
    public class ButtonController : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private AudioSource _source;
        [SerializeField] private TMP_Text _tmpText;
        
        [Header("Audio Settings")]
        [SerializeField] private AudioClip _hoverClip;
        [SerializeField] private AudioClip _clkClip;

        [Header("Hover Settings")]
        [SerializeField] [Range(0,1)] private float _timeHover;
        [SerializeField] private Vector3 _endScaleHover;
        [SerializeField] private AnimationCurve _hoverCurve;

        [Header("Text settings")] 
        [SerializeField] private Color _colorOnHover;

        [Header("Events")] 
        public UnityEvent OnClkEvent;

        private Color _originColorText;
        private Vector3 _hoverOrigin;
        private Coroutine _hoverCoroutine;

        private void Awake()
        {
            _hoverOrigin = transform.localScale;
            _originColorText = _tmpText.color;
        }

        public void HoverStart()
        {
            if (_hoverCoroutine != null)
            {
                StopCoroutine(_hoverCoroutine);
                _hoverCoroutine = null;
            }
            
            _source.Stop();
            _source.clip = _hoverClip;
            _source.Play();

            _hoverCoroutine = StartCoroutine(HoverIE(false));
        }

        public void HoverEnd()
        {
            if (_hoverCoroutine != null)
            {
                StopCoroutine(_hoverCoroutine);
                _hoverCoroutine = null;
            }
            
            _source.Stop();

            _hoverCoroutine = StartCoroutine(HoverIE(true));
        }

        public void Clk()
        {
            if (_hoverCoroutine != null)
            {
                StopCoroutine(_hoverCoroutine);
                _hoverCoroutine = null;
            }
            
            _source.Stop();
            _source.clip = _clkClip;
            _source.Play();
            
            OnClkEvent?.Invoke();
        }

        private IEnumerator HoverIE(bool inverse)
        {
            Color endColorText = _colorOnHover;
            float timeNow = _timeHover * (1 - (_endScaleHover.x - transform.localScale.x) / (_endScaleHover.x - _hoverOrigin.x));
            int multi = inverse ? -1 : 1;
            while (true)
            {
                timeNow += Time.deltaTime * multi;
                float valueCurve = _hoverCurve.Evaluate(timeNow / _timeHover);
                transform.localScale = Vector3.Lerp(_hoverOrigin, _endScaleHover, valueCurve);
                _tmpText.color = Color.Lerp(_originColorText, endColorText, valueCurve);

                if (inverse && timeNow <= 0)
                {
                    transform.localScale = _hoverOrigin;
                    _tmpText.color = _originColorText;
                    timeNow = 0;
                    break;
                }

                if (!inverse && timeNow >= _timeHover)
                {
                    transform.localScale = _endScaleHover;
                    _tmpText.color = endColorText;
                    timeNow = 1;
                    break;
                }
                
                yield return null;
            }
        }
    }
}
