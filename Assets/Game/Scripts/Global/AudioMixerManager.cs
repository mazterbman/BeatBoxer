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

        private const string MainVolume = "MainVolume";
        private const string UIVolume = "UiVolume";
        private const string MusicVolume = "MusicVolume";
        private const string PrefAdd = "Pref";
        
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
                TypeVolume.Main => PlayerPrefs.GetFloat(MainVolume + PrefAdd, 0),
                TypeVolume.Music => PlayerPrefs.GetFloat(MusicVolume + PrefAdd, 0),
                TypeVolume.Ui => PlayerPrefs.GetFloat(UIVolume + PrefAdd, 0),
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
                    PlayerPrefs.SetFloat(MainVolume + PrefAdd ,percent);
                    _audioMixer.SetFloat(MainVolume, value);
                    break;
                case TypeVolume.Music:
                    PlayerPrefs.SetFloat(MusicVolume + PrefAdd, percent);
                    _audioMixer.SetFloat(MusicVolume, value);
                    break;
                case TypeVolume.Ui:
                    PlayerPrefs.SetFloat(UIVolume + PrefAdd, percent);
                    _audioMixer.SetFloat(UIVolume, value);
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
