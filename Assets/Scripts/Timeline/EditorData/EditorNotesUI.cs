using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper
{
    /// <summary>
    /// Responsible for the visual representation of targets.
    /// </summary>
    public class EditorNotesUI
    {
        /// <summary>
        /// Updates the color of all targets 
        /// </summary>
        public void UpdateTargetColors()
        {
            foreach (var target in EditorNotes.OrderedNotes)
            {
                target.gridTargetIcon.UpdateColors();
                target.timelineTargetIcon.UpdateColors();
            }
        }
        /// <summary>
        /// Updates a sustain length from the buttons next to sustains.
        /// </summary>
        /// <param name="target">The target to affect</param>
        /// <param name="increase">If true, increase by one beat snap, if false, the opposite.</param>
        public void UpdateSustainLength(Target target, bool increase)
        {
            if (!target.data.supportsBeatLength) return;
            QNT_Duration increment = EditorBeatSnap.Duration;
            QNT_Duration targetLength = target.data.beatLength;
            if (increase)
            {
                if (targetLength < increment)
                {
                    targetLength = new QNT_Duration(0);
                }
                targetLength += increment;
            }
            else
            {
                targetLength -= increment;
            }
            target.data.beatLength = targetLength;
            target.UpdatePath();
        }
        /// <summary>
        /// Updates chain connector lines for a target.
        /// </summary>
        /// <param name="data">The target do update the connector line for.</param>
        public void UpdateChainConnector(TargetData data)
        {
            var notes = new NoteEnumerator(new(0), data.time); //get all targets from start up until the supplied target
            notes.reverse = true;   //reverse selection to find chainstart

            List<Target> chain = new();
            Target chainStart = null;

            foreach (var note in notes)  //find the first chainstart of the same handtype
            {
                if (note.data.behavior != TargetBehavior.ChainStart) continue;

                if (note.data.handType == data.handType)
                {
                    chainStart = note;
                    chain.Add(chainStart);
                    break;
                }
            }
            if (chainStart != null) //if we found chainstart..
            {
                notes = new NoteEnumerator(chainStart.data.time, EditorNotes.OrderedNotes.Last().data.time); //..we get all notes from chain start until the last target
                foreach (var note in notes)
                {
                    if (note.data.time == chainStart.data.time) continue; //skip our own target (chainstart)
                    if (note.data.handType != chainStart.data.handType) continue; //skip if it's not the same hand
                    if (note.data.behavior == TargetBehavior.Melee || note.data.behavior == TargetBehavior.Mine) continue; //skip if it's a mine or melee
                    if (note.data.behavior != TargetBehavior.ChainNode) break; //finally, break if it's not a node, since that means the chain has ended by now
                    chain.Add(note); //add the found node to the chain
                }

                if (chain.Count <= 1)
                {
                    //return because chain only has a chainstart
                    return;
                }

                chain.Last().gridTargetIcon.DisableChainConnector(); //disable connector on the last node in case it still had a line connecting to something
                for (int i = chain.Count - 2; i >= 0; i--)
                {
                    chain[i].gridTargetIcon.ConnectChain(chain[i + 1], chainStart); //hook up the chain
                }
            }
        }
        /// <summary>
        /// Enables or disables sustain length buttons depending on their musical distance.
        /// </summary>
        public void EnableNearSustainButtons()
        {
            foreach (Target target in EditorNotes.LoadedNotes)
            {
                if (!target.data.supportsBeatLength || target.data.isPathbuilderTarget) continue;
                bool shouldDisplayTimeline;
                bool shouldDisplayGrid = EditorState.IsPaused; //Need to be paused
                                                 //Be in drag select, or be a path builder note in path builder mode
                shouldDisplayGrid &= EditorState.Tool.Current == EditorTool.DragSelect || (target.data.behavior == TargetBehavior.Legacy_Pathbuilder && EditorState.Tool.Current == EditorTool.ChainBuilder);
                shouldDisplayTimeline = shouldDisplayGrid;
                shouldDisplayGrid &= target.GetRelativeBeatTime() < 2 && target.GetRelativeBeatTime() > -2; //Target needs to be "near"

                shouldDisplayTimeline &= target.data.time > (EditorTime.Time - Relative_QNT.FromBeatTime(20f)) && target.data.time < (EditorTime.Time + Relative_QNT.FromBeatTime(20f));
                target.DisplaySustainButtons(shouldDisplayGrid, shouldDisplayTimeline);
            }
        }
    }
}
