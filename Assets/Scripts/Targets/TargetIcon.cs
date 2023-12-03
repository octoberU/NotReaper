using System;
using System.Collections;
using System.Collections.Generic;
using NotReaper.Models;
using UnityEngine;
using UnityEngine.UI;
using NotReaper.Timing;
using NotReaper.Tools.ChainBuilder;
using System.Linq;
using NotReaper.Notifications;
using NotReaper.UI.Particles;

namespace NotReaper.Targets
{

    public enum TargetIconLocation
    {
        Timeline,
        Grid
    }

    public class TargetIcon : MonoBehaviour
    {
        [SerializeField] internal SpriteRenderer note;
        [SerializeField] private SpriteRenderer prefade;
        [SerializeField] private SpriteRenderer ring;
        [SerializeField] private SpriteRenderer selection;
        [SerializeField] private LineRenderer chainConnector;
        [SerializeField] private LineRenderer sustainLineRenderer;


        [SerializeField] float timelineSpread = 1.5f;

        public float targetSize = 1f;
        public float timelineTargetSize = 1f;


        public TargetData data;
        public Target target;

        public float sustainDirection = 0.6f;
        public float doubleMeleeOffset = .25f;

        [SerializeField] private float collisionRadiusClick = 0.95f;
        [SerializeField] private float collisionRadiusDrag = 0.95f;
        public bool isSelected = false;
        public TargetIconLocation location;

        public GameObject sustainButtons;

        public Transform holdEndTrans;

        [Space, Header("Hitsound Icons")]
        [SerializeField] private SpriteRenderer hitsoundDisplay;


        [Space, Header("Sustain")]
        [SerializeField] private BeatLine sustainController;

        public bool SustainButtonsActive => sustainButtons.activeSelf;

        private Canvas canvas;
        private bool hasCachedReferences = false;

        /// <summary>
        /// For when the note is right clicked on.
        /// </summary>
        public event Action OnTryRemoveEvent;

        public void OnTryRemove()
        {
            if (!target.transient)
            {
                OnTryRemoveEvent();
            }
        }

        public void Remove()
        {
            Destroy(gameObject);
        }

        public event Action IconEnterLoadedNotesEvent;
        public event Action IconExitLoadedNotesEvent;

        public void IconEnterLoadedNotes()
        {
            IconEnterLoadedNotesEvent();
        }

        public void IconExitLoadedNotes()
        {
            IconExitLoadedNotesEvent();
        }

        public event Action TrySelectEvent;
        public event Action TryDeselectEvent;

        public void TrySelect()
        {
            TrySelectEvent();
        }

        public void TryDeselect()
        {
            TryDeselectEvent();
        }

        public void SetTransparency(float transparency)
        {
            Color color = Color.white;
            color.a = transparency;
            prefade.color = color;
            ring.color = color;
            note.color = color;
        }

        public void Init(Target target, TargetData targetData)
        {
            data = targetData;
            
            UpdateVisuals(AssetContainer.GetVisualProperties(data.behavior, data.handType, location));
            data.HandTypeChangeEvent += OnHandTypeChanged;
            data.BehaviourChangeEvent += OnBehaviorChanged;
            data.BeatLengthChangeEvent += OnSustainLengthChanged;
            data.VelocityChangeEvent += OnVelocityChanged;
            data.TickChangeEvent += OnTickChanged;
            this.target = target;
            
            OnHandTypeChanged(data.handType);
            OnBehaviorChanged(data.behavior, data.behavior);
            OnSustainLengthChanged(data.beatLength);
            OnVelocityChanged(data.velocity, data.velocity);
            OnTickChanged(data.time, data.time);

            CacheReferences();

            if (location == TargetIconLocation.Timeline)
            {
                canvas.worldCamera = CameraProvider.timeline;
            }
            else
            {
                canvas.worldCamera = CameraProvider.main;
                chainConnector.enabled = false;
                SetHitsoundIcon(data.velocity);
            }
            //SetupFade();
        }

        private void UpdateVisuals(AssetContainer.TargetProperties properties)
        {
            note.sprite = properties.target.sprite;
            note.SetPropertyBlock(properties.target.block);
            
            selection.sprite = properties.selectRing.sprite;
            selection.SetPropertyBlock(properties.selectRing.block);

            if (location is TargetIconLocation.Grid)
            {
                prefade.sprite = properties.preFade.sprite;
                prefade.SetPropertyBlock(properties.preFade.block);

                ring.sprite = properties.ring.sprite;
                ring.SetPropertyBlock(properties.ring.block);

                var hitsoundProperty = AssetContainer.GetHitsoundProperty(data.velocity, data.handType);
                hitsoundDisplay.sprite = hitsoundProperty.sprite;
                hitsoundDisplay.SetPropertyBlock(hitsoundProperty.block);

                //chainConnector.startColor = properties.lineRenderer.color;
                //chainConnector.endColor = properties.lineRenderer.color;
                chainConnector.SetPropertyBlock(properties.lineRenderer.block);
                
                note.material.SetFloat("_FadeThreshold", 1.7f);
                note.material.SetFloat("_OpaqueDuration", 1f);
                note.material.SetFloat("_FadeOutThreshold", 0.5f);
                note.material.SetFloat("_WorldPosOffset", 0f);
                
                prefade.material.SetFloat("_FadeThreshold", 1.7f);
                prefade.material.SetFloat("_OpaqueDuration", 1f);
                prefade.material.SetFloat("_FadeOutThreshold", 0.5f);
                prefade.material.SetFloat("_WorldPosOffset", 0f);
                
                ring.material.SetFloat("_FadeThreshold", 1.7f);
                ring.material.SetFloat("_OpaqueDuration", 1f);
                ring.material.SetFloat("_FadeOutThreshold", 0.5f);
                ring.material.SetFloat("_WorldPosOffset", 0f);
                
                hitsoundDisplay.material.SetFloat("_FadeThreshold", 1.7f);
                hitsoundDisplay.material.SetFloat("_OpaqueDuration", 1f);
                hitsoundDisplay.material.SetFloat("_FadeOutThreshold", 0.5f);
                hitsoundDisplay.material.SetFloat("_WorldPosOffset", 0f);
                
                chainConnector.material.SetFloat("_FadeThreshold", 1.7f);
                chainConnector.material.SetFloat("_OpaqueDuration", 1f);
            }
            else
            {
                sustainLineRenderer.startColor = properties.lineRenderer.color;
                sustainLineRenderer.endColor = properties.lineRenderer.color;
            }
            
            switch (data.behavior)
            {
                 case TargetBehavior.Horizontal:
                     if (location == TargetIconLocation.Grid)
                    {
                        note.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                        prefade.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                        ring.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                        selection.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                    }
                    else
                    {
                        note.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        selection.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                    break;
                case TargetBehavior.Vertical:
                    if (location == TargetIconLocation.Grid)
                    {
                        note.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                        prefade.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                        ring.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                        selection.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    }
                    else
                    {
                        note.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                        selection.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                    break;
                 case TargetBehavior.ChainNode:
                     if (location == TargetIconLocation.Timeline)
                     {
                         note.transform.localScale = Vector3.one * 0.2f;
                         selection.transform.localScale = Vector3.one * .175f;
                     }
                     break;
                 case TargetBehavior.Melee:
                     if (location == TargetIconLocation.Grid)
                     {
                         note.transform.localScale = Vector3.one * 1.5f;
                         selection.transform.localScale = Vector3.one * 1.25f;
                         ring.transform.localScale = Vector3.one * 1.5f;
                     }
                     break;
                default:
                    break;
            }
        }

        private void UpdateSprites(SpritePack pack)
        {
            note.sprite = pack.target;
            selection.sprite = pack.ring;

            if (location is TargetIconLocation.Grid) 
            {
                prefade.sprite = pack.telegraph;
                ring.sprite = pack.ring;
            }


            switch (data.behavior)
            {
                 case TargetBehavior.Horizontal:
                     if (location == TargetIconLocation.Grid)
                    {
                        note.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                        prefade.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                        ring.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                        selection.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
                    }
                    else
                    {
                        note.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                        selection.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    }
                    break;
                case TargetBehavior.Vertical:
                    if (location == TargetIconLocation.Grid)
                    {
                        note.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                        prefade.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                        ring.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                        selection.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    }
                    else
                    {
                        note.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                        selection.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                    }
                    break;
                 case TargetBehavior.ChainNode:
                     if (location == TargetIconLocation.Timeline) note.transform.localScale = Vector3.one * 0.2f;
                     break;
                 case TargetBehavior.Melee:
                     if (location == TargetIconLocation.Grid)
                     {
                         note.transform.localScale = Vector3.one * 1.5f;
                         selection.transform.localScale = Vector3.one * 1.25f;
                         ring.transform.localScale = Vector3.one * 1.5f;
                     }
                     break;
                default:
                    break;
            }
        }

        private void CacheReferences()
        {
            if (hasCachedReferences) return;
            hasCachedReferences = true;
            canvas = sustainButtons.GetComponent<Canvas>();
        }
        

        public void ClearData()
        {
            KillSustainAnimation();
            data.HandTypeChangeEvent -= OnHandTypeChanged;
            data.BehaviourChangeEvent -= OnBehaviorChanged;
            data.BeatLengthChangeEvent -= OnSustainLengthChanged;
            data.VelocityChangeEvent -= OnVelocityChanged;
            data.TickChangeEvent -= OnTickChanged;
            data = null;
            target = null;
            isAnimatingSustain = false;
            
            note.transform.localPosition = Vector3.zero;
            note.transform.rotation = Quaternion.identity;
            note.transform.localScale = Vector3.one * .728f;
        }

        public void ReplaceData(TargetData newData)
        {
            data.HandTypeChangeEvent -= OnHandTypeChanged;
            data.BehaviourChangeEvent -= OnBehaviorChanged;
            data.BeatLengthChangeEvent -= OnSustainLengthChanged;
            data.VelocityChangeEvent -= OnVelocityChanged;
            data = newData;

            newData.HandTypeChangeEvent += OnHandTypeChanged;
            newData.BehaviourChangeEvent += OnBehaviorChanged;
            newData.BeatLengthChangeEvent += OnSustainLengthChanged;
            newData.VelocityChangeEvent += OnVelocityChanged;
        }

        public void EnableSelected(TargetBehavior behavior)
        {
            selection.enabled = true;
            isSelected = true;

            if (location == TargetIconLocation.Grid)
            {
                chainConnector.enabled = true;
            }
        }

        public void DisableSelected()
        {
            selection.enabled = false;
            isSelected = false;
        }


        public void SetOutlineColor(Color color)
        {
            selection.color = color;
        }

        private bool updatingColors = false;
        public void UpdateColors()
        {
            updatingColors = true;
            OnHandTypeChanged(data.handType);
        }

        private void SetHitsoundIcon(InternalTargetVelocity velocity)
        {
            var property = AssetContainer.GetHitsoundProperty(velocity, data.handType);
            hitsoundDisplay.sprite = property.sprite;
            hitsoundDisplay.SetPropertyBlock(property.block);
        }

        private void OnVelocityChanged(InternalTargetVelocity oldVelocity, InternalTargetVelocity velocity)
        {
            if (location != TargetIconLocation.Grid)
                return;

            if (oldVelocity != velocity)
            {
                SetHitsoundIcon(velocity);
            }
            
            Vector2 pos = Vector3.one;
            Vector3 scale = Vector3.one * .15f;
            switch (data.behavior)
            {
                case TargetBehavior.Sustain:
                case TargetBehavior.ChainStart:
                    pos *= .75f;
                    break;
                case TargetBehavior.ChainNode:
                    pos *= .5f;
                    break;
                case TargetBehavior.Melee:
                    pos *= 1.75f;
                    scale = Vector3.one * .25f;
                    break;
                default:
                    break;
            }
            hitsoundDisplay.transform.localPosition = pos;
            hitsoundDisplay.transform.localScale = scale;
            hitsoundDisplay.gameObject.SetActive(NRSettings.config.enableGridHitsoundIcons);
        }

        private void OnHandTypeChanged(TargetHandType handType)
        {
            if (location == TargetIconLocation.Timeline)
            {
                if (data.supportsBeatLength)
                {
                    sustainController.EnableSustain(handType, true);
                }

                switch (handType)
                {
                    case TargetHandType.Left:
                        //sustainDirection = .6f;
                        sustainDirection = Mathf.Abs(sustainDirection);
                        //transform.localPosition += Vector3.up * timelineSpread;
                        break;
                    case TargetHandType.Right:
                        //sustainDirection = -.6f;
                        sustainDirection = Mathf.Abs(sustainDirection) * -1f;
                        //transform.localPosition += Vector3.down * timelineSpread;
                        break;
                    default:
                        Vector3 newPos = new Vector3(transform.localPosition.x, 0f, transform.localPosition.z); // Resets y offset
                        //transform.localPosition = newPos; 
                        break;
                }
            }
            
            UpdateVisuals(AssetContainer.GetVisualProperties(data.behavior, data.handType, location));

            updatingColors = false;
        }

        private void OnSustainLengthChanged(QNT_Duration beatLength)
        {
            UpdateTimelineSustainLength();
        }

        public void IncreaseBeatLength()
        {
            QNT_Duration increment = EditorBeatSnap.Duration;
            QNT_Duration targetLength = data.beatLength;
            if (targetLength < increment)
            {
                targetLength = new QNT_Duration(0);
            }
            targetLength += increment;

            if (targetLength == data.beatLength)
                return;

            QNT_Timestamp endTime = new QNT_Timestamp(data.time.tick + targetLength.tick);
            if (data.isRepeaterTarget)
            {

                if (!data.repeaterData.Section.Contains(endTime))
                {
                    NotificationCenter.SendNotification("Can't change beat length: target would be outside of repeater zone.", NotificationType.Warning);
                    return;
                }
            }
            else
            {
                if (Timeline.Instance.repeaterManager.IsTargetInRepeaterZone(endTime))
                {
                    NotificationCenter.SendNotification("Can't change beat length: target would cross into a repater zone.", NotificationType.Warning);
                    return;
                }
            }
            
            target.UpdateSustainLength(true);
            

        }

        public void DescreseBeatLength()
        {
            target.UpdateSustainLength(false);
        }

        public void UpdateTimelineSustainLength()
        {
            if (!data.supportsBeatLength || location != TargetIconLocation.Timeline)
            {
                return;
            }
            
            QNT_Duration beatLength = data.isPathbuilderTarget ? EditorTargets.IsSimplePathbuilderTarget(target) ? 
                data.pathbuilderData.SimpleData.beatLength : 
                data.pathbuilderData.BeatLength : 
                data.beatLength;

            sustainController.EnableSustain(data.handType, true);
            sustainController.SetBeatLength(beatLength);
        }

        public void MakeSustainIndicatorTransparent(bool transparent)
        {
            sustainController.SetTransparent(transparent);

            if (transparent)
            {
                UpdateTimelineSustainLength();
            }
        }

        public void SetBeatlengthLineActive(bool active)
        {
            //beatLengthLine.SetActive(active);
            sustainController.EnableSustain(data.handType, active);
            if (active)
            {
                UpdateTimelineSustainLength();
            }
            else
            {
                target.DisableSustainButtons();
            }
        }

        private void OnBehaviorChanged(TargetBehavior oldbehavior, TargetBehavior behavior)
        {
            ResetSpriteTransforms();
            UpdateVisuals(AssetContainer.GetVisualProperties(data.behavior, data.handType, location));

            if (location == TargetIconLocation.Timeline)
            {
                //beatLengthLine.SetActive(data.supportsBeatLength);
                sustainController.EnableSustain(data.handType, data.supportsBeatLength);
                transform.localScale = Vector3.one * (timelineTargetSize * 0.4f);
                collisionRadiusClick = collisionRadiusDrag = 0.50f;
            }
            else
            {
                transform.localScale = Vector3.one * (timelineTargetSize * 0.4f);
            }


            if (behavior == TargetBehavior.ChainNode && location == TargetIconLocation.Timeline)
            {
                collisionRadiusClick = 0.2f;
            }
            else if (behavior == TargetBehavior.Melee && location == TargetIconLocation.Grid)
            {
                collisionRadiusClick = collisionRadiusDrag = 1.7f;
            }

            if (location == TargetIconLocation.Grid)
            {
                chainConnector.enabled = behavior is TargetBehavior.ChainNode or TargetBehavior.ChainStart;

                if (behavior == TargetBehavior.Melee || oldbehavior == TargetBehavior.Melee)
                    OnVelocityChanged(data.velocity, data.velocity);
            }

            //Timeline.instance.ReapplyScale();
            if (location == TargetIconLocation.Timeline) transform.localScale = EditorScale.GetNoteScale(transform.localScale);
            UpdateTimelineSustainLength();
        }

        private void ResetSpriteTransforms()
        {
            note.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            if (prefade != null) prefade.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            if (ring != null) ring.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            selection.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
            if (location == TargetIconLocation.Grid)
            {
                note.transform.localScale = Vector3.one * 0.728f;
                selection.transform.localScale = Vector3.one * 0.5414f;
                ring.transform.localScale = Vector3.one * 0.728f;
            }
            else
            {
                note.transform.localScale = Vector3.one * 0.657f;
                selection.transform.localScale = Vector3.one * 0.276f;
            }
        }

        private void OnTickChanged(QNT_Timestamp newTime, QNT_Timestamp oldTime) => SetupFade();


        /* private void UpdateRendererProperties(AssetContainer.TargetProperties properties)
         {
             if (location == TargetIconLocation.Timeline)
             {
                 var timelineBlock = new MaterialPropertyBlock();
                 note.GetPropertyBlock(timelineBlock);
                 timelineBlock.SetColor("_Color", AssetContainer.GetColorForTarget(data.behavior, data.handType));
                 timelineBlock.SetTexture("_MainTex", note.sprite.texture);
                 note.SetPropertyBlock(timelineBlock);
                 sustainLineRenderer.SetPropertyBlock(block);
             }
             else
             {
                 block.SetTexture("_MainTex", note.sprite.texture);
                 note.SetPropertyBlock(block);
                 block.SetTexture("_MainTex", ring.sprite.texture);
                 ring.SetPropertyBlock(block);
                 chainConnector.SetPropertyBlock(block);
                 block = AssetContainer.GetPrefadePropertyBlockForBehavior(prefade, data.behavior);
                 block.SetTexture("_MainTex", prefade.sprite.texture);
                 prefade.SetPropertyBlock(block);
             }
         }*/

        private void SetupFade()
        {
            if (location != TargetIconLocation.Grid) return;

            if (data.behavior == TargetBehavior.ChainNode)
            {
                NoteEnumerator iter = new NoteEnumerator(new QNT_Timestamp(0), data.time);
                iter.reverse = true;
                foreach (Target t in iter)
                {
                    if (t.data.behavior == TargetBehavior.ChainStart && t.data.handType == data.handType)
                    {
                        chainConnector.enabled = true;
                        float offset = t.data.time.ToBeatTime() - data.time.ToBeatTime();
                        UpdateFade(offset, offset);
                        break;
                    }
                }
            }
            else if (data.behavior.IsMelee())
            {
                float worldPos = (transform.position.z - 10) * -1;
                UpdateFade(0, 0);
            }
        }

        public void HideTelegraph(bool hide) => prefade.enabled = !hide;

        public void ConnectChain(Target nextTarget, Target chainStart)
        {
            chainConnector.enabled = true;
            chainConnector.SetPosition(0, transform.position);
            chainConnector.SetPosition(1, nextTarget.gridTargetIcon.transform.position);
            float offset = chainStart.data.time.ToBeatTime() - data.time.ToBeatTime();
            float worldPos = (transform.position.z - 10) * -1;
            UpdateFade(chainConnector, worldPos, offset);
        }

        private void UpdateFade(float worldPos, float offset)
        {
            if (location is TargetIconLocation.Timeline) return;
            
            note.material.SetFloat("_OpaqueDuration", 1 + -offset);
            note.material.SetFloat("_WorldPosOffset", worldPos);
            
            prefade.material.SetFloat("_OpaqueDuration", 1 + -offset);
            prefade.material.SetFloat("_WorldPosOffset", worldPos);
                
            ring.material.SetFloat("_OpaqueDuration", 1 + -offset);
            ring.material.SetFloat("_WorldPosOffset", worldPos);
            
            hitsoundDisplay.material.SetFloat("_OpaqueDuration", 1 + -offset);
            hitsoundDisplay.material.SetFloat("_WorldPosOffset", worldPos);
                
            chainConnector.material.SetFloat("_OpaqueDuration", 1 + -offset);
            chainConnector.material.SetFloat("_OpaqueDuration", worldPos);

            /*UpdateFade(note, worldPos, offset);
            UpdateFade(ring, worldPos, offset);
            UpdateFade(prefade, worldPos, offset);
            UpdateFade(hitsoundDisplay, worldPos, offset);
            UpdateFade(chainConnector, worldPos, offset);*/
        }

        private void UpdateFade(Renderer renderer, float worldPos, float offset)
        {
            MaterialPropertyBlock block = new();
            renderer.GetPropertyBlock(block);
            block = UpdateFade(block, worldPos, offset);
            renderer.SetPropertyBlock(block);
        }

        private MaterialPropertyBlock UpdateFade(MaterialPropertyBlock block, float worldPos, float offset)
        {
            block.SetFloat("_WorldPosOffset", worldPos);
            block.SetFloat("_OpaqueDuration", 1 + -offset);
            return block;
        }

        public void DisableChainConnector()
            => chainConnector.enabled = false;

        public void ResetChainConnector()
        {
            chainConnector.SetPosition(0, Vector3.zero);
            chainConnector.SetPosition(1, Vector3.zero);
        }

        public bool IsCloseToPoint(Vector2 point)
        {
            Vector2 center = transform.TransformPoint(0, 0, 0);
            float collisionRad = transform.TransformVector(collisionRadiusClick, 0, 0).x;
            return (point - center).sqrMagnitude < collisionRad * collisionRad;
        }

        public bool IsInsideRect(Rect rect)
        {
            Vector2 center = transform.TransformPoint(0, 0, 0);
            if (location == TargetIconLocation.Timeline) center.y = rect.center.y;
            Vector2 closestPoint = center;
            closestPoint.x = Mathf.Clamp(closestPoint.x, rect.min.x, rect.max.x);
            closestPoint.y = Mathf.Clamp(closestPoint.y, rect.min.y, rect.max.y);
            float collisionRad = transform.TransformVector(collisionRadiusDrag, 0, 0).x;
            return (closestPoint - center).sqrMagnitude < collisionRad * collisionRad;
        }

        public bool IsInValidTime(QNT_Timestamp time)
        {
            QNT_Duration loadedDuration = Constants.QuarterNoteDuration + Constants.EighthNoteDuration;
            if (location == TargetIconLocation.Grid && Math.Abs((time - target.data.time).tick) > (long)loadedDuration.tick)
            {
                return false;
            }

            return true;
        }

        internal void StartAnimateSustain()
        {
            StartCoroutine(AnimateSustain());
        }
        internal void StopAnimatingSustain()
        {
            stopAnimating = true;
            StopCoroutine(AnimateSustain());
        }

        internal bool updateAnimation = false;
        internal bool stopAnimating = false;
        private bool isAnimatingSustain = false;
        private IEnumerator CheckProximity()
        {
            var waitTime = new WaitForEndOfFrame();
            //Relative_QNT offset = new((long)Constants.QuarterNoteDuration.tick);
            while (true)
            {
                if (stopAnimating)
                {
                    GridParticles.StopEmitSustain(target);
                    stopAnimating = false;
                    yield break;
                }
                if (!isAnimatingSustain)
                {
                    //var start = data.time - offset;
                    //var end = data.time + data.beatLength + offset;
                    if (EditorTime.Time >= data.time && EditorTime.Time <= (data.time + data.beatLength))
                    {
                        ResetAnimationVisuals();
                        isAnimatingSustain = true;
                        StartAnimateSustain();
                    }
                }
                else
                {
                    if (EditorTime.Time < data.time || EditorTime.Time > (data.time + data.beatLength))
                    {
                        isAnimatingSustain = false;
                    }
                }


                yield return waitTime;
            }
        }
        internal void StartCheckProximity()
        {
            StartCoroutine(CheckProximity());
        }
        internal void StopCheckProximity()
        {
            StopCoroutine(CheckProximity());
            GridParticles.StopEmitSustain(target);
        }

        internal void KillSustainAnimation()
        {
            StopCheckProximity();
            StopAnimatingSustain();
            ResetAnimationVisuals();
            GridParticles.StopEmitSustain(target);
        }

        internal void ResetAnimationVisuals()
        {
            if (!NRSettings.config.enableSustainAnimation || isAnimatingSustain) return;
            if (data.behavior != TargetBehavior.Sustain) return;

            var startTime = data.time;
            var endTime = data.time + data.beatLength;
            var startRotation = Quaternion.identity;
            var startPosition = transform.position;
            var targetPosition = startPosition;
            targetPosition.z = endTime.ToBeatTime();

            var startScale = Vector3.one * .7f;
            var targetScale = Vector3.one * .3f;
            float duration = endTime.ToBeatTime() - startTime.ToBeatTime();
            float zRotation = data.handType == TargetHandType.Right ? 180f * Mathf.Clamp(duration, 1f, Mathf.Infinity) : 180f * Mathf.Clamp(duration, 1f, Mathf.Infinity) * -1;
            float currentTime = EditorTime.Time.ToBeatTime();
            float percentage = (currentTime - startTime.ToBeatTime()) / duration;

            note.transform.position = Vector3.Lerp(startPosition, targetPosition, percentage);
            note.transform.rotation = Quaternion.Euler(startRotation.x, startRotation.y, Mathf.SmoothStep(startRotation.z, zRotation, percentage));
            note.transform.localScale = Vector3.Lerp(startScale, targetScale, percentage);
        }

        private IEnumerator AnimateSustain()
        {
            var startTime = data.time;
            var endTime = data.time + data.beatLength;
            var startRotation = Quaternion.identity;
            var startPosition = transform.position;
            var targetPosition = startPosition;
            var startScale = Vector3.one * .7f;
            var targetScale = Vector3.one * .3f;
            GridParticles.StartEmitSustain(target);
            //gridTargetIcon.holdEndTrans.gameObject.SetActive(true);
            while (EditorTime.Time >= startTime && EditorTime.Time <= endTime)
            {
                //update start and end time so it still animates correctly if we change beatlength / move the target on the timeline
                if (stopAnimating || data == null)
                {
                    GridParticles.StopEmitSustain(target);
                    yield break;
                }
                startTime = data.time;
                endTime = data.time + data.beatLength;

                targetPosition.z = endTime.ToBeatTime();

                float duration = endTime.ToBeatTime() - startTime.ToBeatTime();

                float zRotation = data.handType == TargetHandType.Right ? 180f * Mathf.Clamp(duration, 1f, Mathf.Infinity) : 180f * Mathf.Clamp(duration, 1f, Mathf.Infinity) * -1f;
                float currentTime = EditorTime.Time.ToBeatTime();

                float percentage = (currentTime - startTime.ToBeatTime()) / duration;

                note.transform.position = Vector3.Lerp(startPosition, targetPosition, percentage);
                note.transform.rotation = Quaternion.Euler(startRotation.x, startRotation.y, Mathf.SmoothStep(startRotation.z, zRotation, percentage));
                note.transform.localScale = Vector3.Lerp(startScale, targetScale, percentage);

                if (updateAnimation)
                {
                    GridParticles.StartEmitSustain(target);
                    updateAnimation = false;
                    startTime = data.time;
                    endTime = data.time + data.beatLength;
                    startPosition = transform.position;
                    targetPosition = startPosition;
                    targetPosition.z = endTime.ToBeatTime();
                }
                yield return null;
            }
            GridParticles.StopEmitSustain(target);
            if (EditorTime.Time < startTime)
            {
                note.transform.position = startPosition;
                note.transform.rotation = startRotation;
                note.transform.localScale = startScale;
            }
            /*if (updateAnimation)
            {
                updateAnimation = false;
                Timeline.instance.StartCoroutine(AnimateSustain());
            }*/
            yield return null;
        }
    }
}