using DG.Tweening;
using NotReaper;
using NotReaper.Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NotReaper.UI.Timing
{
    public class TimingPointsPanel : NRMenu, IPointerEnterHandler, IPointerExitHandler
    {
        public bool isActive;
        [NRInject] private Timeline timeline;
        public TimingPointItem timingPointItem;
        public Transform scrollContent;
        public List<TimingPointItem> items = new List<TimingPointItem>();
        public bool isHovering;
        private CanvasGroup canvas;
        protected override void Awake()
        {
            base.Awake();
            GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            canvas = GetComponent<CanvasGroup>();
            canvas.alpha = 0f;
            gameObject.SetActive(false);
        }

        public override void Show()
        {
            OnActivated();
            transform.position = new Vector3(0f, 0f, 0f);
            canvas.DOFade(1f, .3f);
            isActive = true;
            isHovering = false;
            UpdateTimingPointList(EditorTempo.TempoChanges);
        }

        public override void Hide()
        {
            isActive = false;
            isHovering = false;

            canvas.DOFade(0f, .3f).OnComplete(() =>
            {
                ClearTimingItems();
                OnDeactivated();
            });
        }

        public override void ShowHelp()
        {
            NRHelp.Instance.ShowTiming();
        }

        public void Toggle()
        {
            if (isActive) Hide();
            else Show();
        }

        public void UpdateTimingPointList(List<TempoChange> timingPoints)
        {
            ClearTimingItems();
            for (int i = 0; i < timingPoints.Count; i++)
            {
                var item = GameObject.Instantiate(timingPointItem.gameObject, scrollContent).GetComponent<TimingPointItem>();
                item.SetInfoFromData(timingPoints[i]);
                item.transform.localPosition = new Vector3(191.3f, -32.1f * (float)i, 0f);
                item.gameObject.SetActive(true);
                items.Add(item);
            }
        }

        public void ClearTimingItems()
        {
            foreach (var item in items)
            {
                Destroy(item.gameObject);
            }
            items.Clear();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovering = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovering = false;
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
    }
}
