using System;
using System.Collections;
using System.Collections.Generic;
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
        #region FIELDS

        [Header("References")]
        [Tooltip("Audio source playing the song.")]
        [SerializeField] private AudioSource _audioSource;
        [Tooltip("Rating UI controller.")]
        [SerializeField] private RatingController _ratingController;
        [Tooltip("Health UI controller.")]
        [SerializeField] private HealthController _healthController;
        [Tooltip("Score UI controller.")]
        [SerializeField] private ScoreUiController _scoreUiController;
        [Tooltip("Combo UI controller.")]
        [SerializeField] private ComboUiController _comboUiController;
        [Tooltip("Center animation controller.")]
        [SerializeField] private CenterUiController _centerUiController;
        
        [Header("Input")]
        [Tooltip("PlayerInput component for Input System actions.")]
        [SerializeField] private PlayerInput _playerInput;

        [Header("Settings")]
        [Tooltip("Message settings ScriptableObject.")]
        [SerializeField] private MessageSettings _messageSettings;
        [Tooltip("Timing settings ScriptableObject.")]
        [SerializeField] private TimingSettings _timingSettings;
        [Tooltip("Pool settings for arrows.")]
        [SerializeField] private PoolSettings _arrowPoolSettings;
        [Tooltip("Pool settings for messages.")]
        [SerializeField] private PoolSettings _messagePoolSettings;

        [Header("Timing Windows")]
        [Tooltip("Time window for perfect hit (±).")]
        [SerializeField] [Range(0, 0.5f)] private float _perfectWindow = 0.05f;
        [Tooltip("Time window for normal hit (±).")]
        [SerializeField] [Range(0, 0.5f)] private float _normalWindow = 0.1f;

        [Header("Movement")]
        [Tooltip("Time for arrow to travel from edge to center.")]
        [SerializeField] [Range(0.5f, 10f)] private float _timeToCenter = 5f;

        private Coroutine _gameCoroutine;
        private List<ArrowController> _activeArrows = new List<ArrowController>();
        private ObjectPool<ArrowController> _arrowPool;
        private ObjectPool<MessageController> _messagePool;

        // Cloned settings to avoid modifying original assets
        private MessageSettings _loadedMessageSettings;
        private List<TimingValue> _adjustedTimingValues; // copies with shifted times

        private bool _isGameActive;
        private bool _inputActionsAdded;

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
            if (!_timingSettings || !_messageSettings)
            {
                Debug.LogError($"<color=red>[GamePlayController] [{gameObject.name}]</color> Missing timing or message settings!");
                return;
            }

            _loadedMessageSettings = _messageSettings.Clone();
            RandomizeAllMessages();

            // Adjust timing values by subtracting travel time so they align with arrow spawn
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

            // Create object pools
            CreatePools();

            _healthController.OnChanged += CheckEnd;
            
            if (!_playerInput)
                _playerInput = GetComponent<PlayerInput>();

            if (!_playerInput)
            {
                Debug.LogError($"<color=red>[GamePlayController] [{gameObject.name}]</color> PlayerInput component missing!");
                return;
            }

            AddInputActions();
            StartGame();
        }

        private void OnDestroy()
        {
            _healthController.OnChanged -= CheckEnd;
            if (_gameCoroutine != null) StopCoroutine(_gameCoroutine);
            RemoveInputActions();
        }

        #endregion

        #region Public API

        public void StartGame()
        {
            if (_gameCoroutine != null) StopCoroutine(_gameCoroutine);
            _isGameActive = true;

            // Reset UI
            _healthController.ResetHealth();
            _comboUiController.ResetCombo();
            _scoreUiController.ResetScore();
            _ratingController.SetRating(1);

            // Clear any existing arrows
            foreach (var arrow in _activeArrows)
                arrow.Hide();
            _activeArrows.Clear();

            _gameCoroutine = StartCoroutine(GameCoroutine());
        }

        public void EndGame()
        {
            if (!_isGameActive) return;
            _isGameActive = false;

            if (_gameCoroutine != null)
                StopCoroutine(_gameCoroutine);

            _audioSource.Stop();
            Debug.Log($"<color=yellow>[GamePlayController] [{gameObject.name}]</color> Game ended.");
        }

        #endregion

        #region Private API (Game Logic)

        private IEnumerator GameCoroutine()
        {
            _audioSource.clip = _timingSettings.AudioClip;
            _audioSource.Play();

            Debug.Log($"<color=white>[GamePlayController] [{gameObject.name}]</color> Start game, duration: {_audioSource.clip.length}");

            yield return new WaitUntil(() => _audioSource.isPlaying);

            int nextIndex = 0;
            float endTime = _adjustedTimingValues.Count > 0 ? _adjustedTimingValues[_adjustedTimingValues.Count - 1].TimeStart : 0f;

            while (_audioSource.time < endTime && _isGameActive)
            {
                // Spawn arrows when their time comes
                if (nextIndex < _adjustedTimingValues.Count &&
                    _adjustedTimingValues[nextIndex].TimeStart <= _audioSource.time)
                {
                    SpawnArrow(_adjustedTimingValues[nextIndex]);
                    nextIndex++;
                }
                yield return null;
            }

            // Spawn any remaining arrows if audio ended early (just in case)
            while (nextIndex < _adjustedTimingValues.Count && _isGameActive)
            {
                SpawnArrow(_adjustedTimingValues[nextIndex]);
                nextIndex++;
                yield return null;
            }

            // Wait for all arrows to finish moving or be hit
            while (_activeArrows.Count > 0 && _isGameActive)
                yield return null;

            EndGame();
        }

        private void SpawnArrow(TimingValue timing)
        {
            // Calculate ideal time in audio (when arrow reaches center)
            float idealTime = timing.TimeStart + _timeToCenter;

            ArrowController arrow = _arrowPool.Get();
            arrow.Show(_arrowPool, timing.ArrowType, timing.ArrowDirection,
                _timeToCenter, idealTime, timing.TimeStart, timing.TimeEnd);

            _activeArrows.Add(arrow);

            Debug.Log($"<color=white>[GamePlayController] [{gameObject.name}]</color> Spawned {timing.ArrowType} {timing.ArrowDirection} at audio time {_audioSource.time:F2}, ideal {idealTime:F2}");
        }

        private void OnArrowPressed(ArrowDirection direction)
        {
            // Find all arrows of this direction that are not held yet
            List<ArrowController> candidates = _activeArrows.FindAll(a => a.ArrowDirection == direction);

            foreach (var arrow in candidates)
            {
                if (arrow.ArrowType == ArrowType.Click)
                {
                    // Evaluate click accuracy
                    float timeDiff = _audioSource.time - arrow.IdealTime;
                    float absDiff = Mathf.Abs(timeDiff);

                    MessageType result;
                    if (absDiff <= _perfectWindow)
                        result = MessageType.Perfect;
                    else if (absDiff <= _normalWindow)
                        result = MessageType.Normal;
                    else if (timeDiff < 0)
                        result = MessageType.Early;
                    else
                        result = MessageType.Late;

                    ProcessHit(arrow, result);
                    break; // One hit per press
                }
                else // Hold
                {
                    // Check if press is within hold start window
                    float time = _audioSource.time;
                    if (time >= arrow.HoldStartTime && time <= arrow.HoldEndTime)
                    {
                        arrow.OnHoldPress();
                        // We'll evaluate at release time
                    }
                    // else – press too early or too late, will be handled by release or timeout
                }
            }
        }

        private void OnArrowReleased(ArrowDirection direction)
        {
            List<ArrowController> candidates = _activeArrows.FindAll(a => a.ArrowDirection == direction && a.ArrowType == ArrowType.Hold);

            foreach (var arrow in candidates)
            {
                if (arrow.IsHeld)
                {
                    float releaseTime = _audioSource.time;
                    bool success = releaseTime >= arrow.HoldStartTime && releaseTime <= arrow.HoldEndTime;

                    if (success)
                    {
                        // Perfect hold – no extra timing, just success
                        ProcessHit(arrow, MessageType.Perfect);
                    }
                    else
                    {
                        // Miss because released too early or too late
                        ProcessHit(arrow, MessageType.Miss);
                    }
                    arrow.OnHoldRelease();
                    break;
                }
            }
        }

        private void ProcessHit(ArrowController arrow, MessageType result)
        {
            // Remove from active list and hide arrow
            _activeArrows.Remove(arrow);
            arrow.Hide();

            // Spawn message
            SpawnMessage(result);

            // Update game systems
            UpdateSystem(result);

            // Shake UI elements for feedback
            _scoreUiController.Shake();
            _comboUiController.Shake();
            _ratingController.ShakeAll();

            // Animate center
            _centerUiController.Clk();

            Debug.Log($"<color=green>[GamePlayController] [{gameObject.name}]</color> Hit {arrow.ArrowType} {arrow.ArrowDirection} with {result}");
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

        #endregion
        
        #region Private API (Input System)

        /// <summary>Subscribes to input actions.</summary>
        private void AddInputActions()
        {
            if (_inputActionsAdded) return;

            var arrowAction = _playerInput.actions["Arrow"];
            if (arrowAction == null)
            {
                Debug.LogError($"<color=red>[GamePlayController] [{gameObject.name}]</color> Action 'Arrow' not found in Input Actions!");
                return;
            }

            arrowAction.started += OnArrowStarted;
            arrowAction.canceled += OnArrowCanceled;
            _inputActionsAdded = true;

            Debug.Log($"<color=green>[GamePlayController] [{gameObject.name}]</color> Input actions registered.");
        }

        /// <summary>Unsubscribes from input actions.</summary>
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

            Debug.Log($"<color=yellow>[GamePlayController] [{gameObject.name}]</color> Input actions unregistered.");
        }

        /// <summary>Handles arrow key press (started).</summary>
        private void OnArrowStarted(InputAction.CallbackContext context)
        {
            if (!_isGameActive) return;

            Vector2 inputVector = context.ReadValue<Vector2>();
            ArrowDirection direction = GetArrowDirectionFromInput(inputVector);
            OnArrowPressed(direction);
        }

        /// <summary>Handles arrow key release (canceled).</summary>
        private void OnArrowCanceled(InputAction.CallbackContext context)
        {
            if (!_isGameActive) return;

            Vector2 inputVector = context.ReadValue<Vector2>();
            ArrowDirection direction = GetArrowDirectionFromInput(inputVector);
            OnArrowReleased(direction);
        }

        /// <summary>Converts Input System Vector2 to ArrowDirection.</summary>
        private ArrowDirection GetArrowDirectionFromInput(Vector2 input)
        {
            // Input System Vector2: (x, y) where:
            // - x: -1 = Left, 1 = Right
            // - y: -1 = Down, 1 = Up
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                return input.x > 0 ? ArrowDirection.Right : ArrowDirection.Left;
            else
                return input.y > 0 ? ArrowDirection.Up : ArrowDirection.Down;
        }

        #endregion

        #region Message & Arrow Pool Management

        private void CreatePools()
        {
            _arrowPool = new ObjectPool<ArrowController>(
                createFunc: () =>
                {
                    GameObject go = Instantiate(_arrowPoolSettings.Prefab, _arrowPoolSettings.Parent ? _arrowPoolSettings.Parent : transform);
                    go.SetActive(false);
                    return go.GetComponent<ArrowController>();
                },
                actionOnGet: (arrow) =>
                {
                    arrow.gameObject.SetActive(true);
                    // Do not add to list here, will be added when spawned
                },
                actionOnRelease: (arrow) =>
                {
                    arrow.gameObject.SetActive(false);
                },
                actionOnDestroy: (arrow) => Destroy(arrow.gameObject),
                collectionCheck: _arrowPoolSettings.CollectionCheck,
                defaultCapacity: _arrowPoolSettings.DefaultCapacity,
                maxSize: _arrowPoolSettings.MaxSize
            );

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
            // Shuffle each message list
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