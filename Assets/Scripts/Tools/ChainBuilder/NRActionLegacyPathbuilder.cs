using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper.Tools
{
    public class NRActionConvertNoteToLegacyPathbuilder : NRAction
    {
        public TargetData data;
        public QNT_Duration oldBeatLength;
        public LegacyPathbuilderData pathBuilderData = new LegacyPathbuilderData();

        public override void DoAction(Timeline timeline)
        {
            if (data.isRepeaterTarget)
            {
                data = timeline.repeaterManager.GetParentTarget(data);
                pathBuilderData.behavior = data.behavior;
                pathBuilderData.velocity = data.velocity;
                oldBeatLength = data.beatLength;
                if (data.beatLength < Constants.QuarterNoteDuration)
                {
                    data.beatLength = Constants.QuarterNoteDuration;
                }
                if (data.repeaterData.Section.flipTargetColors)
                {
                    if (data.handType == TargetHandType.Left)
                    {
                        pathBuilderData.handType = TargetHandType.Right;
                    }
                    else if (data.handType == TargetHandType.Right)
                    {
                        pathBuilderData.handType = TargetHandType.Left;
                    }
                }
                else
                {
                    pathBuilderData.handType = data.handType;
                }
                data.legacyPathbuilderData = pathBuilderData;
                data.behavior = TargetBehavior.Legacy_Pathbuilder;

                foreach (var sibling in timeline.repeaterManager.GetMatchingRepeaterTargets(data))
                {
                    sibling.beatLength = data.beatLength;
                    LegacyPathbuilderData siblingData = new LegacyPathbuilderData();
                    siblingData.Copy(pathBuilderData);
                    sibling.legacyPathbuilderData = siblingData;
                    sibling.behavior = TargetBehavior.Legacy_Pathbuilder;
                }
                ChainBuilder.ChainBuilder.GenerateChainNotes(data);
            }
            else
            {
                pathBuilderData.behavior = data.behavior;
                pathBuilderData.velocity = data.velocity;
                pathBuilderData.handType = data.handType;
                data.legacyPathbuilderData = pathBuilderData;

                //Ensure the path builder always starts with a quarter note of build time
                oldBeatLength = data.beatLength;
                if (data.beatLength < Constants.QuarterNoteDuration)
                {
                    data.beatLength = Constants.QuarterNoteDuration;
                }

                data.behavior = TargetBehavior.Legacy_Pathbuilder;
                ChainBuilder.ChainBuilder.GenerateChainNotes(data);
            }


        }
        public override void UndoAction(Timeline timeline)
        {
            data.legacyPathbuilderData.DeleteCreatedNotes();
            data.behavior = data.legacyPathbuilderData.behavior;
            data.velocity = data.legacyPathbuilderData.velocity;
            data.handType = data.legacyPathbuilderData.handType;
            data.beatLength = oldBeatLength;
            data.legacyPathbuilderData = null;

            if (data.isRepeaterTarget)
            {
                foreach (var sibling in timeline.repeaterManager.GetMatchingRepeaterTargets(data))
                {
                    sibling.legacyPathbuilderData.DeleteCreatedNotes();
                    sibling.behavior = sibling.legacyPathbuilderData.behavior;
                    sibling.velocity = sibling.legacyPathbuilderData.velocity;
                    sibling.handType = sibling.legacyPathbuilderData.handType;
                    sibling.beatLength = oldBeatLength;
                    sibling.legacyPathbuilderData = null;
                }
            }
        }
    }

    public class NRActionBakePath : NRAction
    {
        public NRActionRemoveNote removeNoteAction;

        public override void DoAction(Timeline timeline)
        {
            //Generate and create real notes
            ChainBuilder.ChainBuilder.GenerateChainNotes(removeNoteAction.targetData);
            foreach (TargetData genData in removeNoteAction.targetData.legacyPathbuilderData.generatedNotes)
            {
                TargetData newData = new(genData);
                EditorTargets.AddTargetFromAction(newData);
                if (removeNoteAction.targetData.isRepeaterTarget)
                {
                    removeNoteAction.targetData.repeaterData.Section.AddExistingTargetToRepeater(newData);
                }
            }
            if (removeNoteAction.targetData.isRepeaterTarget)
            {
                foreach (var sibling in timeline.repeaterManager.GetMatchingRepeaterTargets(removeNoteAction.targetData))
                {
                    foreach (var node in sibling.legacyPathbuilderData.generatedNotes)
                    {
                        TargetData newNode = new(node);
                        EditorTargets.AddTargetFromAction(newNode);
                        sibling.repeaterData.Section.AddExistingTargetToRepeater(newNode);
                    }
                    EditorTargets.DeleteTargetFromAction(sibling);
                }
            }

            //Destroy the path builder note (and all the generated transient notes

            removeNoteAction.DoAction(timeline);
        }
        public override void UndoAction(Timeline timeline)
        {
            removeNoteAction.UndoAction(timeline);

            //Recalculate the notes, and remove the "real" notes
            ChainBuilder.ChainBuilder.CalculateChainNotes(removeNoteAction.targetData);
            foreach (TargetData genData in removeNoteAction.targetData.legacyPathbuilderData.generatedNotes)
            {
                var foundData = TargetFinder.FindTargetData(genData.time, genData.behavior, genData.handType);
                if (foundData != null)
                {
                    EditorTargets.DeleteTargetFromAction(foundData);
                }
            }
            ChainBuilder.ChainBuilder.GenerateChainNotes(removeNoteAction.targetData);
        }
    }
}
