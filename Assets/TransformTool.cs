using NotReaper;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.InputSystem;

namespace NotReaper.Tools
{
    public class TransformTool : MonoBehaviour
    {
        [SerializeField] Timeline timeline;
        [SerializeField] Canvas canvas;
        [SerializeField] TextMeshProUGUI countLabel;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] public Transform centerPoint;
        public static TransformTool instance;
        RectTransform rectTransform;
        int lastSelectedTargetCount = 0;
        [SerializeField] SelectionMesh selectionMesh;

        private static InputAction mousePosition;

        internal float CanvasScale => (1 / canvas.transform.localScale.x);

        public static bool ShowTransformTool { get; set; } = true;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            instance = this;
        }

        private void Start()
        {
            mousePosition = KeybindManager.Global.MousePosition;
        }

        private void Update()
        {
            if (!ShowTransformTool)
            {
                ShowOverlay(false);
                return;
            }
            
            if (EditorNotes.SelectedNotes.Count < 2)
            {
                ShowOverlay(false);
                return;
            }
            else if(EditorNotes.SelectedNotes.Count != lastSelectedTargetCount)
            {
                ShowOverlay(true);

                lastSelectedTargetCount = EditorNotes.SelectedNotes.Count;
                UpdateOverlay();

                if (rectTransform.sizeDelta.x == 0 || rectTransform.sizeDelta.y == 0)
                    ShowOverlay(false);
            }
        }

        private void ShowOverlay(bool show)
        {
            if (show)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }
            else
            {
                canvasGroup.alpha = 0f;
                canvasGroup.blocksRaycasts = false;
                lastSelectedTargetCount = 0;
            }
        }

        [ContextMenu("Debug centering")]
        public void UpdateOverlay()
        {
            if (canvasGroup.alpha == 0f) return;
            rectTransform.pivot = new Vector2(0f, 1f);
            rectTransform.rotation = Quaternion.Euler(Vector3.zero);
            float minX = 999f, minY = 999f, maxX = -999f, maxY = -999f;
            float totalX = 0f, totalY = 0f;
            foreach (var selectedTarget in EditorNotes.SelectedNotes)
            {
                NotReaper.Targets.TargetData current = selectedTarget.data;
                if (current.x > maxX) maxX = current.x;
                if (current.y > maxY) maxY = current.y;
                if (current.x < minX) minX = current.x;
                if (current.y < minY) minY = current.y;
                totalX += current.x;
                totalY += current.y;
            }
            float centerX = totalX / EditorNotes.SelectedNotes.Count;
            float centerY = totalY / EditorNotes.SelectedNotes.Count;

            countLabel.text = lastSelectedTargetCount.ToString();
            transform.position = new Vector3(minX, maxY);
            rectTransform.sizeDelta = new Vector2(Mathf.Abs(maxX - minX), Mathf.Abs(maxY - minY)) * (1 / canvas.transform.localScale.x);
            centerPoint.localPosition = new Vector2((float)(rectTransform.sizeDelta.x * 0.5), (float)-(rectTransform.sizeDelta.y * 0.5));
            selectionMesh.GenerateMeshForTimeline();
        }

        public void SetPivot(Vector2 pivot)
        {
        
            Vector3 deltaPosition = rectTransform.pivot - pivot;    // get change in pivot
            deltaPosition.Scale(rectTransform.rect.size);           // apply sizing
            deltaPosition.Scale(rectTransform.localScale);          // apply scaling
            deltaPosition = rectTransform.rotation * deltaPosition; // apply rotation

            rectTransform.pivot = pivot;                            // change the pivot
            rectTransform.localPosition -= deltaPosition;           // reverse the position change
        }
    
        public void SetPivotToCenterPoint()
        {
            rectTransform.rotation = Quaternion.Euler(Vector3.zero);
            SetPivot(Vector2.up);
            Vector3 deltaPosition = rectTransform.position - centerPoint.position;
            deltaPosition *= ((Vector2.one + (Vector2.down * 2f)) / rectTransform.sizeDelta ) * (1 / canvas.transform.localScale.x);
            Vector2 pivot = new Vector2(deltaPosition.x * -1, deltaPosition.y + 1f);
            SetPivot(pivot);
        }

        public void RotateNotes(float angle) => EditorTargets.RotateTargets(EditorNotes.SelectedNotes, angle, centerPoint.position);

        /// <summary>
        /// Checks if the pointer is over the transform overlay.
        /// </summary>
        /// <returns>True if pointer is over transform overlay.</returns>
        public static bool IsPointerOverTransformOverlay()
        {
            if (KeybindManager.Global.Modifier.IsCtrlDown())
                return false;

            var pointerData = new PointerEventData(EventSystem.current);
            pointerData.position = mousePosition.ReadValue<Vector2>();
            List<RaycastResult> result = new();
            EventSystem.current.RaycastAll(pointerData, result);
            return result.Any(r => r.gameObject.tag == "TransformOverlay");
        }
    }

}
