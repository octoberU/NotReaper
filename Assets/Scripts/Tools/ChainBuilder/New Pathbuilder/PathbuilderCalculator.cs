using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;

namespace NotReaper.Tools.PathBuilder
{
	public class PathbuilderCalculator
	{
		private BezierCurve curve;
		public PathbuilderCalculator()
        {
			curve = new BezierCurve();
        }

		/// <summary>
		/// Calculates pathbuilder nodes and generates them if "generate" is set to true.
		/// </summary>
		/// <param name="targetData">The target to calculate nodes for.</param>
		/// <param name="generate">True if the nodes should also be generated.</param>
		public void CalculateNodes(TargetData targetData, bool generate)
        {
			var data = targetData.pathbuilderData;
			QNT_Timestamp startTime = targetData.time;
			QNT_Timestamp lastNodeTime = startTime;
			float keep = 0;
			TargetHandType hand = targetData.handType;
			for (int j = 0; j < data.Segments.Count; j++)
			{

				var segment = data.Segments[j];
				segment.generatedNodes.Clear();
				PathbuilderData.Interval interval = data.IsSegmentScope ? segment.interval : data.IntervalOverride;
				float beatLength = data.IsSegmentScope ? segment.beatLength.tick : data.BeatLength.tick;
				List<Vector2> points = GetPoints(segment, data.IsSegmentScope ? 1 : data.Segments.Count, beatLength, interval, ref keep);
				for (int i = 0; i < points.Count; i++)
				{
					TargetData node = new TargetData();
					node.behavior = GetBehavior(targetData.behavior);
					node.velocity = GetVelocity(targetData);
					node.handType = SwitchHand(ref hand, data.AlternateHands);
					node.position = points[i];
					node.SetTimeFromAction(startTime + QNT_Duration.FromBeatTime((i + 1) * (4f / interval.denominator) * interval.nominator));
					lastNodeTime = node.time;
					segment.generatedNodes.Add(node);
				}
				startTime = lastNodeTime;
			}
			targetData.pathbuilderData = data;
			if (generate)
            {
				GenerateNodes(targetData.pathbuilderData);
				EditorTargets.UpdateChainConnector(targetData);
            }
		}
		/// <summary>
		/// Generates previously calculated pathbuilder nodes.
		/// </summary>
		/// <param name="target">The target to calculate nodes for.</param>
		public void GenerateNodes(PathbuilderData data)
		{
			foreach(var segment in data.Segments)
            {
				foreach(var node in segment.generatedNodes)
                {
					EditorTargets.AddTargetFromAction(node, true, false);
				}
            }
		}
		/// <summary>
		/// Get the points the targets should get positioned to.
		/// </summary>
		/// <param name="data">The segment's data.</param>
		/// <param name="numSegments">The toal number of segments.</param>
		/// <param name="beatLength">The total beat length.</param>
		/// <param name="keep">The leftover from rounding. The value you pass in must be 0 initially.</param>
		/// <returns>The list of points calculated.</returns>
		private List<Vector2> GetPoints(PathbuilderData.Segment data, float numSegments, float beatLength, PathbuilderData.Interval interval, ref float keep)
		{
			//invert the number of segments now so we can multiply later which is less expensive.
			numSegments = 1 / numSegments;
			List<Vector2> points = new List<Vector2>();
			//the amount of nodes that should be on this segment. 
			float nodesFloat = (beatLength / (float)Constants.PulsesPerQuarterNote * ((interval.denominator / interval.nominator) / 4f) * numSegments);
			//update keep
			keep += nodesFloat % 1;
			//to account for stuff like .33, we round in edge cases. This prevents it from ever having less nodes than intended.
			if (keep % 1 >= .99f)
            {
				keep = Mathf.Ceil(keep);
            }
			//because we have a keep, we floor the count first before we add it.
			float nodeCount = Mathf.FloorToInt(nodesFloat) + keep;
			for (float i = 1; i <= nodeCount; i++)
			{
				points.Add(curve.CubicLerp(data.startPoint, data.startPointHandle, data.endPointHandle, data.endPoint, (float)i / (nodeCount)));
			}
			//we never want keep to be more than 1. This basically gets rid of any extra targets we added.
			keep %= 1;
			return points;
		}
		/// <summary>
		/// Switches to the other hand if the alternating option is selected.
		/// </summary>
		/// <param name="hand">The previous node's handtype</param>
		/// <param name="alternate">The alternate hands option.</param>
		/// <returns>The approriate handtype.</returns>
		private TargetHandType SwitchHand(ref TargetHandType hand, bool alternate)
		{
			if (!alternate) return hand;
            else
            {
				hand = hand == TargetHandType.Left ? TargetHandType.Right : TargetHandType.Left;
				return hand;
            }
		}
		/// <summary>
		/// Get the desired behavior based on the root notes behavior.
		/// </summary>
		/// <param name="behavior">The root note's behavior.</param>
		/// <returns>The ddesired behavior.</returns>
		private TargetBehavior GetBehavior(TargetBehavior behavior)
		{
			if (behavior == TargetBehavior.ChainStart) return TargetBehavior.ChainNode;
			else return behavior;
		}
		/// <summary>
		/// Get the desired hitsound based on the selected settings.
		/// </summary>
		/// <param name="data">The pathbuilder root target to get the hitsound for.</param>
		/// <returns>The hitsound that should be applied to nodes.</returns>
		private InternalTargetVelocity GetVelocity(TargetData data)
        {
			if (data.pathbuilderData.IsSilent) return InternalTargetVelocity.Silent;
			else if (data.behavior == TargetBehavior.ChainStart) return InternalTargetVelocity.Chain;
			else return data.velocity;
        }

	}
}

