using System;
using System.Collections;
using Game.Scripts.UI.MainMenuUI;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.SelectMenu
{
    public class SelectItemController : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private ButtonMenuController _buttonMenuController;
        [SerializeField] private CanvasGroup _canvasGroup;
        
        [Space]
        [SerializeField] private GameObject _menu;
        [SerializeField] private Animator _menuAnimator;
        
        [Space]
        [SerializeField] private Image _imageBack;
        [SerializeField] private Image _imageIco;

        [Header("Settings")] 
        [SerializeField] private Color _unSelectColor = Color.gray;
        [SerializeField] private Color _selectColor = Color.white;

        private Color _originBackColor;
        private Color _originIcoColor;
        
        private InfoForMove _infoForMove;
        private RectTransform _content;
        private VerticalLayoutGroup _verticalLayoutGroup;
        private RectTransform _rect;
        private SelectItemManager _manager;

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
            
            _originBackColor = _imageBack.color;
            _originIcoColor = _imageIco.color;
            
            _infoForMove = infoForMove;
            _manager = manager;
            _content = content;
            _verticalLayoutGroup = contentVerticalGroup;
            
            _state = _indexSelect == 0 ? StateItem.EndMove : StateItem.UnSelect;

            _imageBack.color = _originBackColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            _imageIco.color = _originIcoColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            
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
            
            _state = StateItem.UnSelect;
            _imageBack.color = _originBackColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
            _imageIco.color = _originIcoColor * (_state == StateItem.UnSelect ? _unSelectColor : _selectColor);
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

            _moveCoroutine = StartCoroutine(MoveIE());
            _state = StateItem.Move;
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
}
