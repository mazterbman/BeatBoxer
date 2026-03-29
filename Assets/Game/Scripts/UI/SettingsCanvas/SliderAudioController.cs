using Game.Scripts.Global;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.SettingsCanvas
{
    public class SliderAudioController : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private Slider _slider;
        
        [Header("Settings")]
        [SerializeField] private TypeVolume _typeVolume;
        
        private AudioMixerManager _audioMixerManager;
        private float _valueNow = 0;

        private void Start()
        {
            _audioMixerManager = AudioMixerManager.Instance;

            _valueNow = _audioMixerManager.GetPercentValue(_typeVolume);
            
            _slider.minValue = 0;
            _slider.maxValue = 100;
            _slider.wholeNumbers = true;
            _slider.SetValueWithoutNotify(_valueNow * 100);
            _slider.onValueChanged.AddListener(SetVolume);
        }

        private void OnDestroy()
        {
            _slider.onValueChanged.RemoveListener(SetVolume);
        }

        private void SetVolume(float value)
        {
            _audioMixerManager.SetPercentValue(value / 100.0f, _typeVolume);
        }
    }
}
