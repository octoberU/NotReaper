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

        [Header("Main sprites")]
        public Sprite standard;
        public Sprite hold;
        public Sprite horizontal;
        public Sprite vertical;
        public Sprite chainStart;
        public Sprite chain;
        public Sprite melee;
        public Sprite mine;
        public Sprite legacyPathbuilder;
        public Sprite none;

        [Space, Header("Ring sprites")]
        public Sprite standardRing;
        public Sprite holdRing;
        public Sprite horizontalRing;
        public Sprite verticalRing;
        public Sprite chainStartRing;
        public Sprite chainRing;
        public Sprite meleeRing;
        public Sprite mineRing;
        public Sprite noneRing;

        [Space, Header("Telegraph sprites")]
        public Sprite standardTelegraph;
        public Sprite holdTelegraph;
        public Sprite horizontalTelegraph;
        public Sprite verticalTelegraph;
        public Sprite chainStartTelegraph;
        public Sprite chainTelegraph;
        public Sprite meleeTelegraph;
        public Sprite mineTelegraph;
        public Sprite noneTelegraph;

        [Space, Header("Telegraph noise")]
        public Texture standardNoise;
        public Texture fractureTelegraph;

        [Space, Header("Select ring sprites")]
        public Sprite standardSelect;
        public Sprite holdSelect;
        public Sprite horizontalSelect;
        public Sprite verticalSelect;
        public Sprite chainStartSelect;
        public Sprite chainSelect;
        public Sprite meleeSelect;
        public Sprite mineSelect;
        public Sprite noneSelect;

        //public GameObject beatLengthLine;
        public GameObject pathBuilder;

        [Space, Header("Shared Renderer")]
        [SerializeField] internal SpriteRenderer note;
        [Space, Header("Renderer Grid")]
        [SerializeField] SpriteRenderer prefade;
        [SerializeField] SpriteRenderer ring;
        [SerializeField] private SpriteRenderer legacyStamp;
        [SerializeField] private LineRenderer chainConnector;
        [Space, Header("Rendrer Timeline")]
        //[SerializeField] private LineRenderer sustainLine;

        public SpriteRenderer selection;

        [SerializeField] float timelineSpread = 1.5f;

        public float targetSize = 1f;
        public float timelineTargetSize = 1f;


        public TargetData data;
        public Target target;

        public float sustainDirection = 0.6f;

        [SerializeField] private float collisionRadiusClick = 0.95f;
        [SerializeField] private float collisionRadiusDrag = 0.95f;
        public bool isSelected = false;
        public TargetIconLocation location;

        public GameObject sustainButtons;

        public Transform holdEndTrans;

        [Space, Header("Hitsound Icons")]
        [SerializeField] private SpriteRenderer hitsoundDisplay;
        [SerializeField] private Sprite iconKick;
        [SerializeField] private Sprite iconSnare;
        [SerializeField] private Sprite iconPercussion;
        [SerializeField] private Sprite iconChainStart;
        [SerializeField] private Sprite iconChain;
        [SerializeField] private Sprite iconMelee;
        [SerializeField] private Sprite iconSilent;


        [Space, Header("Sustain")]
        [SerializeField] private BeatLine sustainController;

        public bool SustainButtonsActive => sustainButtons.activeSelf;

        private List<Renderer> renderers = new();
        private List<LineRenderer> lineRenderers = new();
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
            data.HandTypeChangeEvent += OnHandTypeChanged;
            data.BehaviourChangeEvent += OnBehaviorChanged;
            data.BeatLengthChangeEvent += OnSustainLengthChanged;
            data.VelocityChangeEvent += OnVelocityChanged;
            data.TickChangeEvent += OnTickChanged;
            this.target = target;

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

            foreach (var r in renderers)
            {
                r.material.SetFloat("_FadeThreshold", 1.7f);
                r.material.SetFloat("_OpaqueDuration", 1f);
                r.material.SetFloat("_FadeOutThreshold", 0.5f);
                r.material.SetFloat("_WorldPosOffset", 0f);
            }

            SetupFade();
        }

        private void CacheReferences()
        {
            if (hasCachedReferences) return;
            hasCachedReferences = true;
            canvas = sustainButtons.GetComponent<Canvas>();
            renderers = GetComponentsInChildren<Renderer>(true).ToList();
            lineRenderers = GetComponentsInChildren<LineRenderer>().ToList();
        }
        

        public void ClearData()
        {
            data.HandTypeChangeEvent -= OnHandTypeChanged;
            data.BehaviourChangeEvent -= OnBehaviorChanged;
            data.BeatLengthChangeEvent -= OnSustainLengthChanged;
            data.VelocityChangeEvent -= OnVelocityChanged;
            data.TickChangeEvent -= OnTickChanged;
            data = null;
            target = null;
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
                foreach (LineRenderer l in gameObject.GetComponentsInChildren<LineRenderer>(true))
                {
                    l.enabled = true;
                }
            }
        }

        public void DisableSelected()
        {
            selection.enabled = false;

            isSelected = false;
            if (location == TargetIconLocation.Grid)
            {
                foreach (LineRenderer l in gameObject.GetComponentsInChildren<LineRenderer>(true))
                {
                    if (l.name == "ChainConnector") continue;

                    l.enabled = false;
                }
            }
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

        public void UpdateHitsoundIcon() => OnVelocityChanged(data.velocity, data.velocity);

        private void SetHitsoundIcon(InternalTargetVelocity velocity)
        {
            switch (velocity)
            {
                case InternalTargetVelocity.Kick:
                    hitsoundDisplay.sprite = iconKick;
                    break;
                case InternalTargetVelocity.Snare:
                    hitsoundDisplay.sprite = iconSnare;
                    break;
                case InternalTargetVelocity.Percussion:
                    hitsoundDisplay.sprite = iconPercussion;
                    break;
                case InternalTargetVelocity.ChainStart:
                    hitsoundDisplay.sprite = iconChainStart;
                    break;
                case InternalTargetVelocity.Chain:
                    hitsoundDisplay.sprite = iconChain;
                    break;
                case InternalTargetVelocity.Melee:
                    hitsoundDisplay.sprite = iconMelee;
                    break;
                case InternalTargetVelocity.Silent:
                    hitsoundDisplay.sprite = iconSilent;
                    break;
                default:
                    hitsoundDisplay.sprite = null;
                    break;
            }
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
                note.color = handType == TargetHandType.Left ? NRSettings.config.leftColor :
                    handType == TargetHandType.Right ? NRSettings.config.rightColor :
                    handType == TargetHandType.Either ? UserPrefsManager.bothColor :
                    UserPrefsManager.neitherColor;

                if (data.supportsBeatLength)
                {
                    sustainController.EnableSustain(handType, true);
                }
            }
            else
            {
                foreach (Renderer r in gameObject.GetComponentsInChildren<Renderer>(true))
                {

                    if (r.name == "WhiteRing") continue;

                    switch (handType)
                    {
                        case TargetHandType.Left:
                            r.material.SetColor("_Tint", NRSettings.config.leftColor);
                            break;
                        case TargetHandType.Right:
                            r.material.SetColor("_Tint", NRSettings.config.rightColor);
                            break;
                        case TargetHandType.Either:
                            r.material.SetColor("_Tint", (data.behavior == TargetBehavior.Mine ? Color.red : UserPrefsManager.bothColor));
                            break;
                        default:
                            r.material.SetColor("_Tint", UserPrefsManager.neitherColor);
                            break;
                    }
                }
            }
            foreach (LineRenderer l in gameObject.GetComponentsInChildren<LineRenderer>(true))
            {
                if (data.behavior == TargetBehavior.Legacy_Pathbuilder)
                {
                    handType = data.legacyPathbuilderData.handType;
                }

                if (l.name == "ChainConnector")
                {
                    l.material.SetColor("_Tint", handType == TargetHandType.Left ? NRSettings.config.leftColor :
                        handType == TargetHandType.Right ? NRSettings.config.rightColor :
                        handType == TargetHandType.Either ? UserPrefsManager.bothColor :
                        UserPrefsManager.neitherColor);
                    continue;
                }

                switch (handType)
                {
                    case TargetHandType.Left:
                        l.startColor = NRSettings.config.leftColor;
                        l.endColor = NRSettings.config.leftColor;
                        sustainDirection = 0.6f;
                        if (location == TargetIconLocation.Timeline && !updatingColors) transform.localPosition += Vector3.up * timelineSpread;
                        break;
                    case TargetHandType.Right:
                        l.startColor = NRSettings.config.rightColor;
                        l.endColor = NRSettings.config.rightColor;
                        sustainDirection = -0.6f;
                        if (location == TargetIconLocation.Timeline && !updatingColors) transform.localPosition += Vector3.down * timelineSpread;
                        break;
                    case TargetHandType.Either:
                        l.startColor = UserPrefsManager.bothColor;
                        l.endColor = UserPrefsManager.bothColor;
                        sustainDirection = 0.6f;
                        if (location == TargetIconLocation.Timeline)
                        {
                            Vector3 newPos = new Vector3(transform.localPosition.x, 0f, transform.localPosition.z); // Resets y offset
                            transform.localPosition = newPos;
                        }
                        break;
                    default:
                        l.startColor = UserPrefsManager.neitherColor;
                        l.endColor = UserPrefsManager.neitherColor;
                        sustainDirection = 0.6f;
                        break;
                }

                /*if (data.supportsBeatLength && l.positionCount >= 3)
                {
                    l.SetPosition(1, new Vector3(0.0f, sustainDirection, 0.0f));
                    var pos2 = l.GetPosition(2);
                    l.SetPosition(2, new Vector3(pos2.x, sustainDirection, pos2.z));
                }*/


            }

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

        private int GetInterval(int currentInterval, bool increase)
        {
            List<int> intervals = new List<int>();
            foreach (string s in NRSettings.config.snaps)
            {
                string temp = s;
                int snap = 4;
                int.TryParse(temp.Substring(2), out snap);
                intervals.Add(snap);
            }
            int index = intervals.IndexOf(currentInterval);
            if (index == intervals.Count - 1 && increase) return currentInterval;
            else if (index == 0 && !increase) return currentInterval;
            else if (increase) return intervals[index + 1];
            else return intervals[index - 1];
        }

        public void UpdateTimelineSustainLength()
        {
            if (!data.supportsBeatLength || location != TargetIconLocation.Timeline)
            {
                return;
            }
            float scale = EditorScale.InvertedScaleAmount;//20.0f / Timeline.scale;
            QNT_Duration beatLength = data.isPathbuilderTarget ? EditorTargets.IsSimplePathbuilderTarget(target) ? data.pathbuilderData.SimpleData.beatLength : data.pathbuilderData.BeatLength : data.beatLength;


            sustainController.EnableSustain(data.handType, true);
            sustainController.SetBeatLength(beatLength);

            /*
            sustainLine.SetPosition(0, new Vector3(0.0f, 0.0f, 0.0f));
            sustainLine.SetPosition(1, new Vector3(0.0f, sustainDirection, 0.0f));
            sustainLine.SetPosition(2, new Vector3((beatLength.ToBeatTime() / 0.7f) * scale * 1.75f, sustainDirection, 0.0f)); //was * 1.32f
            */
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

            UpdateSpriteForBehavior(behavior);

            if (pathBuilder != null) pathBuilder.SetActive(behavior == TargetBehavior.Legacy_Pathbuilder);

            if (location == TargetIconLocation.Timeline)
            {
                //beatLengthLine.SetActive(data.supportsBeatLength);
                sustainController.EnableSustain(data.handType, data.supportsBeatLength);
                transform.localScale = Vector3.one * timelineTargetSize * 0.4f;
                collisionRadiusClick = collisionRadiusDrag = 0.50f;
            }
            else
            {
                transform.localScale = Vector3.one * timelineTargetSize * 0.4f;
            }


            if (behavior == TargetBehavior.ChainNode && location == TargetIconLocation.Timeline)
            {
                collisionRadiusClick = 0.2f;
            }
            else if (behavior == TargetBehavior.Melee && location == TargetIconLocation.Grid)
            {
                collisionRadiusClick = collisionRadiusDrag = 1.7f;
            }

            if (behavior == TargetBehavior.Legacy_Pathbuilder)
            {
                data.velocity = InternalTargetVelocity.Silent;
            }
            if (location == TargetIconLocation.Grid)
            {
                if (behavior == TargetBehavior.ChainNode || behavior == TargetBehavior.ChainStart)
                {
                    chainConnector.enabled = true;
                }
                else
                {
                    chainConnector.enabled = false;
                }
            }

            //Timeline.instance.ReapplyScale();
            if (location == TargetIconLocation.Timeline) transform.localScale = EditorScale.GetNoteScale(transform.localScale);
            UpdateTimelineSustainLength();
        }

        private void UpdateSpriteForBehavior(TargetBehavior behavior)
        {
            if (location == TargetIconLocation.Grid)
            {
                legacyStamp.sprite = null;
            }

            switch (behavior)
            {
                case TargetBehavior.Standard:
                    note.sprite = standard;
                    if (prefade != null) prefade.sprite = standardTelegraph;
                    if (ring != null) ring.sprite = standardRing;
                    selection.sprite = standardSelect;
                    break;
                case TargetBehavior.Sustain:
                    note.sprite = hold;
                    if (prefade != null) prefade.sprite = holdTelegraph;
                    if (ring != null) ring.sprite = holdRing;
                    selection.sprite = holdSelect;
                    break;
                case TargetBehavior.Horizontal:
                    note.sprite = horizontal;
                    if (prefade != null) prefade.sprite = horizontalTelegraph;
                    if (ring != null) ring.sprite = horizontalRing;
                    selection.sprite = horizontalSelect;

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
                    note.sprite = vertical;
                    if (prefade != null) prefade.sprite = verticalTelegraph;
                    if (ring != null) ring.sprite = verticalRing;
                    selection.sprite = verticalSelect;


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
                case TargetBehavior.ChainStart:
                    note.sprite = chainStart;
                    if (prefade != null) prefade.sprite = chainStartTelegraph;
                    if (ring != null) ring.sprite = chainStartRing;
                    selection.sprite = chainStartSelect;
                    break;
                case TargetBehavior.ChainNode:
                    note.sprite = chain;
                    if (prefade != null) prefade.sprite = chainTelegraph;
                    if (ring != null) ring.sprite = chainRing;
                    selection.sprite = chainSelect;
                    if (location == TargetIconLocation.Timeline) note.transform.localScale = Vector3.one * 0.2f;
                    break;
                case TargetBehavior.Melee:
                    note.sprite = melee;
                    if (prefade != null) prefade.sprite = meleeTelegraph;
                    if (ring != null) ring.sprite = meleeRing;
                    selection.sprite = meleeSelect;
                    if (location == TargetIconLocation.Grid)
                    {
                        note.transform.localScale = Vector3.one * 1.5f;
                        selection.transform.localScale = Vector3.one * 1.25f;
                        ring.transform.localScale = Vector3.one * 1.5f;
                    }
                    break;
                case TargetBehavior.Mine:
                    note.sprite = mine;
                    if (prefade != null) prefade.sprite = mineTelegraph;
                    if (ring != null) ring.sprite = mineRing;
                    selection.sprite = mineSelect;
                    break;

                case TargetBehavior.Legacy_Pathbuilder:
                    if (prefade != null) prefade.sprite = null;
                    if (ring != null) ring.sprite = null;
                    if(location == TargetIconLocation.Grid)
                    {
                        legacyStamp.sprite = legacyPathbuilder;
                        legacyStamp.transform.localScale = Vector3.one * .7f;
                    }
                    break;


                default:
                    break;
            }

            foreach (Renderer r in gameObject.GetComponentsInChildren<Renderer>(true))
            {
                if (r.name == "Prefade")
                {

                    switch (behavior)
                    {
                        case TargetBehavior.Standard:
                            r.material.SetTexture("Texture2D_EFB53AD2", standardNoise);
                            r.material.SetFloat("Vector1_6D268C6B", 2.1f);
                            break;

                        case TargetBehavior.Sustain:
                            r.material.SetTexture("Texture2D_EFB53AD2", fractureTelegraph);
                            r.material.SetFloat("Vector1_6D268C6B", 1f);
                            break;

                        case TargetBehavior.Horizontal:
                            r.material.SetTexture("Texture2D_EFB53AD2", standardNoise);
                            r.material.SetFloat("Vector1_6D268C6B", 2.1f);
                            break;

                        case TargetBehavior.Vertical:
                            r.material.SetTexture("Texture2D_EFB53AD2", standardNoise);
                            r.material.SetFloat("Vector1_6D268C6B", 2.1f);
                            break;

                        case TargetBehavior.ChainStart:
                            r.material.SetTexture("Texture2D_EFB53AD2", fractureTelegraph);
                            r.material.SetFloat("Vector1_6D268C6B", 1f);
                            break;

                        default:
                            r.material.SetTexture("Texture2D_EFB53AD2", standardNoise);
                            r.material.SetFloat("Vector1_6D268C6B", 2.1f);
                            break;
                    }
                }
            }
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

        private void OnTickChanged(QNT_Timestamp newTime, QNT_Timestamp oldTime)
        {
            SetupFade();
        }


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
                        foreach (var r in renderers)
                        {
                            float offset = t.data.time.ToBeatTime() - data.time.ToBeatTime();
                            r.material.SetFloat("_WorldPosOffset", offset);
                            r.material.SetFloat("_OpaqueDuration", 1 + (-offset));
                        }

                        break;
                    }
                }
            }
        }

        public void HideTelegraph(bool hide) => prefade.enabled = !hide;

        public void UpdatePath()
        {
            if (data.behavior != TargetBehavior.Legacy_Pathbuilder || location != TargetIconLocation.Grid)
            {
                return;
            }

            if (data.legacyPathbuilderData.parentNotes.Count == 0)
            {
                return;
            }

            foreach (var l in lineRenderers)
            {
                switch (data.legacyPathbuilderData.handType)
                {
                    case TargetHandType.Left:
                        l.startColor = NRSettings.config.leftColor;
                        l.endColor = NRSettings.config.leftColor;
                        break;
                    case TargetHandType.Right:
                        l.startColor = NRSettings.config.rightColor;
                        l.endColor = NRSettings.config.rightColor;
                        break;
                    case TargetHandType.Either:
                        l.startColor = UserPrefsManager.bothColor;
                        l.endColor = UserPrefsManager.bothColor;
                        break;
                    default:
                        l.startColor = UserPrefsManager.neitherColor;
                        l.endColor = UserPrefsManager.neitherColor;
                        break;
                }

                int count = data.legacyPathbuilderData.generatedNotes.Count / data.legacyPathbuilderData.parentNotes.Count;

                Vector3[] positions = new Vector3[count];

                for (int i = 0; i < count; ++i)
                {
                    var note = data.legacyPathbuilderData.generatedNotes[i];
                    positions[i] = new Vector3(note.x, note.y, transform.position.z);
                }

                l.positionCount = positions.Length;
                l.SetPositions(positions);
            }
        }

        public void UpdatePathInitialAngle(float angle)
        {
            if (data.behavior != TargetBehavior.Legacy_Pathbuilder)
            {
                return;
            }


            UpdatePath();
        }

        public void ConnectChain(Target nextTarget, Target chainStart)
        {
            chainConnector.enabled = true;
            chainConnector.SetPosition(0, transform.position);
            chainConnector.SetPosition(1, nextTarget.gridTargetIcon.transform.position);
            float offset = chainStart.data.time.ToBeatTime() - data.time.ToBeatTime();
            float worldPos = (transform.position.z - 10) * -1;
            chainConnector.material.SetFloat("_WorldPosOffset", worldPos);
            chainConnector.material.SetFloat("_OpaqueDuration", 1 + (-offset));
        }

        public void DisableChainConnector()
            => chainConnector.enabled = false;

        public bool IsCloseToPoint(Vector2 point)
        {
            Vector2 center = transform.TransformPoint(0, 0, 0);
            float collisionRad = transform.TransformVector(collisionRadiusClick, 0, 0).x;
            return (point - center).sqrMagnitude < collisionRad * collisionRad;
        }

        public bool IsInsideRect(Rect rect)
        {
            Vector2 center = transform.TransformPoint(0, 0, 0);
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