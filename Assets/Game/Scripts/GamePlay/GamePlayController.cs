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
using UnityEngine.InputSystem;
using UnityEngine.Pool;
using Random = UnityEngine.Random;

namespace Game.Scripts.GamePlay
{
    public class GamePlayController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private RatingController _ratingController;
        [SerializeField] private HealthController _healthController;
        [SerializeField] private ScoreUiController _scoreUiController;
        [SerializeField] private ComboUiController _comboUiController;
        [SerializeField] private CenterUiController _centerUiController;
        [SerializeField] private PlayerInput _playerInput;

        [Header("Settings")]
        [SerializeField] private MessageSettings _messageSettings;
        [SerializeField] private TimingSettings _timingSettings;
        [SerializeField] private PoolSettings _messagePoolSettings; 

        [Header("Timing Windows")]
        [SerializeField] [Range(0, 0.5f)] private float _perfectWindow = 0.05f;
        [SerializeField] [Range(0, 0.5f)] private float _normalWindow = 0.1f;
        [SerializeField] [Range(0, 0.5f)] private float _saveTime = 0.1f;

        [Header("Movement")]
        [SerializeField] [Range(0.5f, 10f)] private float _timeToCenter = 5f;

        // Префаб стрелки (необходимо задать в инспекторе)
        [Header("Arrow Prefab")]
        [SerializeField] private ArrowController _arrowPrefab;
        [SerializeField] private Transform _arrowsParent;

        [Header("Debug")] 
        [SerializeField] private bool _godMode = false;

        private Coroutine _gameCoroutine;
        private List<ArrowController> _activeArrows = new List<ArrowController>();
        private List<ArrowController> _inActiveArrows = new List<ArrowController>();
        private ObjectPool<MessageController> _messagePool;

        private MessageSettings _loadedMessageSettings;
        private List<TimingValue> _adjustedTimingValues;
        private List<TimingValue> _originalTimingValues;

        private bool _isGameActive;
        private bool _wasInteract = false;
        private bool _inputActionsAdded;

        private void Start()
        {
            if (!_timingSettings || !_messageSettings || !_arrowPrefab)
            {
                Debug.LogError($"[GamePlayController] Missing timing, message, or arrow prefab!");
                return;
            }
            
            _loadedMessageSettings = _messageSettings.Clone();
            RandomizeAllMessages();

            // Сдвигаем времена на время движения, чтобы спавнить стрелки раньше
            _adjustedTimingValues = new List<TimingValue>();
            foreach (var tv in _timingSettings.TimingValues)
            {
                var copy = new TimingValue
                {
                    TimeStart = tv.TimeStart - _timeToCenter,
                    TimeEnd = tv.TimeEnd > 0 ? tv.TimeEnd - _timeToCenter : 0,
                    ArrowType = tv.ArrowType,
                    ArrowDirection = tv.ArrowDirection
                };
                _adjustedTimingValues.Add(copy);
            }
            
            _originalTimingValues = new List<TimingValue>(_timingSettings.TimingValues);

            // Создаём пул только для сообщений
            CreateMessagePool();

            _healthController.OnChanged += CheckEnd;

            if (!_playerInput)
                _playerInput = GetComponent<PlayerInput>();

            AddInputActions();
            StartGame();
        }

        private void OnDestroy()
        {
            _healthController.OnChanged -= CheckEnd;
            if (_gameCoroutine != null) StopCoroutine(_gameCoroutine);
            RemoveInputActions();
        }

        private void OnValidate()
        {
            _healthController.SetGodMode(_godMode);
        }

        public void StartGame()
        {
            if (_gameCoroutine != null) StopCoroutine(_gameCoroutine);
            _isGameActive = true;

            _healthController.ResetHealth();
            _comboUiController.ResetCombo();
            _scoreUiController.ResetScore();
            _ratingController.SetRating(1);

            // Удаляем все существующие стрелки
            foreach (var arrow in _activeArrows)
                Destroy(arrow.gameObject);
            _activeArrows.Clear();

            _gameCoroutine = StartCoroutine(GameCoroutine());
        }

        public void EndGame()
        {
            if (!_isGameActive) return;
            _isGameActive = false;

            if (_gameCoroutine != null)
                StopCoroutine(_gameCoroutine);

            _activeArrows.ForEach(arg1=>arg1.StopMove());
            _inActiveArrows.ForEach(arg1 => arg1.StopMove());
            
            _audioSource.Stop();
            _wasInteract = false;
            Debug.Log($"[GamePlayController] Game ended.");
        }
        
        public void ProcessHit(ArrowController arrow, MessageType result)
        {
            if (!arrow) 
                return;
            
            _activeArrows.Remove(arrow);
            _inActiveArrows.Add(arrow);
            
            arrow.Hide();
            Process(result);
            Debug.Log($"[GamePlayController] Hit {arrow.ArrowType} {arrow.ArrowDirection} with {result}");
        }

        public void RemoveArrow(ArrowController arrow)
        {
            if (!arrow)
                return;

            if (_inActiveArrows.Contains(arrow))
            {
                _inActiveArrows.Remove(arrow);
            }
            else
            {
                _activeArrows.Remove(arrow);
            }
        }

        private IEnumerator GameCoroutine()
        {
            // Добавляем задержку перед стартом, если нужно
            float startDelay = Mathf.Max(0, -_adjustedTimingValues[0].TimeStart);
            if (startDelay > 0)
            {
                Debug.Log($"[GamePlayController] Delaying start by {startDelay:F2}s to accommodate early spawn");
                yield return new WaitForSeconds(startDelay);
            }
            
            _audioSource.clip = _timingSettings.AudioClip;
            _audioSource.Play();

            yield return new WaitUntil(() => _audioSource.isPlaying);

            int nextIndex = 0;
            float endTime = _adjustedTimingValues.Count > 0 ? _adjustedTimingValues.Last().TimeStart : 0f;

            while (_audioSource.time < endTime && _isGameActive)
            {
                if (nextIndex < _adjustedTimingValues.Count &&
                    _adjustedTimingValues[nextIndex].TimeStart <= _audioSource.time)
                {
                    SpawnArrow(_adjustedTimingValues[nextIndex], _originalTimingValues[nextIndex]);
                    nextIndex++;
                }
                yield return null;
            }

            while (nextIndex < _adjustedTimingValues.Count && _isGameActive)
            {
                SpawnArrow(_adjustedTimingValues[nextIndex], _originalTimingValues[nextIndex]);
                nextIndex++;
                yield return null;
            }

            while (_activeArrows.Count > 0 && _isGameActive)
                yield return null;

            EndGame();
        }

        // Спавн стрелки без пула
        private void SpawnArrow(TimingValue adjustedTiming, TimingValue originalTiming)
        {
            // idealTime – момент достижения центра в аудиовремени (оригинальное время)
            float idealTime = originalTiming.TimeStart + _timeToCenter;
    
            ArrowController arrow = Instantiate(_arrowPrefab,
                _arrowsParent ? _arrowsParent : transform);
    
            // Используем adjustedTiming для времени старта и конца удержания
            arrow.Show(this, originalTiming.ArrowType,
                originalTiming.ArrowDirection, _timeToCenter, idealTime, 
                adjustedTiming.TimeStart,
                adjustedTiming.TimeEnd,
                _saveTime);
        }

        private void OnArrowPressed(ArrowDirection direction)
        {
            if (_activeArrows.Count <= 0)
            {
                Process(MessageType.Early);
                return;
            }

            ArrowController arrow = _activeArrows[0];

            if (arrow.RemainingTime > _normalWindow + _saveTime)
            {
                Process(MessageType.Early);
                return;
            }
            
            // Проверка направления
            if (arrow.ArrowDirection != direction)
            {
                ProcessHit(arrow, MessageType.Miss);
                return;
            }

            float currentTime = _audioSource.time;

            // Для Click-стрелок
            if (arrow.ArrowType == ArrowType.Click)
            {
                MessageType result;
                if (arrow.RemainingTime <= _perfectWindow + _saveTime)
                    result = MessageType.Perfect;
                else if (arrow.RemainingTime <= _normalWindow + _saveTime)
                    result = MessageType.Normal;
                else
                    result = MessageType.Late;    // нажали позже центра

                ProcessHit(arrow, result);
                return;
            }
            // Для Hold-стрелок
            else
            {
                // Проверяем, что нажатие произошло в интервале удержания
                if (currentTime >= arrow.HoldStartTime && currentTime <= arrow.HoldEndTime)
                {
                    arrow.OnHoldPress();
                }
                // Если вне интервала – игнорируем (промах будет при отпускании или таймауте)
                // Можно сразу считать Miss, если стрелка уже прошла центр
                else
                {
                    ProcessHit(arrow, MessageType.Miss);
                }
            }
        }

        private void OnArrowReleased(ArrowDirection direction)
        {
            if (_wasInteract)
            {
                _wasInteract = false;
                return;
            }

            if (_activeArrows.Count <= 0)
            {
                Process(MessageType.Miss);
                return;
            }

            ArrowController arrow = _activeArrows[0];
            if (arrow.ArrowType != ArrowType.Hold || arrow.ArrowDirection != direction)
            {
                ProcessHit(arrow, MessageType.Miss);
                return;
            }

            if (arrow.IsHeld)
            {
                float releaseTime = _audioSource.time;
                bool success = releaseTime >= arrow.HoldStartTime && releaseTime <= arrow.HoldEndTime;
                ProcessHit(arrow, success ? MessageType.Perfect : MessageType.Miss);
                arrow.OnHoldRelease();
                return;
            }
        }

        private void Process(MessageType result)
        {
            _wasInteract = true;
            SpawnMessage(result);
            UpdateSystem(result);
            
            _scoreUiController.Shake();
            _comboUiController.Shake();
            _healthController.ShakeAll();
            _ratingController.ShakeAll();
            _centerUiController.Clk();
        }

        private void UpdateSystem(MessageType result)
        {
            switch (result)
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

        private void CheckEnd(int health)
        {
            if (health <= 0)
                EndGame();
        }

        #region Input System

        private void AddInputActions()
        {
            if (_inputActionsAdded) return;

            var arrowAction = _playerInput.actions["Arrow"];
            if (arrowAction == null)
            {
                Debug.LogError($"[GamePlayController] Action 'Arrow' not found in Input Actions!");
                return;
            }

            arrowAction.started += OnArrowStarted;
            arrowAction.canceled += OnArrowCanceled;
            _inputActionsAdded = true;
        }

        private void RemoveInputActions()
        {
            if (!_inputActionsAdded) return;

            var arrowAction = _playerInput?.actions["Arrow"];
            if (arrowAction != null)
            {
                arrowAction.started -= OnArrowStarted;
                arrowAction.canceled -= OnArrowCanceled;
            }
            _inputActionsAdded = false;
        }

        private void OnArrowStarted(InputAction.CallbackContext context)
        {
            if (!_isGameActive) return;

            Vector2 inputVector = context.ReadValue<Vector2>();
            ArrowDirection direction = GetArrowDirectionFromInput(inputVector);
            OnArrowPressed(direction);
        }

        private void OnArrowCanceled(InputAction.CallbackContext context)
        {
            if (!_isGameActive) return;

            Vector2 inputVector = context.ReadValue<Vector2>();
            ArrowDirection direction = GetArrowDirectionFromInput(inputVector);
            OnArrowReleased(direction);
        }

        private ArrowDirection GetArrowDirectionFromInput(Vector2 input)
        {
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                return input.x > 0 ? ArrowDirection.Right : ArrowDirection.Left;
            else
                return input.y > 0 ? ArrowDirection.Up : ArrowDirection.Down;
        }

        #endregion

        #region Message Pool (остаётся без изменений)

        private void CreateMessagePool()
        {
            _messagePool = new ObjectPool<MessageController>(
                createFunc: () =>
                {
                    GameObject go = Instantiate(_messagePoolSettings.Prefab, _messagePoolSettings.Parent ? _messagePoolSettings.Parent : transform);
                    go.SetActive(false);
                    return go.GetComponent<MessageController>();
                },
                actionOnGet: (msg) => msg.gameObject.SetActive(true),
                actionOnRelease: (msg) => msg.gameObject.SetActive(false),
                actionOnDestroy: (msg) => Destroy(msg.gameObject),
                collectionCheck: _messagePoolSettings.CollectionCheck,
                defaultCapacity: _messagePoolSettings.DefaultCapacity,
                maxSize: _messagePoolSettings.MaxSize
            );
        }

        private void SpawnMessage(MessageType type)
        {
            string text = GetRandomMessage(type);
            MessageController msg = _messagePool.Get();
            msg.SetMessageType(type);
            msg.SetMessage(text);
            msg.Show(ref _messagePool);
        }

        private string GetRandomMessage(MessageType type)
        {
            List<string> list = type switch
            {
                MessageType.Normal => _loadedMessageSettings.NormalMessages,
                MessageType.Perfect => _loadedMessageSettings.PerfectMessages,
                MessageType.Late => _loadedMessageSettings.LateMessages,
                MessageType.Early => _loadedMessageSettings.EarlyMessages,
                MessageType.Miss => _loadedMessageSettings.MissMessages,
                MessageType.Failed => _loadedMessageSettings.FailedMessages,
                _ => new List<string>()
            };

            if (list.Count == 0) return type.ToString();
            return list[Random.Range(0, list.Count)];
        }

        private void RandomizeAllMessages()
        {
            _loadedMessageSettings.NormalMessages = Shuffle(_loadedMessageSettings.NormalMessages);
            _loadedMessageSettings.PerfectMessages = Shuffle(_loadedMessageSettings.PerfectMessages);
            _loadedMessageSettings.LateMessages = Shuffle(_loadedMessageSettings.LateMessages);
            _loadedMessageSettings.EarlyMessages = Shuffle(_loadedMessageSettings.EarlyMessages);
            _loadedMessageSettings.MissMessages = Shuffle(_loadedMessageSettings.MissMessages);
            _loadedMessageSettings.FailedMessages = Shuffle(_loadedMessageSettings.FailedMessages);
        }

        private List<T> Shuffle<T>(List<T> list)
        {
            if (list == null || list.Count <= 1) return list;
            var shuffled = new List<T>(list);
            for (int i = 0; i < shuffled.Count; i++)
            {
                int r = Random.Range(i, shuffled.Count);
                (shuffled[i], shuffled[r]) = (shuffled[r], shuffled[i]);
            }
            return shuffled;
        }

        #endregion

        #region DATA

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

        #endregion
    }
}