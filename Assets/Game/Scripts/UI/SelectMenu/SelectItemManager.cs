using System;
using System.Collections.Generic;
using Game.Scripts.UI.LoadingCanvas;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.SelectMenu
{
    public class SelectItemManager : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private List<SelectItemController> _controllers;
        [SerializeField] private RectTransform _content;
        [SerializeField] private VerticalLayoutGroup _verticalLayoutGroup;

        [Header("Settings")] 
        [SerializeField] private InfoForMove _infoForMove;

        private void Start()
        {
            foreach (var item in _controllers)
            {
                item.Initialize(_content, _verticalLayoutGroup, 
                    this, _infoForMove);
            }
        }

        public void CloseOther(SelectItemController controller)
        {
            foreach (var item in _controllers)
            {
                if (item != controller) item.Close();
            }
        }

        public void UnInteractiveOther(SelectItemController controller)
        {
            foreach (var item in _controllers)
            {
                if (item != controller) item.UnInteractive();
            }
        }
        
        public void InteractiveAll()
        {
            foreach (var item in _controllers)
            {
                item.Interactive();
            }
        }

        public void OnClkClose()
        {
            bool haveOpen = false;
            foreach (var item in _controllers)
            {
                if (item.State != StateItem.Open) continue;
                
                haveOpen = true;
                item.CloseMenu();
            }
            
            if (!haveOpen)
            {
                LoadingManager.Instance.LoadSceneAsync(0);
            }
        }
        
    }

    [Serializable]
    public class InfoForMove
    {
        public AnimationCurve Curve;
        public float Time;

        public float Evaluate(float time)
        {
            return Curve.Evaluate(time / Time);
        }
    }
}
