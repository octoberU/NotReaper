using System.Collections;
using System.Collections.Generic;
using NotReaper;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper
{
    public class AddContentAction<TData> : TimelineAction<TData> where TData : ContentData
    {
        protected Content addedContent;
        protected QNT_Timestamp startTime;
        protected Track track;

        protected bool initialAdd = true;

        public AddContentAction(QNT_Timestamp startTime, Track track)
        {
            this.startTime = startTime;
            this.track = track;
        }
        
        public override void DoAction(TimelineManager<TData> manager)
        {
            if (initialAdd)
            {
                addedContent = manager.PlaceContentFromAction(startTime, track);
                initialAdd = false;
            }
            else
            {
                addedContent = manager.PlaceContentFromAction(addedContent.timeframe, addedContent.Track);
            }
        }

        public override void UndoAction(TimelineManager<TData> manager)
        {
            if (addedContent != null)
            {
                manager.RemoveContentFromAction(addedContent);
            }
        }
    }

    public abstract class MultiAddContentAction<TData> : TimelineAction<TData> where TData : ContentData
    {
        protected List<Content> addedContent = new();
        protected List<TData> datas = new();

        public MultiAddContentAction(List<TData> datas)
        {
            foreach (var data in datas)
            {
                this.datas.Add(data.Clone() as TData);
            }
        }

        public override void DoAction(TimelineManager<TData> manager)
        {
            foreach (var data in datas)
            {
                addedContent.Add(LoadData(data, manager));
            }
        }

        protected abstract Content LoadData(TData data, TimelineManager<TData> manager);

        public override void UndoAction(TimelineManager<TData> manager)
        {
            if (addedContent.Count <= 0) return;
            
            foreach(var content in addedContent)
                manager.RemoveContentFromAction(content);
                
            addedContent.Clear();
        }
    }

    public abstract class RemoveContentAction<TData> : TimelineAction<TData> where TData : ContentData
    {
        private Content content;
        private TData data;

        public RemoveContentAction(Content content)
        {
            this.content = content;
            data = content.GetData() as TData;
        }
        
        public override void DoAction(TimelineManager<TData> manager)
        {
            manager.RemoveContentFromAction(content);
        }

        public override void UndoAction(TimelineManager<TData> manager)
        {
            content = LoadData(data, manager);
        }

        protected abstract Content LoadData(TData data, TimelineManager<TData> manager);
    }

    public abstract class MultiRemoveContentAction<TData> : TimelineAction<TData> where TData : ContentData
    {
        private List<TData> datas = new();
        private List<Content> content;
        
        public MultiRemoveContentAction(List<Content> content)
        {
            this.content = content;
            foreach (var c in content)
            {
                datas.Add(c.GetData() as TData);
            }
        }
        public override void DoAction(TimelineManager<TData> manager)
        {
            for (int i = content.Count - 1; i >= 0; i--)
            {
                manager.RemoveContentFromAction(content[i]);
            }
        }

        public override void UndoAction(TimelineManager<TData> manager)
        {
            content.Clear();
            foreach (var data in datas)
            {
                content.Add(LoadData(data, manager));
            }
        }

        protected abstract Content LoadData(TData data, TimelineManager<TData> manager);
    }
}
