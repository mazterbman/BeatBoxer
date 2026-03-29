using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Game.Scripts.UI.SettingsCanvas
{
    public class LanguageController : MonoBehaviour
    {
        [Header("Reference")] 
        [SerializeField] private TMP_Text _languageText;

        [Header("Settings")] 
        [SerializeField] private TypeLanguage _selectedType;
        
        private int _selectedIndex = 0;
        private static readonly string NamePref = "LanguageIndex";
        private static readonly Dictionary<TypeLanguage, string> LanguageDic = new Dictionary<TypeLanguage, string>()
        {
            {TypeLanguage.English , "English"},
            {TypeLanguage.Russian, "Русский"}
        };
        
        private void Awake()
        {
            ChangeLanguage(PlayerPrefs.GetInt(NamePref, 0));
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
            PlayerPrefs.SetInt(NamePref, index);
            if (LanguageDic.TryGetValue(_selectedType, out var value))
            {
                _languageText.text = value;
                Debug.Log($"Change Language on {_selectedType}");
            }
        }
    }

    public enum TypeLanguage
    {
        Russian = 0,
        English,
    }
}
