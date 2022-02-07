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

public class SpacingSnapper : MonoBehaviour
{

    [SerializeField] private HoverTarget hover;
    [SerializeField] private GameObject orbit;

    private Target nearestTarget;
    private Camera cam;
    private TrailRenderer trail;

    private float radius = 1f;
    private float radiusIncrement = .1f;

    private float msBetweenTargets;

    private bool prepared = false;

    private List<Vector2> directionals = new List<Vector2> { Vector2.up, Vector2.right, Vector2.down, Vector2.left };

    private void Awake()
    {
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

        if (EditorInput.InputDisabled || EditorInput.selectedMode != EditorMode.Compose || EditorInput.selectedTool == EditorTool.ChainBuilder) return;
        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            Prepare();
        }
        else if (Input.GetKey(KeyCode.LeftAlt))
        {
            if(nearestTarget is null)
            {
                Reset();
                return;
            }
            LockSpacing();
            HandleScrolling();
            HandleHandChange();
        }
        else if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            if (nearestTarget != null)
            {
                nearestTarget.Deselect();
            }
            Reset();
        }
        else if (prepared)
        {
            Reset();
        }
    }

    private void LockSpacing()
    {
        Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
        Vector2 targetPos = nearestTarget.gridTargetIcon.transform.position;
        var direction = (mousePos - targetPos).normalized;
        if (Input.GetKey(KeyCode.LeftShift)) direction = GetClosestDirectional(direction);
        var cursorPos = direction * radius;
        var newCursorPos = targetPos + cursorPos;
        hover.transform.position = newCursorPos;
        orbit.transform.position = hover.transform.position;
    }

    private Vector2 GetClosestDirectional(Vector2 direction)
    {
        return directionals.Aggregate((x, y) => Vector2.Distance(x, direction) < Vector2.Distance(y, direction) ? x : y);
    }

    private void HandleScrolling()
    {
        if (Input.mouseScrollDelta.y > 0.1f)
        {
            ChangeRadius(radiusIncrement);
        }
        else if (Input.mouseScrollDelta.y < -0.1f)
        {
            ChangeRadius(-radiusIncrement);
        }
    }

    private void HandleHandChange()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            EditorInput.I.ToggleHandColor();
        }
    }

    private void Prepare()
    {
        if (Timeline.time.tick == 0) return;
        var targets = new NoteEnumerator(new QNT_Timestamp(0), new QNT_Timestamp(Timeline.time.tick - 1));
        targets.reverse = true;
        nearestTarget = FindNearestTargetPosition(targets);
        trail.startColor = EditorInput.selectedHand == TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
        trail.endColor = EditorInput.selectedHand == TargetHandType.Right ? NRSettings.config.leftColor : NRSettings.config.rightColor;
        if (nearestTarget != null)
        {
            nearestTarget.Select();
            EditorInput.IsSpacingLocked = true;
            IsHoveringGrid.Instance.ChangeColliderSize(true);
            radius = 0f;
            orbit.SetActive(true);
            ChangeRadius(FindSuggestedDistance());
        }
        prepared = true;
    }

    private void Reset()
    {
        EditorInput.IsSpacingLocked = false;
        IsHoveringGrid.Instance.ChangeColliderSize(false);
        nearestTarget = null;
        hover.UpdateDistance("");
        orbit.SetActive(false);
        prepared = false;
    }

    private void ChangeRadius(float amount)
    {
        radius += amount;
        radius = Mathf.Clamp(radius, .1f, 5f);
        radius = (float)Math.Round(radius, 1);
        hover.UpdateDistance(radius.ToString());
    }

    private float FindSuggestedDistance()
    {
        var bpm = Timeline.instance.GetTempoForTime(Timeline.time);
        float beatsBetweenTargets = new QNT_Timestamp(Timeline.time.tick - nearestTarget.data.time.tick).ToBeatTime();
        msBetweenTargets = (bpm.microsecondsPerQuarterNote / 1000) * beatsBetweenTargets;
        msBetweenTargets = Mathf.Floor(msBetweenTargets);
        return .5f + (float)Math.Round(msBetweenTargets / 750f * Mathf.Clamp(msBetweenTargets / 100f, 1f, 3f), 1);
    }

    private Target FindNearestTargetPosition(NoteEnumerator targets)
    {
        foreach(var target in targets)
        {
            TargetBehavior behavior = target.data.behavior;
            if (behavior == TargetBehavior.Mine || behavior == TargetBehavior.Melee) continue;

            return target;
        }
        return null;
    }
}
