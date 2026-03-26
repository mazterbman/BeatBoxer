using System;
using UnityEngine;

namespace Game.Scripts.ScriptableObject
{
    [CreateAssetMenu(fileName = "ShakeSettings", menuName = "Scriptable Objects/ShakeSettings")]
    public class ShakeSettings : UnityEngine.ScriptableObject
    {
        public Transform ShakeTransform;
        public Vector3 EndScale;
        public float TimeShake;
        public AnimationCurve ShakeCurve;

        [NonSerialized] public Vector3 NormalScale;

        public void SetNormalScale()
        {
            if (!ShakeTransform) return;

            NormalScale = ShakeTransform.localScale;
        }

        public float GetEvaluate(float time)
        {
            return ShakeCurve.Evaluate(time / TimeShake);
        }
        
        public ShakeSettings Clone()
        {
            var clone = Instantiate(this);
            return clone;
        }
    }
}
