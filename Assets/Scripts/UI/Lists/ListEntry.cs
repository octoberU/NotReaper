using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI
{
    public class ListEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI indexDisplay;
        [SerializeField] private TextMeshProUGUI tickDisplay;
        [SerializeField] private Image outline;
        
        public int StartTick { get; private set; }
        
        private int index;
        public int Index
        {
            get => index;
            set => SetIndex(value);
        }

        private bool selected;
        public bool Selected
        {
            get => selected;
            set => SelectEntry(value);
        }

        protected ListData data;

        private void SetIndex(int value)
        {
            index = value;
            indexDisplay.text = (index + 1).ToString();
            transform.SetSiblingIndex(value);
        }

        public void SelectEntry(bool select)
        {
            selected = select;
            outline.color = select ? Color.green : Color.white;
        }

        public virtual void SelectEntry() => SelectEntry(true);

        public virtual void SetData(ListData data)
        {
            this.data = data;
            UpdateEntry();
        }

        public virtual void UpdateEntry()
        {
            StartTick = data.tick;
            tickDisplay.text = StartTick.ToString();
        }
    }
}
