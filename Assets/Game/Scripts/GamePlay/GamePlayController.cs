using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Global;
using Game.Scripts.ScriptableObject;
using Game.Scripts.UI.Center;
using Game.Scripts.UI.Combo;
using Game.Scripts.UI.Health;
using Game.Scripts.UI.Rating;
using Game.Scripts.UI.Score;
using Game.Scripts.UI.SettingsCanvas;
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
        [SerializeField] private CanvasSettingsController _canvasSettingsController;
        [SerializeField] private MenuController _menuController;
        [SerializeField] private AudioVisualManager _visualManager;

        [Header("Settings")]
        [SerializeField] private MessageSettings _messageSettings;
        [SerializeField] private PoolSettings _messagePoolSettings;
        [SerializeField] private List<GamePlaySettings> _gamePlaySettingsList;

        [Header("Timing Windows")]
        [SerializeField] [Range(0, 0.5f)] private float _perfectWindow = 0.05f;
        [SerializeField] [Range(0, 0.5f)] private float _normalWindow = 0.1f;
        [SerializeField] [Range(0, 0.5f)] private float _saveTime = 0.1f;

        [Header("Movement")]
        [SerializeField] [Range(0.5f, 10f)] private float _timeToCenter = 5f;
        
        [Header("Arrow Prefab")]
        [SerializeField] private ArrowController _arrowPrefab;
        [SerializeField] private Transform _arrowsParent;

        [Header("Canvas Reference")] 
        [SerializeField] private CanvasGoodEndingController _goodEndCanvas;
        [SerializeField] private GameObject _badEndCanvas;

        [Header("Debug")] 
        [SerializeField] private bool _godMode = false;

        private Coroutine _goodEndCoroutine;
        private Coroutine _gameCoroutine;
        private List<ArrowController> _activeArrows = new List<ArrowController>();
        private List<ArrowController> _inActiveArrows = new List<ArrowController>();
        private ObjectPool<MessageController> _messagePool;

        private GamePlaySettings _gamePlaySettings;
        private MessageSettings _loadedMessageSettings;
        private List<TimingValue> _adjustedTimingValues;
        private List<TimingValue> _originalTimingValues;

        private bool _isPauseGame;
        private bool _isGameActive;
        private bool _wasInteract = false;
        private bool _inputActionsAdded;

        private int _countArrowsSelected = 0;
        private float _startDelay;

        private static readonly int _normalBonus = 20;
        private static readonly int _perfectBonus = 100;

        private void Start()
        {
            _gamePlaySettings = _gamePlaySettingsList[SPlayerPrefs.TrackIndexSelected];
            
            if (!_gamePlaySettings || !_messageSettings || !_arrowPrefab)
            {
                Debug.LogError($"[GamePlayController] Missing timing, message, or arrow prefab!");
                return;
            }
            
            _loadedMessageSettings = _messageSettings.Clone();
            RandomizeAllMessages();

            // 1. Считаем смещенные тайминги (они могут стать отрицательными)
            _adjustedTimingValues = new List<TimingValue>();
            foreach (var tv in _gamePlaySettings.TrackSettings.TimingValues)
            {
                var copy = new TimingValue
                {
                    TimeStart = tv.TimeStart - _timeToCenter,
                    TimeEnd = tv.TimeEnd > 0 ? tv.TimeEnd - _timeToCenter : 0f,
                    ArrowType = tv.ArrowType,
                    ArrowDirection = tv.ArrowDirection
                };
                _adjustedTimingValues.Add(copy);
            }

            _originalTimingValues = new List<TimingValue>(_gamePlaySettings.TrackSettings.TimingValues);
            
            float minStartTime = _adjustedTimingValues.Min(tv => tv.TimeStart);
            float startDelay = Mathf.Max(0f, -minStartTime); 
            
            _startDelay = startDelay; 
            
            CreateMessagePool();

            _healthController.OnChanged += CheckEnd;

            if (!_playerInput)
                _playerInput = GetComponent<PlayerInput>();

            _badEndCanvas.SetActive(false);
            _goodEndCanvas.gameObject.SetActive(false);
            
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
            _scoreUiController.SetMaxScore(_adjustedTimingValues.Count * _perfectBonus);
            _scoreUiController.ResetScore();
            _ratingController.SetRating(1);

            // Удаляем все существующие стрелки
            foreach (var arrow in _activeArrows)
                Destroy(arrow.gameObject);
            _activeArrows.Clear();

            _gameCoroutine = StartCoroutine(GameCoroutine());
        }

        private void GoodEndGame()
        {
            if (!_isGameActive) return;
            _isGameActive = false;
            
            EndGame();
            
            _badEndCanvas.SetActive(false);
            _goodEndCanvas.gameObject.SetActive(true);
            
            GoodEndingInformation info = new GoodEndingInformation
            {
                Combo = _comboUiController.MaxCombo,
                CountRating = _ratingController.Rating,
                Score = _scoreUiController.Score,
                Percent = _scoreUiController.PercentOfMax,
                CountArrowsSelect = _countArrowsSelected
            };
            SavePrefs(info);
            _goodEndCanvas.Show(info);
        }

        private void SavePrefs(GoodEndingInformation info)
        {
            SPlayerPrefs.SavePlayerPref(_gamePlaySettings.ComboPref, info.Combo);
            SPlayerPrefs.SavePlayerPref(_gamePlaySettings.RatingPref, info.CountRating);
            SPlayerPrefs.SavePlayerPref(_gamePlaySettings.ScorePref, info.Score);
        }

        private void BadEndGame()
        {
            if (!_isGameActive) return;
            _isGameActive = false;
            
            EndGame();
            
            _badEndCanvas.SetActive(true);
            _goodEndCanvas.gameObject.SetActive(false);
        }
        
        private void EndGame()
        {
            _canvasSettingsController.CanOpenMenu = false;
            _menuController.CanOpenMenu = false;
            
            if (_gameCoroutine != null)
                StopCoroutine(_gameCoroutine);

            _activeArrows.ForEach(arg1=>arg1.StopMove());
            _inActiveArrows.ForEach(arg1 => arg1.StopMove());
            
            _audioSource.Stop();
            _wasInteract = false;
            Debug.Log($"[GamePlayController] Game ended.");
        }

        public void PauseGame()
        {
            if (_isPauseGame) return;
            
            _isPauseGame = true;
            _audioSource.Pause();
            
            _activeArrows.ForEach(arg1 => arg1.IsPaused = true);
            _inActiveArrows.ForEach(arg1 => arg1.IsPaused = true);
        }

        public void UnPauseGame()
        {
            if (!_isPauseGame) return;
            
            _isPauseGame = false;
            _audioSource.Play();
            
            _activeArrows.ForEach(arg1 => arg1.IsPaused = false);
            _inActiveArrows.ForEach(arg1 => arg1.IsPaused = false);
        }
        
        public void ProcessHit(ArrowController arrow, MessageType result)
        {
            if (!arrow || !_activeArrows.Contains(arrow)) 
                return;
            
            _activeArrows.Remove(arrow);
            _inActiveArrows.Add(arrow);
            
            arrow.Hide();
            _countArrowsSelected++;
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
            if (_startDelay > 0)
            {
                Debug.Log($"[GamePlayController] Startup delay: {_startDelay:F2}s to accommodate early arrows");
                yield return new WaitForSeconds(_startDelay);
            }
            
            _audioSource.clip = _gamePlaySettings.TrackSettings.AudioClip;
            _audioSource.Play();

            yield return new WaitUntil(() => _audioSource.isPlaying);

            int nextIndex = 0;
            float endTime = _adjustedTimingValues.Count > 0 ? _adjustedTimingValues.Last().TimeStart : 0f;

            while (_audioSource.time < endTime && _isGameActive)
            {
                if (nextIndex < _adjustedTimingValues.Count &&
                    _adjustedTimingValues[nextIndex].TimeStart <= _audioSource.time)
                {
                    SpawnArrow(_adjustedTimingValues[nextIndex], _originalTimingValues[nextIndex], nextIndex);
                    nextIndex++;
                }
                
                _scoreUiController.SetMaxScore(nextIndex * _perfectBonus);
                yield return null;
            }

            while (nextIndex < _adjustedTimingValues.Count && _isGameActive)
            {
                SpawnArrow(_adjustedTimingValues[nextIndex], _originalTimingValues[nextIndex], nextIndex);
                nextIndex++;
                yield return null;
            }

            while (_isGameActive && (_activeArrows.Count > 0 || _inActiveArrows.Count > 0))
                yield return null;

            GoodEndGame();
        }

        // Спавн стрелки без пула
        private void SpawnArrow(TimingValue adjustedTiming, TimingValue originalTiming, int index)
        {
            // idealTime – момент достижения центра в аудиовремени (оригинальное время)
            float idealTime = originalTiming.TimeStart + _timeToCenter;
    
            ArrowController arrow = Instantiate(_arrowPrefab,
                _arrowsParent ? _arrowsParent : transform);
    
            _activeArrows.Add(arrow);
            // Используем adjustedTiming для времени старта и конца удержания
            arrow.Show(this, originalTiming.ArrowType,
                originalTiming.ArrowDirection, _timeToCenter, idealTime, 
                adjustedTiming.TimeStart,
                adjustedTiming.TimeEnd,
                _saveTime, index);
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
            
            MessageType result;
            if (arrow.RemainingTime <= _perfectWindow + _saveTime)
                result = MessageType.Perfect;
            else if (arrow.RemainingTime <= _normalWindow + _saveTime)
                result = MessageType.Normal;
            else
                result = MessageType.Late;    // нажали позже центра

            if (result != MessageType.Late && arrow.ArrowType == ArrowType.Hold)
            {
                arrow.OnHoldPress();
                return;
            }
                
            ProcessHit(arrow, result);
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
                MessageType result;
                if (arrow.HoldTimeLeft <= _perfectWindow + _saveTime)
                    result = MessageType.Perfect;
                else result = MessageType.Normal;
                
                ProcessHit(arrow, result);
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
            _visualManager.Shake();
            
            ArrowController controller = _inActiveArrows.Last();
            if (result is MessageType.Perfect or MessageType.Normal)
            {
                _centerUiController.Clk();
                _visualManager.Visual(controller.ColorFromDirection);
            }
            else
            {
                _visualManager.Visual(controller.InActiveColor);
            }
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
                    _scoreUiController.AddScore(_normalBonus * -1);
                    break;
                case MessageType.Perfect:
                    _healthController.Add();
                    _comboUiController.AddCombo();
                    _scoreUiController.AddScore(_perfectBonus);
                    break;
                case MessageType.Normal:
                    _healthController.Add();
                    _comboUiController.AddCombo();
                    _scoreUiController.AddScore(_normalBonus);
                    break;
            }
            
            _ratingController.SetRating(_scoreUiController.PercentOfMax);
        }

        private void CheckEnd(int health)
        {
            if (health <= 0)
                BadEndGame();
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
            if (!_inputActionsAdded || !_playerInput) return;

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