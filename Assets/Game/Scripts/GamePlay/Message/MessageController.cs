using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;
using MessageType = Game.Scripts.GamePlay.GamePlayController.MessageType;

namespace Game.Scripts.GamePlay.Message
{
    public class MessageController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField]
        private TMP_Text _messageText;

        [SerializeField] 
        private RectTransform _holder;
        
        [Header("Settings")]
        [SerializeField]
        private ColorSettings _colorSettings;
        
        [SerializeField] [Range(0,3)]
        private float _showDuration = 0.7f;
        
        [SerializeField] [Range(0,3)]
        private float _hideDuration = 0.2f;
        
        [SerializeField]
        private Vector2 _offsetSpawn = new Vector2(0f, 10f);
        
        private Coroutine _endShowCoroutine;
        private Coroutine _showCoroutine;
        private MessageType _messageType;
        private string _message;
        private ObjectPool<MessageController> _messagePool;

        public string Message => _message;
        public void SetMessage(string message)
        {
            _message = message;
            _messageText.text = _message;
        }
        
        public MessageType MessageType => _messageType;
        public void SetMessageType(MessageType messageType)
        {
            _messageType = messageType;
            _messageText.color = GetColor();
        }

        public void Show(ref ObjectPool<MessageController> messagePool)
        {
            _messagePool = messagePool;
            
            Color originalColor = _messageText.color;
            originalColor.a = 0f;
            _messageText.color = originalColor;
            
            _messageText.rectTransform.anchoredPosition = Vector2.zero;
            _holder.anchoredPosition = Vector2.zero + _offsetSpawn;
     
            gameObject.SetActive(true);
            
            if (_showCoroutine != null)
            {
                StopCoroutine(_showCoroutine);
                _showCoroutine = null;
            }
            _showCoroutine = StartCoroutine(ShowCoroutine());
        }
        
        private float GetRandomAngle()
        {
            float angle = Random.Range(0,25f);
            return Random.Range(0,2) == 0 ? angle : -angle;
        }

        private Vector2 GetRandomOffset()
        {
            return new Vector2(0f, _holder.sizeDelta.y / 2f + Random.Range(0f, 10f));
        }
        
        private IEnumerator ShowCoroutine()
        {
            _holder.transform.localEulerAngles = new Vector3(0f, 0f, GetRandomAngle());
            Color originalColor = _messageText.color;
            Color targetColor = GetColor();
            
            float timeLeft = _showDuration;
            Vector2 originalOffset = _messageText.rectTransform.anchoredPosition;
            Vector2 offset = GetRandomOffset();
            while (timeLeft > 0f)
            {
                timeLeft -= Time.deltaTime;
                _messageText.rectTransform.anchoredPosition = Vector2.Lerp(originalOffset, offset, 1 - (timeLeft / _showDuration));
                _messageText.color = Color.Lerp(originalColor, targetColor, 1 - (timeLeft / _showDuration));
                yield return null;                
            }
            
            _messageText.rectTransform.anchoredPosition = offset;
            _messageText.color = targetColor;
            EndShow(_hideDuration);
        }
        
        private void EndShow(float duration = 0.2f)
        {
            if (_endShowCoroutine != null)
            {
                StopCoroutine(_endShowCoroutine);
                _endShowCoroutine = null;
            }
            _endShowCoroutine = StartCoroutine(EndShowCoroutine(duration));
        }

        private IEnumerator EndShowCoroutine(float duration = 0.2f)
        {
            yield return new WaitForSeconds(duration);
            _messagePool.Release(this);
        }
        
        private Color GetColor()
        {
            switch (_messageType)
            {
                case MessageType.Normal:
                    return _colorSettings.NormalColor;
                case MessageType.Perfect:
                    return _colorSettings.PerfectColor;
                case MessageType.Late:
                    return _colorSettings.LateColor;
                case MessageType.Early:
                    return _colorSettings.EarlyColor;
                case MessageType.Miss:
                    return _colorSettings.MissColor;
                case MessageType.Failed:
                    return _colorSettings.FailedColor;
                default:
                    return Color.white;
            }
        }
    }

    [Serializable]
    public class ColorSettings
    {
        public Color NormalColor = Color.white;
        public Color PerfectColor = Color.green;
        public Color LateColor = Color.yellow;
        public Color EarlyColor = Color.blue;
        public Color MissColor = Color.red;
        public Color FailedColor = Color.gray;
    }
}