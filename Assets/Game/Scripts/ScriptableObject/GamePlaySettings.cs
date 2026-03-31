using UnityEngine;

namespace Game.Scripts.ScriptableObject
{
    [CreateAssetMenu(fileName = "GamePlaySettings", menuName = "Scriptable Objects/GamePlaySettings")]
    public class GamePlaySettings : UnityEngine.ScriptableObject
    {
        [Header("Track Settings")]
        public TrackSettings TrackSettings;

        [Header("Prefs Settings")]
        [TextArea(1, 2)] public string ScorePref;
        [TextArea(1, 2)] public string RatingPref;
        [TextArea(1, 2)] public string ComboPref;
    }
}
