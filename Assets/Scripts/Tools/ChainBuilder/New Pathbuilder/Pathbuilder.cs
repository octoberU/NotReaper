using System;
using System.CodeDom;
using NotReaper.Grid;
using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Repeaters;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools.ChainBuilder;
using NotReaper.UserInput;
using System.Collections.Generic;
using System.Linq;
using NotReaper.HitsoundTimeline;
using NotReaper.UI.Particles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace NotReaper.Tools.PathBuilder
{
	[RequireComponent(typeof(SegmentPool))]
	public class Pathbuilder : NRInput<PathbuilderKeybinds>
	{
        #region Inspector References
        [Space, Header("References")]
		[NRInject] private PathbuilderUI ui;
		[SerializeField] private Transform canvas;
		[SerializeField] private Transform pathbuilderParent;
		[SerializeField] private string anchorPointTag;
		[SerializeField] private PathbuilderNode nodePrefab;
		[Space, Header("Active Segment Indicator")]
		[SerializeField] private LineRenderer activeSegmentIndicator;
		[Space, Header("Handle Settings")]
		[SerializeField] internal Color freeColor = Color.white;
		[SerializeField] internal Color alignedColor = Color.magenta;
		[SerializeField] internal Color vectorColor = Color.green;
		[SerializeField] internal Color autoColor = Color.yellow;

		#endregion

        #region Fields
        #region Dependencies
        [NRInject] private Timeline timeline;
		[NRInject] private RepeaterManager repeaterManager;
		[NRInject] private HitsoundManager hitsoundManager;
        #endregion

        #region Classes
        private SegmentPool segmentPool;
        private PathbuilderCalculator calculator;
		private Camera cam;
		#endregion

        #region Data
        public Target ActiveTarget { get; private set; }
		private Segment activeSegment;
		private Segment tempSegment;
		private Point activePoint;
		private List<Segment> segments = new List<Segment>();
		private List<AnchorPoint> anchorPoints = new();
		private bool alternateHands;
		private bool isSegmentScope = true;
		private bool isSilent;
		private PathbuilderData.Interval intervalOverride = new PathbuilderData.Interval(1, 16);
		private QNT_Duration beatLengthOverride = new QNT_Duration(480);
		internal List<HandleType> handleTypes = new();
		#endregion

        #region State
        private bool snapToGrid = false;
		private bool dragNote = false;
		private bool isMouseDown = false;
		private Vector2 dragStartPos;
		internal PathbuilderMode Mode { get; private set; } = PathbuilderMode.Advanced;
		internal PathbuilderData.SimpleModeData SimpleData { get; private set; }= new();
		public bool isActive;

		private bool isLoadingData = false;
        #endregion

        #region Utility
        private TargetIcon[] _iconsUnderMouse = new TargetIcon[0];
		/** Fetch all icons currently under the mouse
     *  Will only ever happen once per frame */
		public TargetIcon[] iconsUnderMouse
		{
			get
			{
				return _iconsUnderMouse = _iconsUnderMouse == null
					? MouseUtil.IconsUnderMouse(timeline)
					: _iconsUnderMouse;
			}
			set { _iconsUnderMouse = value; }
		}

		// Fetch the highest priority target (closest to current time)
		public TargetIcon iconUnderMouse
		{
			get
			{
				return iconsUnderMouse != null && iconsUnderMouse.Length > 0
					? iconsUnderMouse[0]
					: null;
			}
		}

		public bool IsCreatingTarget(Target target) => ActiveTarget == target && tempSegment != null;
		#endregion
		#endregion

		protected override void Awake()
        {
			base.Awake();
			calculator = new PathbuilderCalculator();
			segmentPool = GetComponent<SegmentPool>();
		}

		private void Start()
		{
			cam = CameraProvider.main;
			EditorTargets.onBeforeTargetDeleted += OnBeforeTargetDeleted;
			EditorNotes.onSelectedNoteCountChanged += OnSelectedNoteCountChanged;
		}

		private void OnSelectedNoteCountChanged(int noteCount)
		{
			if (!isActive || isLoadingData || tempSegment != null)
				return;

			if (noteCount != 1)
			{
				ClearData();
				UpdateUI();
			}
		}

		private void OnBeforeTargetDeleted(Target target)
		{
			if (target == ActiveTarget && tempSegment != null && !target.data.isPathbuilderTarget)
			{
				ClearData();
				UpdateUI();
				
				if(isActive)
					EditorState.SelectTool(EditorTool.Pathbuilder);
			}
		}

		[NRListener]
		private void OnToolChanged(EditorTool tool)
        {
			if(tool == EditorTool.Pathbuilder && !isActive)
            {
				Activate(true);
            }
			else if(tool is EditorTool.None or EditorTool.ModifierCreator && isActive)
            {
				Activate(false);
            }
        }

		[NRListener]
		private void OnBehaviorChanged(TargetBehavior behavior)
        {
			if (isActive)
            {
				EditorState.SelectTool(EditorTool.Pathbuilder);
            }
        }

		private void OnTargetHandChanged(TargetHandType target)
        {
			if (isActive && ActiveTarget != null)
			{
				UpdateSegmentIndicator(ActiveTarget, true);
				ActiveTarget.timelineTargetIcon.MakeSustainIndicatorTransparent(true);

				if(activeSegment != null)
                {
					activeSegment.UpdateColors(ActiveTarget.data.handType);
                }
			}
        }
		


        public void Activate(bool activate)
        {
			if (activate)
            {
				isActive = true;
				ShowUI();
				OnActivated();
				EditorState.SelectSnappingMode(SnappingMode.None);
				if(EditorNotes.SelectedNotes.Count == 1)
                {
                    if (EditorNotes.SelectedNotes[0].data.isPathbuilderTarget)
                    {
						LoadTargetData(EditorNotes.SelectedNotes[0]);
                    }
                }
            }
			else
            {
				EditorState.SelectSnappingMode(EditorState.Snapping.Previous);
				HideUI();
				ClearData();
				isActive = false;
				OnDeactivated();
            }
        }

		private void SetTargetTransparency(Target target, float transparency)
        {
			if (!isActive || target == null)
            {
				return;
            }
            if (target.data.isPathbuilderTarget)
            {
				//NoteEnumerator notes = new(activeTarget.data.time, new QNT_Timestamp(activeTarget.data.pathbuilderData.BeatLength.tick + activeTarget.data.time.tick));
				var foundNotes = new List<Target>();
				foundNotes.Add(target);
				foreach(var segment in target.data.pathbuilderData.Segments)
                {
					foreach(var node in segment.generatedNodes)
                    {
						var found = TargetFinder.FindNote(node);
						if (found != null) foundNotes.Add(found);
                    }
                }
				foreach(var note in foundNotes)
                {                   
					note.gridTargetIcon.SetTransparency(transparency);
                }
            }
        }

		private void ResetTargetTransparency(Target target)
        {
			target.gridTargetIcon.SetTransparency(1f);
        }

		private void RemoveAllSegments()
        {
	        for (int i = segments.Count - 1; i >= 0; i--)
			{
				segmentPool.Return(segments[i]);
			}
			activeSegment = null;
			segments.Clear();

			if(tempSegment != null)
            {
	            segmentPool.Return(tempSegment);
				tempSegment = null;
            }
		}

        internal void OnDenominatorChanged(int denominator)
        {
            if (isSegmentScope)
            {
				activeSegment.SetDenominator(denominator);
			}
            else
            {
				intervalOverride.denominator = denominator;
            }
            UpdateActivePathbuilderTarget(ActiveTarget);
		}

        internal void OnSimpleDenominatorChanged(int denominator)
        {
	        SimpleData.interval = denominator;
	        SaveTargetState();
        }

        internal void OnSimpleAngleChanged(float angle)
        {
	        SimpleData.angle = angle;
	        SaveTargetState();
        }

        internal void OnSimpleAngleIncrementChanged(float increment)
        {
	        SimpleData.angleIncrement = increment;
	        SaveTargetState();
        }

        internal void OnSimpleStepDistanceChanged(float distance)
        {
	        SimpleData.stepDistance = distance;
	        SaveTargetState();
        }

        internal void OnSimpleStepIncrementChanged(float increment)
        {
	        SimpleData.stepIncrement = increment;
	        SaveTargetState();
        }
        
        internal void ChangeAlternateHands()
        {
			alternateHands = !alternateHands;
			ActiveTarget.data.pathbuilderData.AlternateHands = alternateHands;
			UpdateActivePathbuilderTarget(ActiveTarget);
		}

		internal void ToggleSilentChain()
        {
			isSilent = !isSilent;
			ActiveTarget.data.pathbuilderData.IsSilent = isSilent;
			UpdateActivePathbuilderTarget(ActiveTarget);
        }

		public void BakeActiveTarget()
        {
			if (ActiveTarget == null) return;
			SetTargetTransparency(ActiveTarget, 1f);
			NRActionBakePathbuilderTarget action = new NRActionBakePathbuilderTarget(ActiveTarget, this);
			UndoRedoManager.AddAction(action);
        }

        internal void BakeTarget(Target target, bool isRepeaterTarget = false)
        {
			var data = target.data;
			data.isPathbuilderTarget = false;
			
			foreach(var segment in data.pathbuilderData.Segments)
            {
				foreach(var foundTarget in TargetFinder.FindNotes(segment.generatedNodes))
                {
					foundTarget.transient = false;
					hitsoundManager.CreateMarker(foundTarget);
					if (data.isRepeaterTarget)
                    {
						data.repeaterData.Section.AddExistingTargetToRepeater(foundTarget.data);
                    }
                }
            }
			

			if (isRepeaterTarget)
            {
				var repeaterTarget = TargetFinder.FindNote(data);
				if(repeaterTarget != null)
                {
					target.timelineTargetIcon.SetBeatlengthLineActive(repeaterTarget.data.isPathbuilderTarget);
                }
            }
            else
            {
				OnPathbuilderTargetChanged(target);
            }

			target.data.pathbuilderData = null;
        }

        internal void ChangeScope()
        {
			isSegmentScope = !isSegmentScope;
			ActiveTarget.data.pathbuilderData.IsSegmentScope = isSegmentScope;
			SaveTargetState();
		}

        internal void RemovePathbuilderTarget(TargetData targetData)
        {
			RemoveAllNodes(targetData.pathbuilderData);

			if (targetData.isRepeaterTarget)
            {
				foreach(var sibling in repeaterManager.GetMatchingRepeaterTargets(targetData))
                {
	                RemoveAllNodes(sibling.pathbuilderData);
	                TryCleanupUI(sibling);
                }
            }
			
			TryCleanupUI(targetData);

			void TryCleanupUI(TargetData data)
			{
				if(ActiveTarget != null && ActiveTarget.data == data)
				{
					ClearData();
					UpdateUI();
				}
			}
        }

        private Vector2 dragEndPos;
        private float originalAngle;
        private bool hasSavedOriginalValues;
        private void Update()
        {
	        if (!isMouseDown || !dragNote || ActiveTarget == null || activeSegment == null)
	        {
		        return;
	        }
	        var rawPos = GetMousePosition();

			if (Mode == PathbuilderMode.Advanced)
			{
				activeSegment.OnHandleDragStart();
				ActiveTarget.data.position = NoteGridSnap.SnapToGrid(rawPos, snapToGrid ? SnappingMode.Grid : SnappingMode.None);
			}
			else
			{
				var vecFromCenter = ((Vector2)rawPos - ActiveTarget.data.position);
				if (vecFromCenter.sqrMagnitude > 0.5f)
				{
					var angle = Vector2.SignedAngle(vecFromCenter.normalized, new Vector2(0, 1));
					float snappedAngle;
					if (snapToGrid)
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
					
					SimpleData.initialAngle = snappedAngle;
					ActiveTarget.data.pathbuilderData.SimpleData.initialAngle = snappedAngle;
					RemoveAllNodes(ActiveTarget.data.pathbuilderData);
					calculator.CalculateNodes(ActiveTarget.data, true);
				}
				
			}
        }

        private void OnMouseClickEnd()
        {
			isMouseDown = false;
			
			if (!dragNote) return;
			
			dragNote = false;
			if (activeSegment != null) activeSegment.OnHandleDragStop();
			if (ActiveTarget == null) return;
			if (ActiveTarget.data.pathbuilderData.Mode == PathbuilderMode.Simple) return;
			List<TargetGridMoveIntent> intents = new();
			TargetGridMoveIntent parentIntent = new TargetGridMoveIntent();

			if (!ActiveTarget.data.isRepeaterTarget)
			{
				parentIntent.target = ActiveTarget.data;
				parentIntent.startingPosition = dragStartPos;
				parentIntent.intendedPosition = ActiveTarget.data.position;
				intents.Add(parentIntent);
			}
			else
			{
				var parent = repeaterManager.GetParentTarget(ActiveTarget.data);
				var section = ActiveTarget.data.repeaterData.Section;

				Vector2 mult = new(section.mirrorHorizontally ? -1 : 1, section.mirrorVertically ? -1 : 1);
				dragStartPos *= mult;
				
				parentIntent.target = parent;
				parentIntent.startingPosition = dragStartPos;
				parentIntent.intendedPosition = ActiveTarget.data.position * mult;
				intents.Add(parentIntent);

				foreach (var child in repeaterManager.GetMatchingRepeaterTargets(parent))
				{
					var childSection = child.repeaterData.Section;
					mult = new(childSection.mirrorHorizontally ? -1 : 1, childSection.mirrorVertically ? -1 : 1);

					TargetGridMoveIntent childIntent = new()
					{
						target = child,
						startingPosition = parentIntent.startingPosition * mult,
						intendedPosition = parentIntent.intendedPosition * mult
					};
					intents.Add(childIntent);
				}
			}
			
			UndoRedoManager.AddAction(new NRActionMovePathbuilderStartNode(intents));
        }

       

        private Vector3 GetMousePosition()
		{
			Vector3 pos = cam.ScreenToWorldPoint(actions.Pathbuilder.MousePosition.ReadValue<Vector2>());
			pos.z = 0;
			return pos;
		}

		private void LoadTargetData(Target target)
		{
			isLoadingData = true;
			target.data.HandTypeChangeEvent += OnTargetHandChanged;
			target.data.PositionChangeEvent += OnPositionChanged;
			if (!target.data.isPathbuilderTarget)
            {
	            MakeNewPathbuilderTarget(target);
	            isLoadingData = false;
	            return;
            }
            ActiveTarget = target;
			EditorNotes.DeselectAllTargets();
			EditorNotes.SelectTarget(target);
			var data = target.data.pathbuilderData;
			var segmentData = data.Segments;
			Transform startPoint = target.gridTargetIcon.transform;
			isSegmentScope = data.IsSegmentScope;
			alternateHands = data.AlternateHands;
			isSilent = data.IsSilent;
			beatLengthOverride = data.BeatLengthOverride;
			intervalOverride = data.IntervalOverride;
			SimpleData.Copy(data.SimpleData);
			foreach(var handle in data.HandleTypes)
				handleTypes.Add(handle);

			Mode = data.Mode;

			Segment lastSegment = null;
			Segment previousSegment = null;
			
			anchorPoints.Clear();

			for(int i = 0; i < segmentData.Count; i++)
            {
				var segment = segmentPool.Spawn();
				segment.Initialize(this, actions);
				if(i != 0)
                {
					lastSegment = segments.Last();
					lastSegment.childSegment = segment;
					segment.parentSegment = lastSegment;
				}
				
				segment.LoadSegment(this, actions, startPoint, target, segmentData[i], segments.Count, Mode);
				segments.Add(segment);

				startPoint = segment.GetSegmentEndPoint();
				
				if (i == 0)
				{
					anchorPoints.Add(new(this, segment.startPoint, segment.startPointHandle, segment.startConnector, true));
				}
				else
				{
					anchorPoints.Add(new(this, segment.startPoint, previousSegment.endPointHandle, segment.startPointHandle, previousSegment.endConnector, segment.startConnector, previousSegment.endPoint));
				}

				if (i == segmentData.Count - 1)
				{
					anchorPoints.Add(new(this, segment.endPoint.transform, segment.endPointHandle, segment.endConnector, false, segment.endPoint));
				}

				previousSegment = segment;
            }
			
			if (handleTypes.Count < anchorPoints.Count)
			{
				int needed = anchorPoints.Count - handleTypes.Count;
				for(int i = 0; i < needed; i++)
					handleTypes.Add(HandleType.Free);
			}

			for (int i = 0; i < anchorPoints.Count; i++)
			{
				var anchor = anchorPoints[i];
				if (i == 0)
				{
					anchor.SetNextAnchor(anchorPoints[i + 1]);
					
				}
				else if (i == anchorPoints.Count - 1)
				{
					anchor.SetPreviousAnchor(anchorPoints[i - 1]);
				}
				else
				{
					anchor.SetNextAnchor(anchorPoints[i + 1]);
					anchor.SetPreviousAnchor(anchorPoints[i - 1]);
				}
				
				anchor.SetHandleTypeSilent(handleTypes[i]);
			}


			SetActiveSegment(segments[data.ActiveSegment]);
			SetTargetTransparency(target, .5f);
			
			if(ActiveTarget.data.pathbuilderData.Mode == PathbuilderMode.Advanced)
				EditorTargets.UpdateSingleChainConnector(ActiveTarget, ActiveTarget.data.pathbuilderData.GetEndTime(ActiveTarget.data.time), true);

			foreach (var point in anchorPoints)
				SetHandleTypeAnchor(point, point.HandleType, false);

			isLoadingData = false;
		}

		private void OnPositionChanged(float _, float __)
		{
			Realign();
		}

		public void HandleRootNoteDelete(TargetData targetData)
        {
			foreach (var segment in targetData.pathbuilderData.Segments)
			{
				foreach (var node in segment.generatedNodes)
				{
					EditorTargets.DeleteTargetFromAction(node);
				}
			}
		}

		private void SwitchData(Target target)
        {
			ClearData();
			LoadTargetData(target);
			UpdateUI();
        }

		private void UpdateUI()
        {
			if (!ui.isOpen) return;

			if(activeSegment == null)
            {
				ui.ResetPanel();
            }
            else
            {
				ui.LoadData(isSegmentScope ? activeSegment.interval : intervalOverride, isSegmentScope ? activeSegment.beatLength : beatLengthOverride, isSegmentScope, alternateHands, isSilent, Mode, SimpleData);
			}
			
        }

		private void ShowUI()
        {
			ui.Show();
			UpdateUI();
        }

		private void HideUI()
        {
			ui.Hide();
        }


        internal void OnNominatorChanged(int nominator)
        {
            if (isSegmentScope)
            {
				activeSegment.SetNominator(nominator);
            }
            else
            {
				intervalOverride.nominator = nominator;
            }
            UpdateActivePathbuilderTarget(ActiveTarget);
		}

		internal void OnBeatlengthChanged(bool increase)
        {
			//QNT_Duration increment = Constants.DurationFromBeatSnap((uint)timeline.beatSnap);
			bool isSimpleTarget = EditorTargets.IsSimplePathbuilderTarget(ActiveTarget);
			QNT_Duration increment = Constants.DurationFromBeatSnap((uint) (isSimpleTarget ? SimpleData.interval : activeSegment.interval.denominator)); //uses segment interval to increase/decrease length
			
			QNT_Duration targetLength = isSimpleTarget ? SimpleData.beatLength : isSegmentScope ? activeSegment.beatLength : beatLengthOverride;
			
			if (increase)
			{
				if (targetLength < increment)
				{
					targetLength = new QNT_Duration(0);
				}
				targetLength += increment;
			}
			else
			{
				if(targetLength.tick - increment.tick > 0)
                {
					targetLength -= increment;
                }
			}
			if (ActiveTarget.data.isRepeaterTarget)
			{
				if (ActiveTarget.data.repeaterData.Section.activeEndTime < new QNT_Timestamp(ActiveTarget.data.time.tick + targetLength.tick))
				{
					NotificationCenter.SendNotification("Can't change length. It falls outside of this repeater zone.", NotificationType.Warning);
					return;
				}
			}
			else
			{
				var time = ActiveTarget.data.time + ActiveTarget.data.pathbuilderData.TotalSegmentLength;
				if (increase) time += increment;
				else time -= increment;
                if (repeaterManager.IsTargetInRepeaterZone(time))
                {
					NotificationCenter.SendNotification("Can't change length. It would cross into a repeater zone.", NotificationType.Warning);
					return;
                }
            }

			if (isSimpleTarget) SimpleData.beatLength = targetLength;
			else if (isSegmentScope) activeSegment.SetBeatlength(targetLength); 
			else beatLengthOverride = targetLength;

			UpdateActivePathbuilderTarget(ActiveTarget);
			if (ActiveTarget.data.isRepeaterTarget)
            {
				foreach(var section in timeline.repeaterManager.GetMatchingRepeaterSections(ActiveTarget.data.repeaterData))
                {
					section.UpdateActiveNotes();
                }
            }
		}

		private void MakeNewPathbuilderTarget(Target target)
        {
			if(EditorTargets.HasTargetOfHandInTimespan(target.data.time, target.data.time + new QNT_Duration(480), target.data.handType, out QNT_Timestamp foundTime, target.data))
            {
				NotificationCenter.SendNotification($"Can't create pathbuilder target: targets would be stacked at {foundTime}");
				return;
            }
			ActiveTarget = target;
			ActiveTarget.data.pathbuilderData = new PathbuilderData();
			beatLengthOverride = Constants.QuarterNoteDuration;
			EditorNotes.DeselectAllTargets();
			EditorNotes.SelectTarget(ActiveTarget);
			var segment = segmentPool.Spawn();
			segment.Initialize(this, actions);
			segment.StartNewSegment(actions, ActiveTarget.gridTargetIcon.transform, ActiveTarget, this, segments.Count);
			tempSegment = segment;
			segment.SetInterval(new PathbuilderData.Interval());
			segment.SetBeatlength(new QNT_Duration(480));
			IsHoveringGrid.Instance.ChangeColliderSize(false);
		}

        private void AppendSegment()
        {
			var lastSegment = segments.Last();
            if (ActiveTarget.data.isRepeaterTarget)
            {
				if(ActiveTarget.data.repeaterData.Section.activeEndTime < new QNT_Timestamp(ActiveTarget.data.time.tick + ActiveTarget.data.pathbuilderData.BeatLength.tick + lastSegment.beatLength.tick))
                {
					NotificationCenter.SendNotification("Can't append segment. It falls outside of this repeater zone.", NotificationType.Warning);
					return;
                }
            }
            
			var segment = segmentPool.Spawn();
			lastSegment.childSegment = segment;
			segment.parentSegment = lastSegment;
			segment.StartNewSegment(actions, lastSegment.GetSegmentEndPoint(), ActiveTarget, this, segments.Count);
			segments.Add(segment);
			segment.SetInterval(new PathbuilderData.Interval(lastSegment.interval.nominator, lastSegment.interval.denominator));
			segment.SetBeatlength(lastSegment.beatLength);
			segment.SetSegmentEndPoint();
			SaveTargetState();
			if (ActiveTarget.data.isRepeaterTarget)
			{
				foreach (var section in timeline.repeaterManager.GetMatchingRepeaterSections(ActiveTarget.data.repeaterData))
				{
					section.UpdateActiveNotes();
				}
			}
		}

		public void SaveTargetState()
        {
	        ActiveTarget.data.pathbuilderData.SimpleData.initialAngle = originalAngle;
	        UndoRedoManager.AddAction(new NRActionUpdatePathbuilderTarget(ActiveTarget.data, this, GetPathbuilderData()));
			hasSavedOriginalValues = false;
        }

		public void UpdatePathbuilderTargetFromAction(TargetData targetData, PathbuilderData data)
        {
			Target target = (ActiveTarget != null && ActiveTarget.data == targetData) ? ActiveTarget : TargetFinder.FindNote(targetData);
			if (target == null)
            {
				return;
            }
			if (targetData.pathbuilderData == null) targetData.pathbuilderData = new PathbuilderData();
			else RemoveAllNodes(targetData.pathbuilderData);
			targetData.pathbuilderData = data;
			targetData.isPathbuilderTarget = data.Segments.Count > 0;
			calculator.CalculateNodes(targetData, true);
			OnPathbuilderTargetChanged(target);
            if (ui.isOpen && targetData.isPathbuilderTarget)
            {
				if(ActiveTarget != null)
                {
					SetTargetTransparency(target, 1f);
					ActiveTarget.data.HandTypeChangeEvent -= OnTargetHandChanged;
					ActiveTarget.data.PositionChangeEvent -= OnPositionChanged;
                }
				ActiveTarget = null;
				SwitchData(target);
            }
			SetTargetTransparency(target, .5f);
        }

		public void UpdatePathbuilderRepeaterTargetFromAction(TargetData targetData, PathbuilderData data, bool flipSimple = false)
        {
			Target target = TargetFinder.FindNote(targetData);
			if (target == null)
			{
				Debug.LogError("Didn't find target, but it should be there! " + targetData.time);
			}
			if (targetData.pathbuilderData == null) targetData.pathbuilderData = new PathbuilderData();
			else RemoveAllNodes(targetData.pathbuilderData);
			targetData.pathbuilderData = data;
			targetData.isPathbuilderTarget = data.Segments.Count > 0;

			if (flipSimple && targetData.isPathbuilderTarget && targetData.isRepeaterTarget && targetData.pathbuilderData.Mode == PathbuilderMode.Simple && !targetData.repeaterData.Section.isParent)
			{
				var section = targetData.repeaterData.Section;
				targetData.pathbuilderData.FlipSimpleOnly(new(section.mirrorHorizontally ? -1 : 1, section.mirrorVertically ? -1 : 1));
			}
			calculator.CalculateNodes(targetData, true);
			target.timelineTargetIcon.SetBeatlengthLineActive(target.data.isPathbuilderTarget);
        }

		public void TryUpdateActiveTarget()
		{
			if (!isActive || ActiveTarget == null) return;
			var target = ActiveTarget;
			ClearData();
			LoadTargetData(target);
		}

		private void UpdateActivePathbuilderTarget(Target target)
        {
	        if (ActiveTarget == null) return;
			RemoveAllNodes(target.data.pathbuilderData);
			Save();
			if (!target.data.isRepeaterTarget)
			{
				UpdatePathbuilderTargetFromAction(target.data, target.data.pathbuilderData);
			}
			else
			{
				var parent = repeaterManager.GetParentTarget(target.data);
				var state = target.data.pathbuilderData;
				var section = target.data.repeaterData.Section;
				if (section.mirrorHorizontally)
				{
					state.Flip(new(-1f, 1f));
				}
				if (section.mirrorVertically)
				{
					state.Flip(new(1f, -1f));
				}
				UpdatePathbuilderTargetFromAction(parent, state);
				foreach (var repeaterTarget in repeaterManager.GetMatchingRepeaterTargets(parent))
				{
					PathbuilderData data = new();
					data.Copy(parent.data.pathbuilderData);
					var childSection = repeaterTarget.repeaterData.Section;
					if (childSection.mirrorHorizontally)
					{
						data.Flip(new(-1, 1));
					}

					if (childSection.mirrorVertically)
					{
						data.Flip(new (1, -1));
					}
					RemoveAllNodes(repeaterTarget.pathbuilderData);
					UpdatePathbuilderRepeaterTargetFromAction(repeaterTarget, data);
				}
			}
        }

		public void UpdatePathbuilderTarget(TargetData data)
        {
			UpdatePathbuilderTargetFromAction(data, data.pathbuilderData);
        }

		public void OnPathbuilderTargetChanged(Target target)
        {
			target.timelineTargetIcon.SetBeatlengthLineActive(target.data.isPathbuilderTarget);
            if (!target.data.isPathbuilderTarget)
            {
				UpdateSegmentIndicator(target, false);
				if(target != null)
                {
					ResetTargetTransparency(target);
					target.data.HandTypeChangeEvent -= OnTargetHandChanged;
					target.data.PositionChangeEvent -= OnPositionChanged;
                }
				ActiveTarget = null;
				target.data.pathbuilderData = null;
				ClearData();
				UpdateUI();
            }
		}

		private PathbuilderData GetPathbuilderData()
        {
			if (segments.Count == 0) return null;
			PathbuilderData data = new PathbuilderData();

			List<PathbuilderData.Segment> segmentData = new List<PathbuilderData.Segment>();
			QNT_Duration length = new QNT_Duration(0);
			foreach (var segment in segments)
            {
				segmentData.Add(segment.GetSegmentData());
				length += segment.beatLength;
            }
			data.BeatLengthOverride = beatLengthOverride;
			data.ActiveSegment = activeSegment.Index;
			data.AlternateHands = alternateHands;
			data.IsSilent = isSilent;
			data.IsSegmentScope = isSegmentScope;
			data.Segments = segmentData;
			data.IntervalOverride = intervalOverride;
			data.Mode = Mode;
			data.SimpleData.Copy(SimpleData);
			data.HandleTypes = GetHandleTypes();
			return data;
        }

		private List<HandleType> GetHandleTypes()
			=> anchorPoints.Select(anchor => anchor.HandleType).ToList();
		
		
		/// <summary>
		/// Calculates nodes on NR load without generating them.
		/// </summary>
		/// <param name="data">The pathbuilder target's TargetData.</param>
		public void CalculateNodesOnLoad(TargetData data)
        {
			calculator.CalculateNodes(data, false);
        }
		/// <summary>
		/// Generates nodes on NR load.
		/// </summary>
		/// <param name="data">The pathbuilder target's TargetData.</param>
		public void GenerateNodesOnLoad(TargetData data)
        {
			calculator.GenerateNodes(data.pathbuilderData);
			var found = TargetFinder.FindNote(data);
			if (found != null)
            {
				found.timelineTargetIcon.SetBeatlengthLineActive(true);
				EditorTargets.UpdateSingleChainConnector(data, data.pathbuilderData.GetEndTime(data.time));
            }
        }
		/// <summary>
		/// Sets a new segment to active.
		/// </summary>
		/// <param name="segment">The segment to mark active.</param>
		internal void SetActiveSegment(Segment segment)
        {
			if (activeSegment == segment) return;
			foreach (var s in segments) s.SetSelected(false);
			segment.SetSelected(true);
			activeSegment = segment;
			UpdateSegmentIndicator(ActiveTarget, true);
			UpdateUI();
        }
		/// <summary>
		/// Updates the position of the active segment indicator and enables/disables it.
		/// </summary>
		/// <param name="target">The currently selected pathbuilder target.</param>
		/// <param name="enable">True if you want to enable the indicator.</param>
		private void UpdateSegmentIndicator(Target target, bool enable)
        {
	        if (EditorTargets.IsSimplePathbuilderTarget(target))
	        {
		        activeSegmentIndicator.enabled = false;
		        target.timelineTargetIcon.MakeSustainIndicatorTransparent(false);
		        return;
	        }
	        
            if (enable)
            {
				float height = target.timelineTargetIcon.sustainDirection;
				QNT_Duration startTime = new QNT_Duration(0);
				QNT_Duration endTime = startTime;
                if (isSegmentScope)
                {
					foreach (var segment in segments)
					{
						if (segment == activeSegment)
						{
							endTime = startTime + segment.beatLength;
							break;
						}
						startTime += segment.beatLength;

					}
                }
                else
                {
					endTime = beatLengthOverride;
                }

				float scale = EditorScale.InvertedScaleAmount;
				activeSegmentIndicator.enabled = true;
				activeSegmentIndicator.transform.localPosition = target.timelineTargetIcon.transform.localPosition;
				activeSegmentIndicator.SetPosition(0, new Vector3((startTime.ToBeatTime() / 0.7f) * scale * 1.75f, height, 0f));
				activeSegmentIndicator.SetPosition(1, new Vector3((endTime.ToBeatTime() / 0.7f) * scale * 1.75f, height, 0f));
				Color color = target.data.handType == TargetHandType.Left ? NRSettings.config.leftColor : NRSettings.config.rightColor;
				activeSegmentIndicator.startColor = color;
				activeSegmentIndicator.endColor = color;
				target.timelineTargetIcon.MakeSustainIndicatorTransparent(true);
			}
            else
            {
				activeSegmentIndicator.enabled = false;
				target.timelineTargetIcon.MakeSustainIndicatorTransparent(false);
            }		
        }
		/// <summary>
		/// Removes the last segment from the current path.
		/// </summary>
		public void RemoveLastSegment()
        {
			if (segments.Count == 0)
            {
				return;
            }
			else if(segments.Count == 1)
            {
				if(ActiveTarget != null)
                {
					EditorTargets.DeleteTarget(ActiveTarget.data);
					ActiveTarget = null;
                }
				return;
            }

			var segment = segments.Last();
			segments.Remove(segment);

			if(activeSegment == segment)
			{
				SetActiveSegment(segments.Last());
            }

			segmentPool.Return(segment);
			SaveTargetState();
		}
		/// <summary>
		/// Removes all nodes of a path.
		/// </summary>
		/// <param name="data">The pathbuilder data you want to remove nodes for.</param>
		internal void RemoveAllNodes(PathbuilderData data)
		{
			foreach (var segment in data.Segments)
			{
				foreach (var node in segment.generatedNodes)
				{
					EditorTargets.DeleteTargetFromAction(node, false);
				}
			}
		}
		/// <summary>
		/// Resets all data and UI.
		/// </summary>
		private void ClearData()
        {
			
			//Save();
			RemoveAllSegments();
			//EditorData.DeselectAllTargets();
			if(ActiveTarget != null)
            {
				UpdateSegmentIndicator(ActiveTarget, false);
				SetTargetTransparency(ActiveTarget, 1f);
				EditorTargets.UpdateSingleChainConnector(ActiveTarget, ActiveTarget.data.pathbuilderData.GetEndTime(ActiveTarget.data.time));
				ActiveTarget.data.HandTypeChangeEvent -= OnTargetHandChanged;
				ActiveTarget.data.PositionChangeEvent -= OnPositionChanged;
            }
			ActiveTarget = null;
			activePoint = null;
			activeSegment = null;
			alternateHands = false;
			isSilent = false;
			handleTypes.Clear();
			intervalOverride = new PathbuilderData.Interval();
			beatLengthOverride = Constants.QuarterNoteDuration;
			isSegmentScope = true;
        }
		/// <summary>
		/// Saves current pathbuilder data to active target.
		/// </summary>
		private void Save()
        {
			if (ActiveTarget != null)
			{
				var data = GetPathbuilderData();
				if (data != null) ActiveTarget.data.pathbuilderData = data;
			}
		}

        /// <summary>
        /// Checks if a target is a valid Pathbuilder target.
        /// </summary>
        /// <param name="target">The target to check</param>
        /// <returns>True if target is not chain, melee, mine, a NR tool or already a pathbuilder target and not a transient.
		/// Also checks if there's enough space if the target is inside of a repeater.</returns>
        private bool IsValidPathbuilderCandidate(Target target)
        {
			if (target == null) return false;

            if (target.data.isRepeaterTarget)
            {
				if(new QNT_Timestamp(Constants.QuarterNoteDuration.tick + target.data.time.tick) > target.data.repeaterData.Section.activeEndTime)
                {
					NotificationCenter.SendNotification("Can't make pathbuilder target: Not enough space in repeater.", NotificationType.Warning);
					return false;
                }
            }
			else if(repeaterManager.IsTargetInRepeaterZone(target.data.time + EditorBeatSnap.Duration))
            {
				NotificationCenter.SendNotification("Can't make pathbuilder target: Target would cross into repeater section.", NotificationType.Warning);
				return false;
            }
			if(target.data.behavior == TargetBehavior.Legacy_Pathbuilder)
            {
				return false;
            }
			int index = (int)target.data.behavior;
			return target.data.behavior != TargetBehavior.Melee && !target.transient && !target.data.isPathbuilderTarget; //index < 6
        }

		#region Input Callbacks
		protected override void RegisterCallbacks()
		{
			actions.Pathbuilder.SelectTarget.performed += _ => OnMouseClick();
			actions.Pathbuilder.SelectTarget.canceled += _ => OnMouseClickEnd();
			actions.Pathbuilder.AppendSegment.performed += _ => OnAppendSegment();
			actions.Pathbuilder.DeleteSegment.performed += _ => RemoveLastSegment();
			actions.Pathbuilder.SnapToGrid.performed += _ => OnShiftDown(true);
			actions.Pathbuilder.SnapToGrid.canceled += _ => OnShiftDown(false);

			actions.Pathbuilder.AlternateHands.performed += _ => OnAlternateHandsPressed();
			actions.Pathbuilder.ChangeScope.performed += _ => OnChangeScopePressed();
			actions.Pathbuilder.ChangeToNextSegment.performed += _ => OnChangeActiveSegment(true);
			actions.Pathbuilder.ChangeToPreviousSegment.performed += _ => OnChangeActiveSegment(false);
			actions.Pathbuilder.IncreaseInterval.performed += _ => OnChangeInterval(true);
			actions.Pathbuilder.DecreaseInterval.performed += _ => OnChangeInterval(false);
			actions.Pathbuilder.IncreaseLength.performed += _ => OnChangeLength(true);
			actions.Pathbuilder.DecreaseLength.performed += _ => OnChangeLength(false);

			actions.Pathbuilder.Bake.started += _ => BakeActiveTarget();
			actions.Pathbuilder.ChangeHandleTypeGlobal.started += _ => TryChangeAllAnchorHandleTypes();
			actions.Pathbuilder.ChangeHandleTypeAnchor.started += _ => TryChangeAnchorHandleType();
		}

		private void OnMouseClick()
		{
			iconsUnderMouse = null;
			isMouseDown = true;
			if(tempSegment == null)
            {
				if (iconUnderMouse != null)
				{
					var target = iconUnderMouse.target;
					if(activePoint == null)
                    {
						if (target == ActiveTarget)
						{
							if (segments.Count > 0)
							{
								SetActiveSegment(segments[0]);
							}
							dragStartPos = GetMousePosition();
							originalAngle = SimpleData.initialAngle;
							dragNote = true;
							return;
						}
						else if (target.data.isPathbuilderTarget)
						{
							if (ActiveTarget != target)
							{
								SwitchData(target);

								if (target.data.pathbuilderData.Mode == PathbuilderMode.Simple)
									dragNote = true;
								
								return;
							}
						}
						//Edit 29/12/22: Removed to allow creating of new targets while PB is active. Might feel bad though
						else if (IsValidPathbuilderCandidate(target)) //&& ActiveTarget == null
						{
							SwitchData(target);
							return;
						}
					}
				}
			}			

			if (tempSegment != null)
			{
				tempSegment.SetSegmentEndPoint();
				segments.Add(tempSegment);
				SimpleData.initialAngle = tempSegment.GetAngle();
				tempSegment = null;
				SaveTargetState();
				if(ActiveTarget != null && ActiveTarget.data.isRepeaterTarget)
				{
					foreach(var section in repeaterManager.GetMatchingRepeaterSections(ActiveTarget.data.repeaterData))
					{
						section.UpdateActiveNotes();
					}
				}
				IsHoveringGrid.Instance.ChangeColliderSize(true);
			}
		}

		/// <summary>
		/// Callback for when a point in a segment is clicked.
		/// </summary>
		/// <param name="segment">The segment the point belongs to.</param>
		/// <param name="point">The point registering a click.</param>
		public void OnPointClicked(Segment segment, Point point)
		{
			activePoint = point;
			point.ShouldSnap(snapToGrid);
			if (KeybindManager.Global.Modifier == KeybindManager.Global.Modifiers.None)
			{
				SetActiveSegment(segment);
			}
		}

		public void ClearActivePoint()
        {
			activePoint = null;
        }

		public bool IsDraggingNote()
        {
			return dragNote;
        }

		private void OnShiftDown(bool down)
        {
			snapToGrid = down;
			if (activePoint != null)
			{
				activePoint.ShouldSnap(down);
			}
        }
		private void OnAppendSegment()
        {
			if (!CanPerformAction || IsDraggingNote() || activePoint != null) return;			
			AppendSegment();
		}

		private void OnAlternateHandsPressed()
        {
			if (!CanPerformAction) return;			
			ChangeAlternateHands();        
        }

		private void OnChangeScopePressed()
        {
			if (!CanPerformAction) return;		
			ChangeScope();
        }

		private void OnChangeActiveSegment(bool next)
        {
			if (!CanPerformAction) return;
			int index = activeSegment.Index + (next ? 1 : -1);
			if(index >= 0 && index < segments.Count)
            {
				SetActiveSegment(segments[index]);
            }
        }

		private void OnChangeInterval(bool increase)
        {
			if (!CanPerformAction) return;
			ui.OnIntervalChanged(increase);
        }

		private void OnChangeLength(bool increase)
        {
			if (!CanPerformAction || KeybindManager.Global.Modifier.IsCtrlDown()) return;
			OnBeatlengthChanged(increase);
        }

		private bool CanPerformAction => ActiveTarget != null && activeSegment != null && Mode == PathbuilderMode.Advanced;

		protected override void OnEscPressed(InputAction.CallbackContext context)
		{
			EditorState.SelectTool(EditorTool.Pathbuilder);
		}

        protected override void SetRebindConfiguration(ref RebindConfiguration options, PathbuilderKeybinds myKeybinds)
        {
			options.SetAssetTitle("Pathbuilder").SetPriority(20);
			options.AddCustomKeybindName(myKeybinds.Pathbuilder.ChangeToNextSegment, "Next Segment").AddCustomKeybindName(myKeybinds.Pathbuilder.ChangeToPreviousSegment, "Previous Segment");
			options.AddHiddenKeybinds(myKeybinds.Pathbuilder.MousePosition);
			options.AddNonRebindableKeybinds(myKeybinds.Pathbuilder.SelectTarget);
			options.AddNonRebindableKeybinds(myKeybinds.Pathbuilder.SnapToGrid);
        }
        #endregion

        public void SetMode(PathbuilderMode mode)
        {
			Mode = mode;

			UpdateActivePathbuilderTarget(ActiveTarget);
			
			foreach (var segment in segments)
			{
				segment.SetMode(mode);
			}

			if (ActiveTarget != null)
				EditorTargets.UpdateSingleChainConnector(ActiveTarget, ActiveTarget.data.pathbuilderData.GetEndTime(ActiveTarget.data.time), mode == PathbuilderMode.Advanced);
        }

        public void TryUpdateActiveTargetPosition(Vector2 moveBy)
        {
	        if (!isActive || ActiveTarget == null) return;

	        NRActionMovePathbuilderTarget action = new(ActiveTarget.data, moveBy, repeaterManager);
	        UndoRedoManager.AddAction(action);
	        TryUpdateActiveTarget();
        }

        internal void Realign()
        {
	        foreach(var anchor in anchorPoints)
		        anchor.Realign();
        }

        internal HandleType GetNextHandleType(HandleType current)
	        => (HandleType)(((int)current + 1) % Enum.GetValues(typeof(HandleType)).Length);


        internal void SetHandleTypeAnchor(AnchorPoint anchor, HandleType newType, bool save = true)
        {
	        anchor.HandleType = newType;

	        Realign();

	        if(save)
		        SaveTargetState();
        }


        /*internal void SetHandleTypeSegment(Segment segment, HandleType newType, bool isStartPoint = false, bool save = true)
        {
	        segment.HandleType = newType;
	        
	        bool hasChildSegment = segment.childSegment != null;
	        bool isLastAnchor = !hasChildSegment && !isStartPoint;
	        if (segment.parentSegment != null)
	        {
		        segment.parentSegment.endPoint.Unlink();
	        }
	        
	        segment.endPoint.Unlink();

	        switch (newType)
	        {
		        case HandleType.Aligned when hasChildSegment:
			        segment.endPoint.AlignWith(segment.endPointHandle, segment.childSegment.startPointHandle);
			        break;
		        case HandleType.Aligned when isLastAnchor:
			        segment.endPoint.AlignWith(segment.endPointHandle);
			        break;
			    case HandleType.Aligned when isStartPoint:
				    segment.startPointHandle.AlignWithAsRoot(segment.startPoint);
			        break;
		        case HandleType.Vector:
			        segment.AlignAsVector(isLastAnchor, isStartPoint);
			        break;
		        case HandleType.Automatic:
			        segment.AutoAlign();
			        break;
	        }
	        
	        Realign();

	        if(save)
				SaveTargetState();
        }*/

        private void TryChangeAllAnchorHandleTypes()
        {
	        if (tempSegment != null || segments.Count == 0)
		        return;

	        SetHandleTypeAllAnchor(GetNextHandleType(anchorPoints[0].HandleType));
	        //SetHandleTypeAll(GetNextHandleType(segments[0].HandleType));
        }

        private void TryChangeAnchorHandleType()
        {
	        if (tempSegment != null || segments.Count <= 1)
		        return;
	        
	        var pointerData = new PointerEventData(EventSystem.current);
	        pointerData.position = actions.Pathbuilder.MousePosition.ReadValue<Vector2>();
	        List<RaycastResult> result = new();
	        EventSystem.current.RaycastAll(pointerData, result);
	        var anchor = result.FirstOrDefault(r => r.gameObject.CompareTag(anchorPointTag));
			iconsUnderMouse = null;

			if (anchor.gameObject != null && anchor.gameObject.TryGetComponent(out Point point))
			{
				SetHandleTypeAnchor(point.Anchor, GetNextHandleType(point.Anchor.HandleType));
			}
			else if (iconUnderMouse != null && iconUnderMouse.target == ActiveTarget)
			{
				SetHandleTypeAnchor(anchorPoints[0], GetNextHandleType(anchorPoints[0].HandleType));
			}
			/*
	        if (anchor.gameObject != null && anchor.gameObject.TryGetComponent(out Point point))
	        {
		        SetHandleTypeSegment(point.segment, GetNextHandleType(point.segment.HandleType));
	        }
	        else if (iconUnderMouse != null && iconUnderMouse.target == ActiveTarget)
	        {
		        SetHandleTypeSegment(segments[0], GetNextHandleType(segments[0].HandleType), true);
	        }*/
        }

        public void SetHandleTypeAllAnchor(HandleType newType)
        {
	        foreach (var anchor in anchorPoints)
		        anchor.SetHandleTypeSilent(newType);
	        
	        Realign();
	        SaveTargetState();
        }
        
/*
        [Button]
        public void SetHandleTypeAll(HandleType newType)
        {
	        if (segments.Count <= 1)
		        return;

	        Segment rootSegment = null;
	        Segment lastSegment = null;
	        lastHandleType = newType;
	        
	        foreach (var segment in segments)
	        {

		        bool isRootSegment = segment.parentSegment == null;
		        bool hasChildSegment = segment.childSegment != null;

		        if (isRootSegment)
			        rootSegment = segment;
		        
		        if(!hasChildSegment)
			        lastSegment = segment;

		        
		        segment.HandleType = newType;
		        segment.endPoint.Unlink();

		        switch (newType)
		        {
			        case HandleType.Aligned when hasChildSegment:
				        segment.endPoint.AlignWith(segment.endPointHandle, segment.childSegment.startPointHandle);
				        break;
			        case HandleType.Vector:
				        segment.AlignAsVector(false, false);
				        break;
			        case HandleType.Automatic when hasChildSegment && !isRootSegment:
				        segment.AutoAlign();
				        break;
		        }
	        }

	        if (newType == HandleType.Automatic && rootSegment != lastSegment && rootSegment != null)
	        {
		        rootSegment.AutoAlign();
		        lastSegment.AutoAlign();
	        }

	        
	        foreach(var segment in segments)
		        segment.UpdateSegment();
		        
	        
	        SaveTargetState();

	        NotificationCenter.SendNotification($"Set handle type to {newType}", NotificationType.Info, false);
        }
        */

        internal void OnPointMoved()
        {
	        Realign();
        }
	}

	public enum PathbuilderMode
    {
		Simple,
		Advanced
    }

	public enum HandleType
	{
		Free = 0,
		Aligned = 1,
		Vector = 2,
		Automatic = 3,
	}
}

