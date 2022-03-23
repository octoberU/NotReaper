using NotReaper;
using NotReaper.Grid;
using NotReaper.Managers;
using NotReaper.Models;
using NotReaper.Notifications;
using NotReaper.Repeaters;
using NotReaper.ReviewSystem;
using NotReaper.Timing;
using NotReaper.UI.BPM;
using NotReaper.UI.Countin;
using NotReaper.UI.ModifyAudio;
using NotReaper.UI.Timing;
using NotReaper.UserInput;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace NotReaper.UI
{
	public class UIInput : MonoBehaviour
	{
		[Header("References")]
		[SerializeField] private Timeline timeline;
		[SerializeField] private HoverTarget hover;
		[SerializeField] private TMP_Dropdown soundDropdown;
		[SerializeField] private NRDiscordPresence discord;
		[Space, Header("Selectors")]
		[SerializeField] private UIToolSelect uiToolSelect;
		[SerializeField] private UIModeSelect editorMode;
		[SerializeField] private HandTypeSelect handSelect;
		[SerializeField] private SoundSelect soundSelect;
		[Space, Header("Grid")]
		[SerializeField] private OutsideGridBounds grid;
		[SerializeField] private GameObject normalGrid;
		[SerializeField] private GameObject meleeGrid;
		[SerializeField] private GameObject noGrid;
		[SerializeField] private RawImage background;
		[Space, Header("Menus")]
		[NRInject] private DynamicBPMWindow bpmWindow;
		[NRInject] private BPMListWindow bpmListWindow;
		public NRHelp help;
		[NRInject] private CountInWindow countin;
		[NRInject] private AddOrTrimAudioWindow audioModify;
		[NRInject] private TimingPointsPanel timingPoints;
		[NRInject] private RepeaterMenu repeaterMenu;
 
		private QNT_Timestamp? detectBpmStart;
		[NRInject] private NewPauseMenu pauseMenu;

        public static UIInput Instance { get; private set; } = null;
        private void Awake()
        {
            if (Instance != null)
            {
                Debug.Log("UIInput already exists.");
                return;
            }
            Instance = this;
        }

		private void Start()
		{
			NRSettings.OnLoad(OnNRStart);
			//hover.UpdateUIHandColor(EditorState.GetSelectedColor());
			//hover.UpdateUITool(EditorTool.Standard);
		}

		private void OnNRStart()
		{
			EditorState.SelectHand(TargetHandType.Left);
			EditorState.SelectBehavior(TargetBehavior.Standard);
			EditorState.SelectHitsound(TargetHitsound.Standard);
			EditorState.SelectMode(EditorMode.Compose);
			/*
			SelectMode(EditorMode.Compose);
			SelectTool(EditorTool.Standard);
			SelectHand(TargetHandType.Left);
			SelectHitsound(UITargetVelocity.Standard);
			SelectSnappingMode(SnappingMode.Grid);
			*/
			//PauseMenu.Instance.OpenPauseMenu();
			//soundSelect.LoadUIColors();
			timeline.UpdateUIColors();

			StartCoroutine(LoadBGImage(NRSettings.config.bgImagePath));

			discord.InitPresence();
		}

		IEnumerator LoadBGImage(string URL)
		{
			try
			{
				byte[] imageBytes = System.IO.File.ReadAllBytes(URL);
				Texture2D texture = new Texture2D(1, 1);
				texture.LoadImage(imageBytes);
				texture.wrapMode = TextureWrapMode.Repeat;
				Sprite sprite = Sprite.Create(texture,
					new Rect(0, 0, texture.width, texture.height), Vector2.zero);
				background.texture = texture;
				//bgImage.sprite = sprite;
			}
			catch (System.Exception e)
			{
				Debug.LogError(e.Message);
			}

			yield return null;
		}

		[NRListener]
		private void SelectMode(EditorMode mode)
		{

			if (EditorState.Mode.Current == EditorMode.Timing && Timeline.inTimingMode) return;
			editorMode.UpdateUI(mode);
			//state.SetSelectedMode(mode);

		}

		[NRListener]
		private void SelectBehavior(TargetBehavior behavior)
        {
			if(EditorState.IsInUI) return;

			var prevBehavior = EditorState.Behavior.Previous;
			if (prevBehavior.IsMeleeOrMine() && EditorState.Hand.Current == TargetHandType.Either && !behavior.IsMeleeOrMine())
			{
				EditorState.SelectHand(EditorState.Hand.Previous);
			}
			EditorState.SelectHitsound(GetVelocityOnToolChange());
			EditorState.SelectSnappingMode(GetSnappingModeForSelectedBehavior());

            if (behavior.IsMeleeOrMine())
            {
				EditorState.SelectHand(TargetHandType.Either);
            }
		}
		/*
		[NRListener]
		private void SelectTool(EditorTool tool)
		{
			var prevTool = EditorState.Tool.Previous;
			if ((prevTool == EditorTool.Melee || prevTool == EditorTool.Mine) && EditorState.Hand.Current == TargetHandType.Either)
			{
				//SelectHand(EditorState.PreviousHand);
				EditorState.SelectHand(EditorState.Hand.Previous);
			}

			EditorState.SelectBehavior(ToolToBehavior(tool, out bool isTarget));

			if (isTarget)
			{
				EditorState.SelectHitsound(GetVelocityOnToolChange());
				EditorState.SelectSnappingMode(GetSnappingModeForSelectedBehavior());
				//SelectHitsound(GetVelocityOnToolChange());
				//SelectSnappingMode(GetSnappingModeForSelectedBehavior());
				if (tool == EditorTool.Melee || tool == EditorTool.Mine) EditorState.SelectHand(TargetHandType.Either);//SelectHand(TargetHandType.Either);
			}
		}
		*/

		public void ShiftBpmMarker()
		{
			timeline.ShiftNearestBPMToCurrentTime();
		}

		public void DetectBpm()
		{
            if (bpmListWindow.isActive)
            {
				bpmListWindow.Hide();
            }
			else if (detectBpmStart == null)
			{
				NotificationCenter.SendNotification("BPM Detect start position set.", NotificationType.Info);
				detectBpmStart = Timeline.time;
			}
			else
			{
				bpmListWindow.Show(timeline.DetectBPM(detectBpmStart.Value, Timeline.time));
				detectBpmStart = null;
			}
		}

		[NRListener]
		private void SelectSnappingMode(SnappingMode mode)
		{
			bool melee = mode == SnappingMode.Melee;
			normalGrid.SetActive(!melee);
			noGrid.SetActive(melee);
			meleeGrid.SetActive(melee);
		}

		public void SwitchToNoGrid()
		{
			//state.SetSnapping.Current(EditorState.Snapping.Current);
			var mode = EditorState.Snapping.Current == SnappingMode.None ? EditorState.Behavior.Current.IsMeleeOrMine() ? SnappingMode.Melee : SnappingMode.Grid : SnappingMode.None;
			//SelectSnappingMode(mode);
			EditorState.SelectSnappingMode(EditorState.Snapping.Current == SnappingMode.None ? EditorState.Behavior.Current.IsMeleeOrMine() ? SnappingMode.Melee : SnappingMode.Grid : SnappingMode.None);
		}

		public void RevertGrid()
		{
			SelectSnappingMode(EditorState.Snapping.Previous);
		}

		public void ToggleWaveform()
		{
			timeline.ToggleWaveform();
		}

		private SnappingMode GetSnappingModeForSelectedBehavior()
		{
			bool isMeleeOrMine = EditorState.Behavior.Current == TargetBehavior.Melee || EditorState.Behavior.Current == TargetBehavior.Mine;
			if (!isMeleeOrMine && EditorState.Snapping.Current == SnappingMode.Melee) return SnappingMode.Grid;
			else if (isMeleeOrMine) return SnappingMode.Melee;
			else return EditorState.Snapping.Current;
		}

		private TargetHitsound GetVelocityOnToolChange()
		{
			var prev = EditorState.Behavior.Previous;
			var current = EditorState.Behavior.Current;
			var velocity = EditorState.Hitsound.Current;
			if (current == TargetBehavior.ChainStart) return TargetHitsound.ChainStart;
			else if (current == TargetBehavior.ChainNode) return TargetHitsound.ChainNode;
			else if (current == TargetBehavior.Melee) return TargetHitsound.Melee;
			else if (current == TargetBehavior.Mine) return TargetHitsound.Mine;
			else if (current == TargetBehavior.Melee && velocity != TargetHitsound.Percussion) return TargetHitsound.Melee;
			else if (prev == TargetBehavior.ChainStart && velocity == TargetHitsound.ChainStart) return TargetHitsound.Standard;
			else if (prev == TargetBehavior.ChainNode && velocity == TargetHitsound.ChainNode) return TargetHitsound.Standard;
			else if (prev.IsMeleeOrMine() && velocity == TargetHitsound.Melee || velocity == TargetHitsound.Mine) return TargetHitsound.Standard;
			else return velocity;
		}

		public void ShowBpmWindow()
		{
			bpmWindow.ToggleWindow();
		}

		public void MoveGrid(Vector2 direction)
		{
			grid.SetGridPosition(direction);
		}

		public void ShowHelpWindow()
		{
            if (NRHelp.Instance.isOpened)
            {
				NRHelp.Instance.Hide();
            }
            else
            {
				NRHelp.Instance.Show();
            }
		}

		public void ShowCountinWindow()
		{
			if (countin.isActive) countin.Hide();
			else countin.Show();
		}

		public void ShowRepeaterWindow()
        {
			if (repeaterMenu.isActive) repeaterMenu.Hide();
			else repeaterMenu.Show();
        }

		public void ShowModifyAudioWindow()
		{
			if (audioModify.isActive) audioModify.Hide();
			else audioModify.Show();
		}

		public void ShowTimingPointsWindow()
		{
			timingPoints.Toggle();
		}

		public void ShowModifierHelpWindow()
		{
            if (ModifierInfo.isOpened)
            {
				ModifierInfo.Instance.Hide();
            }
            else
            {
				ModifierInfo.Instance.Show();
            }
		}

		public void ShowReviewWindow()
		{
			ReviewManager.Instance.ShowWindow(!ReviewManager.IsOpen);
		}

		public void ShowPauseWindow()
		{
			//PauseMenu.Instance.OpenPauseMenu();
			pauseMenu.Show();
		}

		public void SetPreviewPoint()
		{
			MiniTimeline.Instance.SetPreviewStartPointToCurrent();
		}

		public void SetBookmark()
		{
			MiniTimeline.Instance.SetBookmark();
		}
	}

}
