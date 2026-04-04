using System;
using System.Collections;
using System.Collections.Generic;
using Game.Scripts.ScriptableObject;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.GamePlay
{
    /// <summary>
    /// Controls the arrow movement and state (click/hold).
    /// </summary>
    public class ArrowController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Image _arrowImage;
        [SerializeField] private Image _lightImage;
        [SerializeField] private Image _traceImage;
        [SerializeField] private RectTransform _holder;

        [Header("Settings")]
        [SerializeField] private ColorsArrowSetting _colorsArrowSetting;

        private Coroutine _moveCoroutine;
        private Coroutine _holdCoroutine;
        private GamePlayController _controller;

        // Timing data
        private ColorsArrowSetting.ColorArrow _colorArrowSelected;
        private ArrowType _arrowType;
        private ArrowDirection _arrowDirection;
        private float _holdTimeLeft;
        private float _idealTime;
        private float _holdStartTime;
        private float _holdEndTime;
        private bool _isHeld;
        private bool _isPassedCenter;
        private bool _wasPressed;

        // Movement state
        private float _remainingTime;
        private float _totalMoveDuration;

        // ----- Публичные свойства -----
        public Color ColorFromDirection => _colorArrowSelected.Color;
        public Color InActiveColor => _colorArrowSelected.Color * _colorArrowSelected.ColorMultiplay;
        public ArrowType ArrowType => _arrowType;
        public ArrowDirection ArrowDirection => _arrowDirection;
        public float RemainingTime => _remainingTime;
        public float IdealTime => _idealTime;
        public float HoldStartTime => _holdStartTime;
        public float HoldEndTime => _holdEndTime;
        public float HoldTimeLeft => _holdTimeLeft;
        public bool IsHeld => _isHeld;
        public bool IsPassedCenter => _isPassedCenter;
        public bool IsPaused;


        /// <summary>
        /// Принудительно скрывает стрелку (промах). Теперь уничтожает объект.
        /// </summary>
        public void Hide()
        {
            if (_isPassedCenter || _wasPressed) return;
            
            _wasPressed = true;
            OnReachCenter();
        }
        
        private void Destroy()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            _controller.RemoveArrow(this);
            Destroy(gameObject);
        }

        public void OnHoldPress()
        {
            if (_arrowType != ArrowType.Hold || _isHeld) return;
            
            _isHeld = true;
            Hide();
        }

        public void OnHoldRelease()
        {
            if (_arrowType == ArrowType.Hold && _isHeld)
                _isHeld = false;
        }

        public void Show(GamePlayController controller,ArrowType arrowType, 
            ArrowDirection direction, float moveDuration, float idealTime,
            float holdStart = 0f, float holdEnd = 0f, float saveTime = 0f,
            int indexName = 0)
        {
            _controller = controller;
            
            _arrowType = arrowType;
            _arrowDirection = direction;
            _idealTime = idealTime;
            _holdStartTime = holdStart;
            _holdEndTime = holdEnd;

            // Полное время движения от левого края до правого края
            _remainingTime = moveDuration + saveTime;
            _totalMoveDuration = moveDuration * 2f;
            _isPassedCenter = false;
            
            // Начальная позиция (левый край)
            _holder.anchoredPosition = new Vector2(-_holder.sizeDelta.x, 0f);
            
            SetSizeHolder(arrowType);
            SetDirectionImage(direction);
            SetColorsFromDirection(direction);
            gameObject.name = $"{indexName}_Arrow_{arrowType}_{direction}";
            
            gameObject.SetActive(true);

            StopMove();
            _moveCoroutine = StartCoroutine(MoveCoroutine());
        }

        public void StopMove()
        {
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);
        }

        private void SetSizeHolder(ArrowType arrowType)
        {
            _traceImage.gameObject.SetActive(arrowType == ArrowType.Hold);
            if (arrowType != ArrowType.Hold)
                return;
            
            float time = Mathf.Abs(_holdEndTime - _holdStartTime);
            float speed = (1920f + _holder.sizeDelta.x) / 2f / _remainingTime;
            float wight = speed * time;
            
            RectTransform rect = _traceImage.rectTransform;
            Vector2 position = rect.anchoredPosition;
            rect.sizeDelta = new Vector2(wight, rect.sizeDelta.y);
            rect.anchoredPosition = position;
        }
        
        private IEnumerator MoveCoroutine()
        {
            float startRemaining = _totalMoveDuration;
            Vector2 startPos = _holder.anchoredPosition;
            float targetX = 1920f + _holder.sizeDelta.x; // правый край экрана (зависит от разрешения)
            float centerX = targetX / 2f;
            Vector2 targetPos = new Vector2(targetX, startPos.y);

            while (_totalMoveDuration > 0f)
            {
                if (IsPaused)
                {
                    yield return null;
                    continue;
                }
                
                _totalMoveDuration -= Time.deltaTime;
                float t = 1f - (_totalMoveDuration / startRemaining);
                _holder.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                _remainingTime -= Time.deltaTime;

                // Проверяем достижение центра (x = 960)
                if (!_isPassedCenter && _holder.anchoredPosition.x >= centerX)
                {
                    OnReachCenter();
                }

                yield return null;
            }

            // Достигли правого края – уничтожаем
            Destroy();
        }

        private void OnReachCenter()
        {
            _isPassedCenter = true;
            _lightImage.enabled = false;

            if (!_wasPressed)
            {
                _controller.ProcessHit(this, GamePlayController.MessageType.Late);
            }
            
            // Можно также запустить анимацию или звуковой эффект
            SetColors(_colorArrowSelected.Color * _colorArrowSelected.ColorMultiplay);
        }

        private void SetDirectionImage(ArrowDirection direction)
        {
            switch (direction)
            {
                case ArrowDirection.Up:
                    _arrowImage.rectTransform.localEulerAngles = Vector3.forward * 90;
                    break;
                case ArrowDirection.Down:
                    _arrowImage.rectTransform.localEulerAngles = Vector3.back * 90;
                    break;
                case ArrowDirection.Left:
                    _arrowImage.rectTransform.localEulerAngles = Vector3.forward * 180;
                    break;
                default: // Right
                    _arrowImage.rectTransform.localEulerAngles = Vector3.zero;
                    break;
            }
        }

        private void SetColorsFromDirection(ArrowDirection direction)
        {
            _colorArrowSelected = _colorsArrowSetting.ColorArrows.Find
                (arg1 => arg1.ArrowDirection == direction);
            SetColors(_colorArrowSelected.Color);
        }

        private void SetColors(Color color)
        {
            _arrowImage.color = color;
            _lightImage.color = color;
            _traceImage.color = color;
        }
    }

    [Serializable]
    public class ColorsArrowSetting
    {
        public List<ColorArrow> ColorArrows;
        
        [Serializable]
        public struct ColorArrow
        {
            public ArrowDirection ArrowDirection;
            public Color Color;
            public Color ColorMultiplay;
        }
    }
}