using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using NotReaper.Grid;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Modifier;
using NotReaper.Notifications;
using NotReaper.ReviewSystem;
using NotReaper.Targets;
using NotReaper.Timing;
using NotReaper.Tools.ChainBuilder;
using NotReaper.Tools.CustomSnapMenu;
using NotReaper.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace NotReaper.UserInput {

	public enum EditorTool { Standard, Hold, Horizontal, Vertical, ChainStart, ChainNode, Melee, Mine, DragSelect, ChainBuilder, ModifierCreator, SnapEditor, None }

	public class EditorInput : MonoBehaviour {
		public static bool isTesting = false;

		public static EditorInput I;
		public static EditorTool selectedTool = EditorTool.Standard;
		public static EditorTool previousTool = EditorTool.Standard;
		public static TargetHandType selectedHand = TargetHandType.Left;
		public static TargetHandType previousHand = TargetHandType.Left;
		public static SnappingMode selectedSnappingMode = SnappingMode.None;
		public static SnappingMode previousSnappingMode = SnappingMode.None;
		public static TargetBehavior selectedBehavior = TargetBehavior.Standard;
		public static UITargetVelocity selectedVelocity { get; set; } = UITargetVelocity.Standard;
		//public static TargetModifier selectedModifier = TargetModifier.None;

		public static EditorMode selectedMode = EditorMode.Compose;
		public static bool isOverGrid { get; set; }
		public static bool inUI = false;
		public static bool enableScrolling = false;
		public static bool isFocusGrid = false;
		public static bool IsSpacingLocked { get; set; }

		//public PlaceNote toolPlaceNote;
		[SerializeField] public EditorToolkit Tools;
		[SerializeField] private NRDiscordPresence nrDiscordPresence;

		public PauseMenu pauseMenu;

		public ShortcutInfo shortcutMenu;
		public SoundSelect soundSelect;
		[SerializeField] private Timeline timeline;

		[SerializeField] private TMP_Dropdown soundDropdown;

		[SerializeField] private UIToolSelect uiToolSelect;
		[SerializeField] private HandTypeSelect handTypeSelect;

		[SerializeField] private GameObject focusGrid;
		[SerializeField] private GameObject bpmWindow;
		[SerializeField] private GameObject bpmResultWindow;
		[SerializeField] private CountInWindow countInWindow;
		[SerializeField] private AddOrTrimAudioWindow addOrTrimAudioWindow;
		[SerializeField] private AddRepeaterWindow repeaterWindow;
		[SerializeField] private CustomSnapMenu snapWindow;

		/// <summary>
		/// A list of gameobjects that all need to be inactive for the input to be enabled.
		/// Append your panel's gameobject.activeSelf to this.
		/// </summary>
		public static List<GameObject> disableInputWhenActive = new List<GameObject> ();

		/// <summary>
		/// Checks whether there are any panels active that require the input to be disabled.
		/// </summary>
		public static bool InputDisabled { get => disableInputWhenActive.Any (x => x.activeSelf == true); }

		public HoverTarget hover;

		public GameObject normalGrid;
		public GameObject noGrid;
		public GameObject meleeGrid;

		public TimingPointsPanel timingPointsPanel;

		public UIModeSelect editorMode;

		public RawImage bgImage;

		public Color normalGridDisabledColor;

		bool isCTRLDown;
		bool isShiftDown;

		private bool hasSpaceBeenPressed = false;
		private bool setHitsoundSilent = false;

		QNT_Timestamp? bpmStartTimestamp = null;

		private void Start () {
			InputManager.LoadHotkeys ();
			NRSettings.OnLoad (SetUserColors);
			I = this;
		}

		private void SetUserColors () {
			selectedTool = EditorTool.None;
			SelectMode (EditorMode.Compose);
			SelectTool (EditorTool.Standard);
			SelectHand (TargetHandType.Left);
			SelectVelocity (UITargetVelocity.Standard);
			SelectSnappingMode(SnappingMode.Grid);

			pauseMenu.OpenPauseMenu ();

			shortcutMenu.LoadUIColors ();
			soundSelect.LoadUIColors ();
			timeline.UpdateUIColors ();

			FigureOutIsInUI ();
			StartCoroutine (LoadBGImage (NRSettings.config.bgImagePath));

			nrDiscordPresence.InitPresence ();
			NRSettings.PostLoad.Invoke ();
		}

		IEnumerator LoadBGImage (string URL) {
			try {
				byte[] imageBytes = System.IO.File.ReadAllBytes (URL);
				Texture2D texture = new Texture2D (1, 1);
				texture.LoadImage (imageBytes);
				texture.wrapMode = TextureWrapMode.Repeat;
				Sprite sprite = Sprite.Create (texture,
					new Rect (0, 0, texture.width, texture.height), Vector2.zero);
				bgImage.texture = texture;
				//bgImage.sprite = sprite;
			} catch (System.Exception e) {
				Debug.LogError (e.Message);
			}

			yield return null;
		}

		public static Color GetSelectedColor () {
			if (selectedHand == TargetHandType.Left) {
				return NRSettings.config.leftColor;
			} else if (selectedHand == TargetHandType.Right) {
				return NRSettings.config.rightColor;
			}
			return Color.gray;
		}
		public static Color GetOppositeSelectedColor () {
			if (selectedHand == TargetHandType.Left) {
				return NRSettings.config.rightColor;
			} else if (selectedHand == TargetHandType.Right) {
				return NRSettings.config.leftColor;
			}
			return Color.gray;
		}

		public void FocusGrid (bool focus) {
			focusGrid.SetActive (focus);

			isFocusGrid = focus;
		}

		public void SelectSnappingMode (SnappingMode mode) {
			if (selectedSnappingMode == mode) return;

			selectedSnappingMode = mode;

			switch (mode) {
				case SnappingMode.Grid:
					normalGrid.SetActive (true);

					//normalGrid.GetComponent<SpriteRenderer>().color = Color.white;

					noGrid.SetActive (false);
					meleeGrid.SetActive (false);
					break;
				case SnappingMode.None:
					normalGrid.SetActive (true);
					//normalGrid.GetComponent<SpriteRenderer>().color = normalGridDisabledColor;

					noGrid.SetActive (false);
					meleeGrid.SetActive (false);
					break;
				case SnappingMode.Melee:
					normalGrid.SetActive (false);
					noGrid.SetActive (true);
					meleeGrid.SetActive (true);
					break;

			}
		}

		public void SelectHand (TargetHandType type) {
			selectedHand = type;
			uiToolSelect.UpdateUINoteSelected (selectedTool);
			handTypeSelect.UpdateUI (type);
			hover.UpdateUIHandColor (GetSelectedColor ());

		}

		/// <summary>
		/// Select a sound for the current tool.
		/// </summary>
		/// <param name="velocity">The "sound" type to play.</param>
		public void SelectVelocity (UITargetVelocity velocity) {

			switch (velocity) {
				case UITargetVelocity.Standard:
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Standard);
					break;
				case UITargetVelocity.Snare:
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Snare);
					break;

				case UITargetVelocity.Percussion:
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Percussion);
					break;

				case UITargetVelocity.ChainStart:
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.ChainStart);
					break;

				case UITargetVelocity.Chain:
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Chain);
					break;

				case UITargetVelocity.Melee:
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Melee);
					break;
				case UITargetVelocity.Silent:
					soundDropdown.SetValueWithoutNotify(6);
					break;

			}

			selectedVelocity = velocity;

		}

		private TargetVelocity ConvertUIVelocity(UITargetVelocity uiVelocity)
		{
			switch (uiVelocity)
			{
				case UI.UITargetVelocity.Chain:
					return TargetVelocity.Chain;
				case UI.UITargetVelocity.ChainStart:
					return TargetVelocity.ChainStart;
				case UI.UITargetVelocity.Melee:
					return TargetVelocity.Melee;
				case UI.UITargetVelocity.Mine:
					return TargetVelocity.Mine;
				case UI.UITargetVelocity.Percussion:
					return TargetVelocity.Percussion;
				case UI.UITargetVelocity.Snare:
					return TargetVelocity.Snare;
				default:
					return TargetVelocity.Standard;
			}
		}

		public void SelectMode (EditorMode mode) {

			if (selectedMode == EditorMode.Timing && Timeline.inTimingMode) return;
			editorMode.UpdateUI (mode);
			selectedMode = mode;

			FigureOutIsInUI ();
		}

		private UITargetVelocity GetVelocityOnToolChange()
        {
			if (previousTool == EditorTool.ChainStart && selectedVelocity == UITargetVelocity.ChainStart) return UITargetVelocity.Standard;
			else if (previousTool == EditorTool.ChainNode && selectedVelocity == UITargetVelocity.Chain) return UITargetVelocity.Standard;
			else if (previousTool == EditorTool.Melee && selectedVelocity == UITargetVelocity.Melee) return UITargetVelocity.Standard;
			else if (previousTool == EditorTool.Mine && selectedVelocity == UITargetVelocity.Mine) return UITargetVelocity.Standard;
			else if (previousTool == EditorTool.ChainBuilder && selectedVelocity == UITargetVelocity.ChainStart) return UITargetVelocity.Standard;
			else return selectedVelocity;
        }

		public void SelectTool (EditorTool tool) {
			if (tool == selectedTool) {
				return;
			}

			if (ModifierHandler.activated && !ModifierHandler.pendingClose) {
				switch (tool) {
					case EditorTool.ChainNode:
					case EditorTool.ChainStart:
					case EditorTool.Hold:
					case EditorTool.Horizontal:
					case EditorTool.Melee:
					case EditorTool.Mine:
					case EditorTool.Standard:
					case EditorTool.Vertical:
					case EditorTool.ChainBuilder:
					case EditorTool.DragSelect:
						return;
					default:
						break;
				}
			}

			uiToolSelect.UpdateUINoteSelected (tool);
			hover.UpdateUITool (tool);
			//if(tool != EditorTool.ModifierCreator) previousTool = selectedTool;
			previousTool = selectedTool;
			selectedTool = tool;

			if (previousTool == EditorTool.Melee && selectedHand == TargetHandType.Either) {
				SelectHand (previousHand);
			} else if (previousTool == EditorTool.Mine && selectedHand == TargetHandType.Either) {
				SelectHand (previousHand);
			}

			//Update the UI based on the tool:
			switch (tool) {
				case EditorTool.Standard:
					selectedBehavior = TargetBehavior.Standard;
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Standard);
					//SelectVelocity (UITargetVelocity.Standard);
					SelectVelocity(GetVelocityOnToolChange());
					//SelectSnappingMode (SnappingMode.Grid);
					if (selectedSnappingMode == SnappingMode.Melee) SelectSnappingMode(SnappingMode.Grid);
					else SelectSnappingMode(selectedSnappingMode);
					break;

				case EditorTool.Hold:
					selectedBehavior = TargetBehavior.Hold;
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Standard);
					//SelectVelocity (UITargetVelocity.Standard);
					SelectVelocity(GetVelocityOnToolChange());
					//SelectSnappingMode (SnappingMode.Grid);
					if (selectedSnappingMode == SnappingMode.Melee) SelectSnappingMode(SnappingMode.Grid);
					else SelectSnappingMode(selectedSnappingMode);
					break;

				case EditorTool.Horizontal:
					selectedBehavior = TargetBehavior.Horizontal;
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Standard);
					//SelectVelocity (UITargetVelocity.Standard);
					SelectVelocity(GetVelocityOnToolChange());
					//SelectSnappingMode (SnappingMode.Grid);
					if (selectedSnappingMode == SnappingMode.Melee) SelectSnappingMode(SnappingMode.Grid);
					else SelectSnappingMode(selectedSnappingMode);
					break;

				case EditorTool.Vertical:
					selectedBehavior = TargetBehavior.Vertical;
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Standard);
					//SelectVelocity (UITargetVelocity.Standard);
					SelectVelocity(GetVelocityOnToolChange());
					//SelectSnappingMode (SnappingMode.Grid);
					if (selectedSnappingMode == SnappingMode.Melee) SelectSnappingMode(SnappingMode.Grid);
					else SelectSnappingMode(selectedSnappingMode);
					break;

				case EditorTool.ChainStart:
					selectedBehavior = TargetBehavior.ChainStart;
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.ChainStart);
					SelectVelocity (UITargetVelocity.ChainStart);
					//SelectSnappingMode (SnappingMode.Grid);
					if (selectedSnappingMode == SnappingMode.Melee) SelectSnappingMode(SnappingMode.Grid);
					else SelectSnappingMode(selectedSnappingMode);
					break;

				case EditorTool.ChainNode:
					selectedBehavior = TargetBehavior.Chain;
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Chain);
					SelectVelocity (UITargetVelocity.Chain);
					if (selectedSnappingMode == SnappingMode.Melee) SelectSnappingMode(SnappingMode.Grid);
					else SelectSnappingMode(selectedSnappingMode);
					break;

				case EditorTool.Melee:
					selectedBehavior = TargetBehavior.Melee;
					previousHand = selectedHand;
					soundDropdown.SetValueWithoutNotify ((int) UITargetVelocity.Melee);
					SelectVelocity (UITargetVelocity.Melee);
					SelectSnappingMode (SnappingMode.Melee);
					SelectHand (TargetHandType.Either);
					break;

				case EditorTool.Mine:
					selectedBehavior = TargetBehavior.Mine;
					previousHand = selectedHand;
					//soundDropdown.SetValueWithoutNotify((int) UITargetVelocity.Melee);
					SelectVelocity (UITargetVelocity.Mine);
					SelectSnappingMode (SnappingMode.Melee);
					SelectHand (TargetHandType.Either);
					break;

				case EditorTool.DragSelect:
					selectedBehavior = TargetBehavior.None;

					Tools.dragSelect.Activate (true);
					Tools.chainBuilder.Activate (false);
					break;

				case EditorTool.ChainBuilder:
					selectedBehavior = TargetBehavior.None;

					Tools.dragSelect.Activate (false);
					Tools.chainBuilder.Activate (true);
					break;
				case EditorTool.ModifierCreator:
					selectedBehavior = TargetBehavior.None;
					Tools.modifierCreator.Activate (true);
					break;					

				default:
					break;
			}

			if (tool != EditorTool.ChainBuilder) {
				Tools.chainBuilder.Activate (false);
				if(previousTool == EditorTool.ChainBuilder && tool != EditorTool.DragSelect) timeline.DeselectAllTargets(); //Disappearing chains fix
			}
			if (tool != EditorTool.DragSelect) {
				Tools.dragSelect.Activate (false);
			}

			if (tool != EditorTool.ModifierCreator) {
				/*
				if (ModifierHandler.instance.activated && tool == EditorTool.DragSelect) return;              
				Tools.modifierCreator.Activate(false);
				previousTool = EditorTool.None;*/
				Tools.modifierCreator.Activate (false);
			}
		}

		public void RevertTool () {
			SelectTool (previousTool);
			if (selectedTool != EditorTool.ModifierCreator) previousTool = selectedTool;
		}

		public void FigureOutIsInUI () {
			enableScrolling = false;

			if (InputDisabled) {
				inUI = true;
				if (ReviewWindow.IsOpen && ReviewWindow.Instance.SelectedMode == ReviewWindow.ReviewMode.Read && !Timeline.instance.paused)
				{
					enableScrolling = true;
				}
				return;
			}

			if (pauseMenu.isOpened) {
				inUI = true;
				return;
			}

			if (shortcutMenu.isOpened) {
				inUI = true;
				return;
			}

			if (ModifierInfo.isOpened) {
				inUI = true;
				return;
			}

			switch (selectedMode) {
				case EditorMode.Compose:
					inUI = false;
					break;

				case EditorMode.Metadata:
				case EditorMode.Timing:
				case EditorMode.Settings:
					inUI = true;
					break;

			}

			if (bpmWindow.activeSelf ||
				timingPointsPanel.gameObject.activeSelf ||
				bpmResultWindow.activeSelf ||
				countInWindow.gameObject.activeSelf ||
				addOrTrimAudioWindow.gameObject.activeSelf ||
				repeaterWindow.gameObject.activeSelf||
				snapWindow.window.gameObject.activeInHierarchy||
				BookmarkMenu.isActive) {
				inUI = true;

				if (repeaterWindow.gameObject.activeSelf || !timingPointsPanel.isHovering) {
					enableScrolling = true;
				}

				if(snapWindow.window.gameObject.activeInHierarchy)
					enableScrolling = false;

				
				return;
			}
		}

		private void Update () {
			if (isTesting) {
				return;
			}

			if (Input.GetKeyDown (KeyCode.F6)) timingPointsPanel.Toggle ();

			if ((Timeline.inTimingMode || countInWindow.gameObject.activeSelf) && inUI) {
				if (Input.GetKeyDown (InputManager.timelineTogglePlay)) {
					timeline.TogglePlayback ();
				}
			}

			if (Input.GetKey (KeyCode.LeftControl) && Input.GetKeyDown (KeyCode.S)) {
				timeline.Export ();
			}

			if (Input.GetKey (KeyCode.LeftControl) && Input.GetKeyDown (KeyCode.C)) {
				timeline.CopyTimestampToClipboard ();
			}

			if (Input.GetKeyDown (KeyCode.Escape)) {
				if (pauseMenu.isOpened) {
					if (!Timeline.audioLoaded) return;

					pauseMenu.ClosePauseMenu ();
				} else {
					if (selectedMode == EditorMode.Timing) return;

					if (bpmWindow.activeSelf) {
						bpmWindow.GetComponent<DynamicBPMWindow> ().Deactivate ();
					} else if (countInWindow.gameObject.activeSelf) {
						countInWindow.Deactivate ();
					} else if (addOrTrimAudioWindow.gameObject.activeSelf) {
						addOrTrimAudioWindow.Deactivate ();
					} else if (addOrTrimAudioWindow.gameObject.activeSelf) {
						addOrTrimAudioWindow.Deactivate ();
					} else {
						pauseMenu.OpenPauseMenu ();
					}
				}
			}

			if (Input.GetKey (KeyCode.LeftShift) || Input.GetKey (KeyCode.RightShift)) {
				isShiftDown = true;
			} else {
				isShiftDown = false;
			}

			if (Input.GetKeyDown (KeyCode.F7)) {
				if (isShiftDown) {
					FindAndRemoveCurrentRepeater ();
				} else {
					repeaterWindow.Activate ();
				}
			}

			if (Input.GetKeyDown (KeyCode.B) && !Input.GetKey(KeyCode.LeftControl) && !Input.GetKeyUp(KeyCode.RightControl)) {
			if (ModifierHandler.activated || BookmarkMenu.isActive || ReviewWindow.IsOpen) return;
				if (isShiftDown) {
					if (bpmStartTimestamp == null) {
						bpmStartTimestamp = Timeline.time;
					} else {
						bpmResultWindow.GetComponent<BPMListWindow> ().Activate (timeline.DetectBPM (bpmStartTimestamp.Value, Timeline.time));
						bpmStartTimestamp = null;
					}
				} else if (Input.GetKey (KeyCode.LeftAlt) || Input.GetKey (KeyCode.LeftAlt)) {
					timeline.ShiftNearestBPMToCurrentTime ();
				} else {
					if (bpmWindow.activeSelf) {
						bpmWindow.GetComponent<DynamicBPMWindow> ().Deactivate ();
					} else if (!inUI) {
						bpmWindow.GetComponent<DynamicBPMWindow> ().Activate ();
					}
				}
			}

			if(Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl))
            {
                if (Input.GetKeyDown(KeyCode.B))
                {
					List<Target> pathbuilderTargets = Timeline.instance.selectedNotes.Where(target => target.data.pathBuilderData != null).ToList();
					foreach (Target target in pathbuilderTargets) ChainBuilder.Instance.BakePathFromSelectedNote();
				}
            }

			if (Input.GetKeyUp (KeyCode.LeftShift) || Input.GetKeyUp (KeyCode.RightShift)) {
				if (selectedSnappingMode != previousSnappingMode) {
					SelectSnappingMode (previousSnappingMode);
				}
			}

			if (Input.GetKeyDown (KeyCode.F3)) {
				if (countInWindow.gameObject.activeSelf) {
					countInWindow.Deactivate ();
				}
			}

			if (Input.GetKeyDown (KeyCode.F5)) {
				if (addOrTrimAudioWindow.gameObject.activeSelf) {
					addOrTrimAudioWindow.Deactivate ();
				}
			}

			if (Input.GetKeyDown (KeyCode.F1)) {
				if (shortcutMenu.isOpened) {
					shortcutMenu.Hide ();
				}
			}

			if (Input.GetKeyDown (KeyCode.F8)) {
				if (ModifierInfo.isOpened) ModifierInfo.Instance.Hide ();
			}
            if (Input.GetKeyDown(KeyCode.F9))
            {
				if (ReviewWindow.IsOpen) ReviewWindow.Instance.ShowWindow(false);
            }

			bool wasInUI = inUI;
			FigureOutIsInUI ();

			if (wasInUI || inUI) return;

			if (Input.GetKeyDown (KeyCode.F3)) {
				if (!countInWindow.gameObject.activeSelf) {
					countInWindow.Activate ();
				}
			}

			if (Input.GetKeyDown (KeyCode.F1)) {
				if (!shortcutMenu.isOpened) {
					shortcutMenu.Show ();
				}
			}

			if (Input.GetKeyDown (KeyCode.F8)) {
				if (!ModifierInfo.isOpened) ModifierInfo.Instance.Show ();
			}

            if (Input.GetKeyDown(KeyCode.F9))
            {
				if (!ReviewWindow.IsOpen) ReviewWindow.Instance.ShowWindow(true);
            }

			if (Input.GetKeyDown (KeyCode.F5)) {
				if (!addOrTrimAudioWindow.gameObject.activeSelf) {
					addOrTrimAudioWindow.Activate ();
				}
			}

			if (Input.GetKeyDown (KeyCode.LeftControl) || Input.GetKeyDown (KeyCode.RightControl)) {
				SelectTool (EditorTool.DragSelect);
				previousSnappingMode = selectedSnappingMode;
			}
			if (Input.GetKeyUp (KeyCode.LeftControl) || Input.GetKeyUp (KeyCode.RightControl)) {
				Tools.dragSelect.EndAllDragStuff ();
				if (selectedTool == EditorTool.DragSelect) {
					isOverGrid = false;
					RevertTool ();
				}
				SelectSnappingMode (previousSnappingMode);
			}

			if (Input.GetKeyDown (KeyCode.LeftShift) || Input.GetKeyDown (KeyCode.RightShift)) {
				previousSnappingMode = selectedSnappingMode;
				switch (selectedSnappingMode) {
					case SnappingMode.Grid:
					case SnappingMode.Melee:
						SelectSnappingMode (SnappingMode.None);
						break;
					case SnappingMode.None:
						SelectSnappingMode (selectedTool == EditorTool.Melee ? SnappingMode.Melee : SnappingMode.Grid);
						break;
				}
			}

			if (Input.GetKey (KeyCode.LeftControl) || Input.GetKey (KeyCode.RightControl)) {
				isCTRLDown = true;
			} else {
				isCTRLDown = false;
			}

			if (Input.GetKeyDown (KeyCode.T)) {
				//Tools.chainBuilder.NewChain(new Vector2(0, 0));
			}

			if (Input.GetMouseButtonDown (0)) {

				switch (selectedTool) {
					case EditorTool.Standard:
					case EditorTool.Hold:
					case EditorTool.Horizontal:
					case EditorTool.Vertical:
					case EditorTool.ChainStart:
					case EditorTool.ChainNode:
					case EditorTool.Melee:
					case EditorTool.Mine:
						Tools.placeNote.TryPlaceNote ();
						break;

					case EditorTool.ChainBuilder:
						//We let the chain builder script handle input since we activated it from SelectTool()
						break;

					default:
						break;
				}

			}

			if (Input.GetMouseButtonDown (1)) {
				switch (selectedTool) {
					case EditorTool.Standard:
					case EditorTool.Hold:
					case EditorTool.Horizontal:
					case EditorTool.Vertical:
					case EditorTool.ChainStart:
					case EditorTool.ChainNode:
					case EditorTool.Melee:
					case EditorTool.Mine:
						Tools.placeNote.TryRemoveNote ();
						break;

					default:
						break;
				}
			}

			if (Input.GetKeyDown (InputManager.timelineTogglePlay) && !ModifierHandler.inputFocused && !BookmarkMenu.inputFocused) {
				timeline.TogglePlayback ();
			}

			if (Input.GetKeyDown (InputManager.timelineToggleWaveform)) {
				timeline.ToggleWaveform ();
			}

			if (Input.GetKeyDown (InputManager.selectStandard)) {

				SelectTool (EditorTool.Standard);

			}
			if (Input.GetKeyDown (InputManager.selectHold)) {
				SelectTool (EditorTool.Hold);
			}
			if (Input.GetKeyDown (InputManager.selectHorz)) {
				SelectTool (EditorTool.Horizontal);
			}
			if (Input.GetKeyDown (InputManager.selectVert)) {
				SelectTool (EditorTool.Vertical);
			}
			if (Input.GetKeyDown (InputManager.selectChainStart)) {
				SelectTool (EditorTool.ChainStart);
			}
			if (Input.GetKeyDown (InputManager.selectChainNode)) {
				SelectTool (EditorTool.ChainNode);
			}
			if (Input.GetKeyDown (InputManager.selectMelee)) {
				SelectTool (EditorTool.Melee);
			}
			if (Input.GetKeyDown (InputManager.selectMine)) {
				SelectTool (EditorTool.Mine);
			}

			//Toggles the chain builder state
			if (Input.GetKeyDown (KeyCode.H)) {
				if (Tools.chainBuilder.activated) {
					Tools.chainBuilder.Activate (false);
					RevertTool ();
				} else {
					SelectTool (EditorTool.ChainBuilder);

				}
			}
			if ((Input.GetKeyDown (KeyCode.O) && !ModifierHandler.inputFocused) && !BookmarkMenu.isActive) {
				if (ModifierHandler.activated) {
					if (ZOffsetBaker.active) ZOffsetBaker.Instance.ToggleWindow ();
					Tools.modifierCreator.Activate (false);
					RevertTool ();

				} else {
					SelectTool (EditorTool.ModifierCreator);

				}
			}

			if (Input.GetKeyDown (KeyCode.V) && !isCTRLDown) {
				SelectTool (EditorTool.DragSelect);
			}

			if (Input.GetKeyDown (KeyCode.G)) {
				SelectSnappingMode (SnappingMode.Grid);
			}
			if (Input.GetKeyDown (KeyCode.N)) {
				SelectSnappingMode (SnappingMode.None);
			}
			if (Input.GetKeyDown (KeyCode.M) && !ModifierHandler.activated && !BookmarkMenu.isActive) {
				SelectSnappingMode (SnappingMode.Melee);
			}

			if (Input.GetKeyDown (InputManager.toggleColor)) {
				ToggleHandColor ();
			}
            if (Input.GetKeyDown(KeyCode.Alpha0) && !inUI)
            {
				SelectHand(TargetHandType.Either);
            }

			if (Input.GetKeyDown (InputManager.selectSoundKick)) {
				soundDropdown.value = 0;
			} else if (Input.GetKeyDown (InputManager.selectSoundSnare)) {
				soundDropdown.value = 1;
			} else if (Input.GetKeyDown (InputManager.selectSoundPercussion)) {
				soundDropdown.value = 2;
			} else if (Input.GetKeyDown (InputManager.selectSoundChainStart)) {
				soundDropdown.value = 3;
			} else if (Input.GetKeyDown (InputManager.selectSoundChainNode)) {
				soundDropdown.value = 4;
			} else if (Input.GetKeyDown (InputManager.selectSoundMelee)) {
				soundDropdown.value = 5;
			} else if (setHitsoundSilent)
            {
				soundDropdown.value = 6;
				setHitsoundSilent = false;
            }

            if (Input.GetKey(KeyCode.X) && Input.GetKey(KeyCode.Space))
            {
				if (Input.GetKeyDown(KeyCode.Space)) hasSpaceBeenPressed = true;
            }
			else if(Input.GetKeyUp(KeyCode.X))
            {
                if (!hasSpaceBeenPressed)
                {
					setHitsoundSilent = true;
                }
				hasSpaceBeenPressed = false;
            }


			if (!isShiftDown && isCTRLDown && Input.GetKeyDown (InputManager.undo) && !ModifierHandler.activated) {
				Tools.undoRedoManager.Undo ();
				Debug.Log ("Undoing...");
			}

			if (isShiftDown && isCTRLDown && Input.GetKeyDown (InputManager.redo) && !ModifierHandler.activated) {
				Tools.undoRedoManager.Redo ();
				Debug.Log ("Redoing...");
			}

		}

		public void FindAndRemoveCurrentRepeater () {
			var foundRepeaterSection = timeline.FindRepeaterForTime (Timeline.time);
			if (foundRepeaterSection != null) {
				timeline.RemoveRepeaterSection (foundRepeaterSection);
			}
		}

		public void ToggleHandColor () {
			if (selectedHand == TargetHandType.Left) {
				SelectHand (TargetHandType.Right);
			} else if (selectedHand == TargetHandType.Right) {
				SelectHand (TargetHandType.Left);
			} else {
				SelectHand (TargetHandType.Left);
			}
		}
	}

}