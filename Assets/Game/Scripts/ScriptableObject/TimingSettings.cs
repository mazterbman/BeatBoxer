using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Scripts.ScriptableObject
{
    [CreateAssetMenu(fileName = "TimingSettings", menuName = "Scriptable Objects/TimingSettings")]
    public class TimingSettings : UnityEngine.ScriptableObject  
    {
        public AudioClip AudioClip;
        public List<TimingValue> TimingValues = new List<TimingValue>();

        public TimingSettings Clone()
        {
            var clone = Instantiate(this);
            return clone;
        }
    }

    [Serializable]
    public class TimingValue
    {
        public float TimeStart;
        public float TimeEnd;
        public ArrowType ArrowType;
        public ArrowDirection ArrowDirection;
    }

    public enum ArrowType
    {
        Click = 0,
        Hold
    }
    
    public enum ArrowDirection
    {
        Up = 0,
        Down,
        Left,
        Right
    }
}