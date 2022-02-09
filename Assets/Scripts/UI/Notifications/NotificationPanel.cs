using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.Events;
using NotReaper.UserInput;
using UnityEngine.EventSystems;

namespace NotReaper.Notifications
{
    [RequireComponent(typeof(CanvasGroup))]
    public class NotificationPanel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IDeselectHandler
    {
        public static bool IsOpen = false;

        public static UnityEvent OnPanelOpened = new UnityEvent();

        [Header("References")]
        [SerializeField] private RectTransform parent;
        [SerializeField] private Puller puller;
        [SerializeField] private GameObject inputCatcher;
        [Space, Header("Slide Settings")]
        [SerializeField] private float slideDistance = 5f;
        [SerializeField] private float slideDuration = .5f;
        [SerializeField] private Ease openEasing;
        [SerializeField] private Ease closeEasing;
        [Space, Header("Badge Settings")]
        [SerializeField] private float animationDuration = 1f;

        private CanvasGroup canvas;
        private Vector3 startPos = new Vector3(512f, -38f, 0f);
        private bool isMouseOverPanel = false;

        private void Awake()
        {
            canvas = GetComponent<CanvasGroup>();
            parent.anchoredPosition = startPos;
            inputCatcher.gameObject.SetActive(false);
        }

        public void ToggleShow()
        {         
            IsOpen = !IsOpen;
            if (IsOpen)
            {
                EditorInput.disableInputWhenActive.Add(gameObject);
                inputCatcher.gameObject.SetActive(true);
                EventSystem.current.SetSelectedGameObject(gameObject);
            }
            else
            {
                EditorInput.disableInputWhenActive.Remove(gameObject);
                inputCatcher.gameObject.SetActive(false);
            }

            float distance = IsOpen ? -slideDistance : slideDistance;
            Ease easing = IsOpen ? openEasing : closeEasing;
            Transform pullerTransform = puller.GetPullerIconTransform();
            var sequence = DOTween.Sequence();
            sequence.OnComplete(() => OnAnimationComplete());
            sequence.Append(transform.DOMove(new Vector3(transform.position.x + distance, transform.position.y, transform.position.z), slideDuration).SetEase(easing));
            sequence.Join(puller.transform.DOMoveX(puller.transform.position.x + distance, slideDuration).SetEase(easing));
            sequence.Join(canvas.DOFade(IsOpen ? 1f : 0f, slideDuration).SetEase(easing));
            if (IsOpen)
            {
                sequence.Join(pullerTransform.transform.DORotate(new Vector3(0f, 0f, 180f), .2f, RotateMode.LocalAxisAdd).SetDelay(slideDuration * .5f));
                sequence.Join(puller.GetBadge().DOFade(0f, .2f));
            }
            else
            {
                sequence.Append(pullerTransform.transform.DORotate(new Vector3(0f, 0f, 180f), .2f, RotateMode.LocalAxisAdd));
            }
            sequence.Play();

            puller.UpdateAlpha(IsOpen);
            puller.SetPullerInteractable(false);
        }

        internal void UpdateBadge(Color badgeColor, bool playAnimation)
        {
            var badge = puller.GetBadge();
            badge.DOKill();
            badge.transform.DOKill();
            badge.DOBlendableColor(badgeColor, animationDuration);
            if (!playAnimation) return;
            Vector3 target = badge.transform.localScale;
            target *= 1.5f;
            var sequence = DOTween.Sequence();            
            sequence.Append(badge.transform.DOScale(target, animationDuration * .25f));
            sequence.Append(badge.transform.DOScale(Vector3.one, animationDuration * .25f));
            sequence.Append(badge.transform.DOScale(target, animationDuration * .25f));
            sequence.Append(badge.transform.DOScale(Vector3.one, animationDuration * .25f));
            sequence.Play();
        }

        internal void HideBadge()
        {
            puller.GetBadge().DOFade(0f, .2f);
        }

        public void Clear()
        {
            NotificationCenter.ClearNotifications();
        }

        private void OnAnimationComplete()
        {
            puller.SetPullerInteractable(true);
            if(IsOpen)
            {
                OnPanelOpened.Invoke();
            }
            
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isMouseOverPanel = true;
        }
        public void OnPointerExit(PointerEventData eventData)
        {
            isMouseOverPanel = false;
        }

        public void OnDeselect(BaseEventData eventData)
        {            
            if(!isMouseOverPanel) ToggleShow();
        }
    }
}

