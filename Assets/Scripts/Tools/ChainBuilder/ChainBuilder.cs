using System;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Grid;
using NotReaper.Models;
using NotReaper.Targets;
using NotReaper.UserInput;
using UnityEngine;
using DG.Tweening;
using NotReaper.Timing;
using UnityEngine.InputSystem;
using NotReaper.Notifications;

namespace NotReaper.Tools.ChainBuilder { 

	public static class Vector2Extension {
     public static Vector2 Rotate(this Vector2 v, float degrees) {
         float radians = degrees * Mathf.Deg2Rad;
         float sin = Mathf.Sin(radians);
         float cos = Mathf.Cos(radians);
         
         float tx = v.x;
         float ty = v.y;
 
         return new Vector2(cos * tx - sin * ty, sin * tx + cos * ty);
     }
 }
 

	public class ChainBuilder : NRInput<ChainbuilderKeybinds> {
		public LayerMask notesLayer;


		private TargetIcon[] _iconsUnderMouse = new TargetIcon[0];

		/** Fetch all icons currently under the mouse
		 *  Will only ever happen once per frame */
		public TargetIcon[] iconsUnderMouse {
			get {
				return _iconsUnderMouse = _iconsUnderMouse == null
					? MouseUtil.IconsUnderMouse(timeline)
					: _iconsUnderMouse;
			}
			set { _iconsUnderMouse = value; }
		}

		// Fetch the highest priority target (closest to current time)
		public TargetIcon iconUnderMouse {
			get {
				return iconsUnderMouse != null && iconsUnderMouse.Length > 0
					? iconsUnderMouse[0]
					: null;
			}
		}


		public GameObject curvedLinePrefab;
		public GameObject linePointPrefab;
		public GameObject nodeTempPointPrefab;

		[SerializeField] private Transform chainBuilderLinesParent;
		private Transform tempNodeIconsParent;

		public LayerMask chainBuilderLayer;

		public bool isDragging = false;
		public bool isEditMode = false;
		public bool activated = false;
		public bool isHovering = false;

		private Transform draggingPoint;
		public GameObject activeChain;

		private int prevDrawPointsAmt = 0;

		private Target startClickNote = null;

		private bool isMouseDown;
		public bool snapAngle;

		[NRInject] private ChainBuilderWindow chainBuilderWindow;
		[NRInject] private MappingInput mappingInput;

		public static ChainBuilder Instance = null;
		private static Timeline timeline;

		/*private CanvasGroup canvas;
		private RectTransform rect;
		private BoxCollider2D boxCollider;*/

		protected override void Awake() 
		{
			if (Instance is null) Instance = this;
            else
            {
				Debug.LogWarning("ChainBuilder already exists.");
				return;
            }

			base.Awake();
			Vector3 defaultPos;
			defaultPos.x = 289.0f;
			defaultPos.y = -92.2f;
			defaultPos.z = -10.0f;
			//rect = chainBuilderWindow.GetComponent<RectTransform>();
			//canvas = chainBuilderWindow.GetComponent<CanvasGroup>();
			//boxCollider = chainBuilderWindow.GetComponent<BoxCollider2D>();
			/*rect.localPosition = defaultPos;
			canvas.alpha = 0.0f;
			canvas.blocksRaycasts = false;
			canvas.interactable = false;
			boxCollider.enabled = false;*/
			
		}

        private void Start()
        {
			chainBuilderWindow.angleIncrement.OnValueChanged += OnAngleVelocityChange;
			chainBuilderWindow.angleIncrementIncrement.OnValueChanged += OnAngleAccelerationChange;
			chainBuilderWindow.stepDistance.OnValueChanged += OnStepDistanceChange;
			chainBuilderWindow.stepIncrement.OnValueChanged += OnStepIncrementChange;
			timeline = NRDependencyInjector.Get<Timeline>();
			EditorNotes.onSelectedNoteCountChanged += OnSelectedNoteCountChanged;
			OnSelectedNoteCountChanged(0);
		}

        [NRListener]
		private void OnBehaviorChanged(TargetBehavior behavior)
		{
			if (activated)
			{
				Activate(false);
			}
		}

		/// <summary>
		/// Sets if the tool is active or not.
		/// </summary>
		/// <param name="active"></param>
		public void Activate(bool active) 
		{
			bool wasActive = activated == true;
			isHovering = false;
			activated = active;

			startClickNote = null;

			chainBuilderWindow.pathBuilderInterval.elements = NRSettings.config.snaps;
			EditorState.SelectTool(EditorTool.ChainBuilder);
			if (active) 
			{
				OnActivated();
				bool validNoteSelected = (EditorNotes.SelectedNotes.Count == 1 && EditorNotes.SelectedNotes[0].data.behavior == TargetBehavior.Legacy_Pathbuilder);
				//EditorState.SelectTool(EditorTool.ChainBuilder);
				if(!validNoteSelected) 
				{
					/*if(EditorNotes.SelectedNotes.Count == 1 && !EditorNotes.SelectedNotes[0].data.isPathbuilderTarget)
                    {
						SelectTarget();
                    }
                    else
                    {
						EditorNotes.DeselectAllTargets();
                    }*/
					EditorNotes.DeselectAllTargets();
				}
				EditorState.SelectSnappingMode(SnappingMode.None);
				
				ShowPathbuilderWindow(true);

				if(validNoteSelected) 
				{
					SetPathbuilderStateToSelectedNote();
				}
			}
			else 
			{
				EditorState.SelectSnappingMode(EditorState.Snapping.Previous);
				ShowPathbuilderWindow(false);
				OnDeactivated();
			}

			if(wasActive && active == false) {
				List<TargetData> nonGeneratedNotes = new List<TargetData>();

				foreach(Target note in EditorNotes.Notes) {
					if(note.data.behavior == TargetBehavior.Legacy_Pathbuilder && note.data.legacyPathbuilderData.createdNotes == false) {
						nonGeneratedNotes.Add(note.data);
					}
				}

				foreach(var data in nonGeneratedNotes) {
					GenerateChainNotes(data);
				}
			}
		}

		public void SwitchToNewPB()
        {
			Activate(false);
			mappingInput.TogglePathbuilder();
        }

		private void ShowPathbuilderWindow(bool show)
        {
			if (show) chainBuilderWindow.Show();
			else chainBuilderWindow.Hide();
		}

        protected override void OnActivated()
        {
			base.OnActivated();
        }

        protected override void OnDeactivated()
        {
            base.OnDeactivated();
        }

        public void OnIntervalChange() {
			Target target = EditorNotes.SelectedNotes.First();
			if(target == null || target.data.behavior != TargetBehavior.Legacy_Pathbuilder) {
				return;
			}

			string temp = chainBuilderWindow.pathBuilderInterval.elements[chainBuilderWindow.pathBuilderInterval.index];
			int snap = 4;
			int.TryParse(temp.Substring(2), out snap);

			chainBuilderWindow.angleIncrement.value = target.data.legacyPathbuilderData.angle;
			chainBuilderWindow.stepDistance.value = target.data.legacyPathbuilderData.stepDistance;

			target.data.legacyPathbuilderData.interval = snap;
			GenerateChainNotes(target.data);
		}

		public void ChangeInterval(bool next)
        {
			if (next) chainBuilderWindow.pathBuilderInterval.ForwardClick();
			else chainBuilderWindow.pathBuilderInterval.PreviousClick();
        }


		public void OnAngleVelocityChange(float value) {
			Target target = EditorNotes.SelectedNotes.First();
			if(target == null || target.data.behavior != TargetBehavior.Legacy_Pathbuilder) {
				return;
			}

			target.data.legacyPathbuilderData.angle = value;
			GenerateChainNotes(target.data);
		}

		public void OnAngleAccelerationChange(float value) {
			Target target = EditorNotes.SelectedNotes.First();
			if(target == null || target.data.behavior != TargetBehavior.Legacy_Pathbuilder) {
				return;
			}

			target.data.legacyPathbuilderData.angleIncrement = value;
			GenerateChainNotes(target.data);
		}

		public void OnStepDistanceChange(float value) {
			Target target = EditorNotes.SelectedNotes.First();
			if(target == null || target.data.behavior != TargetBehavior.Legacy_Pathbuilder) {
				return;
			}

			target.data.legacyPathbuilderData.stepDistance = value;
			GenerateChainNotes(target.data);
		}

		public void OnStepIncrementChange(float value) {
			Target target = EditorNotes.SelectedNotes.First();
			if(target == null || target.data.behavior != TargetBehavior.Legacy_Pathbuilder) {
				return;
			}

			target.data.legacyPathbuilderData.stepIncrement = value;
			GenerateChainNotes(target.data);
		}

		public void GeneratePathFromSelectedNote() {
			Target target = EditorNotes.SelectedNotes.First();
			if(target == null || target.data.behavior != TargetBehavior.Legacy_Pathbuilder) {
				return;
			}
			GenerateChainNotes(target.data);
		}

		public static void GenerateChainNotes(TargetData data, bool ignoreRepeater = false) {

            if (data.isRepeaterTarget && !ignoreRepeater)
            {
				#region repeater
                var parent = timeline.repeaterManager.GetParentTarget(data);
				parent.legacyPathbuilderData.Copy(data.legacyPathbuilderData, false);
                if (data.repeaterData.Section.flipTargetColors)
                {
					if (data.handType == TargetHandType.Left)
					{
						parent.legacyPathbuilderData.handType = TargetHandType.Right;
					}
					else if (data.handType == TargetHandType.Right)
					{
						parent.legacyPathbuilderData.handType = TargetHandType.Left;
					}
				}
				if (data.repeaterData.Section.mirrorHorizontally)
				{
					parent.legacyPathbuilderData.initialAngle = FlipAngleHorizontal(parent.legacyPathbuilderData.initialAngle);
					parent.legacyPathbuilderData.angle *= -1;
					parent.legacyPathbuilderData.angleIncrement *= -1;
				}
				if (data.repeaterData.Section.mirrorVertically)
				{
					parent.legacyPathbuilderData.initialAngle = FlipAngleVertical(parent.legacyPathbuilderData.initialAngle);
					parent.legacyPathbuilderData.angle *= -1;
					parent.legacyPathbuilderData.angleIncrement *= -1;
				}
				CalculateChainNotes(parent);

				foreach (var node in parent.legacyPathbuilderData.generatedNotes)
				{
					EditorTargets.AddTargetFromAction(node, true);
				}
				parent.handType = parent.legacyPathbuilderData.handType;
				parent.legacyPathbuilderData.createdNotes = true;
				foreach (var sibling in timeline.repeaterManager.GetMatchingRepeaterTargets(parent))
				{
					sibling.legacyPathbuilderData.Copy(parent.legacyPathbuilderData, false);
					if (sibling.repeaterData.Section.flipTargetColors)
					{
						if (parent.legacyPathbuilderData.handType == TargetHandType.Left)
						{
							sibling.legacyPathbuilderData.handType = TargetHandType.Right;
						}
						else if (parent.legacyPathbuilderData.handType == TargetHandType.Right)
						{
							sibling.legacyPathbuilderData.handType = TargetHandType.Left;
						}
                    }
                    else
                    {
						sibling.legacyPathbuilderData.handType = parent.legacyPathbuilderData.handType;
                    }
                    if (sibling.repeaterData.Section.mirrorHorizontally)
                    {
						sibling.legacyPathbuilderData.initialAngle = FlipAngleHorizontal(sibling.legacyPathbuilderData.initialAngle);
						sibling.legacyPathbuilderData.angle *= -1;
						sibling.legacyPathbuilderData.angleIncrement *= -1;
					}
                    if (sibling.repeaterData.Section.mirrorVertically)
                    {
						sibling.legacyPathbuilderData.initialAngle = FlipAngleVertical(sibling.legacyPathbuilderData.initialAngle);
						sibling.legacyPathbuilderData.angle *= -1;
						sibling.legacyPathbuilderData.angleIncrement *= -1;
					}
					CalculateChainNotes(sibling);

					foreach (var node in sibling.legacyPathbuilderData.generatedNotes)
					{
						EditorTargets.AddTargetFromAction(node, true);
					}
					sibling.legacyPathbuilderData.createdNotes = true;
					sibling.handType = sibling.legacyPathbuilderData.handType;
				}				
				#endregion				
			}
            else
            {
				CalculateChainNotes(data);
				//Add new notes
				data.legacyPathbuilderData.generatedNotes.ForEach(t => {
					var newTarget = EditorTargets.AddTargetFromAction(t, true);
				});
				data.legacyPathbuilderData.createdNotes = true;
			}
		}

		internal static float FlipAngleVertical(float angle)
		{
			angle = ((angle + 180) % 360) - 180;

			if (angle >= 0)
				return 180 - angle;
			else
				return -180 - angle;
		}

		internal static float FlipAngleHorizontal(float angle)
		{
			angle = ((angle + 180) % 360) - 180;
			return -angle;
		}

		internal static void MirrorChainVertical(TargetData data)
        {
			data.legacyPathbuilderData.initialAngle = FlipAngleVertical(data.legacyPathbuilderData.initialAngle);
			data.legacyPathbuilderData.angle *= -1;
			data.legacyPathbuilderData.angleIncrement *= -1;
		}
		internal static void MirrorChainHorizontal(TargetData data)
        {
			data.legacyPathbuilderData.initialAngle = FlipAngleHorizontal(data.legacyPathbuilderData.initialAngle);
			data.legacyPathbuilderData.angle *= -1;
			data.legacyPathbuilderData.angleIncrement *= -1;
		}

		public static void CalculateChainNotes(TargetData parentData) {
			if(parentData.behavior != TargetBehavior.Legacy_Pathbuilder) {
				return;
			}

			if (parentData.legacyPathbuilderData.createdNotes) {
                parentData.legacyPathbuilderData.generatedNotes.ForEach(t => {
					EditorTargets.DeleteTargetFromAction(t);
				});
                parentData.legacyPathbuilderData.createdNotes = false;
			}

			//No notes can be generated
			if(parentData.beatLength.tick == 0) {
				return;
			}

            parentData.legacyPathbuilderData.generatedNotes = new List<TargetData>();

            foreach (TargetData data in parentData.legacyPathbuilderData.parentNotes) {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                /////////                                            WARNING!                                                      /////////
                /////////       Chainging this calculation breaks backwards compatibility with saves of older NotReaper versions!  /////////
                /////////                    Make sure to update NRCueData.Version, and handle an upgrade path!                    /////////
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                //Generate first note at the start
                TargetData firstData = new TargetData();
                firstData.behavior = data.legacyPathbuilderData.behavior;
                firstData.velocity = data.legacyPathbuilderData.velocity;
                firstData.handType = data.legacyPathbuilderData.handType;

                //Force set the time, since these transient notes will get generated for all pathbuilders in repeaters
                firstData.SetTimeFromAction(data.time);

                firstData.position = data.position;
                data.legacyPathbuilderData.generatedNotes.Add(firstData);

                //We increment as if all these values were for 1/4 notes over 4 beats, makes the ui much better
                float quarterIncrConvert = (4.0f / data.legacyPathbuilderData.interval) * (Constants.PulsesPerQuarterNote * 4.0f / data.beatLength.tick);

                //Generate new notes
                Vector2 currentPos = data.position;
                Vector2 currentDir = new Vector2(Mathf.Sin(data.legacyPathbuilderData.initialAngle * Mathf.Deg2Rad), Mathf.Cos(data.legacyPathbuilderData.initialAngle * Mathf.Deg2Rad));
                float currentAngle = (data.legacyPathbuilderData.angle / 4) * quarterIncrConvert;
                float currentStep = data.legacyPathbuilderData.stepDistance * quarterIncrConvert;

                TargetBehavior generatedBehavior = data.legacyPathbuilderData.behavior;
                if (generatedBehavior == TargetBehavior.ChainStart) {
                    generatedBehavior = TargetBehavior.ChainNode;
                }

                InternalTargetVelocity generatedVelocity = data.legacyPathbuilderData.velocity;
                if (generatedVelocity == InternalTargetVelocity.ChainStart) {
                    generatedVelocity = InternalTargetVelocity.Chain;
                }

                for (int i = 1; i <= (data.beatLength.tick / (float)Constants.PulsesPerQuarterNote) * (data.legacyPathbuilderData.interval / 4.0f); ++i) {
                    currentPos += currentDir * currentStep;
                    currentDir = currentDir.Rotate(currentAngle);

                    currentAngle += (data.legacyPathbuilderData.angleIncrement / 4) * quarterIncrConvert;
                    currentStep += data.legacyPathbuilderData.stepIncrement * quarterIncrConvert;

                    TargetData newData = new TargetData();
                    newData.behavior = generatedBehavior;
                    newData.velocity = generatedVelocity;
                    newData.handType = data.legacyPathbuilderData.handType;

                    //Force set the time, since these transient notes will get generated for all pathbuilders in repeaters
                    newData.SetTimeFromAction(data.time + QNT_Duration.FromBeatTime(i * (4.0f / data.legacyPathbuilderData.interval)));

                    newData.position = currentPos;
                    data.legacyPathbuilderData.generatedNotes.Add(newData);
                }
            }

			parentData.legacyPathbuilderData.OnFinishRecalculate();
		}

		public void BakePathFromSelectedNote() {
			Target target = EditorNotes.SelectedNotes.First();
			if(target == null || target.data.behavior != TargetBehavior.Legacy_Pathbuilder) {
				return;
			}

			NRActionBakePath action = new NRActionBakePath();
            action.removeNoteAction = new NRActionRemoveNote { targetData = target.data };
			UndoRedoManager.AddAction(action);
		}

		/// <summary>
		/// Sets the tool to be in select mode.
		/// </summary>
		public void SelectMode() {
			isEditMode = false;
		}

		public void EditMode() {
			//TODO: Find which chain we clicked on to trigger edit mode
			isEditMode = true;
		}

		
		public GameObject AddPointToActive(Vector3 pos, bool isChainStart = false) {
			var point = Instantiate(linePointPrefab, new Vector3(pos.x, pos.y, 0f), Quaternion.identity, activeChain.transform);
			if (isChainStart)
				point.GetComponent<CurvedLinePoint>().MakeChainStart();
			else
				point.GetComponent<CurvedLinePoint>().MakeChainNode();

			return point;
		}

		private void SelectTarget()
		{
			//if (!EditorState.IsOverGrid) return;
			if (startClickNote == null && iconUnderMouse != null && !iconUnderMouse.target.transient && !iconUnderMouse.target.data.isPathbuilderTarget)
			{
				if (iconUnderMouse.data.behavior != TargetBehavior.Legacy_Pathbuilder)
				{
					NRActionConvertNoteToLegacyPathbuilder action = new NRActionConvertNoteToLegacyPathbuilder();
					action.data = iconUnderMouse.data;
					var time = new QNT_Timestamp(action.data.time.tick + (action.data.isRepeaterTarget ? Constants.QuarterNoteDuration.tick : EditorBeatSnap.Duration.tick));
					if (action.data.isRepeaterTarget)
                    {						
						if(time > action.data.repeaterData.Section.activeEndTime)
                        {
							NotificationCenter.SendNotification("Can't create legacy pathbuilder target: Target would be out of bounds of repeater zone.", NotificationType.Warning);
							return;
						}
                    }
					else if(timeline.repeaterManager.IsTargetInRepeaterZone(time))
                    {
						NotificationCenter.SendNotification("Can't create legacy pathbuilder target: Target would cross into repeater zone.", NotificationType.Warning);
						return;
                    }
                    if (action.data.isPathbuilderTarget)
                    {
						NotificationCenter.SendNotification("Can't create legacy pathbuilder target out of a new pathbuilder target.", NotificationType.Warning);
						return;
                    }
					UndoRedoManager.AddAction(action);
				}

				EditorNotes.DeselectAllTargets();
				iconUnderMouse.TrySelect();

				startClickNote = iconUnderMouse.target;

				if (EditorNotes.SelectedNotes.Count == 1)
				{
					SetPathbuilderStateToSelectedNote();
				}
            }
		}

		private void Update() {
			if (!activated || !isMouseDown || EditorState.IsOverTimeline) return;

			if (EditorNotes.SelectedNotes.Count == 1 && EditorNotes.SelectedNotes[0] == startClickNote && EditorNotes.SelectedNotes[0].data.behavior == TargetBehavior.Legacy_Pathbuilder)
			{
				var mousePosV3 = Camera.main.ScreenToWorldPoint(actions.Pathbuilder.MousePosition.ReadValue<Vector2>());
				var mousePos = new Vector2(mousePosV3.x, mousePosV3.y);

				var vecFromCenter = (mousePos - startClickNote.data.position);
				if (vecFromCenter.sqrMagnitude > 0.5f)
				{
					var angle = Vector2.SignedAngle(vecFromCenter.normalized, new Vector2(0, 1));
					float snappedAngle;
					if (snapAngle)
					{
						snappedAngle = Mathf.Floor((Math.Abs(angle) + 22.5f) / 45.0f) * 45.0f;
					}
					else
					{
						snappedAngle = Mathf.Floor((Math.Abs(angle) + 2.5f) / 5.0f) * 5.0f;
					}
					if (Math.Sign(angle) < 0)
					{
						snappedAngle = 180 + (180 - snappedAngle);
					}

					startClickNote.data.legacyPathbuilderData.initialAngle = snappedAngle;
					EditorScale.ReapplyScale();
					GenerateChainNotes(startClickNote.data);
				}
			}

			iconsUnderMouse = null;
		}

        private void OnSelectedNoteCountChanged(int count)
        {
			if (count == 1)
			{
				chainBuilderWindow.chainBuilderWindowSelectedControls.SetActive(true);
				chainBuilderWindow.chainBuilderWindowUnselectedControls.SetActive(false);
			}
			else
			{
				chainBuilderWindow.chainBuilderWindowSelectedControls.SetActive(false);
				chainBuilderWindow.chainBuilderWindowUnselectedControls.SetActive(true);
			}
		}


		private void SetPathbuilderStateToSelectedNote() {
			chainBuilderWindow.angleIncrement.value = EditorNotes.SelectedNotes[0].data.legacyPathbuilderData.angle;
			chainBuilderWindow.angleIncrementIncrement.value = EditorNotes.SelectedNotes[0].data.legacyPathbuilderData.angleIncrement;
			chainBuilderWindow.stepDistance.value = EditorNotes.SelectedNotes[0].data.legacyPathbuilderData.stepDistance;
			chainBuilderWindow.stepIncrement.value = EditorNotes.SelectedNotes[0].data.legacyPathbuilderData.stepIncrement;

			var intervalStr = "1/" + EditorNotes.SelectedNotes[0].data.legacyPathbuilderData.interval;
			for(int i = 0; i < chainBuilderWindow.pathBuilderInterval.elements.Count; ++i) {
				if(chainBuilderWindow.pathBuilderInterval.elements[i] == intervalStr) {
					chainBuilderWindow.pathBuilderInterval.defaultIndex = i;
					chainBuilderWindow.pathBuilderInterval.UpdateToIndex(i);
					break;
				}
			}
		}

		public void DrawTempChain() {
			List<Vector2> points = FindPointsAlongChain(10);
			if (points != null) DrawPointsAlongChain(points);
		}




		private void DrawPointsAlongChain(List<Vector2> points) {

			RemoveTempPointsAlongChain();

			foreach (Vector2 point in points) {
				Instantiate(nodeTempPointPrefab, new Vector3(point.x, point.y, 0f), Quaternion.identity, tempNodeIconsParent);
			}
		}

		private void RemoveTempPointsAlongChain() {
			foreach (Transform child in tempNodeIconsParent) {
				Destroy(child.gameObject);
			}
		}


		private List<Vector2> FindPointsAlongChain(int noteCount) {

			if (!activeChain) return null;

			//If we need to draw a new amount of notes or something else
			//if (noteCount == prevDrawPointsAmt) return null;

			prevDrawPointsAmt = noteCount;

			var lr = activeChain.GetComponent<LineRenderer>();
			int pCount = lr.positionCount;

			//If the amount of points on the line is lower than the positions we want to get, return to prevent crash.
			if (pCount < noteCount) return null;
			
			double indexDist = pCount / noteCount;


			List<Vector2> points = new List<Vector2>();

			for (double i = indexDist; i < pCount; i += indexDist) {
				//Get the previous index
				Vector3 pos1 = lr.GetPosition((int)Math.Floor(i));
				Vector3 pos2 = lr.GetPosition((int)Math.Ceiling(i));

				//4.3 -> 0.3
				double firstOffsetFromIndex = i - Math.Floor(i);
				//4.3 -> 0.7
				double secondOffsetFromIndex = Math.Ceiling(i) - i;

				if (firstOffsetFromIndex == 0) firstOffsetFromIndex = 1;

				Vector2 avg1 = pos1 * (float)firstOffsetFromIndex;
				Vector2 avg2 = pos2 * (float)secondOffsetFromIndex;

				Vector2 final = avg1 + avg2;
				points.Add(final);
			}
			return points;
		}
        protected override void RegisterCallbacks()
        {
			actions.Pathbuilder.SelectTarget.performed += _ => OnMouseClicked();
			actions.Pathbuilder.SelectTarget.canceled += _ => OnDone();
			actions.Pathbuilder.SnapAngle.performed += _ => snapAngle = true;
			actions.Pathbuilder.SnapAngle.canceled += _ => snapAngle = false;
        }

        protected override void OnEscPressed(InputAction.CallbackContext context)
        {
			Activate(false);
        }

		private void OnMouseClicked()
		{
			iconsUnderMouse = null;
			isMouseDown = true;
			SelectTarget();
		}

		private void OnDone()
		{
			isMouseDown = false;
			startClickNote = null;
		}

        protected override void SetRebindConfiguration(ref RebindConfiguration options, ChainbuilderKeybinds myKeybinds)
        {
			options.SetAssetTitle("Legacy Pathbuilder").SetPriority(11);
			options.AddNonRebindableKeybinds(myKeybinds.Pathbuilder.SelectTarget);
			options.AddHiddenKeybinds(myKeybinds.Pathbuilder.MousePosition);
			options.AddNonRebindableKeybinds(myKeybinds.Pathbuilder.SnapAngle);
        }
    }
}