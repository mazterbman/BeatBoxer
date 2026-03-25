using System.Collections;
using Game.Scripts.ScriptableObject;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UI;

namespace Game.Scripts.GamePlay.Arrow
{
    public class ArrowController : MonoBehaviour
    {
        [Header("Reference")] [SerializeField] 
        private Image _arrowImage;

        [SerializeField] 
        private Image _lightImage;    [SerializeField]
        private RectTransform _holder;
        
        private Coroutine _showCoroutine;
        private Coroutine _hideCoroutine;
        private ObjectPool<ArrowController> _arrowPool;

        private float _time;
        public float TimeLeft => _time;
        public void SetTime(float time)
        {
            _time = time;
        }
        
        private ArrowType _arrowType;
        public ArrowType ArrowType => _arrowType;        
        public void SetArrowType(ArrowType arrowType)
        {
            _arrowType = arrowType;
        }

        private ArrowDirection _arrowDirection;
        public ArrowDirection ArrowDirection => _arrowDirection;
        public void SetArrowDirection(ArrowDirection direction)
        {
            _arrowDirection = direction;
            SetDirectionImage(direction);
        }

        public void Show(ref ObjectPool<ArrowController> arrowPool)
        {
            gameObject.name = $"Arrow_{_arrowType}_{_arrowDirection}";
            
            _holder.anchoredPosition = new Vector2(_holder.sizeDelta.x * -1, 0f);
            _arrowPool = arrowPool;
            gameObject.SetActive(true);
            
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            _showCoroutine = StartCoroutine(ShowCoroutine());
        }

        private IEnumerator ShowCoroutine()
        {
            float timeStart = _time;
            Vector2 position = _holder.anchoredPosition;
            Vector2 targetPosition = new Vector2(1920, position.y);
            while (_time > 0f)
            {
                _time -= Time.deltaTime;
                _holder.anchoredPosition = Vector2.Lerp(position, targetPosition, 1 - (_time / timeStart));
                yield return null;
            }
            
            _time = -1f;
            Hide();
        }

        public void Hide()
        {
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
            
            _hideCoroutine = StartCoroutine(HideCoroutine());
        }
        
        private IEnumerator HideCoroutine()
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
                default:
                    _arrowImage.rectTransform.localEulerAngles = Vector3.zero;
                    break;
            }            
        }

        public void StopMove()
        {
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            
            if (_hideCoroutine != null)
            {
                StopCoroutine(_hideCoroutine);
                _hideCoroutine = null;
            }
        }
    }
}
