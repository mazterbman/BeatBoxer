using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.UI.Health
{
    public class HealthController : MonoBehaviour
    {
        public UnityAction<int> OnChanged;
        public UnityAction OnAdd;
        public UnityAction OnRemove;
        
        [Header("Reference")] [SerializeField] 
        private List<HeartUiController> _controllers;

        private int _countHeart = 5;

        private void Awake()
        {
            _countHeart = _controllers.Count;
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

        [ContextMenu("Add Heart")]
        public void Add()
        {
            if (_countHeart >= _controllers.Count)
            {
                return;
            }

            _countHeart++;
            OnAdd?.Invoke();
            OnChanged?.Invoke(_countHeart);
        }

        [ContextMenu("Remove Heart")]
        public void Remove()
        {
            if (_countHeart <= 0)
            {
                return;
            }

            _countHeart--;
            OnRemove?.Invoke();
            OnChanged?.Invoke(_countHeart);
        }

        public void ResetHealth()
        {
            _countHeart = _controllers.Count;
            _controllers.ForEach(arg1 => arg1.Show());
            OnChanged?.Invoke(_countHeart);
        }

        private void Removed()
        {
            _controllers[_countHeart].Hide();
        }

        private void Added()
        {
            _controllers[_countHeart - 1].Show();
        }
    }
}
