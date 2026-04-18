using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts
{
    public class ExitGame : MonoBehaviour
    {
        public void OnExitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
        } 
    }
}
