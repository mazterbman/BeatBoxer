using System.Collections;
using Game.Scripts.UI.Health;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.Combo
{
    public class ComboUiController : MonoBehaviour
    {
        [Header("Reference")] [SerializeField]
        private TMP_Text _comboText;

        [Header("Settings")] [SerializeField] 
        private ShakeSettings _shakeSettings;
        
        private Coroutine _shakeCoroutine;
        
        private int _combo;
        public int Combo => _combo;
        

        private void Awake()
        {
            _combo = 0;
            UpdateText();
        }

        public void AddCombo()
        {
            _combo++;
            UpdateText();
        }

        public void ResetCombo()
        {
            _combo = 0;
            UpdateText();
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

        private void UpdateText()
        {
            _comboText.text = "x" + _combo.ToString("000");
        }
    }
}
