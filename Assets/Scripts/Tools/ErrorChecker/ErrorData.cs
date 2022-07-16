using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.UI;
using UnityEngine;

namespace NotReaper.Tools.ErrorChecker
{
    public class ErrorData : ListData
    {
        public QNT_Timestamp Time { get; }
        public string Description => description;
        public List<Target> AffectedTargets { get; } = new();

        private Action fixItAction = null;

        public ErrorEntry Entry { get; private set; }

        public bool HasAutoFix => fixItAction != null;

        public ErrorData(QNT_Timestamp time, string description)
        {
            Time = time;
            this.description = description;
        }

        public ErrorData(QNT_Timestamp time, string description, Target affectedTarget)
        {
            Time = time;
            this.description = description;
            AffectedTargets.Add(affectedTarget);
        }

        public ErrorData(QNT_Timestamp time, string description, List<Target> affectedTargets)
        {
            Time = time;
            this.description = description;
            AffectedTargets = affectedTargets;
        }

        public ErrorData(QNT_Timestamp time, string description, Action fixItAction, params Target[] affectedTargets)
        {
            Time = time;
            this.description = description;
            AffectedTargets.AddRange(affectedTargets);
            this.fixItAction = fixItAction;
        }
        
        public ErrorData(QNT_Timestamp time, string description, Action fixItAction, List<Target> affectedTargets)
        {
            Time = time;
            this.description = description;
            AffectedTargets = affectedTargets;
            this.fixItAction = fixItAction;
        }

        public void FixError()
        {
            if (!HasAutoFix) return;
            fixItAction?.Invoke();
        }

        public void AddEntry(ErrorEntry entry) => Entry = entry;

        public void Select(bool select)
        {
            Entry.SelectEntry(select);
            
            if (select)
            {
                foreach (var target in AffectedTargets)
                {
                    target.VisualSelect();
                }
            }
            else
            {
                foreach (var target in AffectedTargets)
                {
                    target.VisualDeselect();
                }
            }
        }
    }
}
