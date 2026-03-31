using System;
using System.Collections.Generic;
using Game.Scripts.ScriptableObject;
using Game.Scripts.UI.LoadingCanvas;
using Game.Scripts.UI.MainMenuUI;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.SelectMenu
{
    public class MenuItemController : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private ButtonMenuController _buttonControl;
        [SerializeField] private List<RatingImageController> _ratingImageControllers;
        [SerializeField] private TMP_Text _scoreText;
        [SerializeField] private TMP_Text _comboText;

        public GamePlaySettings Settings;

        private int _countRating;
        private int _score;
        private int _combo;

        private void Awake()
        {
            _buttonControl.OnClkEvent.AddListener(LoadGamePlay);
        }

        private void OnDestroy()
        {
            _buttonControl.OnClkEvent.RemoveListener(LoadGamePlay);
        }

        public void Show()
        {
            LoadPrefs();
            ApplyValues();
        }

        private void LoadGamePlay()
        {
            // Load GamePLay
            LoadingManager.Instance.LoadSceneAsync(2);
        }
        
        
        private void LoadPrefs()
        {
            _countRating = PlayerPrefs.GetInt(Settings.RatingPref, 0);
            _score = PlayerPrefs.GetInt(Settings.ScorePref, 0);
            _combo = PlayerPrefs.GetInt(Settings.ComboPref, 0);
        }

        private void ApplyValues()
        {
            _ratingImageControllers.ForEach(arg1 => arg1.Off());
            for (int i = 0; i < _countRating; i++)
            {
                if (i < _ratingImageControllers.Count) _ratingImageControllers[i].On();
            }

            _scoreText.text = _score.ToString("000000");
            _comboText.text = "x" + _combo.ToString("000");
        }
        
    }
}
