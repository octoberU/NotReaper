using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper;
using UnityEngine.UI;
using System.Linq;
using System;
using NotReaper.Timing;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using NotReaper.UI;
using NotReaper.UserInput;
using UnityEngine.InputSystem;

namespace NotReaper.Modifier
{
    public class ModifierSelectionHandler : MonoBehaviour
    {
        public static ModifierSelectionHandler Instance = null;
        public Transform posGetter = null;
        public GameObject selectionBox;
        public static bool isPasting = false;
        public List<Modifier> tempCopiedModifiers = new List<Modifier>();

        private List<Modifier> selectedEntries = new List<Modifier>();
        private List<ModifierDTO> copiedEntries = new List<ModifierDTO>();

        private Camera cam;
        private CopyMode mode = CopyMode.Copy;
        private Vector3 dragStartPos;
        private Renderer rend;

        private bool isCtrlDown;
        private bool dragSelect;
        private bool isMouseDown;

        private void Start()
        {
            if (Instance is null) Instance = this;
            else
            {
                Debug.LogWarning("Trying to create a second ModifierSelectionhandler instance.");
                return;
            }
            cam = NRDependencyInjector.Get<Timeline>().timelineCamera.GetComponent<Camera>();
            posGetter = GameObject.Instantiate(new GameObject("PosGetter").transform);
            posGetter.SetParent(Timeline.timelineNotesStatic);
            rend = selectionBox.GetComponent<Renderer>();
            selectionBox.transform.SetParent(Timeline.timelineNotesStatic);
            selectionBox.SetActive(false);
        }

        public void CleanUp()
        {
            selectedEntries.Clear();
            copiedEntries.Clear();
            mode = CopyMode.Copy;
        }

        public void DeleteSelectedModifiers()
        {
            bool couldAdd = false;
            if(!ZOffsetBaker.baking) couldAdd = ModifierUndoRedo.Instance.AddAction(selectedEntries.ToList(), Action.Delete);
            for (int i = 0; i < selectedEntries.Count; i++)
            {
                selectedEntries[i].Delete();
            }
            selectedEntries.Clear();
            ModifierHandler.Instance.HideWindow(false);
            StartCoroutine(ModifierHandler.Instance.IUpdateLevels());
        }

        public void Restore(List<ModifierDTO> dtoList)
        {
            mode = CopyMode.Restore;
            copiedEntries.Clear();
            copiedEntries = dtoList.ToList();
            PasteCopiedModifiers();
        }

        public void CopySelectedModifiers()
        {
            mode = CopyMode.Copy;
            copiedEntries.Clear();
            copiedEntries = GetDTOList();
        }

        public void CutSelectedModifiers()
        {
            if (selectedEntries.Count == 0) return;
            mode = CopyMode.Cut;
            copiedEntries.Clear();
            copiedEntries = GetDTOList();
            ModifierUndoRedo.Instance.AddAction(selectedEntries.ToList(), Action.Delete);
            /*for (int i = 0; i < selectedEntries.Count; i++)
            {
                ModifierHandler.Instance.DeleteModifier();
                selectedEntries[i].Delete();
            }*/
            ModifierHandler.Instance.DeleteModifier();
            selectedEntries.Clear();
            StartCoroutine(ModifierHandler.Instance.IUpdateLevels());
        }

        private List<ModifierDTO> GetDTOList()
        {
            List<ModifierDTO> list = new List<ModifierDTO>();
            foreach (Modifier m in selectedEntries)
            {
               list.Add(m.GetDTO());
            }
            return list;
        }

        public void PasteCopiedModifiers()
        {
            if (copiedEntries.Count == 0) return;
            copiedEntries.Sort((mod1, mod2) => mod1.startTick.CompareTo(mod2.startTick));
            ModifierHandler.Instance.DropCurrentModifier();
            QNT_Timestamp newStartTick = Timeline.time;
            QNT_Timestamp firstTick = new QNT_Timestamp((ulong)copiedEntries.First().startTick);
            float tickOffset = newStartTick.tick - copiedEntries.First().startTick;
            posGetter.position = cam.transform.position; //Vector3.zero;
            float positionOffset = posGetter.position.x - copiedEntries.First().startPosX;
            float miniOffset = MiniTimeline.Instance.GetXForTheBookmarkThingy() - copiedEntries.First().miniStartX;
            if (tickOffset == 0 && mode == CopyMode.Copy) return;
            isPasting = mode != CopyMode.Restore;
            if(mode != CopyMode.Restore)
            {
                foreach (ModifierDTO dto in copiedEntries)
                {
                    dto.startTick += tickOffset;
                    dto.startPosX += positionOffset;
                    dto.miniStartX += miniOffset;
                    if (dto.endTick != 0)
                    {
                        dto.endTick += tickOffset;
                        dto.endPosX += positionOffset;
                        dto.miniEndX += miniOffset;
                    }
                }
            }
            StartCoroutine(ModifierHandler.Instance.LoadModifiers(copiedEntries, false, true));
            isPasting = false;
            DeselectAllModifiers();
            if (mode == CopyMode.Restore)
            {
                copiedEntries.Clear();
                mode = CopyMode.Copy;
            }
            else
            {
                ModifierUndoRedo.Instance.AddAction(tempCopiedModifiers.ToList(), Action.Create);
                tempCopiedModifiers.Clear();
            }
           
           
        }
        
        public void DeselectAllModifiers()
        {
            ModifierHandler.Instance.DropCurrentModifier();
            foreach (Modifier m in selectedEntries)
            {
                m.Select(false);
            }
            selectedEntries.Clear();
            if(mode == CopyMode.Cut) copiedEntries.Clear();
            ModifierHandler.Instance.HideWindow(false);
            ModifierHandler.Instance.FillData(null, false, true);
        }

        public void SelectModifier(Modifier m, bool singleSelect)
        {
            ModifierHandler.Instance.DropCurrentModifier();
            if (selectedEntries.Count == 0) singleSelect = true;
            if (selectedEntries.Contains(m))
            {
                if (singleSelect)
                {
                    bool reselect = selectedEntries.Count > 1;
                    DeselectAllModifiers();
                   
                    if (reselect)
                    {
                        selectedEntries.Add(m);
                        m.Select(true);
                    }
                }
                else
                {                  
                    selectedEntries.Remove(m);
                    m.Select(false);

                }                
            }
            else
            {
                if (singleSelect)
                {
                    foreach (Modifier mod in selectedEntries) mod.Select(false);
                    selectedEntries.Clear();
                    selectedEntries.Add(m);
                    m.Select(true);
                }
                else
                {
                    selectedEntries.Add(m);
                    m.Select(true);
                }                
            }
            bool singleActive = selectedEntries.Count < 2;
            ModifierHandler.Instance.HideWindow(!singleActive || !singleSelect);
            if (selectedEntries.Count == 0) ModifierHandler.Instance.HideWindow(false);
            ModifierHandler.Instance.FillData(m, singleActive && singleSelect, selectedEntries.Count == 0);
        }

        private void SelectAll()
        {
            ModifierHandler.Instance.DropCurrentModifier();
            if (selectedEntries.Count > 0)
            {
                ModifierHandler.Instance.DropCurrentModifier();
                foreach (Modifier m in selectedEntries) m.Select(false);
            }
            foreach (Modifier m in ModifierHandler.Instance.modifiers)
            {
                selectedEntries.Add(m);
                m.Select(true);
            }
            ModifierHandler.Instance.HideWindow(true);
        }

        private void Update()
        {
            if (!ModifierHandler.activated) return;
            if (dragSelect && isMouseDown)
            {
                UpdateDragSelect();
            }
        }

        private void UpdateDragSelect()
        {
            float sizeX = dragStartPos.x - Timeline.timelineNotesStatic.InverseTransformPoint(cam.ScreenToWorldPoint(Input.mousePosition)).x;

            if (Mathf.Abs(sizeX) > .2f)
            {
                if (!selectionBox.activeInHierarchy) selectionBox.SetActive(true);
                Vector3 newPos = selectionBox.transform.localPosition;

                sizeX *= -1f;
                selectionBox.transform.localScale = new Vector2(sizeX, selectionBox.transform.localScale.y);
                newPos.x = dragStartPos.x + (sizeX / 2);
                selectionBox.transform.localPosition = new Vector2(newPos.x, selectionBox.transform.localPosition.y);

                for (int i = 0; i < ModifierHandler.Instance.modifiers.Count; i++)
                {
                    if (ModifierHandler.Instance.modifiers[i].startMark.transform.position.x > rend.bounds.min.x && ModifierHandler.Instance.modifiers[i].startMark.transform.position.x < rend.bounds.max.x)
                    {
                        if (!selectedEntries.Contains(ModifierHandler.Instance.modifiers[i]))
                        {
                            SelectModifier(ModifierHandler.Instance.modifiers[i], false);
                        }
                    }
                    else
                    {
                        if (selectedEntries.Contains(ModifierHandler.Instance.modifiers[i]))
                        {
                            SelectModifier(ModifierHandler.Instance.modifiers[i], false);
                        }
                    }
                }
            }
        }

        private enum CopyMode
        {
            Copy,
            Cut,
            Restore
        }

        private void MouseDown()
        {
            isMouseDown = true;
            int layerMask = LayerMask.GetMask("Modifier");
            RaycastHit2D hit = Physics2D.Raycast(cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 0f, layerMask);
            if (hit.collider != null)
            {
                Modifier m = hit.transform.GetComponent<ClickNotifier>().GetModifier();

                if (isCtrlDown)
                {
                    if (m.isCreated) SelectModifier(m, false);
                }
                else
                {
                    if (m.isCreated) SelectModifier(m, true);
                }

            }
            else if (isCtrlDown)
            {
                DeselectAllModifiers();
            }
            else
            {
                layerMask = LayerMask.GetMask("Timeline");
                hit = Physics2D.Raycast(cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 1000000f, layerMask);
                if (hit.collider != null)
                {
                    if (hit.transform.gameObject.layer == 14)
                    {
                        DeselectAllModifiers();
                    }
                }
            }



            if (isCtrlDown)
            {
                dragStartPos = Timeline.timelineNotesStatic.InverseTransformPoint(cam.ScreenToWorldPoint(Input.mousePosition));
                selectionBox.transform.SetParent(Timeline.timelineNotesStatic);
                Vector3 newPos = selectionBox.transform.localPosition;
                newPos.x = dragStartPos.x;
                selectionBox.transform.localPosition = newPos;
                dragSelect = true;
            }
            
        }

        private void MouseUp()
        {
            if (dragSelect)
            {
                dragStartPos = Vector3.zero;
                selectionBox.SetActive(false);
                dragSelect = false;
            }
            isMouseDown = false;
        }

        private void OnCtrlDown()
        {
            //dragSelect = true;
            isCtrlDown = true;
        }

        private void OnCtrlUp()
        {
            if (selectionBox.activeInHierarchy)
            {
                selectionBox.SetActive(false);
            }
            dragSelect = false;
            isCtrlDown = false;
        }

        private void RemoveModifier()
        {
            int layerMask = LayerMask.GetMask("Modifier");
            RaycastHit2D hit = Physics2D.Raycast(cam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 1000000f, layerMask);
            if (hit.collider != null)
            {
                Modifier m = hit.transform.GetComponent<ClickNotifier>().GetModifier();
                DeselectAllModifiers();
                selectedEntries.Add(m);
                DeleteSelectedModifiers();
            }
        }

        private void OnScrubPerformed(InputAction.CallbackContext ctx)
        {
            if (ModifierHandler.Instance.IsDropdownOpen()) return;
            Timeline.instance.ScrubTimeline(ctx.ReadValue<float>() < 0, isCtrlDown);
        }

        internal void RegisterCallbacks(ModifierKeybinds actions)
        {
            actions.Modifiers.DragSelect.performed += _ => OnCtrlDown();
            actions.Modifiers.DragSelect.canceled += _ => OnCtrlUp();
            actions.Modifiers.Copy.performed += _ => CopySelectedModifiers();
            actions.Modifiers.Paste.performed += _ => PasteCopiedModifiers();
            actions.Modifiers.Cut.performed += _ => CutSelectedModifiers();
            actions.Modifiers.DeselectAll.performed += _ => DeselectAllModifiers();
            actions.Modifiers.SelectAll.performed += _ => SelectAll();
            actions.Modifiers.Undo.performed += _ => ModifierUndoRedo.Instance.Undo();
            actions.Modifiers.Redo.performed += _ => ModifierUndoRedo.Instance.Redo();
            actions.Modifiers.LeftMouseClick.performed += _ => MouseDown();
            actions.Modifiers.LeftMouseClick.canceled += _ => MouseUp();
            actions.Modifiers.Delete.performed += _ => ModifierHandler.Instance.DeleteModifier();
            actions.Modifiers.RemoveModifier.performed += _ => RemoveModifier();
            actions.Modifiers.Scrub.performed += OnScrubPerformed;
        }
    }
}

