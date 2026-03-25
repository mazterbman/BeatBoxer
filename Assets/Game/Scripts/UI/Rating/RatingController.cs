using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.UI.Rating
{
    public class RatingController : MonoBehaviour
    {
        public UnityAction<int> OnChanged;
        public UnityAction OnAdd;
        public UnityAction OnRemove;
        
        [Header("Reference")] [SerializeField] 
        private List<RatingUiController> _controllers;

        private int _countRating = 5;
        
        private void Awake()
        {
            _countRating = _controllers.Count;
            OnAdd += Added;
            OnRemove += Removed;
        }

        private void OnDestroy()
        {
            OnAdd -= Added;
            OnRemove -= Removed;
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
            
            if (value > _countRating)
            {
                _countRating = value;
                OnAdd?.Invoke();
                OnChanged?.Invoke(_countRating);
            }
            else if (value < _countRating)
            {
                _countRating = value;
                OnRemove?.Invoke();
                OnChanged?.Invoke(_countRating);
            }
        }

        private void Removed()
        {
            _controllers[_countRating].Hide();
        }

        private void Added()
        {
            _controllers[_countRating - 1].Show();
        }
    }
}