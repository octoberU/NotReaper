using NotReaper.Audio;
using NotReaper.Models;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

namespace NotReaper.Targets
{
    public class BeatLine : MonoBehaviour
    {
        [SerializeField] private TargetIcon target;

        [SerializeField] private RectTransform leftContainer;
        [SerializeField] private RectTransform rightContainer;

        [SerializeField] private Image leftLine;
        [SerializeField] private Image leftStem;
        [SerializeField] private Image rightLine;
        [SerializeField] private Image rightStem;

        private RectTransform leftLineRect;
        private RectTransform rightLineRect;
        private Camera timelineCam;

        private Camera mainCam;
        private SoundEffects sounds;
        private void Start()
        {
            UpdateColor();
            timelineCam = CameraProvider.timeline;
            mainCam = CameraProvider.main;
            sounds = NRDependencyInjector.Get<SoundEffects>();
            leftContainer.GetComponent<Canvas>().worldCamera = timelineCam;
            rightContainer.GetComponent<Canvas>().worldCamera = timelineCam;
            leftLineRect = leftLine.GetComponent<RectTransform>();
            rightLineRect = rightLine.GetComponent<RectTransform>();
        }

        public void UpdateColor()
        {
            leftLine.color = NRSettings.config.leftColor;
            leftStem.color = NRSettings.config.leftColor;
            rightLine.color = NRSettings.config.rightColor;
            rightStem.color = NRSettings.config.rightColor;
        }

        public void SetTransparent(bool transparent)
        {
            var color = leftLine.color;
            color.a = transparent ? .5f : 1f;
            leftLine.color = color;
            leftStem.color = color;

            color = rightLine.color;
            color.a = transparent ? .5f : 1f;
            rightLine.color = color;
            rightStem.color = color;

            //sustainLine.sortingOrder = transparent ? -1 : 1;
        }

        public void EnableSustain(TargetHandType hand, bool enable)
        {
            leftContainer.gameObject.SetActive(false);
            rightContainer.gameObject.SetActive(false);

            if (enable)
            {
                if (hand == TargetHandType.Left)
                    leftContainer.gameObject.SetActive(true);
                else
                    rightContainer.gameObject.SetActive(true);
            }
        }

        public void SetBeatLength(QNT_Duration length)
        {
            SetBeatLength(length.ToBeatTime());   
        }

        private float ConvertToLength(float beatTime) => beatTime / 0.7f * EditorScale.InvertedScaleAmount * 1.75f;

        private void SetBeatLength(float beatTime)
        {
            float x = ConvertToLength(beatTime);
            var size = leftContainer.sizeDelta;
            size.x = x;
            leftContainer.sizeDelta = size;

            size = rightContainer.sizeDelta;
            size.x = x;
            rightContainer.sizeDelta = size;
        }

        bool doDrag = false;
        public void OnLinePressed()
        {
            if (target.data.isPathbuilderTarget || !EditorState.IsToolActive(EditorTool.DragSelect))
                return;

            sounds.PlaySound(SoundEffects.Sound.Open);
            KeybindManager.onMouseDown += OnLineReleased;
            Fade(true);
            doDrag = true;
            StartCoroutine(DragLine());

        }

        private void Fade(bool dragging)
        {
            leftLineRect.DOComplete();
            rightLineRect.DOComplete();

            var size = leftLineRect.sizeDelta;
            size.y = dragging ? .3f : .25f;
            leftLineRect.DOSizeDelta(size, .25f);
            size.x = rightLineRect.sizeDelta.x;
            rightLineRect.DOSizeDelta(size, .25f);
        }

        private void OnLineReleased(bool down)
        {
            if (!down)
            {
                doDrag = false;
                KeybindManager.onMouseDown -= OnLineReleased;
                sounds.PlaySound(SoundEffects.Sound.Close);
                StopCoroutine(DragLine());
                Fade(false);

            }
        }
        private IEnumerator DragLine()
        {
            while (doDrag)
            {
                var mousePos = MousePosition;
                mousePos.x /= Timeline.scaleTransform;
                QNT_Timestamp newTime = SnapToBeat(mousePos.x);

                if(newTime > target.data.time)
                {
                    target.data.beatLength = new QNT_Duration(newTime.tick - target.data.time.tick);
                }
                yield return null;
            }
        }

        private QNT_Timestamp SnapToBeat(float posX)
        {
            QNT_Timestamp time = EditorTime.Time + Relative_QNT.FromBeatTime(posX);
            return EditorTime.GetSnappedTime(time + EditorBeatSnap.Duration / 2, EditorBeatSnap.BeatSnap);
        }

        private Vector3 MousePosition => mainCam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>());
    }

}
