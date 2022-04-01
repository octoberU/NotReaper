using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Overlays
{
    public class OverlayManager : MonoBehaviour
    {
        public static OverlayManager Instance { get; private set; } = null;
        [NRInject] private Timeline timeline;

        private CanvasGroup canvas;
        private Camera cam;
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("OverlayManager already exists.");
                return;
            }
            Instance = this;
            canvas = GetComponent<CanvasGroup>();
            canvas.alpha = 0f;
            cam = Camera.main;
        }

        public void FadeIn(RectTransform rect, Location location, float fadeDuration, bool avoidOpeningOverTargets)
        {
            if (avoidOpeningOverTargets)
            {
                Rect bounds = new Rect(rect.localPosition, rect.sizeDelta);
                if (EditorNotes.HasSelectedNotes)
                {
                    if (EditorNotes.SelectedNotes.Count == 1)
                    {
                        if (EditorNotes.SelectedNotes[0].IsInsideRectAtTime(EditorTime.Time, bounds))
                        {
                            if (rect.localPosition.x > 0) location = Location.BottomLeft;
                            else location = Location.BottomRight;
                        }
                    }
                }
            }
            SetLocation(rect, location);
            canvas.DOFade(1f, fadeDuration);
        }

        public void FadeOut(GameObject overlay, float fadeDuration)
        {
            canvas.DOFade(0f, fadeDuration).OnComplete(() =>
            {
                overlay.SetActive(false);
            });
        }

        private void SetLocation(RectTransform rect, Location location)
        {
            switch (location)
            {
                case Location.BottomLeft:
                    rect.position = cam.ViewportToWorldPoint(new Vector3(0, 0, 0));
                    break;
                case Location.BottomRight:
                    rect.position = cam.ViewportToWorldPoint(new Vector3(1, 0, 0));
                    break;
                case Location.Center:
                    rect.position = Vector3.zero;
                    break;
            }
        }
    }

    public enum Location
    {
        BottomRight,
        BottomLeft,
        Center
    }
}

