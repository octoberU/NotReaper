using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using NotReaper.Models;
using UnityEngine.UI;
using NotReaper.Timing;

namespace NotReaper.UI {

    public class Bookmark : MonoBehaviour {

        public int index = 0;

        //public TextMeshProUGUI text;
        public bool isSelected = false;
        private Bookmark mini;
        public Image image;
        public BoxCollider2D boxCollider;
        public TargetHandType handType;
        public float xPosMini;
        public double percentBookmark = 0;
        private BookmarkUIColor myUIColor;
        public QNT_Timestamp time = new(0);
        private Vector3 originalScale = new Vector3(0.05f, 0.03f, 1f);

        private int timelineTextId;
        private string text = "";
        private void Start()
        {
            originalScale = transform.localScale;
        }

        public void SetIndex(int i) {
            index = i;
            //text.text = index.ToString();
        }

        public void SetText(string _text)
        {
            this.text = _text;
            //text.text = _text;
            TimelineTextManager.Instance.UpdateText(_text, timelineTextId);
            UpdateColor();
        }

        public void UpdateColor()
        {
            image.color = BookmarkColorPicker.selectedColor;
        }

        public void Select()
        {
            MiniTimeline.Instance.selectedBookmark = this;
            isSelected = !isSelected;
            if (isSelected)
            {
                BookmarkMenu.Instance.SetColor(myUIColor);
            }
            MiniTimeline.Instance.OpenBookmarksMenu();
        }

        public void Deselect()
        {
            isSelected = false;
            MiniTimeline.Instance.selectedBookmark = null;
        }

        public string GetText()
        {
            return text;
        }

        public void Initialize(Bookmark b, string text, int id, TargetHandType _handType, float _xPosMini, QNT_Timestamp time)
        {
            
            timelineTextId = id;
            this.text = text;
            mini = b;
            handType = _handType;
            xPosMini = _xPosMini;
            originalScale = transform.localScale;
            this.time = time;
        }

        public void DeleteBookmark()
        {
            //MiniTimeline.Instance.bookmarks.Remove(mini);
            TimelineTextManager.Instance.RemoveText(timelineTextId);
            MiniTimeline.Instance.bookmarks.Remove(this);
            MiniTimeline.Instance.selectedBookmark = null;
            MiniTimeline.Instance.OpenBookmarksMenu();
            GameObject.Destroy(mini.gameObject);
            GameObject.Destroy(this.gameObject);
        }

        public void DeleteBookmarkForShift()
        {
            TimelineTextManager.Instance.RemoveText(timelineTextId);
            Destroy();
        }

        public void FixScaling()
        {
            //transform.localScale = originalScale;
            //transform.localScale = new 
            //rend.size = new Vector2(0.1f, 22f);
            //boxCollider.size = new Vector2(.1f, 22f);
            //text.rectTransform.localPosition = new Vector3(26, -730, 0);
            //text.rectTransform.localScale = new Vector3(1.3f, .1f, 1f);
        }

        public void Scale()
        {
            transform.localScale = originalScale;
            /*if (needsScaling)
            {
            }*/
        }

        public Color GetColor()
        {
            return image.color;
        }

        public BookmarkUIColor GetUIColor()
        {
            return myUIColor;
        }

        public void SetColor(Color col, BookmarkUIColor uiCol)
        {
            image.color = col;
            mini.GetComponent<Image>().color = col;
            myUIColor = uiCol;
        }

        public void Destroy()
        {
            Destroy(mini.gameObject);
            Destroy(this.gameObject);
        }
    }

}