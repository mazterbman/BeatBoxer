using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.UI.Rating
{
    public class RatingController : MonoBehaviour
    {
        public UnityAction<int> OnChanged;
        
        [Header("Reference")] [SerializeField] 
        private List<RatingUiController> _controllers;

        private int _countRating = 5;
        public int Rating => _countRating;
        
        private void Awake()
        {
            _countRating = _controllers.Count;
            SetRating(0);
        }

        [ContextMenu("ShakeAll")]
        public void ShakeAll()
        {
            _controllers.ForEach(arg1 => arg1.Shake());
        }

        public void SetRating(float percent)
        {
            percent = Mathf.Clamp01(percent);
            int value = Mathf.Clamp(Mathf.RoundToInt(percent * _controllers.Count), 0, _controllers.Count);
            
            for (int i = 0; i < value; i++)
            {
                _controllers[i].Show();
            }
            for (int i = value; i < _controllers.Count; i++)
            {
                _controllers[i].Hide();
            }
            
            _countRating = value;
            OnChanged?.Invoke(_countRating);
        }
    }
}