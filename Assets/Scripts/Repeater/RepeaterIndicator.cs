using NotReaper.Models;
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
        [SerializeField] private GameObject settingsBar;
        [SerializeField] private GameObject flipTargetColorsIcon;
        [SerializeField] private GameObject mirrorHorizontallyIcon;
        [SerializeField] private GameObject mirrorVerticallyIcon;
        [Space]
        [SerializeField] private Color normalColor;
        [SerializeField] private Color selectedColor;
        [SerializeField] private Color miniNormalColor;
        [SerializeField] private Color miniSelectedColor;
        [SerializeField] private Color barNormalColor;
        [SerializeField] private Color barSelectedColor;
        private Color miniBarNormalColor;
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
        private RepeaterManager manager;

        private Camera mainCam;

        private int textId = -1;

        public void Initialize(Transform miniTimelineParent, bool isParent)
        {
            miniBarNormalColor = barNormalColor;
            miniTimeline = NRDependencyInjector.Get<MiniTimeline>();
            manager = NRDependencyInjector.Get<RepeaterManager>();
            raycaster = GetComponent<GraphicRaycaster>();
            overlay = NRDependencyInjector.Get<RepeaterMenu>();
            timeline = NRDependencyInjector.Get<Timeline>();
            GetComponent<Canvas>().worldCamera = CameraProvider.timeline;
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
            mainCam = CameraProvider.main;
        }

        public void SetColor(Color color)
        {
            miniNormalColor = color;
            color.a = 1f;
            miniBarNormalColor = color;
            if (miniTimelineIndicator != null)
                miniTimelineIndicator.SetColor(miniNormalColor, miniBarNormalColor);


            color.a = .25f;
            normalColor = color;
            barNormalColor = miniBarNormalColor;

            topBarBackground.color = barNormalColor;
            bottomBarBackground.color = barNormalColor;
            background.color = normalColor;
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
            UpdateSettingsIcons();
        }

        public void UpdateSettingsIcons()
        {
            flipTargetColorsIcon.SetActive(section.flipTargetColors);
            mirrorHorizontallyIcon.SetActive(section.mirrorHorizontally);
            mirrorVerticallyIcon.SetActive(section.mirrorVertically);
            settingsBar.SetActive(section.flipTargetColors || section.mirrorHorizontally || section.mirrorVertically);
            var position = settingsBar.transform.position;
            position.z = 0;
            settingsBar.transform.position = position;
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
            settingsBar.transform.localScale = scale;
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

        private void GetAllowedTime(bool isStartHandle, out QNT_Timestamp minAllowedTime, out QNT_Timestamp maxAllowedTime)
        {
            if (isStartHandle)
                GetStartHandleAllowedTime(out minAllowedTime, out maxAllowedTime);
            else
                GetEndHandleAllowedTime(out minAllowedTime, out maxAllowedTime);
        }

        private void GetStartHandleAllowedTime(out QNT_Timestamp minAllowedTime, out QNT_Timestamp maxAllowedTime)
        {
            var duration = QNT_Duration.FromBeatTime(1);
            minAllowedTime = section.startTime;
            maxAllowedTime = section.activeEndTime - duration;

            float length = (float)section.activeEndTime.tick - section.activeStartTime.tick;
            if (length < duration.tick)
                maxAllowedTime = section.activeStartTime;

            QNT_Timestamp earliestRepeaterSectionEndTime = new QNT_Timestamp(0);
            foreach (var repeater in manager.GetSections())
            {
                if(section == repeater)
                    continue;

                if (repeater.activeEndTime < section.activeStartTime && repeater.activeEndTime > earliestRepeaterSectionEndTime)
                    earliestRepeaterSectionEndTime = repeater.activeEndTime;
            }

            NoteEnumerator notes = new NoteEnumerator(new QNT_Timestamp(0), maxAllowedTime);
            notes.reverse = true;
            foreach(var note in notes)
            {
                if (section.Contains(note.data.time))
                {
                    continue;
                }

                if (note.data.time < earliestRepeaterSectionEndTime)
                {
                    minAllowedTime = earliestRepeaterSectionEndTime + EditorBeatSnap.Duration;
                }
                else
                {
                    if(note.data.behavior == TargetBehavior.Sustain)
                    {
                        var temp = note.data.time + note.data.beatLength + EditorBeatSnap.Duration;
                        if (temp > minAllowedTime)
                            minAllowedTime = temp;
                    }
                    else
                    {
                        var temp = note.data.time + EditorBeatSnap.Duration;
                        if (temp > minAllowedTime)
                            minAllowedTime = temp;
                    }
                }

                break;
            }
        }

        private void GetEndHandleAllowedTime(out QNT_Timestamp minAllowedTime, out QNT_Timestamp maxAllowedTime)
        {
            var duration = QNT_Duration.FromBeatTime(1);
            minAllowedTime = section.activeStartTime + duration;
            maxAllowedTime = EditorAudio.SongEndTime;

            float length = (float)section.activeEndTime.tick - section.activeStartTime.tick;
            if (length < duration.tick)
                minAllowedTime = section.activeEndTime;

            QNT_Timestamp earliestRepeaterSectionStartTime = EditorAudio.SongEndTime;
            foreach(var repeater in manager.GetSections())
            {
                if (section == repeater)
                    continue;

                if (repeater.activeStartTime > section.activeEndTime && repeater.activeStartTime < earliestRepeaterSectionStartTime)
                    earliestRepeaterSectionStartTime = repeater.activeStartTime;
            }

            NoteEnumerator notes = new NoteEnumerator(minAllowedTime, maxAllowedTime);

            foreach(var note in notes)
            {
                if (section.Contains(note.data.time))
                    continue;

                if (note.data.time > earliestRepeaterSectionStartTime)
                {
                    maxAllowedTime = earliestRepeaterSectionStartTime - EditorBeatSnap.Duration;
                }
                else
                {
                    maxAllowedTime = note.data.time - EditorBeatSnap.Duration;
                }
                break;
            }
        }

        private IEnumerator DoDrag(bool isStartHandle)
        {

            QNT_Timestamp minAllowedTime;
            QNT_Timestamp maxAllowedTime;
            GetAllowedTime(isStartHandle, out minAllowedTime, out maxAllowedTime);

            while (dragging)
            {
                var mousePos = mainCam.ScreenToWorldPoint(mousePosition.ReadValue<Vector2>());
                mousePos.x /= Timeline.scaleTransform;
                mousePos.x -= timeline.transform.position.x;
                QNT_Timestamp newTime = SnapToBeat(mousePos.x);
                if(newTime != lastTime)
                {
                    lastTime = newTime;
                    if(newTime >= minAllowedTime && newTime <= maxAllowedTime)
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
            => overlay.SetActiveSection(this);

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
                miniTimelineIndicator.SetColor(miniNormalColor, miniBarNormalColor);
                topBarBackground.color = bottomBarBackground.color = barNormalColor;
            }
        }

        public RepeaterSection GetSection() => section;

        public void Destroy()
        {
            Destroy(miniTimelineIndicator.gameObject);
            TimelineTextManager.Instance.RemoveText(textId);
            Destroy(gameObject);
        }
    }
}

