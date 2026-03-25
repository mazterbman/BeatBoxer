using System.Collections;
using Game.Scripts.ScriptableObject;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Game.Scripts.GamePlay.Arrow
{
    /// <summary>
    /// Controls the arrow movement and state (click/hold).
    /// </summary>
    public class ArrowController : MonoBehaviour
    {
        #region FIELDS

        [Header("References")]
        [Tooltip("Image component for the arrow sprite.")]
        [SerializeField] private Image _arrowImage;

        [Tooltip("Image for the light effect (optional).")]
        [SerializeField] private Image _lightImage;

        [Tooltip("RectTransform that moves along the screen.")]
        [SerializeField] private RectTransform _holder;

        private Coroutine _moveCoroutine;
        private ObjectPool<ArrowController> _arrowPool;

        // Timing data
        private ArrowType _arrowType;
        private ArrowDirection _arrowDirection;
        private float _idealTime;           // Time in audio when arrow reaches center
        private float _holdStartTime;       // Time in audio when hold should start
        private float _holdEndTime;         // Time in audio when hold should end
        private bool _isHeld;               // Whether hold is currently active

        // Movement state
        private float _remainingTime;       // Time left until center
        private float _totalMoveDuration;

        #endregion

        #region Public API

        /// <summary>Gets the arrow type (Click or Hold).</summary>
        public ArrowType ArrowType => _arrowType;

        public ArrowDirection ArrowDirection => _arrowDirection;
        
        /// <summary>Gets the remaining time until the arrow reaches the center.</summary>
        public float RemainingTime => _remainingTime;

        /// <summary>Gets the ideal hit time in audio timeline.</summary>
        public float IdealTime => _idealTime;

        /// <summary>Gets the hold start time in audio timeline.</summary>
        public float HoldStartTime => _holdStartTime;

        /// <summary>Gets the hold end time in audio timeline.</summary>
        public float HoldEndTime => _holdEndTime;

        /// <summary>Gets whether the hold is currently active.</summary>
        public bool IsHeld => _isHeld;

        /// <summary>
        /// Initialises the arrow with all required data and starts movement.
        /// </summary>
        /// <param name="arrowPool">Pool to release the arrow to.</param>
        /// <param name="arrowType">Click or Hold.</param>
        /// <param name="direction">Direction (Up, Down, Left, Right).</param>
        /// <param name="moveDuration">Time to travel from edge to center.</param>
        /// <param name="idealTime">Audio time when arrow reaches center.</param>
        /// <param name="holdStart">Audio time when hold should start (0 for Click).</param>
        /// <param name="holdEnd">Audio time when hold should end (0 for Click).</param>
        public void Show(ObjectPool<ArrowController> arrowPool, ArrowType arrowType,
            ArrowDirection direction, float moveDuration, float idealTime,
            float holdStart = 0f, float holdEnd = 0f)
        {
            _arrowPool = arrowPool;
            _arrowType = arrowType;
            _idealTime = idealTime;
            _holdStartTime = holdStart;
            _holdEndTime = holdEnd;
            _remainingTime = moveDuration;
            _totalMoveDuration = moveDuration;
            _arrowDirection = direction;
            _isHeld = false;

            // Set visual direction
            SetDirectionImage(direction);

            // Name for debugging
            gameObject.name = $"Arrow_{arrowType}_{direction}";

            // Start from left edge
            _holder.anchoredPosition = new Vector2(-_holder.sizeDelta.x, 0f);
            gameObject.SetActive(true);

            // Stop any previous movement
            if (_moveCoroutine != null)
                StopCoroutine(_moveCoroutine);

            _moveCoroutine = StartCoroutine(MoveCoroutine());
        }

        /// <summary>Forces the arrow to hide immediately (miss).</summary>
        public void Hide()
        {
            if (_moveCoroutine != null)
            {
                StopCoroutine(_moveCoroutine);
                _moveCoroutine = null;
            }

            // Release back to pool after one frame
            StartCoroutine(ReleaseCoroutine());
        }

        /// <summary>Called when player presses the key for this arrow (only for Hold type).</summary>
        public void OnHoldPress()
        {
            if (_arrowType == ArrowType.Hold && !_isHeld)
                _isHeld = true;
        }

        /// <summary>Called when player releases the key for this arrow (only for Hold type).</summary>
        public void OnHoldRelease()
        {
            if (_arrowType == ArrowType.Hold && _isHeld)
                _isHeld = false;
        }

        #endregion

        #region Private API

        private IEnumerator MoveCoroutine()
        {
            float startRemaining = _remainingTime;
            Vector2 startPos = _holder.anchoredPosition;
            Vector2 targetPos = new Vector2(1920, startPos.y); // Right edge

            while (_remainingTime > 0f)
            {
                _remainingTime -= Time.deltaTime;
                float t = 1f - (_remainingTime / startRemaining);
                _holder.anchoredPosition = Vector2.Lerp(startPos, targetPos, t);
                yield return null;
            }

            // Arrow reached center – if it's a Click, it's a miss; if Hold, also miss if not held correctly
            _remainingTime = -1f;
            Hide();
        }

        private IEnumerator ReleaseCoroutine()
        {
            yield return null;
            _arrowPool.Release(this);
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

        #endregion
    }
}