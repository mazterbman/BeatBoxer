using System;
using UnityEngine;

namespace Game.Scripts.UI.Center
{
    public class CenterUiController : MonoBehaviour
    {
        private static readonly int ClkID = Animator.StringToHash("Clk"); 
        
        [Header("Reference")] [SerializeField] 
        private Animator _animator;

        public void Clk()
        {
            _animator.SetTrigger(ClkID);
        }
    }
}
