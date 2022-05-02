using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper.UserInput;
using NotReaper;
using NotReaper.Timing;
using NotReaper.Targets;
using NotReaper.Models;
using NotReaper.Grid;
using NotReaper.UI;
using System;
using System.Linq;
using UnityEngine.InputSystem;
using Mathf = UnityEngine.Mathf;
using NotReaper.Notifications;

namespace NotReaper.Tools.SpacingSnap
{
    public class SpacingSnapper : NRInput<SpacingSnapKeybinds>
    {

        [SerializeField] private HoverTarget hover;
        [SerializeField] private GameObject orbit;


        private Target nearestTarget;
        private Camera cam;
        private TrailRenderer trail;


        private float radius = 1f;
        private float radiusIncrement = .1f;
        private float radiusMultiplier = 1f;

        private float msBetweenTargets;

        private bool prepared = false;

        private bool lockDirectional;

        private List<Vector2> directionals = new List<Vector2> { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

        private SnapTarget snapMode = SnapTarget.AnyColor;
        private DistanceMode distanceMode = DistanceMode.Local;

        protected override void Awake()
        {
            base.Awake();
            cam = Camera.main;
            orbit.SetActive(false);
            trail = orbit.GetComponent<TrailRenderer>();
            directionals.Add(new Vector2(.7f, .7f));
            directionals.Add(new Vector2(.7f, -.7f));
            directionals.Add(new Vector2(-.7f, .7f));
            directionals.Add(new Vector2(-.7f, -.7f));
        }

        void Update()
        {
            if (prepared)
            {
                if(nearestTarget != null)
                {
                    LockSpacing();
                }
            }
        }

        public void EnableSpacingSnap()
        {
            EditorState.SelectSnappingMode(SnappingMode.None);
            OnActivated();
            Prepare();
        }

        public void DisableSpacingSnap()
        {
            EditorState.SelectSnappingMode(EditorState.Snapping.Previous);
            Reset();
            OnDeactivated();
        }

        private void LockSpacing()
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(actions.SpacingSnap.MousePosition.ReadValue<Vector2>());
            Vector2 targetPos = nearestTarget.gridTargetIcon.transform.position;
            var direction = (mousePos - targetPos).normalized;
            if (lockDirectional) direction = GetClosestDirectional(direction);
            var cursorPos = direction * radius * radiusMultiplier;
            var newCursorPos = targetPos + cursorPos;
            hover.transform.position = newCursorPos;
            orbit.transform.position = hover.transform.position;
        }

        private Vector2 GetClosestDirectional(Vector2 direction)
        {
            return directionals.Aggregate((x, y) => Vector2.Distance(x, direction) < Vector2.Distance(y, direction) ? x : y);
        }

        private void HandleScrolling(bool increase)
        {
            if ((KeybindManager.Global.Modifier & KeybindManager.Global.Modifiers.Ctrl) == KeybindManager.Global.Modifiers.Ctrl) return;
            ChangeRadius(increase ? -radiusIncrement : radiusIncrement, false);
        }

        private void Prepare()
        {
            if (EditorTime.Time.tick == 0) return;
            var targets = new NoteEnumerator(new QNT_Timestamp(0), new QNT_Timestamp(EditorTime.Time.tick - 1));
            targets.reverse = true;
            if(nearestTarget != null)
            {
                nearestTarget.VisualDeselect();
                nearestTarget = null;
            }
            nearestTarget = FindNearestTargetPosition(targets);
            trail.startColor = EditorState.Hand.Current == TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
            trail.endColor = EditorState.Hand.Current == TargetHandType.Right ? NRSettings.config.leftColor : NRSettings.config.rightColor;
            EditorNotes.DeselectAllTargets();
            if (nearestTarget != null)
            {
                nearestTarget.VisualSelect();
                IsHoveringGrid.Instance.ChangeColliderSize(true);
                orbit.SetActive(true);
                radius = 0f;
                ChangeRadius(FindSuggestedDistance(), true);
            }
            else
            {
                return;
            }
            hover.LockSpacing(true);
            UpdateHoverText();
            prepared = true;
        }

        private void Reset()
        {
            IsHoveringGrid.Instance.ChangeColliderSize(false);
            if (nearestTarget != null) nearestTarget.VisualDeselect();
            nearestTarget = null;
            hover.LockSpacing(false);
            hover.UpdateDistance("");
            orbit.SetActive(false);
            prepared = false;
        }

        private void UpdateHoverText()
        {
            string mode = distanceMode == DistanceMode.Local ? "Distance" : "Multiplier";
            float distance = distanceMode == DistanceMode.Local ? radius : radiusMultiplier;
            hover.UpdateDistance($"{mode}\n{distance}");
        }

        private void ChangeRadius(float amount, bool prepare)
        {
            if(distanceMode == DistanceMode.Local || prepare)
            {
                radius += amount;
                radius = Mathf.Clamp(radius, .1f, 5f);
                radius = (float)Math.Round(radius, 1);
            }
            else
            {
                radiusMultiplier += amount;
                radiusMultiplier = Mathf.Clamp(radiusMultiplier, .1f, 5f);
                radiusMultiplier = (float)Math.Round(radiusMultiplier, 1);
            }
            UpdateHoverText();
        }

        private float FindSuggestedDistance()
        {
            var bpm = EditorTempo.GetTempoForTime(EditorTime.Time);
            float beatsBetweenTargets = new QNT_Timestamp(EditorTime.Time.tick - nearestTarget.data.time.tick).ToBeatTime();
            msBetweenTargets = (bpm.microsecondsPerQuarterNote / 1000) * beatsBetweenTargets;
            msBetweenTargets = Mathf.Floor(msBetweenTargets);
            return .5f + (float)Math.Round(msBetweenTargets / 750f * Mathf.Clamp(msBetweenTargets / 100f, 1f, 3f), 1);
        }

        private Target FindNearestTargetPosition(NoteEnumerator targets)
        {
            foreach (var target in targets)
            {
                if(snapMode == SnapTarget.SameColor)
                {
                    if (target.data.handType != EditorState.Hand.Current) continue;
                }

                TargetBehavior behavior = target.data.behavior;
                if (behavior == TargetBehavior.Mine || behavior == TargetBehavior.Melee) continue;

                return target;
            }
            return null;
        }

        private void SwitchTargetColor()
        {
            EditorState.SelectHand(EditorState.Hand.Current == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left);
            Prepare();
        }

        private void ToggleDistanceMode()
        {
            if (distanceMode == DistanceMode.Local)
            {
                distanceMode = DistanceMode.GlobalMultiplier;
            }
            else
            {
                distanceMode = DistanceMode.Local;
            }
            UpdateHoverText();
        }

        private void ToggleSnapTarget()
        {
            if (snapMode == SnapTarget.AnyColor)
            {
                snapMode = SnapTarget.SameColor;
                NotificationCenter.SendNotification("Locking to previous target with same hand", NotificationType.Info, false);
            }
            else
            {
                snapMode = SnapTarget.AnyColor;
                NotificationCenter.SendNotification("Locking to previous target", NotificationType.Info, false);
            } 

            Prepare();
        }

        protected override void RegisterCallbacks()
        {
            actions.SpacingSnap.ChangeDistance.performed += ctx => HandleScrolling(ctx.ReadValue<float>() < 0);
            actions.SpacingSnap.LockDirectional.performed += _ => lockDirectional = true;
            actions.SpacingSnap.LockDirectional.canceled += _ => lockDirectional = false;
            actions.SpacingSnap.Tab.performed += _ => DisableSpacingSnap();
            actions.SpacingSnap.SwitchTargetColor.performed += _ => SwitchTargetColor();
            actions.SpacingSnap.ToggleDistanceMode.performed += _ => ToggleDistanceMode();
            actions.SpacingSnap.ToggleSnapTarget.performed += _ => ToggleSnapTarget();
        }

        protected override void OnEscPressed(InputAction.CallbackContext context) { }

        protected override void SetRebindConfiguration(ref RebindConfiguration options, SpacingSnapKeybinds myKeybinds)
        {
            options.SetAssetTitle("Spacing Snapper").SetPriority(20);
            options.AddHiddenKeybinds(myKeybinds.SpacingSnap.MousePosition, myKeybinds.SpacingSnap.Tab);
            options.AddNonRebindableKeybinds(myKeybinds.SpacingSnap.ChangeDistance);
            options.AddNonRebindableKeybinds(myKeybinds.SpacingSnap.LockDirectional);
        }

        private enum DistanceMode
        {
            Local,
            GlobalMultiplier
        }
        private enum SnapTarget
        {
            AnyColor,
            SameColor
        }
    }

}
