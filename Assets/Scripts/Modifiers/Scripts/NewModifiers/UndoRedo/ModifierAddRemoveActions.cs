using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NotReaper.Timing;
using UnityEngine;

namespace NotReaper.Modifiers
{

    public class AddModifierAction : AddContentAction<Data>
    {
        public AddModifierAction(QNT_Timestamp startTime, Track track) : base(startTime, track)
        {
        }
        
        public override void DoAction(TimelineManager<Data> manager)
        {
            if (initialAdd)
            {
                addedContent = manager.PlaceContentFromAction(startTime, track);
                initialAdd = false;
            }
            else
            {
                addedContent = manager.PlaceContentFromAction(addedContent.timeframe, track);
            }
            
            (addedContent as Modifier)?.SetupSprites();
        }
    }
    
    public class MultiAddModifierAction : MultiAddContentAction<Data>
    {
        public MultiAddModifierAction(List<Data> datas) : base(datas)
        {
        }

        protected override Content LoadData(Data data, TimelineManager<Data> manager)
            => ((ModifierManager)manager).LoadModifier(data);
    }

    public class RemoveModifierAction : RemoveContentAction<Data>
    {
        public RemoveModifierAction(Content content) : base(content)
        {
        }
        
        protected override Content LoadData(Data data, TimelineManager<Data> manager)
            => ((ModifierManager)manager).LoadModifier(data);
    }

    public class MultiRemoveModifierAction : MultiRemoveContentAction<Data>
    {
        public MultiRemoveModifierAction(List<Content> content) : base(content)
        {
        }

        protected override Content LoadData(Data data, TimelineManager<Data> manager)
            => ((ModifierManager)manager).LoadModifier(data);
    }
    
}
