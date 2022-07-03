using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Tools;
using UnityEngine;

namespace NotReaper.Modifiers
{
    public class ModifierUndoRedo : Singleton<ModifierUndoRedo>
    {
        /// <summary>
        /// Contains the complete list of actions the user has done recently.
        /// </summary>
        private static List<ModifierAction> actions = new List<ModifierAction>();

        /// <summary>
        /// Contains the actions the user has "undone" for future use.
        /// </summary>
        private static List<ModifierAction> redoActions = new List<ModifierAction>();

        private static ModifierManager manager;

        private const int MaxSavedActions = 20;
       private void Start()
        {
            manager = NRDependencyInjector.Get<ModifierManager>();
            EditorState.OnEditorReset += ClearActions;
        }

        /// <summary>
        /// Undo the last action performed by the user.
        /// </summary>
        public static void Undo()
        {
            if (actions.Count <= 0) return;

            ModifierAction action = actions.Last();

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

            ModifierAction action = redoActions.Last();

            action.DoAction(manager);

            actions.Add(action);
            redoActions.RemoveAt(redoActions.Count - 1);
            //EditorScale.ReapplyScale();
        }
        /// <summary>
        /// Add an action that can be un- and redone.
        /// </summary>
        /// <param name="action">The action to add.</param>
        public static void AddAction(ModifierAction action)
        {
            if (actions.Count <= MaxSavedActions)
            {
                actions.Add(action);
            }
            else
            {
                while (MaxSavedActions > actions.Count)
                {
                    actions.RemoveAt(0);
                }
                actions.Add(action);
            }
            action.DoAction(manager);
            redoActions = new List<ModifierAction>();
        }
        /// <summary>
        /// Removes an action from the undo/redo history.
        /// </summary>
        /// <param name="action">The action to remove.</param>
        public static void RemoveAction(ModifierAction action)
        {
            if(actions.Contains(action))
                actions.Remove(action);

            if (redoActions.Contains(action))
                redoActions.Remove(action);
        }

        public static void ClearActions()
        {
            actions = new List<ModifierAction>();
            redoActions = new List<ModifierAction>();
        }
    }

    public abstract class ModifierAction
    {
        public abstract void DoAction(ModifierManager manager);
        public abstract void UndoAction(ModifierManager manager);
    }
}
