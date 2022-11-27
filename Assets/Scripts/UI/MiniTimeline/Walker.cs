using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace NotReaper
{
    public class Walker<T>
    {
        private List<T> entries;
        private int index = 0;
        private int capacity;

        public bool HasData => entries.Count > 0;

        public Walker(int capacity)
        {
            this.capacity = capacity;
            entries = new(capacity);
        }

        public T Next()
        {
            index = Mathf.Min(Mathf.Min(entries.Count, capacity) - 1, index + 1);
            return entries[index];
        }

        public T Previous()
        {
            index = Mathf.Max(0, Mathf.Min(entries.Count - 1, index - 1));
            return entries[index];
        }

        public void Add(T data)
        {
            for (int i = entries.Count - 1; i > index; i--)
                entries.RemoveAt(i);   
            
            entries.Add(data);
           
           if(entries.Count > capacity)
               entries.RemoveAt(0);

           index = entries.Count - 1;
        }

        public void Clear()
        {
            entries.Clear();
            index = 0;
        }
    }
}
