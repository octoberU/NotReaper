using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

namespace NotReaper
{
    public abstract class TimelineUndoRedo<TData> : Singleton<TimelineUndoRedo<TData>> where TData : ContentData
    {
        private static List<TimelineAction<TData>> actions = new();
        private static List<TimelineAction<TData>> redoActions = new();
        private static TimelineManager<TData> manager;

        private void Start()
        {
            manager = GetManager();
            EditorState.OnEditorReset += ClearActions;
        }
        protected abstract TimelineManager<TData> GetManager();
        
          /// <summary>
        /// Undo the last action performed by the user.
        /// </summary>
        public static void Undo()
        {
            if (actions.Count <= 0) return;

            TimelineAction<TData> action = actions.Last();

            action.UndoAction(manager);

            redoActions.Add(action);
            actions.RemoveAt(actions.Count - 1);

            //EditorScale.ReapplyScale();
        }
        /// <summary>
        /// Redo the last action the user has undone.
        /// </summary>
        public static void Redo()
        {

            if (redoActions.Count <= 0) return;

            TimelineAction<TData> action = redoActions.Last();

            action.DoAction(manager);

            actions.Add(action);
            redoActions.RemoveAt(redoActions.Count - 1);
            //EditorScale.ReapplyScale();
        }
        /// <summary>
        /// Add an action that can be un- and redone.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public static void AddAction(TimelineAction<TData> action)
        {
            var size = NRSettings.config.historySize;
            if (actions.Count <= size)
            {
                actions.Add(action);
            }
            else
            {
                while (size < actions.Count)
                {
                    actions.RemoveAt(0);
                }
                actions.Add(action);
            }
            action.DoAction(manager);
            redoActions = new List<TimelineAction<TData>>();
            EditorIO.SetDirty();
        }
        /// <summary>
        /// Removes an action from the undo/redo history.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        public static void RemoveAction(TimelineAction<TData> action)
        {
            if(actions.Contains(action))
                actions.Remove(action);

            if (redoActions.Contains(action))
                redoActions.Remove(action);
        }

        public static void ClearActions()
        {
            actions = new List<TimelineAction<TData>>();
            redoActions = new List<TimelineAction<TData>>();
        }
    }
    
    public abstract class TimelineAction<TData> where TData : ContentData
    {
        public abstract void DoAction(TimelineManager<TData> manager);
        public abstract void UndoAction(TimelineManager<TData> manager);
    }
}
