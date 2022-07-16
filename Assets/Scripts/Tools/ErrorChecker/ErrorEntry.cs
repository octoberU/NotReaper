using System.Collections;
using System.Collections.Generic;
using NotReaper.UI;
using System;
using UnityEngine;

namespace NotReaper.Tools.ErrorChecker
{
    public class ErrorEntry : ListEntry
    {
        public event Action<int> onSelected;
        [SerializeField] public RectTransform rect;
        public override void SelectEntry()
        {
            base.SelectEntry();
            onSelected?.Invoke(Index);
        }

        public void FixItForMe()
        {
            var error = data as ErrorData;
            error.FixError();
        }
    }
    
}
