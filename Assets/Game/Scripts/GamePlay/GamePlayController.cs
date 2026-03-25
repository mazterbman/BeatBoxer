using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.GamePlay.Arrow;
using Game.Scripts.GamePlay.Message;
using Game.Scripts.ScriptableObject;
using Game.Scripts.UI.Center;
using Game.Scripts.UI.Combo;
using Game.Scripts.UI.Health;
using Game.Scripts.UI.Rating;
using Game.Scripts.UI.Score;
using UnityEngine;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Game.Scripts.GamePlay
{
    public class GamePlayController : MonoBehaviour
    {
        [Header("Reference")] [SerializeField] 
        private AudioSource _audioSource;
        
        [SerializeField]
        private RatingController _ratingController;

        [SerializeField] 
        private HealthController _healthController;

        [SerializeField] 
        private ScoreUiController _scoreUiController;

        [SerializeField] 
        private ComboUiController _comboUiController;

        [SerializeField] 
        private CenterUiController _centerUiController;
        
        [Header("Settings")]
        [SerializeField] 
        private MessageSettings _messageSettings;
        
        [SerializeField]
        private TimingSettings _timingSettings;
        
        [SerializeField]
        private PoolSettings _arrowPoolSettings;
        
        [SerializeField]
        private PoolSettings _messagePoolSettings;
        
        [Header("Timing Settings")]
        [SerializeField] [Range(0,10)]
        private float _timeToCenter = 5f;
        
        [SerializeField] [Range(0,1)]
        private float _timeNormal = 0.1f;
        
        [SerializeField] [Range(0,1)]
        private float _timePerfect = 0.05f;
        
        private Coroutine _startGameCoroutine;
        private Coroutine _endGameCoroutine;

        private MessageSettings _loadedMessageSettings;
        private TimingSettings _loadedTimingSettings;

        private static ObjectPool<ArrowController> _arrowPool;
        private static ObjectPool<MessageController> _messagePool;

        private void Start()
        {
            if (!_timingSettings || !_messageSettings)
            {
                return;
            }
            
            _loadedMessageSettings = _messageSettings.Clone();
            _loadedTimingSettings = _timingSettings.Clone();

            _healthController.OnChanged += CheckEnd;
            
            RandomizeAllMessages();
            SetTimingValues();
            
            _arrowPool = new ObjectPool<ArrowController>(CreateArrow, OnGetArrow, OnReleaseArrow, OnDestroyArrow, _arrowPoolSettings.CollectionCheck, _arrowPoolSettings.DefaultCapacity, _arrowPoolSettings.MaxSize);
            _messagePool = new ObjectPool<MessageController>(CreateMessage, OnGetMessage, OnReleaseMessage, OnDestroyMessage, _messagePoolSettings.CollectionCheck, _messagePoolSettings.DefaultCapacity, _messagePoolSettings.MaxSize);
            
            StartGame();
        }

        private void OnDestroy()
        {
            _healthController.OnChanged -= CheckEnd;
        }
        
        public void StartGame()
        {
            if (_startGameCoroutine != null)
            {
                StopCoroutine(_startGameCoroutine);
                _startGameCoroutine = null;
            }
    
            _healthController.ResetHealth();
            _comboUiController.ResetCombo();
            _scoreUiController.ResetScore();
            _ratingController.SetRating(1);
    
            _startGameCoroutine = StartCoroutine(StartGameIE());
        }

        private IEnumerator StartGameIE()
        {
            _audioSource.clip = _timingSettings.AudioClip;
            _audioSource.Play();
 
            Debug.Log($"<color=white>[StrengthController] [{gameObject.name}]</color> Start game with time: {_audioSource.clip.length}");
    
            yield return new WaitUntil(() => _audioSource.isPlaying);
    
            float timeEnd = _loadedTimingSettings.TimingValues.Last().TimeStart;
            int index = 0;
            while (_audioSource.time < timeEnd)
            {
                if (_loadedTimingSettings.TimingValues[index].TimeStart <= _audioSource.time)
                {
                    Debug.Log($"Spawn arrow on {_audioSource.time} when need {_loadedTimingSettings.TimingValues[index].TimeStart} TIME = {Time.time}");
                    SpawnArrow(_loadedTimingSettings.TimingValues[index].ArrowType);
                    index++;
                }
                yield return null;                
            }

            if (index < _loadedTimingSettings.TimingValues.Count)
            {
                SpawnArrow(_loadedTimingSettings.TimingValues[index].ArrowType);
            }
            
        }
        
        public void EndGame()
        {
            if (_startGameCoroutine != null)
            {
                StopCoroutine(_startGameCoroutine);
                _startGameCoroutine = null;
            }
     
            if (_endGameCoroutine != null)
            {
                StopCoroutine(_endGameCoroutine);
                _endGameCoroutine = null;
            }
     
            _endGameCoroutine = StartCoroutine(EndGameIE());
        }
        
        private IEnumerator EndGameIE()
        {
            _audioSource.Stop();
            yield return new WaitForEndOfFrame();
    
            _arrowPool.Clear();
            yield return new WaitForEndOfFrame();
            _arrowPool.Dispose();
            yield return new WaitForEndOfFrame();
            _messagePool.Clear();
            yield return new WaitForEndOfFrame();
            _messagePool.Dispose();
            yield return new WaitForEndOfFrame();
        }

        private void SetTimingValues()
        {
            foreach (var timingValue in _loadedTimingSettings.TimingValues)
            {
                timingValue.TimeStart -= _timeToCenter;
                if (timingValue.TimeEnd != 0) timingValue.TimeEnd -= _timeToCenter;
            }
        }
        
        #region Message Management
        
        [ContextMenu("Spawn Random Message")]
        private void SpawnRandomMessage()
        {
            MessageType messageType = (MessageType)Random.Range(0, 6);
            SpawnMessage(messageType);
        }
        
        private void SpawnMessage(MessageType messageType)
        {
            string message = GetMessage(messageType);
            
            MessageController messageController = _messagePool.Get();
            messageController.SetMessageType(messageType);
            messageController.SetMessage(message);
            messageController.Show(ref _messagePool);
            
            UpdateSystem(messageType);
            
            Debug.Log($"<color=white>[StrengthController] [{gameObject.name}]</color> Spawned message: {messageType} - {message}");
        }

        private void UpdateSystem(MessageType messageType)
        {
            switch (messageType)
            {
                case MessageType.Early:
                case MessageType.Late:
                case MessageType.Miss:
                    _healthController.Remove();
                    _comboUiController.ResetCombo();
                    break;
                case MessageType.Perfect:
                    _healthController.Add();
                    _comboUiController.AddCombo();
                    _scoreUiController.AddScore(100);
                    break;
                case MessageType.Normal:
                    _healthController.Add();
                    _comboUiController.AddCombo();
                    _scoreUiController.AddScore(20);
                    break;
            }
        }

        private void CheckEnd(int countHealth)
        {
            if (countHealth > 0) return;
            EndGame();
        }

        private string GetMessage(MessageType messageType)
        {
            string message = string.Empty;
            switch (messageType)
            {
                case MessageType.Normal:
                    if (_loadedMessageSettings.NormalMessages.Count == 0)
                        RandomizeMessage(messageType);
                    message = _loadedMessageSettings.NormalMessages[Random.Range(0, _loadedMessageSettings.NormalMessages.Count)];
                    break;
                case MessageType.Perfect:
                    if (_loadedMessageSettings.PerfectMessages.Count == 0)
                        RandomizeMessage(messageType);
                    message = _loadedMessageSettings.PerfectMessages[Random.Range(0, _loadedMessageSettings.PerfectMessages.Count)];
                    break;
                case MessageType.Late:
                    if (_loadedMessageSettings.LateMessages.Count == 0)
                        RandomizeMessage(messageType);
                    message = _loadedMessageSettings.LateMessages[Random.Range(0, _loadedMessageSettings.LateMessages.Count)];
                    break;
                case MessageType.Early:
                    if (_loadedMessageSettings.EarlyMessages.Count == 0)
                        RandomizeMessage(messageType);
                    message = _loadedMessageSettings.EarlyMessages[Random.Range(0, _loadedMessageSettings.EarlyMessages.Count)];
                    break;
                case MessageType.Miss:
                    if (_loadedMessageSettings.MissMessages.Count == 0)
                        RandomizeMessage(messageType);
                    message = _loadedMessageSettings.MissMessages[Random.Range(0, _loadedMessageSettings.MissMessages.Count)];
                    break;
                case MessageType.Failed:
                    if (_loadedMessageSettings.FailedMessages.Count == 0)
                        RandomizeMessage(messageType);
                    message = _loadedMessageSettings.FailedMessages[Random.Range(0, _loadedMessageSettings.FailedMessages.Count)];
                    break;
            }

            return message;
        }

        private void RandomizeAllMessages()
        {
            RandomizeMessage(MessageType.Normal);
            RandomizeMessage(MessageType.Perfect);
            RandomizeMessage(MessageType.Late);
            RandomizeMessage(MessageType.Early);
            RandomizeMessage(MessageType.Miss);
            RandomizeMessage(MessageType.Failed);
        }
        
        private void RandomizeMessage(MessageType messageType)
        {
            List<string> messages = new List<string>();
            List<string> loadedMessages = new List<string>();
            switch (messageType)
            {
                case MessageType.Normal:
                    loadedMessages = new List<string>(_messageSettings.NormalMessages);
                    break;
                case MessageType.Perfect:
                    loadedMessages = new List<string>(_messageSettings.PerfectMessages);
                    break;
                case MessageType.Late:
                    loadedMessages = new List<string>(_messageSettings.LateMessages);
                    break;
                case MessageType.Early:
                    loadedMessages = new List<string>(_messageSettings.EarlyMessages);
                    break;
                case MessageType.Miss:
                    loadedMessages = new List<string>(_messageSettings.MissMessages);
                    break;
                case MessageType.Failed:
                    loadedMessages = new List<string>(_messageSettings.FailedMessages);
                    break;
            }
            
            if (loadedMessages.Count == 0)
                return;
            
            int count = loadedMessages.Count;
            for (int i = 0; i < count; i++)
            {
                int index = Random.Range(0, loadedMessages.Count);
                string message = loadedMessages[index];
                messages.Add(message);
                loadedMessages.RemoveAt(index);
            }
            
            switch (messageType)
            {
                case MessageType.Normal:
                    _loadedMessageSettings.NormalMessages = new List<string>(messages);
                    break;
                case MessageType.Perfect:
                    _loadedMessageSettings.PerfectMessages = new List<string>(messages);
                    break;
                case MessageType.Late:
                    _loadedMessageSettings.LateMessages = new List<string>(messages);
                    break;
                case MessageType.Early:
                    _loadedMessageSettings.EarlyMessages = new List<string>(messages);
                    break;
                case MessageType.Miss:
                    _loadedMessageSettings.MissMessages = new List<string>(messages);
                    break;
                case MessageType.Failed:
                    _loadedMessageSettings.FailedMessages = new List<string>(messages);
                    break;
            }
            
            loadedMessages.Clear();
            messages.Clear();
        }
        
        #endregion
        
        #region Arrow Management

        [ContextMenu("Spawn Random Arrow")]
        private void SpawnRandomArrow()
        {
            ArrowType arrowType = (ArrowType)Random.Range(0, 2);
            SpawnArrow(arrowType);
        }
 
        private void SpawnArrow(ArrowType arrowType)
        {
            ArrowController arrowController = _arrowPool.Get();
            arrowController.SetArrowType(arrowType);
            arrowController.SetTime(_timeToCenter);
            arrowController.Show(ref _arrowPool);
     
            Debug.Log($"<color=white>[StrengthController] [{gameObject.name}]</color> Spawned arrow: {arrowType}");
        }

        #endregion
        
        #region Pooling
        // invoked when creating an item to populate the object pool
        private ArrowController CreateArrow()
        {
            GameObject arrowInstance = Instantiate(_arrowPoolSettings.Prefab, _arrowPoolSettings.Parent ? _arrowPoolSettings.Parent : transform, false);
            arrowInstance.SetActive(false);
            return arrowInstance.GetComponent<ArrowController>();
        }

        // invoked when creating an item to populate the object pool
        private MessageController CreateMessage()
        {
            GameObject messageInstance = Instantiate(_messagePoolSettings.Prefab, _messagePoolSettings.Parent ? _messagePoolSettings.Parent : transform, false);
            messageInstance.SetActive(false);
            return messageInstance.GetComponent<MessageController>();
        }
 
        // invoked when returning an item to the object pool
        private void OnReleaseMessage(MessageController messageController)
        {
            messageController.gameObject.SetActive(false);
        }
 
        // invoked when returning an item to the object pool
        private void OnReleaseArrow(ArrowController arrowController)
        {
            arrowController.gameObject.SetActive(false);
            if (arrowController.TimeLeft < 0f)
            {
                SpawnMessage(MessageType.Late);
            }
            //_activeArrows.Remove(arrowController);
        }
 
        // invoked when retrieving the next item from the object pool
        private void OnGetArrow(ArrowController arrowController)
        {
            arrowController.gameObject.SetActive(true);
            //_activeArrows.Add(arrowController);
        }
 
        // invoked when retrieving the next item from the object pool
        private void OnGetMessage(MessageController messageController)
        {
            messageController.gameObject.SetActive(true);
        }
 
        // invoked when returning an item to the object pool
        private void OnDestroyMessage(MessageController messageController)
        {
            Destroy(messageController.gameObject);
        }
 
        // invoked when returning an item to the object pool
        private void OnDestroyArrow(ArrowController arrowController)
        {
            Destroy(arrowController.gameObject);
        }
        #endregion

    }
    
    [Serializable]
    public class PoolSettings
    {
        public GameObject Prefab;
        public Transform Parent;
        public bool CollectionCheck = true;
        public int DefaultCapacity = 10;
        public int MaxSize = 15;
    }

    public enum MessageType
    {
        Normal = 0, 
        Perfect, 
        Late,
        Early,
        Miss,
        Failed,
    }
}
