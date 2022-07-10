using System.Collections;
using System.Collections.Generic;
using System.Data.OleDb;
using NotReaper;
using NotReaper.Modifiers;
using UnityEngine;

namespace NotReaper
{
    public class TimelineMoveAction<TData> : TimelineAction<TData> where TData : ContentData
    {
        protected List<MoveData> moveData;
        protected bool isInitialMove = true;
        
        public TimelineMoveAction(List<MoveData> moveData) => this.moveData = new (moveData);

        public override void DoAction(TimelineManager<TData> manager)
        {
            if (isInitialMove)
            {
                isInitialMove = false;
                return;
            }
            
            foreach (var data in moveData)
            {
                manager.MoveContentFromAction(data.content, data.newTimeframe, data.newTrack);
            }
        }

        public override void UndoAction(TimelineManager<TData> manager)
        {
            foreach (var data in moveData)
            {
                manager.MoveContentFromAction(data.content, data.oldTimeframe, data.oldTrack);
            }
        }
    }

    public class MoveData
    {
        public Content content;
        public Timeframe oldTimeframe;
        public Timeframe newTimeframe;
        public int oldTrack;
        public int newTrack;
        public float distanceToMouse;
    }

    public enum MoveOperation
    {
        ChangeStart,
        ChangeEnd,
        ChangeTrack,
        Move
    }
}
