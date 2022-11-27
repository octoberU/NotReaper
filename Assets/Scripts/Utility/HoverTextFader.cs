using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace NotReaper.UI
{
    public class HoverTextFader : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [SerializeField] private TextMeshProUGUI text;

        private void Awake() => text.alpha = 0f;

        private void OnBecameInvisible()
        {
            text.alpha = 0f;
        }

        public void OnPointerEnter(PointerEventData eventData) => text.DOFade(1f, .25f);

        public void OnPointerExit(PointerEventData eventData) => text.DOFade(0f, .25f);
    }
}
