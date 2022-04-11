using DG.Tweening;
using NotReaper.UI;
using NotReaper.UI.Components;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace NotReaper.Notifications
{
    public class NotificationPopup : Notification, IPointerEnterHandler, IPointerExitHandler
    {
        public bool IsActive { get; private set; } = false;
        public bool IsForceShowing { get; set; } = false;

        [Header("Settings")]
        [SerializeField] private float stayDuration = 3f;
        [SerializeField] private float fadeoutDuration = 1f;
        [SerializeField] private float closeDuration = .5f;
        [SerializeField] private float changeDuration = 1f;

        private RectTransform mask;
        private RectTransform contentRect;
        private Vector2 originalMaskSize;
        private bool isHoveredOver = false;
        private float time = 0f;

        private Sequence showSequence;

        private bool isBeingClosed = false;

        private Vector3 startPos = new Vector3(900f, 380f, 0f);
        private RectTransform rect;

        protected override void Awake()
        {
            base.Awake();
            mask = transform.GetChild(0).GetComponent<RectTransform>();
            contentRect = mask.GetChild(0).GetComponent<RectTransform>();
            originalMaskSize = mask.sizeDelta;
            changeDuration *= .5f;
            rect = GetComponent<RectTransform>();
            rect.anchoredPosition = startPos;
            NotificationPanel.OnPanelOpened.AddListener(OnNotificationPanelOpened);
        }

        public override void Close()
        {
            isBeingClosed = true;
            RemoveFromNotificationList();
            FadeoutPopup(closeDuration);
        }

        internal override void Show(NotificationType type, string text, int id)
        {
            if (IsActive)
            {
                StartCoroutine(ShowAsFade(type, text, id));
            }
            else
            {
                Setup(type, text, id);
                StartCoroutine(ShowAsFlyout());
                IsActive = true;
            }
        }
        private Sequence fadeMoveSequence;
        private IEnumerator ShowAsFade(NotificationType type, string text, int id)
        {
            //wait for end of frame so the contentRect has time to update it's size.
            yield return new WaitForEndOfFrame();
            StopFadeout();
            canvas.DOKill();
            if (fadeMoveSequence != null && fadeMoveSequence.IsPlaying())
            {
                fadeMoveSequence.Complete();
            }
            //mask.sizeDelta = new Vector2(mask.sizeDelta.y, mask.sizeDelta.y);
            var sequence = DOTween.Sequence();
            sequence.OnComplete(() => StartCoroutine(OnChangeFadeoutComplete(type, text, id)));
            sequence.Append(transform.DOScale(new Vector3(.95f, .95f, .95f), changeDuration));
            sequence.Join(iconHolder.DOFade(0f, changeDuration));
            sequence.Join(notificationText.DOFade(0f, changeDuration));
            sequence.Play();
            fadeMoveSequence = DOTween.Sequence();
            Vector3 startPos = transform.localPosition;
            Vector3 amount = new Vector3(5f, 3f, 0f);
            fadeMoveSequence.Append(transform.DOLocalMove(startPos + amount, changeDuration).SetEase(Ease.OutBack));
            fadeMoveSequence.Append(transform.DOLocalMove(startPos, changeDuration));
            fadeMoveSequence.Play();
            //transform.DOPunchPosition(new Vector3(transform.position.x + .001f, transform.position.y, transform.position.z), changeDuration * 2f, 0, .001f);
        }

        private IEnumerator OnChangeFadeoutComplete(NotificationType type, string text, int id)
        {
            Setup(type, text, id);
            yield return new WaitForEndOfFrame();
            var sequence = DOTween.Sequence();
            sequence.OnComplete(() => OnPopupShown());
            sequence.Append(transform.DOScale(Vector3.one, changeDuration));
            sequence.Join(mask.DOSizeDelta(new Vector2(originalMaskSize.x, contentRect.sizeDelta.y), changeDuration));
            sequence.Join(iconHolder.DOFade(1f, changeDuration));
            sequence.Join(notificationText.DOFade(1f, changeDuration));
            sequence.Play();
        }

        private IEnumerator ShowAsFlyout()
        {
            //wait for end of frame so the contentRect has time to update it's size.
            yield return new WaitForEndOfFrame();
            canvas.DOKill();
            mask.sizeDelta = new Vector2(mask.sizeDelta.y, mask.sizeDelta.y);
            var sequence = DOTween.Sequence();
            sequence.SetAutoKill(false);
            sequence.OnComplete(() => OnPopupShown());
            sequence.Append(transform.DOLocalMoveX(transform.localPosition.x - 210f, .2f).SetEase(Ease.OutQuart));
            sequence.Join(canvas.DOFade(1f, .2f));
            sequence.Append(transform.DOLocalMoveX(transform.localPosition.x - 200f, .2f).SetEase(Ease.InQuart));
            sequence.Join(mask.DOSizeDelta(new Vector2(originalMaskSize.x, mask.sizeDelta.y), .2f).SetEase(Ease.InQuart));
            sequence.Join(mask.DOSizeDelta(new Vector2(originalMaskSize.x, contentRect.sizeDelta.y), .4f).SetEase(Ease.OutBack).SetDelay(.2f));
            sequence.Play();
            showSequence = sequence;
        }

        private void OnPopupShown()
        {
            time = 0f;
            StartCoroutine(PopupTimer());
        }

        private IEnumerator PopupTimer()
        {
            while (time < stayDuration || isHoveredOver)
            {
                time += Time.deltaTime;
                yield return null;
            }
            FadeoutPopup(fadeoutDuration);
            yield return null;
        }

        private void FadeoutPopup(float duration)
        {
            canvas.DOKill();
            canvas.DOFade(0f, duration).OnComplete(() => OnFadeoutComplete());
        }

        private void OnFadeoutComplete()
        {
            Reset();
        }

        private void StopFadeout()
        {
            StopAllCoroutines();
            canvas.DORewind();
        }

        private void Reset()
        {
            if (showSequence != null)
            {
                showSequence.Kill();
                showSequence = null;
            }
            canvas.DOKill();
            isHoveredOver = false;
            isBeingClosed = false;
            time = 0f;
            IsActive = false;
            rect.anchoredPosition = startPos;
            IsForceShowing = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            DOTween.KillAll();
        }

        private void RemoveFromNotificationList()
        {
            NotificationCenter.RemoveNotification(ID);
        }

        private void OnNotificationPanelOpened()
        {
            if (IsActive)
            {
                StopAllCoroutines();
                Close();
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (isBeingClosed) return;
            isHoveredOver = true;
            StopFadeout();
            RemoveFromNotificationList();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (isBeingClosed) return;
            RemoveFromNotificationList();
            isHoveredOver = false;
            StopFadeout();
            OnPopupShown();
        }


    }
}

