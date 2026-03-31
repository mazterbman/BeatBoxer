using System;
using System.Collections;
using Game.Scripts.ScriptableObject;
using Game.Scripts.UI.MainMenuUI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.SelectMenu
{
    public class SelectItemController : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private ButtonMenuController _buttonMenuController;
        [SerializeField] private CanvasGroup _canvasGroup;
        [SerializeField] private TMP_Text _textNameTrack;
        
        [Space]
        [SerializeField] private GameObject _menu;
        [SerializeField] private Animator _menuAnimator;
        
        [Space]
        [SerializeField] private Image _imageBack;
        [SerializeField] private Image _imageIco;

        [Header("Settings")] 
        [SerializeField] private Color _unSelectColor = Color.gray;
        [SerializeField] private Color _selectColor = Color.white;

        [Space] 
        [SerializeField] private RotateSettings _rotateSettings;
        
        [Space] 
        [SerializeField] private TimingSettings _timingSettings;

        [Header("Settings for Track")]
        [SerializeField] [TextArea(2,2)] private string _nameTrack;

        private Color _originBackColor;
        private Color _originIcoColor;
        private Color _originalNameTrackColor;

        private BeatUiManager _beatUiManager;
        private InfoForMove _infoForMove;
        private RectTransform _content;
        private VerticalLayoutGroup _verticalLayoutGroup;
        private RectTransform _rect;
        private SelectItemManager _manager;

        private Coroutine _rotateCoroutine;
        private Coroutine _moveCoroutine;
        private Coroutine _colorCoroutine;
        
        private StateItem _state;
        private bool _isInitialize;
        private int _indexSelect;
        private float _contentYPosition = 0;

        private void Awake()
        {
            _rect = GetComponent<RectTransform>();
            _indexSelect = transform.GetSiblingIndex();
            _menu.SetActive(false);
        }

        public StateItem State => _state;

        public void Initialize(RectTransform content, VerticalLayoutGroup contentVerticalGroup,
            SelectItemManager manager, InfoForMove infoForMove)
        {
            if (_isInitialize) return;

            _beatUiManager = BeatUiManager.Instance;
            
            _originBackColor = _imageBack.color;
            _originIcoColor = _imageIco.color;
            _originalNameTrackColor = _textNameTrack.color;
            
            _infoForMove = infoForMove;
            _manager = manager;
            _content = content;
            _verticalLayoutGroup = contentVerticalGroup;

            _textNameTrack.text = _nameTrack;

            if (_indexSelect == 0)
            {
                _state = StateItem.EndMove;
                _beatUiManager.ChangeTimings(_timingSettings);
                StartRotate();
            }
            else
            {
                _state = StateItem.UnSelect;
            }

            _imageBack.color = _originBackColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            _imageIco.color = _originIcoColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            _textNameTrack.color = _originalNameTrackColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            
            _contentYPosition = (_rect.sizeDelta.y + _verticalLayoutGroup.spacing) * _indexSelect;
            gameObject.name = $"Level_{_indexSelect}";

            _isInitialize = true;
        }

        public void Close()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }
            
            if (_rotateCoroutine != null)
            {
                StopCoroutine(_rotateCoroutine);
                _rotateCoroutine = null;
            }
            
            _state = StateItem.UnSelect;
            _imageBack.color = _originBackColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            _imageIco.color = _originIcoColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            _textNameTrack.color = _originalNameTrackColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
        }

        public void UnInteractive()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;
        }

        public void Interactive()
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
        }

        public void OnClk()
        {
            if (!_isInitialize) return;

            switch (_state)
            {
                case StateItem.UnSelect:
                    //Need Move
                    StartMove();
                    StartRotate();
                    break;
                
                case StateItem.EndMove:
                    //Need Open Menu
                    ShowMenu();
                    break;
                
                case StateItem.Open:
                    //Need Close Menu
                    CloseMenu();
                    break;
                case StateItem.Move:
                    // Nothing
                    break;
                
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            _imageBack.color = _originBackColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            _imageIco.color = _originIcoColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            _textNameTrack.color = _originalNameTrackColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            _manager.CloseOther(this);
        }

        private void ShowMenu()
        {
            _menu.gameObject.SetActive(true);
            _manager.UnInteractiveOther(this);
            _state = StateItem.Open;
        }

        public void CloseMenu()
        {
            _menu.SetActive(false);
            _manager.InteractiveAll();
            _state = StateItem.EndMove;
        }

        private void StartMove()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            if (_rotateCoroutine != null)
            {
                StopCoroutine(_rotateCoroutine);
                _rotateCoroutine = null;
            }

            _beatUiManager.ChangeTimings(_timingSettings);
            _moveCoroutine = StartCoroutine(MoveIE());
            _state = StateItem.Move;
        }

        private void StartRotate()
        {
            if (_rotateCoroutine != null)
            {
                StopCoroutine(_rotateCoroutine);
                _rotateCoroutine = null;
            }
            
            _rotateCoroutine = StartCoroutine(RotateIE());
        }

        private IEnumerator RotateIE()
        {
            while (true)
            {
                _imageBack.rectTransform.Rotate(_rotateSettings.SelectedTypeRotate == RotateSettings.TypeRotate.Left ? Vector3.forward : Vector3.back,
                    Time.deltaTime * _rotateSettings.SpeedRotate);
                yield return null;
            }
        }

        private IEnumerator MoveIE()
        {
            float timePast = 0;
            Vector2 startPosition = _content.anchoredPosition;
            Vector2 endPosition = new Vector2(startPosition.x, _contentYPosition);
            while (timePast < _infoForMove.Time)
            {
                timePast += Time.deltaTime;
                _content.anchoredPosition = Vector2.Lerp(startPosition, endPosition, _infoForMove.Evaluate(timePast));
                yield return null;
            }

            _content.anchoredPosition = endPosition;
            _state = StateItem.EndMove;
        }
    }

    public enum StateItem
    {
        Move = 0,
        EndMove,
        Open,
        UnSelect
    }

    [Serializable]
    public class RotateSettings
    {
        public TypeRotate SelectedTypeRotate = TypeRotate.Left;
        public float SpeedRotate = 1;
        
        public enum TypeRotate
        {
            Left = 0,
            Right,
        }
    }
}
