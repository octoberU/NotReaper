using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NotReaper.Models;
using NotReaper.Timing;
using NotReaper.UserInput;
using Random = UnityEngine.Random;
using NotReaper.Modifier;
using Newtonsoft.Json;
using System.Linq;

namespace NotReaper.UI
{
    public class MiniTimeline : MonoBehaviour
    {
        //bar is 440 pixels long total

        public static MiniTimeline Instance = null;

        public Transform songPreviewIcon;


        public float mouseClickAreaLength = 12.34f;
        public float barLength;

        public Transform bar;

        public Timeline timeline;
        public GameObject repeaterSectionPrefab;


        public Transform bookmarksParent;

        public GameObject bookmarkPrefab;

        private Camera timelineCam;
        private Camera mainCam;

        [NRInject] private BookmarkMenu bookmarkMenu;


        [HideInInspector] public bool isMouseOver = false;

        private List<GameObject> repeaterSections = new List<GameObject>();

        public Bookmark selectedBookmark = null;

        private void Start()
        {
            timelineCam = timeline.timelineCamera.GetComponent<Camera>();
            EditorTime.onTimeChanged += _ => SetPercentagePlayed(EditorAudio.SongPercentage);
            SetPercentagePlayed(0);
            mainCam = Camera.main;
            if (Instance is null)
            {
                Instance = this;
            }
            else
            {
                Debug.LogWarning("Trying to create a second MiniTimeline.");
                return;
            }
            KeybindManager.onMouseDown += OnMouseDown;
            EditorState.OnEditorReset += () => ClearBookmarks(false);
        }

        private void SetPercentagePlayed(float percent)
        {
            float x = barLength * percent;
            x -= barLength / 2;
            bar.localPosition = new Vector3((float)x, 0, 0);
        }


        public void SetPreviewStartPointToCurrent()
        {
            SetPreviewStartPoint(EditorTime.Time);
        }

        public void SetPreviewStartPoint(QNT_Timestamp timestamp)
        {
            EditorFile.SongDesc.previewStartSeconds = timestamp.ToSeconds();
            songPreviewIcon.localPosition = new Vector3(TimestampToMinitimeline(timestamp), 10.71f, 0);
        }

        public float TimestampToMinitimeline(QNT_Timestamp timestamp)
        {
            double percent = EditorAudio.GetPercentagePlayed(timestamp.ToSeconds());
            double pos = barLength * percent;
            pos -= barLength / 2;
            return (float)pos;
        }

        internal void LoadBookmarks()
        {
            if (EditorFile.AudicaFile.desc.bookmarks != null)
            {
                foreach (BookmarkData data in EditorFile.AudicaFile.desc.bookmarks)
                {
                    if (data.r == 0 && data.g == 0 && data.b == 0)
                    {
                        Color c = BookmarkColorPicker.Instance.GetUIColor((BookmarkUIColor)data.uiColor);
                        data.r = c.r;
                        data.g = c.g;
                        data.b = c.b;
                        SetBookmark(data.xPosMini, data.xPosTop, new QNT_Timestamp(0), data.type, data.text, c, (BookmarkUIColor)data.uiColor, true, true);
                    }

                    SetBookmark(data.xPosMini, data.xPosTop, new QNT_Timestamp(0), data.type, data.text, new Color(data.r, data.g, data.b), (BookmarkUIColor)data.uiColor, true, true);

                }
            }
        }

        public double MinitimelineToSeconds(float pos)
        {
            double newPos = pos + (barLength * 2);
            double percent = newPos / barLength;
            double seconds = timeline.songPlayback.song.Length / 100d * percent;
            return seconds;
        }

        float prevX = 0f;


        bool timelineWasPlaying = false;

        public void MouseDown()
        {
            if (EditorState.Tool.Current != EditorTool.None) return; //EditorState.IsInUI || 
            if (EditorAudio.IsPlaying)
            {
                timelineWasPlaying = true;
                EditorAudio.TogglePlay();
            }
        }
        public void MouseUp()
        {
            if (EditorState.Tool.Current != EditorTool.None) return; //EditorState.IsInUI || 
            if (timelineWasPlaying && !EditorAudio.IsPlaying)
            {
                EditorAudio.TogglePlay();
            }

            timelineWasPlaying = false;
        }

        public void DoDrag()
        {
            if (EditorState.Tool.Current != EditorTool.None) return; //EditorState.IsInUI || 
            var x = mainCam.ScreenToWorldPoint(Input.mousePosition).x;

            x -= mainCam.transform.position.x;

            if (x == prevX)
            {
                return;
            }
            else
            {
                prevX = x;
            }

            //x -= transform.position.x;
            //-4.7 to 4.7
            //9.4 length
            x += mouseClickAreaLength / 2;
            float percent = x / mouseClickAreaLength;

            EditorAudio.ForceJumpToPercent(percent); //JumpToPercent
        }


        //public void SetSongPreviewPoint(double percent) {
        //    double x = barLength * percent;
        //    x -= barLength / 2;
        //     songPreviewIcon.localPosition = new Vector3((float)x, 0, 0);
        //}

        private void OnMouseOver()
        {
            isMouseOver = true;
        }

        private void OnMouseExit()
        {
            isMouseOver = false;
        }


        public Bookmark SetBookmark(float miniXPos, float topXPos, QNT_Timestamp time, TargetHandType newType, string text, Color myColor, BookmarkUIColor uiCol, bool useTopXPos, bool fromLoad = false)
        {
            Bookmark bookmarkMini = Instantiate(bookmarkPrefab, new Vector3(0, 0, 0), Quaternion.identity, bookmarksParent).GetComponent<Bookmark>();
            Bookmark bookmarkTop = Instantiate(bookmarkPrefab, Timeline.timelineNotesStatic).GetComponent<Bookmark>();
            bookmarkMini.gameObject.layer = 0;
            Color background = fromLoad ? myColor : BookmarkColorPicker.selectedColor;

            bookmarkMini.transform.localPosition = new Vector3((float)miniXPos, 0, 0);
            //bookmarkTop.transform.localScale = new Vector3(.05f, .03f, 1f);

            //bookmarkTop.transform.localScale = new Vector3(0.06f, 0.006f, 0.06f);

            if (!useTopXPos)
            {
                bookmarkTop.transform.position = new Vector3(0, bookmarkTop.transform.position.y - .62f, 0);
            }
            else
            {
                if (newType == TargetHandType.Left)
                {
                    bookmarkTop.transform.localPosition = new Vector3(topXPos, bookmarkTop.transform.localPosition.y - .62f, 0);
                    //background = Color.green;
                }
                else
                {
                    bookmarkTop.transform.localPosition = new Vector3(topXPos, bookmarkTop.transform.localPosition.y - .62f, 0);
                    //background = Color.red;
                }
            }
            //bookmarkMini.GetComponent<SpriteRenderer>().color = background;
            //bookmarkTop.GetComponent<SpriteRenderer>().color = background;

            bookmarkMini.transform.localScale = new Vector3(2f, 1.5f, 1f);
            if (fromLoad) bookmarkTop.FixScaling();


            /*bookmarkTop.GetComponent<SpriteRenderer>().size = new Vector2(0.1f, 22f);
            bookmarkTop.GetComponent<BoxCollider2D>().size = new Vector2(.1f, 22f);
            bookmarkTop.GetComponentInChildren<TMPro.TextMeshProUGUI>().rectTransform.localPosition = new Vector3(26, -730, 0);
            bookmarkTop.GetComponentInChildren<TMPro.TextMeshProUGUI>().rectTransform.localScale = new Vector3(1.3f, .1f, 1f);*/



            //bookmarkTop.transform.localScale = new Vector3(.05f, .03f, 1f);

            //bookmarkTop.glow.transform.localPosition = new Vector3(0f, -.35f, 0f);
            if (fromLoad)
            {
                time = new QNT_Timestamp((ulong)topXPos * Constants.PulsesPerQuarterNote);
            }
            int id = TimelineTextManager.Instance.AddText(text, time);
            bookmarkTop.Initialize(bookmarkMini, text, id, newType, miniXPos, time);
            bookmarkTop.SetColor(background, fromLoad ? uiCol : BookmarkColorPicker.selectedUIColor);
            //bookmarks.Add(bookmarkMini);
            bookmarks.Add(bookmarkTop);

            return bookmarkTop;
        }

        internal void JumpToPreviousBookmark()
        {
            var currentTime = EditorTime.Time;
            foreach (var bookmark in bookmarks.OrderByDescending(b => b.transform.position.x))
            {
                if (bookmark.transform.position.x >= currentTime.ToBeatTime()) continue;
                EditorAudio.ForceJumpToBeat(bookmark.transform.position.x);
                break;
            }
        }

        internal void JumpToNextBookmark()
        {
            var currentTime = EditorTime.Time;
            foreach (var bookmark in bookmarks.OrderBy(b => b.transform.position.x))
            {
                if (bookmark.transform.position.x <= currentTime.ToBeatTime()) continue;
                EditorAudio.ForceJumpToBeat(bookmark.transform.position.x);
                break;
            }
        }

        public void SaveSelectedBookmark()
        {
            EditorFile.AudicaFile.desc.bookmarks.Clear();
            foreach (Bookmark b in bookmarks) SaveBookmark(b);
        }

        public void SaveBookmark(Bookmark b)
        {
            EditorFile.AudicaFile.desc.bookmarks.Add(new BookmarkData() { type = b.handType, xPosMini = b.xPosMini, xPosTop = b.transform.localPosition.x, text = b.GetText(), r = b.GetColor().r, g = b.GetColor().g, b = b.GetColor().b, uiColor = (int)b.GetUIColor() });
        }

        public void DeleteBookmark()
        {
            selectedBookmark.DeleteBookmark();
            EditorFile.AudicaFile.desc.bookmarks.Clear();
            foreach (Bookmark b in bookmarks) SaveBookmark(b);
        }

        public List<Bookmark> bookmarks = new List<Bookmark>();

        public void ClearBookmarks(bool deleteInAudica = false)
        {
            foreach (Bookmark t in bookmarks)
            {

                t.Destroy();
            }

            bookmarks.Clear();

            if (deleteInAudica) EditorFile.AudicaFile.desc.bookmarks.Clear();
        }

        public float GetXForTheBookmarkThingy()
        {
            float percent = EditorAudio.SongPercentage;
            float x = (float)barLength * (float)percent;
            x -= (float)barLength / 2f;
            return x;
        }

        public void OpenBookmarksMenu()
        {
            if (BookmarkMenu.isActive)
            {
                bookmarkMenu.Activate(false);
            }
            else
            {
                bookmarkMenu.SetText(selectedBookmark.GetText());
                bookmarkMenu.Activate(true);
            }
        }

        public void SetBookmark()
        {
            if(bookmarks.Any(b => b.time == EditorTime.Time))
            {
                selectedBookmark = bookmarks.First(b => b.time == EditorTime.Time);
                selectedBookmark.Select();
                return;
            }

            SetBookmark(GetXForTheBookmarkThingy(), timeline.timelineCamera.transform.position.x * EditorScale.ScaleAmount, EditorTime.Time, EditorState.Hand.Current,
                "", BookmarkColorPicker.selectedColor, BookmarkColorPicker.selectedUIColor, true, false).Select();
        }


        private void OnMouseDown(bool down)
        {
            if (down)
            {
                RaycastHit2D hit = Physics2D.Raycast(timelineCam.ScreenToWorldPoint(Input.mousePosition), Vector2.zero, 0f, 1 << LayerMask.NameToLayer("Bookmark"));

                if (hit.collider != null)
                {
                    selectedBookmark = hit.transform.GetComponent<Bookmark>();
                    selectedBookmark.Select();
                }
            }
        }
    }



    [Serializable]
    public class BookmarkData
    {
        public TargetHandType type = TargetHandType.Left;
        public float xPosMini = 0.0f;
        public float xPosTop = 0.0f;
        public string text;
        public Color color;
        /*public Color color
        {
			get 
			{
				if (r == 0 && g == 0 && b == 0) return new Color(r, g, b);
				else return color;
			}
            set { r = value.r; g = value.g; b = value.b; }
        }*/
        public bool ShouldSerializecolor()
        {
            return false;
        }
        public float r;
        public float g;
        public float b;
        public int uiColor;
    }
}