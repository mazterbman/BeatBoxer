using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.ScriptableObject
{
    [CreateAssetMenu(fileName = "MessageSettings", menuName = "Scriptable Objects/MessageSettings")]
    public class MessageSettings : UnityEngine.ScriptableObject
    {
        public List<string> NormalMessages = new List<string>();
        public List<string> PerfectMessages = new List<string>();
        public List<string> LateMessages = new List<string>();
        public List<string> EarlyMessages = new List<string>();
        public List<string> MissMessages = new List<string>();
        public List<string> FailedMessages = new List<string>();
        
        public MessageSettings Clone()
        {
            var clone = Instantiate(this);
            return clone;
        }
    }
}
