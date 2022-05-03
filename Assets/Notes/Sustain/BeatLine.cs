using NotReaper.Audio;
using NotReaper.Models;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using NotReaper.Tools;
using NotReaper.MapEditor.Notes;

namespace NotReaper.Targets
{
    public class BeatLine : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private TargetIcon target;

        [SerializeField] private LineRenderer sustain;
        [SerializeField] private BoxCollider2D boxCollider;

        private Camera mainCam;
        private SoundEffects sounds;

        private TargetHandType hand = TargetHandType.None;

        private float currentLength = 0f;

        private QNT_Duration initialBeatLength;

        private void Start()
        {
            mainCam = CameraProvider.main;
            sounds = NRDependencyInjector.Get<SoundEffects>();
        }

        public void SetTransparent(bool transparent)
        {
            var color = sustain.startColor;
            color.a = transparent ? .4f : 1f;
            sustain.startColor = color;
            sustain.endColor = color;

            sustain.sortingOrder = transparent ? -1 : 1;
        }

        public void EnableSustain(TargetHandType hand, bool enable)
        {
            sustain.enabled = enable;
            this.hand = hand;
            SetBeatLength(currentLength);
        }

        public void SetBeatLength(QNT_Duration length)
        {
            SetBeatLength(length.ToBeatTime());   
        }

        private float ConvertToLength(float beatTime) => beatTime / 0.7f * EditorScale.InvertedScaleAmount * 1.75f;

        private void SetBeatLength(float beatTime)
        {
            currentLength = beatTime;
            float x = ConvertToLength(beatTime);
            float dir = hand == TargetHandType.Left ? .6f : -.6f;
            sustain.SetPosition(1, new(0, dir, 0));
            sustain.SetPosition(2, new(x, dir, 0));

            Vector2 offset = new(x * .5f, dir);
            boxCollider.offset = offset;

            Vector2 size =  new(x, boxCollider.size.y);
            boxCollider.size = size;
        }

        bool doDrag = false;
        public void OnLinePressed()
        {
            if (target.data.isPathbuilderTarget || !EditorState.IsToolActive(EditorTool.DragSelect))
                return;

            initialBeatLength = target.data.beatLength;
            sounds.PlaySound(SoundEffects.Sound.Open);
            KeybindManager.onMouseDown += OnLineReleased;
            doDrag = true;
            StartCoroutine(DragLine());

        }

        private void OnLineReleased(bool down)
        {
            if (!down)
            {
                doDrag = false;
                KeybindManager.onMouseDown -= OnLineReleased;
                sounds.PlaySound(SoundEffects.Sound.Close);
                StopCoroutine(DragLine());

                if (initialBeatLength != target.data.beatLength)
                {
                    UndoRedoManager.AddAction(new NRActionChangeBeatLength(target.target, initialBeatLength, target.data.beatLength));
                }
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

        public void OnPointerDown(PointerEventData eventData)
        {
            Debug.Log("Clicked!");
            OnLinePressed();
        }

        private Vector3 MousePosition => mainCam.ScreenToWorldPoint(KeybindManager.Global.MousePosition.ReadValue<Vector2>());
    }

}
