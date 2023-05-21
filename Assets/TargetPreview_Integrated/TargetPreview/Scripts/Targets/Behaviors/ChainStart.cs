using System;
using System.Collections;
using System.Collections.Generic;
using TargetPreview.Targets;
using Unity.Collections;
using UnityEngine;

public class ChainStart : GridTarget
{
    public List<ChainNode> nodes = new List<ChainNode>();
    [SerializeField] LineRenderer lineRenderer;
    int nodeCount = 0;

    NativeArray<Vector3> nativePositions;
    NativeSlice<Vector3> nativePositionSlice;

    public override void UpdateVisuals(TargetData newData)
    {
        base.UpdateVisuals(newData);

        for (var index = nodes.Count - 1; index >= 0; index--)
        {
            var chainNode = nodes[index];
            creator.Return(chainNode);
            nodes.Remove(chainNode);
        }

        if (newData.cue.children == null) return;
        
        foreach (var targetCue in newData.cue.children) 
            nodes.Add((ChainNode)creator.Take(targetCue));
    }

    public override void Awake()
    {
        nativePositions = new NativeArray<Vector3>(new Vector3[500], Allocator.Persistent);
        base.Awake();
    }

    public override void TimeUpdate(float time)
    {
        base.TimeUpdate(time);
        DrawLine(time);
    }

    void DrawLine(float time)
    {
        var shouldRender = TemporalDistance < 0.99f;
        lineRenderer.enabled = shouldRender;
        if (!shouldRender || !nativePositions.IsCreated) return;
        
        
        nodeCount = nodes.Count;
        nativePositions[0] = physicalTarget.transform.position;
        var shouldSkipFirstNode = TemporalDistance < 0.01f;
        int currentNodeCount = shouldSkipFirstNode ? 0 : 1;
        int nodesSkipped = shouldSkipFirstNode ? 1 : 0; //Current chainstart is already done, skip it.
        for (int i = 0; i < nodeCount; i++)
        {
            if (!(nodes[i].TargetData.time > time)) 
            {
                nodesSkipped++;
                continue;
            }
                
            
            nativePositions[currentNodeCount] = nodes[i].PhysicalPosition;
            currentNodeCount++;
        }
        
        nativePositionSlice = new NativeSlice<Vector3>(nativePositions, 0, currentNodeCount);

        lineRenderer.positionCount = currentNodeCount;
        lineRenderer.SetPositions(nativePositionSlice);
    }

    public override void AnimateFlyIn(float distance)
    {
        base.AnimateFlyIn(distance);
        foreach (var chainNode in nodes)
        {
            chainNode.AnimateChildFlyIn(distance);
        }
    }

    void OnDestroy()
    {
        nativePositions.Dispose();
    }

    public override void OnReturnedToPool()
    {
        base.OnReturnedToPool();
        foreach (var chainNode in nodes) creator.Return(chainNode);
        nodes.Clear();
    }
}