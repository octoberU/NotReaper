using System;
using Newtonsoft.Json;

namespace NotReaper.UI
{
    [Serializable]
    public class ListData
    {
        public int tick;
        public string description;

        [NonSerialized, JsonIgnore]
        public ListEntry entry;
        
        public ListData(){}
    }
}
