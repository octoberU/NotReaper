using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using UnityEngine.Events;
using NotReaper.UserInput;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using NotReaper.Audio;
using NotReaper.UI.Components;

namespace NotReaper.Notifications
{
    [RequireComponent(typeof(CanvasGroup))]
    public class NotificationPanel : NRMenu, IPointerEnterHandler, IPointerExitHandler, IDeselectHandler
    {
        public static bool IsOpen = false;

        public static UnityEvent OnPanelOpened = new UnityEvent();

        [Header("References")]
        [SerializeField] private RectTransform parent;
        [SerializeField] private Puller puller;
        [SerializeField] private NRButton clearButton;
        [SerializeField] private GameObject inputBlocker;
        //[SerializeField] private GameObject inputCatcher;
        [Space, Header("Slide Settings")]
        [SerializeField] private float slideDistance = 5f;
        [SerializeField] private float slideDuration = .5f;
        [SerializeField] private Ease openEasing;
        [SerializeField] private Ease closeEasing;
        [Space, Header("Badge Settings")]
        [SerializeField] private float animationDuration = 1f;

        [Space, Header("Modes")]
        [SerializeField] private NRTitle title;
        [SerializeField] private GameObject notificationScroller;
        [SerializeField] private GameObject undoHistoryScroller;

        private CanvasGroup canvas;
        private Vector3 startPos = new Vector3(10.8404f, -0.805f, 0f);
        private bool isMouseOverPanel = false;

        private SoundEffects effects;
        private bool isAnimating;

        private bool isShowingNotifications = true;

        [NRInject] private ChangeHistoryManager historyManager;

        protected override void Awake()
        {
            base.Awake();
            canvas = GetComponent<CanvasGroup>();
            parent.anchoredPosition = startPos;
            inputBlocker.SetActive(false);
        }

        private void Start()
        {
            effects = NRDependencyInjector.Get<SoundEffects>();
        }

        public void ToggleShow()
        {
            if (isAnimating) return;
            IsOpen = !IsOpen;
            if (IsOpen) Show();
            else Hide();
        }

        public override void ShowHelp() { }

        private IEnumerator SelectGameObject(bool select)
        {
            while (EventSystem.current.alreadySelecting)
            {
                yield return null;
            }
            EventSystem.current.SetSelectedGameObject(select ? gameObject : null);
        }

        private IEnumerator CheckSelectedGameObject()
        {
            while (IsOpen)
            {
                if(EventSystem.current.currentSelectedGameObject != gameObject && !EventSystem.current.alreadySelecting)
                {
                    EventSystem.current.SetSelectedGameObject(gameObject);
                }
                yield return null;
            }
        }

        public override void Show()
        {
            if (isAnimating) return;
            isAnimating = true;
            IsOpen = true;
            
            if(!isShowingNotifications)
                historyManager.UpdateHistory();
            
            StopAllCoroutines();
            StartCoroutine(SelectGameObject(true));
            StartCoroutine(CheckSelectedGameObject());
            effects.PlaySound(SoundEffects.Sound.Open);
            clearButton.interactable = true;
            inputBlocker.SetActive(true);
            OnActivated();

            float distance = -slideDistance;
            Ease easing = openEasing;
            Transform pullerTransform = puller.GetPullerIconTransform();
            var sequence = DOTween.Sequence();
            sequence.OnComplete(() => OnAnimationComplete());
            sequence.Append(transform.DOLocalMove(new Vector3(transform.localPosition.x + distance, transform.localPosition.y, transform.localPosition.z), slideDuration).SetEase(easing));
            sequence.Join(puller.transform.DOLocalMoveX(puller.transform.localPosition.x + distance, slideDuration).SetEase(easing));
            sequence.Join(canvas.DOFade(1f, slideDuration).SetEase(easing));           
            sequence.Join(pullerTransform.transform.DORotate(new Vector3(0f, 0f, 180f), .2f, RotateMode.LocalAxisAdd).SetDelay(slideDuration * .5f));
            sequence.Join(puller.GetBadge().DOFade(0f, .2f));           
            sequence.Play();

            puller.UpdateAlpha(true);
            puller.SetPullerInteractable(false);
            
            if(KeybindManager.WasPlaybackEnabled)
                KeybindManager.EnablePlayback(true);
        }

        public override void Hide()
        {
            if (isAnimating) return;

            isAnimating = true;
            IsOpen = false;
            StopAllCoroutines();
            StartCoroutine(SelectGameObject(false));
            effects.PlaySound(SoundEffects.Sound.Close);
            inputBlocker.SetActive(false);
            OnDeactivated();
            
            float distance = slideDistance;
            Ease easing = closeEasing;
            Transform pullerTransform = puller.GetPullerIconTransform();
            var sequence = DOTween.Sequence();
            sequence.OnComplete(() => OnAnimationComplete());
            sequence.Append(transform.DOLocalMove(new Vector3(transform.localPosition.x + distance, transform.localPosition.y, transform.localPosition.z), slideDuration).SetEase(easing));
            sequence.Join(puller.transform.DOLocalMoveX(puller.transform.localPosition.x + distance, slideDuration).SetEase(easing));
            sequence.Join(canvas.DOFade(0f, slideDuration).SetEase(easing));
            sequence.Append(pullerTransform.transform.DORotate(new Vector3(0f, 0f, 180f), .2f, RotateMode.LocalAxisAdd));          
            sequence.Play();

            puller.UpdateAlpha(false);
            puller.SetPullerInteractable(false);
        }

        internal void UpdateBadge(Color badgeColor, bool playAnimation)
        {
            var badge = puller.GetBadge();
            badge.DOKill();
            badge.transform.DOKill();
            badge.transform.localScale = Vector3.one;
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
            clearButton.interactable = false;
            StartCoroutine(DoClear());
        }

        private IEnumerator DoClear()
        {
            NotificationCenter.ClearNotifications();
            yield return new WaitForSeconds(.2f);
            ToggleShow();
        }

        private void OnAnimationComplete()
        {
            puller.SetPullerInteractable(true);
            if(IsOpen)
            {
                OnPanelOpened.Invoke();
            }
            isAnimating = false;
            
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

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
            Hide();
        }
        
        
        #region Undo Redo stuffs

        public void ToggleMode()
        {
            isShowingNotifications = !isShowingNotifications;
            
            if(!isShowingNotifications)
                historyManager.UpdateHistory();
            
            notificationScroller.SetActive(isShowingNotifications);
            undoHistoryScroller.SetActive(!isShowingNotifications);
            clearButton.gameObject.SetActive(isShowingNotifications);
            title.textContainer.SetText(isShowingNotifications ? "notifications" : "change history");
        }
        #endregion
    }
}

