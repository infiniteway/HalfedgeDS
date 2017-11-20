using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace HalfedgeDS
{
    public class BaseProperty : IDisposable
    {
        private PropertyH m_propHandle;
        public BaseProperty() { }
        public BaseProperty(PropertyH propH) 
        {
            m_propHandle = propH;
        }
        public virtual void Clear() { }
        public virtual void Dispose()
        {
            if (m_propHandle != null)
                m_propHandle.Reset();
            m_propHandle = null;
        }
        public virtual void Resize(int size) { }
        public virtual void Reserve(int cap) { }        
    }

    public class Property<T> : BaseProperty where T : new()
    {
        private SectionList<T> m_props = new SectionList<T>();
        public Property() : base() { }
        public Property(PropertyH ph) : base(ph) { }

        public T this[int idx] { get { return m_props[idx]; } set { m_props[idx] = value; } }

        public override void Clear() { if (m_props != null) m_props.Clear(); }
        public override void Dispose() { if (m_props != null) m_props.Dispose(); base.Dispose(); }        
        public override void Resize(int size)
        {
            if (m_props != null)
            {
                if (size > m_props.Count)
                {
                    int add = size - m_props.Count;
                    for(int i = 0; i < add; ++i)
                    {
                        var def = default(T);
                        m_props.Add(def == null ? new T() : def);
                    }
                }
            }
        }

        public override void Reserve(int cap)
        {
            if (m_props != null && cap > m_props.Capacity)
                m_props.Capacity = cap;
        }
    }

    public class PropertyH
    {
        private string m_name = "<Unknown>";
        private bool m_persistent = false;
        private int m_idx = -1;

        public int idx { get { return m_idx; } }
        public string propertyName { get { return m_name; } }
        public bool persistent { get { return m_persistent; } }
                
        internal void Reset() { m_idx = -1; }
        internal void Set(int i) { m_idx = i; }

        public PropertyH() { }
        //public PropertyH(int _idx) { m_idx = _idx; }
        public PropertyH(string name, bool persist)
        {
            this.m_name = name;
            this.m_persistent = persist;
        }
    }

    public class PropertyH<T> : PropertyH
    {
        public PropertyH() : base() { }
        public PropertyH(string name, bool persist) : base(name, persist) { }
    }

    public class VPropH<T> : PropertyH<T>
    {
        public VPropH() : base() { }
        public VPropH(string name, bool persist = false) : base(name, persist) { }
    }
    public class EPropH<T> : PropertyH<T>
    {
        public EPropH() : base() { }
        public EPropH(string name, bool persist = false) : base(name, persist) { }
    }
    public class FPropH<T> : PropertyH<T>
    {
        public FPropH() : base() { }
        public FPropH(string name, bool persist = false) : base(name, persist) { }
    }
    public class HPropH<T> : PropertyH<T>
    {
        public HPropH() : base() { }
        public HPropH(string name, bool persist = false) : base(name, persist) { }
    }

    public class PropertyContainer : IDisposable
    {
        private List<BaseProperty> m_properties = new List<BaseProperty>(8);

        public BaseProperty this[int idx]
        {
            get
            {
                //Debug.Assert(idx >= 0 && idx < m_properties.Count);
                if (idx < 0 || idx >= m_properties.Count)
                    return null;
                return m_properties[idx];
            }
        }
        
        // clear just clear the content in the array
        public void Clear()
        {
            foreach (var v in m_properties)
                if (v != null) v.Clear();
        }

        // clean will create a new list with zero capacity, this will release the memory
        public void Dispose()
        {
            foreach (var v in m_properties)
                if (v != null) v.Dispose();
            m_properties.Clear();
        }

        public void Resize(int size)
        {
            foreach (var v in m_properties)
                if (v != null)
                    v.Resize(size);
        }

        public void Reserve(int cap)
        {
            foreach (var v in m_properties)
                if (v != null)
                    v.Reserve(cap);
        }

        public void AddProperty<T>(PropertyH<T> ph) where T : new()
        {
            int idx = 0;
            int end = m_properties.Count;
            for (; idx < end; )
            {
                if (m_properties[idx] == null)
                    break;
                idx++;
            }
            if (idx == end) m_properties.Add(null);
            m_properties[idx] = new Property<T>(ph);
            ph.Set(idx);
        }

        public void RemoveProperty<T>(PropertyH<T> ph)
        {
            if (ph.idx < 0 || ph.idx >= m_properties.Count)
                return;
            var p = m_properties[ph.idx];
            m_properties[ph.idx] = null;
            if(p != null) p.Clear();
        }

        public Property<T> GetProperty<T>(PropertyH<T> ph) where T : new()
        {
            return this[ph.idx] as Property<T>;
        }
    }
}
