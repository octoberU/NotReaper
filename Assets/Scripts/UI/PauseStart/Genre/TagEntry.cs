using NotReaper.UI.Components;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace NotReaper.Genres
{
    public class TagEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI tagText;
        private string _tag = "";
        public string Tag
        {
            get { return _tag; }
            set
            {
                SetTag(value);
            }
        }

        public bool IsSet => !string.IsNullOrEmpty(_tag);

        public void SetTag(string tag)
        {
            _tag = tag;
            tagText.text = _tag.ToLower();
            gameObject.SetActive(true);
        }

        public void RemoveTag()
        {
            _tag = "";
            gameObject.SetActive(false);
        }
    }
}
