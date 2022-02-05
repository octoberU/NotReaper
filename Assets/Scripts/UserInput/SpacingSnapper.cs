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

public class SpacingSnapper : MonoBehaviour
{

    [SerializeField] private HoverTarget hover;

    private Target nearestTarget;
    private Camera cam;

    private const uint MinuteInMs = 60000;

    private float radius = 1f;
    private float radiusIncrement = .1f;

    private float msBetweenTargets;

    private void Awake()
    {
        cam = Camera.main;
    }
    void Update()
    {
        if (EditorInput.InputDisabled || EditorInput.selectedMode != EditorMode.Compose) return;
        if (Input.GetKey(KeyCode.LeftControl)) return;

        if (Input.GetKeyDown(KeyCode.LeftAlt))
        {
            var targets = new NoteEnumerator(new QNT_Timestamp(0), new QNT_Timestamp(Timeline.time.tick - 1));
            targets.reverse = true;
            nearestTarget = FindNearestTargetPosition(targets);
            if (nearestTarget != null)
            {
                nearestTarget.Select();
                EditorInput.IsSpacingLocked = true;
                radius = 0f;
                ChangeRadius(FindSuggestedDistance());
            }
        }

        if (nearestTarget is null) return;

        if (Input.GetKey(KeyCode.LeftAlt))
        {
            Vector2 mousePos = cam.ScreenToWorldPoint(Input.mousePosition);
            Vector2 targetPos = nearestTarget.gridTargetIcon.transform.position;
            var direction = (mousePos - targetPos).normalized;
            var cursorPos = direction * radius;
            var newCursorPos = targetPos + cursorPos;
            hover.transform.position = newCursorPos;
            if (Input.mouseScrollDelta.y > 0.1f)
            {
                ChangeRadius(radiusIncrement);
            }
            else if (Input.mouseScrollDelta.y < -0.1f)
            {
                ChangeRadius(-radiusIncrement);
            }
            
        }
        else if (Input.GetKeyUp(KeyCode.LeftAlt))
        {
            nearestTarget.Deselect();
            EditorInput.IsSpacingLocked = false;
            nearestTarget = null;
            hover.UpdateDistance("");
        }
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
