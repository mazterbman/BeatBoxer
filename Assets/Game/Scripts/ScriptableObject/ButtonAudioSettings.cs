using UnityEngine;

namespace Game.Scripts.ScriptableObject
{
    [CreateAssetMenu(fileName = "ButtonAudioSettings", menuName = "Scriptable Objects/ButtonAudioSettings")]
    public class ButtonAudioSettings : UnityEngine.ScriptableObject
    {
        public AudioClip ClkClip;
        public AudioClip HoverClip;
    }
}
