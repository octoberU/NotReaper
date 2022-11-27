using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NotReaper.Repeaters
{   
    public class AddRepeaterAction : NRAction
    {
        public override string ActionName => "Add repeater";
        
        private RepeaterManager manager;
        private string id;
        private QNT_Timestamp startTime;
        private QNT_Timestamp endTime;
        private QNT_Timestamp activeStartTime;
        private QNT_Timestamp activeEndTime;
        private bool flipColors;
        private bool mirrorHorizontally;
        private bool mirrorVertically;
        public bool success;
        public AddRepeaterAction(RepeaterManager manager, string id, QNT_Timestamp startTime, QNT_Timestamp endTime, QNT_Timestamp activeStartTime, QNT_Timestamp activeEndTime, bool flipColors, bool mirrorHorizontally, bool mirrorVertically)
        : base(startTime)
        {
            this.manager = manager;
            this.id = id;
            
            if(startTime > endTime)
                (startTime, endTime) = (endTime, startTime);
            
            this.startTime = startTime;
            this.endTime = endTime;
            this.activeStartTime = activeStartTime;
            this.activeEndTime = activeEndTime;
            this.flipColors = flipColors;
            this.mirrorHorizontally = mirrorHorizontally;
            this.mirrorVertically = mirrorVertically;
        }

        public override void DoAction(Timeline timeline)
        {
            success = manager.AddRepeaterFromAction(id, startTime, endTime, activeStartTime, activeEndTime, flipColors, mirrorHorizontally, mirrorVertically);
        }

        public override void UndoAction(Timeline timeline)
        {
            manager.RemoveRepeaterFromAction(id, startTime);
        }
    }

    public class RemoveRepeaterAction : NRAction
    {
        public override string ActionName => "Remove repeater";
        
        private RepeaterManager manager;
        private RepeaterSection section;

        public RemoveRepeaterAction(RepeaterManager manager, RepeaterSection section) : base(section.startTime)
        {
            this.manager = manager;
            this.section = section;
        }

        public override void DoAction(Timeline timeline)
        {
            manager.RemoveRepeaterFromAction(section.ID, section.startTime);
        }

        public override void UndoAction(Timeline timeline)
        {
            manager.AddRepeaterFromAction(section);
        }
    }

    public class MultiRemoveRepeaterAction : NRAction
    {
        public override string ActionName => "Remove multiple repeaters";
        
        private RepeaterManager manager;
        private List<RepeaterSection> sections;
        private RepeaterSection parentSection;
        private string id;

        public MultiRemoveRepeaterAction(RepeaterManager manager, List<RepeaterSection> sections, string id, RepeaterSection parentSection = null) 
            : base(sections.First()?.startTime ?? new(0))
        {
            this.manager = manager;
            this.sections = sections;
            this.parentSection = parentSection;
            this.id = id;
        }

        public override void DoAction(Timeline timeline)
        {

            for (int i = sections.Count - 1; i >= 0; i--)
            {
                manager.RemoveRepeaterFromAction(id, sections[i].startTime);
            }

            if(parentSection != null)
            {
                manager.RemoveRepeaterFromAction(id, parentSection.startTime);
            }
        }

        public override void UndoAction(Timeline timeline)
        {
            if(parentSection != null)
            {
                manager.CreateParentRepeater(parentSection);
            }
            foreach(var section in sections)
            {
                manager.AddRepeaterFromAction(section);
            }
        }
    }

    public class RenameRepeaterAction : NRAction
    {
        public override string ActionName => "Rename repeater";
        
        private RepeaterManager manager;
        private string oldID;
        private string newID;

        public RenameRepeaterAction(RepeaterManager manager, string oldID, string newID) 
            : base(manager.GetSectionById(oldID)?.First(s => s.isParent).startTime ?? new(0))
        {
            this.manager = manager;
            this.oldID = oldID;
            this.newID = newID;
        }

        public override void DoAction(Timeline timeline)
        {
            manager.RenameRepeaterFromAction(oldID, newID);
        }

        public override void UndoAction(Timeline timeline)
        {
            manager.RenameRepeaterFromAction(newID, oldID);
        }
    }

    public class BakeRepeaterAction : NRAction
    {
        public override string ActionName => "Bake repeater";
        
        private RepeaterManager manager;
        private RepeaterSection section;
        public BakeRepeaterAction(RepeaterManager manager, RepeaterSection section) : base(section.startTime)
        {
            this.manager = manager;
            this.section = section;
        }

        public override void DoAction(Timeline timeline)
        {

            manager.BakeRepeaterSectionFromAction(section);
        }

        public override void UndoAction(Timeline timeline)
        {
            manager.CreateChildRepeater(section);
        }
    }

    public class MakeUniqueAction : NRAction
    {
        public override string ActionName => "Make repeater unique";
        
        private RepeaterManager manager;
        private RepeaterSection section;
        private string oldID;
        private bool flipColors;
        private bool mirrorHorizontally;
        private bool mirrorVertically;

        public string newID;
        public bool success;

        public MakeUniqueAction(RepeaterManager manager, RepeaterSection section, string oldID) : base(section.startTime)
        {
            this.manager = manager;
            this.section = section;
            this.oldID = oldID;
            this.newID = oldID;
            this.flipColors = section.flipTargetColors;
            this.mirrorHorizontally = section.mirrorHorizontally;
            this.mirrorVertically = section.mirrorVertically;
        }

        public override void DoAction(Timeline timeline)
        {
            success = manager.MakeSectionUniqueFromAction(section, out newID);
        }

        public override void UndoAction(Timeline timeline)
        {
            manager.UndoMakeSectionUniqueFromAction(section, oldID, flipColors, mirrorHorizontally, mirrorVertically);
        }
    }

    public class FlipRepeaterAction : NRAction
    {
        public override string ActionName => "Flip repeater";
        
        private RepeaterManager manager;
        private RepeaterSection section;

        public bool mirror;
        public Mode mode;
        public FlipRepeaterAction(RepeaterManager manager, RepeaterSection section, bool mirror, Mode axis) : base(section.startTime)
        {
            this.manager = manager;
            this.section = section;
            this.mirror = mirror;
            this.mode = axis;
        }

        public override void DoAction(Timeline timeline)
        {
            if (mode == Mode.Horizontal)
                manager.MirrorRepeaterHorizontallyFromAction(section, mirror);
            else if(mode == Mode.Vertical)
                manager.MirrorRepeaterVerticallyFromAction(section, mirror);
            else
                manager.FlipRepeaterTargetColorsFromAction(section, mirror);
        }

        public override void UndoAction(Timeline timeline)
        {
            if (mode == Mode.Horizontal)
                manager.MirrorRepeaterHorizontallyFromAction(section, !mirror);
            else if (mode == Mode.Vertical)
                manager.MirrorRepeaterVerticallyFromAction(section, !mirror);
            else
                manager.FlipRepeaterTargetColorsFromAction(section, !mirror);
        }

        public enum Mode
        {
            Horizontal,
            Vertical,
            Colors
        }
    }
}

