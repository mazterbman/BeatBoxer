using System;
using System.Collections.Generic;
using Game.Scripts.Global;
using Game.Scripts.ScriptableObject;
using Game.Scripts.UI.LoadingCanvas;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.SelectMenu
{
    public class SelectItemManager : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private List<GamePlaySettings> _settingsList;
        [SerializeField] private RectTransform _content;
        [SerializeField] private VerticalLayoutGroup _verticalLayoutGroup;

        [Space] 
        [SerializeField] private GameObject _controllerPrefab;
        [SerializeField] private Transform _parentTransform;

        [Header("Settings")] 
        [SerializeField] private InfoForMove _infoForMove;

        [Header("Debug")] 
        [SerializeField] private GamePlaySettings _selectedSetting;

        private int _indexSelected = 0;
        private List<SelectItemController> _controllers = new List<SelectItemController>();

        private void Awake()
        {
            foreach (var setting in _settingsList)
            {
                SelectItemController controller = Instantiate(_controllerPrefab, _parentTransform)
                    .GetComponent<SelectItemController>();
                _controllers.Add(controller);
            }
        }

        private void Start()
        {
            _selectedSetting = _settingsList[0];
            _indexSelected = 0;
            for (var i = 0; i < _controllers.Count; i++)
            {
                _controllers[i].Initialize(_content, _verticalLayoutGroup,
                    this, _infoForMove, _settingsList[i]);
            }
            
            SPlayerPrefs.TrackIndexSelected = _indexSelected;
        }

        public void CloseOther(SelectItemController controller)
        {
            foreach (var item in _controllers)
            {
                if (item != controller) item.Close();
            }
        }

        public void SelectSettings(int index)
        {
            if (index < 0 || index >= _settingsList.Count)
            {
                return;
            }

            _indexSelected = index;
            _selectedSetting = _settingsList[index];
            
            SPlayerPrefs.TrackIndexSelected = _indexSelected;
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
