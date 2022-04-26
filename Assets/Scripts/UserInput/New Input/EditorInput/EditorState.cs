using NotReaper.Models;
using NotReaper.UserInput;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
namespace NotReaper
{
	public static class EditorState
	{
		public static State<EditorMode> Mode { get; private set; } = new State<EditorMode>(EditorMode.Compose);
		public static State<TargetHandType> Hand { get; private set; } = new State<TargetHandType>(TargetHandType.Left);
		public static State<EditorTool> Tool { get; private set; } = new State<EditorTool>(EditorTool.None);
		public static State<TargetBehavior> Behavior { get; private set; } = new State<TargetBehavior>(TargetBehavior.Standard);
		public static State<SnappingMode> Snapping { get; private set; } = new State<SnappingMode>(SnappingMode.Grid);
		public static State<TargetHitsound> Hitsound { get; private set; } = new State<TargetHitsound>(TargetHitsound.Standard);
		public static bool IsInUI { get; private set; }
		private static int uiElements = 0;
		private static bool inUiLocked = false;
		public static bool IsOverGrid { get; private set; }
		public static bool IsOverTimeline { get; private set; }
		public static bool IsToolActive(EditorTool tool) => activeTools.Contains(tool);

		private static List<EditorTool> activeTools = new List<EditorTool>();

		#region Events
		public delegate void ModeChangedEventHandler(EditorMode mode);
		public static event ModeChangedEventHandler ModeChanged;

		public delegate void BehaviorChangedEventHandler(TargetBehavior behavior);
		public static event BehaviorChangedEventHandler BehaviorChanged;

		public delegate void ToolChangedEventHandler(EditorTool tool);
		public static event ToolChangedEventHandler ToolChanged;

		public delegate void HandChangedEventHandler(TargetHandType hand);
		public static event HandChangedEventHandler HandChanged;

		public delegate void SnappingChangedEventHandler(SnappingMode mode);
		public static event SnappingChangedEventHandler SnappingChanged;

		public delegate void HitsoundChangedEventHandler(TargetHitsound hitsound);
		public static event HitsoundChangedEventHandler HitsoundChanged;

		public delegate void IsInUIChangedEventHandler(bool inUI);
		public static event IsInUIChangedEventHandler IsInUIChanged;

		public delegate void EditorResetEventHandler();
		public static event EditorResetEventHandler OnEditorReset;
		#endregion

		#region Setter
		public static void SelectHand(TargetHandType hand)
		{
			Hand.Current = hand;
			HandChanged?.Invoke(hand);
		}
		public static void SelectTool(EditorTool tool)
		{
            if (activeTools.Contains(tool))
            {
				activeTools.Remove(tool);
            }
            else
            {
				activeTools.Add(tool);
            }

			if(activeTools.Count > 0)
            {
				tool = activeTools.Last();
            }
            else
            {
				tool = EditorTool.None;
            }

			Tool.Current = tool;
			ToolChanged?.Invoke(tool);
		}

		public static void SelectBehavior(TargetBehavior behavior)
		{
			Behavior.Current = behavior;
			BehaviorChanged?.Invoke(behavior);
		}

		public static void SelectSnappingMode(SnappingMode snap)
		{
			Snapping.Current = snap;
			SnappingChanged?.Invoke(snap);
		}

		public static void SelectHitsound(TargetHitsound hitsound)
		{
			Hitsound.Current = hitsound;
			HitsoundChanged?.Invoke(hitsound);
		}

		public static void SelectMode(EditorMode mode)
		{
			Mode.Current = mode;
			SetIsInUI(mode != EditorMode.Compose);
			ModeChanged?.Invoke(mode);
		}

		public static void SetIsInUI(bool inUI)
        {
			/*
			//first, count the number of active UI elements
			if (inUI)
				uiElements++;
			else
				uiElements--;

			if (uiElements < 0)
				uiElements = 0;

			//don't raise the event if the new state equals old state
			if (IsInUI == inUI)
				return;
			//don't raise the event if any ui elements are active still, since it doesn't change the sate
			if (!inUI && uiElements > 0)
				return;
			//don't raise the event if ui elements were active already, since it doesn't change the state
			if (inUI && uiElements > 1)
				return;
			*/
			if (inUiLocked)
				return;

			IsInUI = inUI;
			IsInUIChanged?.Invoke(inUI);
        }

		public static void LockInUI() => inUiLocked = true;
		public static void UnlockInUI() => inUiLocked = false;

		public static void SetIsOverGrid(bool isOverGrid)
        {
			IsOverGrid = isOverGrid;
        }
		public static void SetOverTimeline(bool isOver)
        {
			IsOverTimeline = isOver;
        }
		/// <summary>
		/// Resets all registered components to their initial state.
		/// </summary>
		public static void ResetEditor()
        {
			OnEditorReset?.Invoke();
        }

		#endregion

		#region State
		public class State<T> where T : Enum
        {
            private T current;
			private T previous;
            public T Current
            {
                get { return current; }
                set 
				{
					if(Equals(current, value))
                    {
						return;
                    }
					previous = current;
					current = value; 
				}
            }

            public T Previous
            {
				get { return previous; }
				set { previous = value; }
            }

			public State(T current)
            {
				this.current = current;
				this.previous = current;
            }

			public State(T current, T previous)
            {
				this.current = current;
				this.previous = previous;
            }
        }
        #endregion
    }
}

