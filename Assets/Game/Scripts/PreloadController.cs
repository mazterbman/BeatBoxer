using System;
using System.Collections;
using Game.Scripts.UI;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Game.Scripts
{
    public class PreloadController : MonoBehaviour
    {
        private IEnumerator Start()
        {
            yield return LocalizationSettings.InitializationOperation;
            LoadingManager.Instance.LoadSceneAsync(1);
        }
    }
}
