using System;
using System.Collections.Generic;
using Game.Scripts.Global;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Game.Scripts.UI.SettingsCanvas
{
    public class LanguageController : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private TMP_Text _languageText;

        [Header("Settings")] 
        [SerializeField] private TypeLanguage _selectedType;
        
        private int _selectedIndex = 0;
        private static readonly Dictionary<TypeLanguage, string> LanguageDic = new Dictionary<TypeLanguage, string>()
        {
            {TypeLanguage.English , "English"},
            {TypeLanguage.Russian, "Русский"}
        };
        private static readonly Dictionary<TypeLanguage, string> LanguageDicLoc = new Dictionary<TypeLanguage, string>()
        {
            {TypeLanguage.English , "en"},
            {TypeLanguage.Russian, "ru"}
        };
        
        private void Awake()
        {
            ChangeLanguage(SPlayerPrefs.LanguageIndex);
        }

        public void NextLanguage()
        {
            _selectedIndex++;
            if (_selectedIndex >= Enum.GetValues(typeof(TypeLanguage)).Length)
            {
                _selectedIndex = 0;
            }
            
            ChangeLanguage(_selectedIndex);
        }

        public void PreviousLanguage()
        {
            _selectedIndex--;
            if (_selectedIndex < 0)
            {
                _selectedIndex = Enum.GetValues(typeof(TypeLanguage)).Length -1;
            }
            
            ChangeLanguage(_selectedIndex);
        }

        private void ChangeLanguage(int index)
        {
            _selectedType = (TypeLanguage)index;
            SPlayerPrefs.LanguageIndex = index;
            
            if (LanguageDic.TryGetValue(_selectedType, out var value))
            {
                _languageText.text = value;
                Debug.Log($"Change Language on {_selectedType}");
            }

            if (LanguageDicLoc.TryGetValue(_selectedType, out var text))
            {
                SelectLocale(text);
            }
        }
        
        void SelectLocale(string localeCode)
        {
            Locale locale =
                LocalizationSettings.AvailableLocales.GetLocale(localeCode);

            LocalizationSettings.SelectedLocale = locale;
        }
    }

    public enum TypeLanguage
    {
        Russian = 0,
        English,
    }
}
