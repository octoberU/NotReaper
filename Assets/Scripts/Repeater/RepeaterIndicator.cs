using NotReaper.Timing;
using NotReaper.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
namespace NotReaper.Repeaters
{
    public class RepeaterIndicator : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private RectTransform rect;
        [SerializeField] private Image background;
        [SerializeField] private TextMeshProUGUI textContainer;
        [SerializeField] private Transform startHandle;
        [SerializeField] private Transform endHandle;
        [SerializeField] private RepeaterMiniIndicator miniTimelineIndicatorPrefab;
        [SerializeField] private RectTransform topBar;
        [SerializeField] private RectTransform bottomBar;
        [Space]
        [SerializeField] private Color normalColor;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color miniNormalColor;
        [SerializeField] private Color miniSelectedColor;
        [SerializeField] private Color barNormalColor;
        [SerializeField] private Color barSelectedColor;
        private Timeline timeline;
        private InputAction mousePosition;
        private RepeaterSection section;

        private QNT_Timestamp minTime;
        private QNT_Timestamp maxTime;
        private QNT_Timestamp lastTime = new QNT_Timestamp(0);

        private bool dragging = false;
        private RepeaterMenu overlay;
        private GraphicRaycaster raycaster;
        private MiniTimeline miniTimeline;
        private Transform miniTimelineParent;
        private RepeaterMiniIndicator miniTimelineIndicator;
        private Image miniBackground;
        private Image topBarBackground;
        private Image bottomBarBackground;

        private int textId = -1;

        public void Initialize(Transform miniTimelineParent, bool isParent)
        {
            miniTimeline = NRDependencyInjector.Get<MiniTimeline>();
            raycaster = GetComponent<GraphicRaycaster>();
            overlay = NRDependencyInjector.Get<RepeaterMenu>();
            timeline = NRDependencyInjector.Get<Timeline>();
            GetComponent<Canvas>().worldCamera = timeline.timelineCamera.GetComponent<Camera>();
            mousePosition = KeybindManager.Global.MousePosition;
            this.miniTimelineParent = miniTimelineParent;
            miniTimelineIndicator = Instantiate(miniTimelineIndicatorPrefab, miniTimelineParent);
            miniBackground = miniTimelineIndicator.GetComponent<Image>();
            bottomBarBackground = bottomBar.GetComponent<Image>();
            topBarBackground = topBar.GetComponent<Image>();
            SetIsParent(isParent);
            background.color = normalColor;
            miniBackground.color = miniNormalColor;
            topBarBackground.color = barNormalColor;
            bottomBarBackground.color = barNormalColor;
            miniTimelineIndicator.SetColor(miniNormalColor, barNormalColor);
            var pos = transform.localPosition;
            pos.y = .031f;
            transform.localPosition = pos;
        }

        public void SetIsParent(bool isParent)
        {
            topBar.gameObject.SetActive(isParent);
            bottomBar.gameObject.SetActive(isParent);
            miniTimelineIndicator.SetIsParent(isParent);
        }

        public void SetSection(RepeaterSection section)
        {
            this.section = section;
        }

        public void SetText(string text)
        {
            if(textId == -1)
            {
                textId = TimelineTextManager.Instance.AddText(text, section.activeStartTime);
            }
            else
            {
                TimelineTextManager.Instance.UpdateText(text, textId);
            }
            //textContainer.text = text;
        }

        public void SetWidth(float width)
        {
            rect.sizeDelta = new Vector2(width, 1.062f);
            Vector2 size = topBar.sizeDelta;
            size.x = width;
            topBar.sizeDelta = size;
            size.y = bottomBar.sizeDelta.y;
            bottomBar.sizeDelta = size;
            Bounds bounds = new(rect.position, rect.sizeDelta);
            startHandle.position = new Vector2(bounds.min.x + rect.sizeDelta.x * .5f, bounds.min.y);
            endHandle.position = new Vector2(bounds.max.x + rect.sizeDelta.x * .5f, bounds.min.y);
            transform.localScale = Vector3.one;

            UpdateMiniIndicatorPosition();
            //miniTimelineIndicator.sizeDelta = new Vector2(miniWidth, 22.1f);
        }

        public void UpdateMiniIndicatorPosition()
        {
            Vector3 pos = miniTimelineIndicator.transform.localPosition;
            pos.x = miniTimeline.TimestampToMinitimeline(section.activeStartTime);
            miniTimelineIndicator.transform.localPosition = pos;
            float miniWidth = miniTimeline.TimestampToMinitimeline(section.activeEndTime) - pos.x;
            miniTimelineIndicator.SetWidth(miniWidth);
        }

        public void FixScaling()
        {
            transform.localScale = Vector3.one;
            Vector3 scale = startHandle.localScale;
            scale.x = EditorScale.ScaleAmount;
            startHandle.localScale = scale;
            endHandle.localScale = scale;
        }

        public void SetInteractable(bool interactable)
        {
            startHandle.gameObject.SetActive(interactable);
            endHandle.gameObject.SetActive(interactable);
            raycaster.enabled = interactable;
        }

        public void StartDrag(bool isStartHandle)
        {
            if (dragging) return;
            OnPointerDown(null);
            dragging = true;
            if (isStartHandle)
            {
                minTime = section.startTime;
                maxTime = section.activeEndTime;
            }
            else
            {
                minTime = section.activeStartTime;
                maxTime = QNT_Timestamp.ShiftTick(timeline.songPlayback.song.Length); 
            }
            StopAllCoroutines();
            StartCoroutine(DoDrag(isStartHandle));
        }

        public void EndDrag()
        {
            StopAllCoroutines();
            lastTime = new QNT_Timestamp(0);
            dragging = false;
        }

        private IEnumerator DoDrag(bool isStartHandle)
        {
            bool foundTransient = false;
            bool foundBoundary = false;
            while (dragging)
            {
                var mousePos = Camera.main.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());
                mousePos.x /= Timeline.scaleTransform;
                mousePos.x -= timeline.transform.position.x;
                QNT_Timestamp newTime = SnapToBeat(mousePos.x);
                if (newTime != lastTime)
                {
                    lastTime = newTime;
                    float length = isStartHandle ? (section.activeEndTime.tick - newTime.tick) : newTime.tick - section.activeStartTime.tick;
                    if(length > 0f)
                    {
                        var search = TargetFinder.BinarySearchOrderedNotes(newTime);
                        bool found = search.found;
                        if (!foundBoundary && !isStartHandle && timeline.repeaterManager.IsTargetInRepeaterZone(newTime, section.startTime))
                        {
                            foundBoundary = true;
                            maxTime = newTime;
                        }
                        else if (found)
                        {
                            var foundTarget = EditorNotes.OrderedNotes[search.index];
                            found = foundTarget.data.time < section.activeStartTime || foundTarget.data.time > section.activeEndTime;
                            if (found)
                            {
                                maxTime = foundTarget.data.time;
                                foundBoundary = true;
                            }
                            if (foundTarget.transient && !foundTransient && !isStartHandle)
                            {
                                foundTransient = true;
                                minTime = new QNT_Timestamp(foundTarget.data.time.tick - 1);
                            }
                            else if (foundTarget.data.isPathbuilderTarget || foundTarget.data.legacyPathbuilderData != null)
                            {
                                maxTime = new QNT_Timestamp(foundTarget.data.time.tick + 1);
                            }
                        }

                        if (!found && newTime >= minTime && newTime <= maxTime)
                        {
                            if (isStartHandle)
                            {
                                section.SetActiveStartTime(newTime);
                                transform.localPosition = new Vector3(newTime.ToBeatTime(), 0f, 0f);
                                TimelineTextManager.Instance.RemoveText(textId);
                                textId = TimelineTextManager.Instance.AddText(section.ID, newTime);
                            }
                            else
                            {
                                section.SetActiveEndTime(newTime);
                            }
                            SetWidth((section.activeEndTime - section.activeStartTime).ToBeatTime());
                        }
                        else
                        {
                            if (!isStartHandle && !foundBoundary) maxTime = newTime;
                        }
                    }
                    
                }
                yield return null;
            }
            yield return null;
        }

        private QNT_Timestamp SnapToBeat(float posX)
        {
            QNT_Timestamp time = EditorTime.Time + Relative_QNT.FromBeatTime(posX);
            return EditorTime.GetSnappedTime(time + EditorBeatSnap.Duration / 2, EditorBeatSnap.BeatSnap);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            overlay.SetActiveSection(this);
        }

        public void SetSectionActive(bool active)
        {
            if (active)
            {
                background.color = selectedColor;
                miniTimelineIndicator.SetColor(miniSelectedColor, barSelectedColor);
                topBarBackground.color = bottomBarBackground.color = barSelectedColor;
            }
            else
            {
                background.color = normalColor;
                miniTimelineIndicator.SetColor(miniNormalColor, barNormalColor);
                topBarBackground.color = bottomBarBackground.color = barNormalColor;
            }
        }

        public RepeaterSection GetSection()
        {
            return section;
        }

        public void Destroy()
        {
            Destroy(miniTimelineIndicator.gameObject);
            TimelineTextManager.Instance.RemoveText(textId);
            Destroy(gameObject);
        }
    }
}

