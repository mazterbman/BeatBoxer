using System.Collections;
using System.Collections.Generic;
using Game.Scripts.UI.SelectMenu;
using Game.Scripts.UI.SettingsCanvas;
using TMPro;
using UnityEngine;

namespace Game.Scripts.GamePlay
{
    public class CanvasGoodEndingController : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private List<RatingImageController> _ratingImageControllers;
        [SerializeField] private Animator _animator;
        
        [Space]
        [SerializeField] private TMP_Text _scoreNumberText;
        [SerializeField] private TMP_Text _percentText;
        [SerializeField] private TMP_Text _comboText;
        [SerializeField] private TMP_Text _numberArrowSelectText;

        [Header("Settings")]
        [SerializeField] [Range(0, 5)] private float _timeToScore;
        [SerializeField] [Range(0, 5)] private float _timeStopRating;
        [SerializeField] [Range(0, 5)] private float _timeToCombo;
        [SerializeField] [Range(0, 5)] private float _timeToArrow;
        [SerializeField] [Range(0, 5)] private float _timeToPercent;

        private Coroutine _showCoroutine;
        
        public void Show(GoodEndingInformation info)
        {
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            
            _ratingImageControllers.ForEach(arg1 => arg1.Off());
            _showCoroutine = StartCoroutine(ShowIE(info));
        }

        private IEnumerator ShowIE(GoodEndingInformation info)
        {
            yield return new WaitForEndOfFrame();
            yield return new WaitForSeconds(_animator.GetCurrentAnimatorStateInfo(0).length);
            
            StartCoroutine(ScoreIE(info.Score, _timeToScore));
            StartCoroutine(RatingIE(info.CountRating, _timeStopRating));
            StartCoroutine(ComboIE(info.Combo, _timeToCombo));
            StartCoroutine(ArrowsSelectedIE(info.CountArrowsSelect, _timeToArrow));
            StartCoroutine(PercentIE(info.Percent, _timeToPercent));
        }

        private IEnumerator ScoreIE(int score, float time)
        {
            float timePast = 0;
            while (timePast < time)
            {
                timePast += Time.deltaTime;
                _scoreNumberText.text = Mathf.RoundToInt(Mathf.Lerp(0, score, timePast / time)).ToString("000000");
                yield return null;
            }

            _scoreNumberText.text = score.ToString("000000");
        }

        private IEnumerator RatingIE(int count, float time)
        {
            for (int i = 0; i < count; i++)
            {
                _ratingImageControllers[i].On();
                yield return new WaitForSeconds(time);
            }
        }

        private IEnumerator ComboIE(int combo, float time)
        {
            float timePast = 0;
            while (timePast < time)
            {
                timePast += Time.deltaTime;
                _comboText.text = "x" + Mathf.RoundToInt(Mathf.Lerp(0, combo, timePast / time)).ToString("000");
                yield return null;
            }

            _comboText.text = "x" + combo.ToString("000");
        }
        
        private IEnumerator PercentIE(float percent, float time)
        {
            float timePast = 0;
            while (timePast < time)
            {
                timePast += Time.deltaTime;
                var value = Mathf.RoundToInt(Mathf.Lerp(0, percent * 100, timePast / time));
                _percentText.text = $"{value}%";
                yield return null;
            }

            _percentText.text = $"{Mathf.RoundToInt(percent * 100)}%";
        }
        
        private IEnumerator ArrowsSelectedIE(int countArrow, float time)
        {
            float timePast = 0;
            while (timePast < time)
            {
                timePast += Time.deltaTime;
                _numberArrowSelectText.text = Mathf.RoundToInt(Mathf.Lerp(0, countArrow, timePast / time)).ToString("000");
                yield return null;
            }

            _numberArrowSelectText.text = countArrow.ToString("000");
        }
    }

    public struct GoodEndingInformation
    {
        public int CountRating;
        public int Score;
        public int Combo;
        public float Percent;
        public int CountArrowsSelect;
    }
}
