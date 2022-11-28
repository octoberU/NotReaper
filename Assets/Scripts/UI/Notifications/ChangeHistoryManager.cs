using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Timing;
using NotReaper.Tools;
using NotReaper.UI;
using NotReaper.UI.Components;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.Notifications
{
    public class ChangeHistoryManager : MonoBehaviour
    {
        [SerializeField] private GameObject divider;
        [SerializeField] private ChangeHistoryItem prefab;
        [SerializeField] private Transform contentParent;
        [SerializeField] private List<ChangeHistoryItem> items = new();

        internal readonly struct ChangeData
        {
            public readonly int index;
            public readonly bool isUndo;
            public readonly string actionName;
            public readonly QNT_Timestamp time;
            public readonly bool browsable;
            
            public ChangeData(int index, bool isUndo, NRAction action)
            {
                this.index = index;
                this.isUndo = isUndo;
                actionName = action.ActionName;
                time = action.Time;
                browsable = action.Browsable;
            }
        }

        private void Awake()
        {
            NRSettings.onSettingsSaved += _ =>
            {
                CreateItems();
            };
            
            NRSettings.OnLoad(CreateItems);

            void CreateItems()
            {
                if (NRSettings.config.historySize > items.Count)
                {
                    var needed = NRSettings.config.historySize - items.Count;
                    for(int i = 0; i < needed; i++)
                    {
                        var item = Instantiate(prefab, contentParent);
                        item.gameObject.SetActive(false);
                        items.Add(item);
                    }
                }
            }
            
            gameObject.SetActive(false);
        }

        internal void OnClick(ChangeData data)
        {
            if (data.isUndo)
            {
                for (int i = 0; i <= data.index; i++)
                    UndoRedoManager.Undo();
            }
            else
            {
                for(int i = 0; i <= data.index; i++)
                    UndoRedoManager.Redo();
            }
            
            UpdateHistory();
        }

        public void UpdateHistory()
        {
            var redoActions = UndoRedoManager.RedoActions;
            var actions = UndoRedoManager.Actions;

            List<ChangeData> allActions = new();
            
            List<ChangeData> undoActions = new();
            for(int i = 0; i < actions.Count; i++)
                undoActions.Add(new(actions.Count - 1 - i, true, actions[i]));

            undoActions.Reverse();


            for(int i = 0; i < redoActions.Count; i++)
                allActions.Add(new(redoActions.Count - 1 - i, false, redoActions[i]));

            allActions.AddRange(undoActions);

            if(redoActions.Count == 0 && divider.activeSelf)
                divider.SetActive(false);

            bool wasRedoAction = false;
            
            for(int i = 0; i < 20; i++)
            {

                var item = items[i];

                if (i >= allActions.Count)
                {
                    if(item.gameObject.activeSelf)
                        item.gameObject.SetActive(false);

                    continue;
                }

                if (!item.gameObject.activeSelf)
                {
                    item.gameObject.SetActive(true);
                    item.UpdateSkin();
                }

                var action = allActions[i];
                
                if (wasRedoAction && action.isUndo)
                {
                    divider.transform.SetSiblingIndex(i);

                    if (!divider.activeSelf)
                        divider.SetActive(true);
                }

                item.Init(this, action);
                wasRedoAction = !action.isUndo;
            }
        }
    }
}
