using UnityEngine;

namespace Game.Scripts.Global
{
    public static class SPlayerPrefs
    {
        private const string TRACK_INDEX_SELECTED = "TrackSelected";
        public static int TrackIndexSelected
        {
            get => PlayerPrefs.GetInt(TRACK_INDEX_SELECTED, 0);
            set
            {
                PlayerPrefs.SetInt(TRACK_INDEX_SELECTED, value);
                PlayerPrefs.Save();
            }
        }
        
        private const string MAIN_VOLUME = "MainVolume";
        public static float MainVolume
        {
            get => PlayerPrefs.GetFloat(MAIN_VOLUME, 0.5f);
            set
            {
                PlayerPrefs.SetFloat(MAIN_VOLUME, value);
                PlayerPrefs.Save();
            }
        }
        
        private const string UI_VOLUME = "UiVolume";
        public static float UiVolume
        {
            get => PlayerPrefs.GetFloat(UI_VOLUME, 0.5f);
            set
            {
                PlayerPrefs.SetFloat(UI_VOLUME, value);
                PlayerPrefs.Save();
            }
        }
        
        private const string MUSIC_VOLUME = "MusicVolume";
        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MUSIC_VOLUME, 0.5f);
            set
            {
                PlayerPrefs.SetFloat(MUSIC_VOLUME, value);
                PlayerPrefs.Save();
            }
        }
        
        private const string LANGUAGE_INDEX = "LanguageIndex";
        public static int LanguageIndex
        {
            get => PlayerPrefs.GetInt(LANGUAGE_INDEX, 0);
            set
            {
                PlayerPrefs.SetInt(LANGUAGE_INDEX, value);
                PlayerPrefs.Save();
            }
        }

        public static void SavePlayerPref(string key, int value)
        {
            PlayerPrefs.SetInt(key, value);
            PlayerPrefs.Save();
        }

        public static void SavePlayerPref(string key, float value)
        {
            PlayerPrefs.SetFloat(key, value);
            PlayerPrefs.Save();
        }
    }
}
