using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Scripts.Global
{
    public class AudioMixerManager : MonoBehaviour
    {
        public static AudioMixerManager Instance { get; private set; }
        
        [Header("Reference")] 
        [SerializeField] private AudioMixer _audioMixer;
        
        private const string MAIN_VOLUME = "MainVolume";
        private const string UI_VOLUME = "UiVolume";
        private const string MUSIC_VOLUME = "MusicVolume";
        
        private void Awake()
        {
            if (Instance && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSetValues();
        }

        private void Start()
        {
            LoadSetValues();
        }

        public float GetPercentValue(TypeVolume typeVolume)
        {
            float value = typeVolume switch
            {
                TypeVolume.Main => SPlayerPrefs.MainVolume,
                TypeVolume.Music => SPlayerPrefs.MusicVolume,
                TypeVolume.Ui => SPlayerPrefs.UiVolume,
                _ => 0
            };

            Debug.Log($"Value = {value}");
            return value;
        }

        public void SetPercentValue(float percent, TypeVolume type)
        {
            percent = Mathf.Clamp01(percent);
            float value = percent != 0 ? 20 - 60 * (1 - percent) : -80;
            switch (type)
            {
                case TypeVolume.Main:
                    SPlayerPrefs.MainVolume = percent;
                    _audioMixer.SetFloat(MAIN_VOLUME, value);
                    break;
                case TypeVolume.Music:
                    SPlayerPrefs.MusicVolume = percent;
                    _audioMixer.SetFloat(MUSIC_VOLUME, value);
                    break;
                case TypeVolume.Ui:
                    SPlayerPrefs.UiVolume = percent;
                    _audioMixer.SetFloat(UI_VOLUME, value);
                    break;
            }
            
            PlayerPrefs.Save();
        }

        private void LoadSetValues()
        {
            SetPercentValue(GetPercentValue(TypeVolume.Main), TypeVolume.Main);
            SetPercentValue(GetPercentValue(TypeVolume.Music), TypeVolume.Music);
            SetPercentValue(GetPercentValue(TypeVolume.Ui), TypeVolume.Ui);
        }
    }

    public enum TypeVolume
    {
        Main =0,
        Ui,
        Music,
    }
}
