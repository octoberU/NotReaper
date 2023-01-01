using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using NotReaper.ReviewSystem;
using System.Linq;
using NotReaper.UI;
using UnityEngine.UI;

namespace NotReaper.ReviewSystem
{
    public class CommentEntry : ListEntry
    {
        [SerializeField] private Image typeDisplay;
        [SerializeField] private Image suggestionDisplay;
        [Space]
        [Header("Icons")]
        [SerializeField] private Sprite pog;
        [SerializeField] private Sprite notLikeThis;
        [SerializeField] private Sprite thonk;
        [SerializeField] private Sprite checkSprite;

        private CommentType commentType;
        private ReviewComment comment;

        protected override ListData GetData() => comment;
        public override void SetData(ListData comment)
        {
            this.comment = comment as ReviewComment;
            UpdateEntry();
        }

        public override void UpdateEntry()
        {
            base.UpdateEntry();
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
            typeDisplay.enabled = true;
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
                case CommentType.General:
                    typeDisplay.enabled = false;
                    break;
            }
            typeDisplay.color = Color.white;
        }

        public override void SelectEntry()
        {
            ReviewManager.Instance.SelectComment(Index);
            base.SelectEntry();         
        }

        public void SetChecked(bool check)
        {
            if (check)
            {
                typeDisplay.enabled = true;
                typeDisplay.sprite = checkSprite;
                typeDisplay.color = Color.green;
            }
            else SetSprite(commentType);
        }
    }
}

