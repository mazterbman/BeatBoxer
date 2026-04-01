using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.SelectMenu
{
    public class RatingImageController : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private Image _imageOn;
        [SerializeField] private Image _imageOff;

        [Space] 
        [SerializeField] private ParticleSystem _particleSystem;

        public void On()
        {
            _imageOn.gameObject.SetActive(true);
            _imageOff.gameObject.SetActive(false);

            if (_particleSystem)
            {
                _particleSystem.gameObject.SetActive(true);
                _particleSystem.Play();
            }
        }

        public void Off()
        {
            _imageOn.gameObject.SetActive(false);
            _imageOff.gameObject.SetActive(true);
            
            if (_particleSystem)
            {
                _particleSystem.gameObject.SetActive(false);
                _particleSystem.Stop();
            }
        }
    }
}
