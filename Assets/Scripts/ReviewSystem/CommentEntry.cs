using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NotReaper.ReviewSystem;
using System.Linq;
using UnityEngine.UI;

namespace NotReaper.ReviewSystem
{
    public class CommentEntry : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI indexDisplay;
        [SerializeField] private TextMeshProUGUI tickDisplay;
        [SerializeField] private Image typeDisplay;
        [SerializeField] private Image suggestionDisplay;
        [SerializeField] private Image outline;
        [Space]
        [Header("Icons")]
        [SerializeField] private Sprite pog;
        [SerializeField] private Sprite notLikeThis;
        [SerializeField] private Sprite thonk;
        [SerializeField] private Sprite checkSprite;

        private CommentType commentType;
        private ReviewComment comment;
        public int StartTick;
        public int Index
        {
            get
            {
                return _index;
            }
            set
            {
                SetIndex(value);
            }
        }
        public bool IsSelected
        {
            get
            {
                return _isSelected;
            }
            set
            {
                SetSelected(value);
            }
        }
        private int _index = 0;
        private bool _isSelected = false;

        private void SetIndex(int value)
        {
            _index = value;
            indexDisplay.text = (_index + 1).ToString();
            transform.SetSiblingIndex(value);
        }

        public void SetComment(ReviewComment comment)
        {
            this.comment = comment;
            UpdateEntry();            
        }

        public void UpdateEntry()
        {
            StartTick = comment.selectedCues.First().tick;
            tickDisplay.text = StartTick.ToString();
            commentType = comment.type;
            EnableSuggestion(comment.HasSuggestion);
            SetSprite(commentType);
            SetChecked(comment.isChecked);
        }

        public void EnableSuggestion(bool enable)
        {
            suggestionDisplay.enabled = enable;
        }

        private void SetSprite(CommentType type)
        {
            switch (type)
            {
                case CommentType.Negative:
                    typeDisplay.sprite = notLikeThis;
                    break;
                case CommentType.Positive:
                    typeDisplay.sprite = pog;
                    break;
                case CommentType.Suggestion:
                    typeDisplay.sprite = thonk;
                    break;
            }
            typeDisplay.color = Color.white;
        }

        public void SelectComment()
        {
            ReviewWindow.Instance.SelectComment(Index);

            SetSelected(true);          
        }

        public void SetSelected(bool selected)
        {
            _isSelected = selected;
            outline.color = _isSelected ? Color.green : Color.white;
        }

        public void SetChecked(bool check)
        {
            if (check)
            {
                typeDisplay.sprite = checkSprite;
                typeDisplay.color = Color.green;
            }
            else SetSprite(commentType);
        }
    }
}

