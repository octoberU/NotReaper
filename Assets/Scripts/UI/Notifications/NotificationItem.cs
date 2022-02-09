using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
namespace NotReaper.Notifications
{
    public class NotificationItem : Notification
    {
        public bool Seen { get; private set; }

        [Header("Settings")]
        [SerializeField] private float fadeInSpeed = .3f;
        [SerializeField] private float fadeOutSpeed = .3f;

        private NotificationSpawner spawner;

        protected override void Awake()
        {
            base.Awake();
        }
        internal void SetSpawner(NotificationSpawner spawner)
        {
            this.spawner = spawner;
        }

        internal void SetSeen()
        {
            Seen = true;
        }

        internal override void Show(NotificationType type, string text, int id)
        {
            Setup(type, text, id);
            gameObject.SetActive(true);
            if (NotificationPanel.IsOpen)
            {
                canvas.alpha = 0f;
                canvas.DOFade(1f, fadeInSpeed);
            }
            else
            {
                canvas.alpha = 1f;
            }
        }

        public override void Close()
        {
            if (NotificationPanel.IsOpen)
            {
                var sequence = DOTween.Sequence();
                sequence.OnComplete(() => OnFadeoutComplete());
                //sequence.Append(transform.DOMoveX(transform.position.x - .2f, .1f));
                //sequence.Append(transform.DOMoveX(transform.position.x + 10f, .5f).SetEase(Ease.InExpo));
                sequence.Append(transform.DOMoveX(transform.position.x + 10f, fadeOutSpeed).SetEase(Ease.InBack));
                sequence.Join(canvas.DOFade(0f, fadeOutSpeed));
                sequence.Play();
            }
            else
            {
                OnFadeoutComplete();
            }          
        }


        private void OnFadeoutComplete()
        {
            Seen = false;
            spawner.RemoveNotification(this);
            gameObject.SetActive(false);
        }
    }
}

