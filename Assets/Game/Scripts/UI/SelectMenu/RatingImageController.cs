using UnityEngine;
using UnityEngine.UI;

namespace Game.Scripts.UI.SelectMenu
{
    public class RatingImageController : MonoBehaviour
    {
        [Header("Reference")]
        [SerializeField] private Image _imageOn;
        [SerializeField] private Image _imageOff;

        public void On()
        {
            _imageOn.gameObject.SetActive(true);
            _imageOff.gameObject.SetActive(false);
        }

        public void Off()
        {
            _imageOn.gameObject.SetActive(false);
            _imageOff.gameObject.SetActive(true);
        }
    }
}
