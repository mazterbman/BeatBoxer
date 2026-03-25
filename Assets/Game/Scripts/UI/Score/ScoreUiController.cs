using System;
using System.Collections;
using Game.Scripts.UI.Health;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.Score
{
    public class ScoreUiController : MonoBehaviour
    {
        [Header("Reference")] [SerializeField] 
        private TMP_Text _scoreText;
        
        [Header("Settings")] [SerializeField] 
        private ShakeSettings _shakeSettings;
        
        private Coroutine _shakeCoroutine;

        private int _score;
        public int Score => _score;

        private void Awake()
        {
            _score = 0;
            UpdateText();
        }

        public void AddScore(int score)
        {
            _score += score;
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

        public void ResetScore()
        {
            _score = 0;
            UpdateText();
        }

        private void UpdateText()
        {
            _scoreText.text = _score.ToString("000000");
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
    }
}
