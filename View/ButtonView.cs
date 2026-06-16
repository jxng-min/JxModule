using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace JxModule
{
    [RequireComponent(typeof(Button))]
    public class ButtonView : ImageView
    {
        [SerializeField] private TMP_Text label;
        private Button _button;
        
        public Button Button => _button ??= GetComponent<Button>();
        public TMP_Text Label => label ??= GetComponent<TMP_Text>();

        public void AddListener(UnityAction listener)
        {
            _button.onClick.AddListener(listener);
        }

        public void RemoveListener(UnityAction listener)
        {
            _button.onClick.RemoveListener(listener);
        }

        public void RemoveAllListeners()
        {
            _button.onClick.RemoveAllListeners();
        }
    }
}