using System.Collections;
using Game.Scripts.ScriptableObject;
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
        
        private ShakeSettings _loadedShakeSettings;
        private Coroutine _shakeCoroutine;
        
        private int _combo;
        public int Combo => _combo;
        

        private void Awake()
        {
            _loadedShakeSettings = _shakeSettings.Clone();
            _loadedShakeSettings.ShakeTransform = _comboText.transform;
            _loadedShakeSettings.SetNormalScale();
            
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

        private void UpdateText()
        {
            _comboText.text = "x" + _combo.ToString("000");
        }
    }
}
