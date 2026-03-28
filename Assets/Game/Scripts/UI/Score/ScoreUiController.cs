using System;
using System.Collections;
using Game.Scripts.ScriptableObject;
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
        
        private ShakeSettings _loadedShakeSettings;
        private Coroutine _shakeCoroutine;

        private int _score;
        public int Score => _score;

        private int _maxScore;
        public int MaxScore => _maxScore;
        public float PercentOfMax => (float)_score / (float)_maxScore;

        private void Awake()
        {
            _loadedShakeSettings = _shakeSettings.Clone();
            _loadedShakeSettings.ShakeTransform = _scoreText.transform;
            _loadedShakeSettings.SetNormalScale();
            
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

        public void SetMaxScore(int value)
        {
            _maxScore = value;
            _score = Mathf.Clamp(_score,0, _maxScore);
        }

        private void UpdateText()
        {
            _scoreText.text = _score.ToString("000000");
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
