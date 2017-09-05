using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace HalfedgeDS
{
    public class BaseProperty
    {        
        private string name = "";
        private bool persistent = false;
        public BaseProperty() { }
        public BaseProperty(string _name, bool _persistent = false) 
        {
            name = _name; persistent = _persistent;
        }
        public virtual void Clear() { }
        public virtual void Resize(int size) { }
        public virtual void Reserve(int cap) { }
    }

    public class Property<T> : BaseProperty where T : new()
    {
        private List<T> m_props; 
        public Property(int cap = 0) : base() { m_props = new List<T>(cap); }
        public Property(string _name, bool _persistent = false, int cap = 0) : base(_name, _persistent) { m_props = new List<T>(cap); }
       
        public T this[int idx] { get { return m_props[idx]; } set { m_props[idx] = value; } }
        public T[] InternalArray { get { return m_props.GetInternalArray(); } }

        public override void Clear() { if (m_props != null) m_props.Clear(); }
        public override void Resize(int size)
        {
            if (m_props != null)
            {
                if (size > m_props.Count)
                {
                    var def = default(T);
                    m_props.Add(def == null ? new T() : def);
                }
            }
        }

        public override void Reserve(int cap)
        {
            if (m_props != null && cap > m_props.Capacity)
                m_props.Capacity = cap;
        }
    }

    public class PropertyH<T>
    {
        private int m_idx = -1;
        public int idx { get { return m_idx; } }
        internal void Reset() { m_idx = -1; }
        internal void Set(int i) { m_idx = i; }
        public PropertyH() { }
        public PropertyH(int _idx) { m_idx = _idx; }
    }

    public class VPropH<T> : PropertyH<T> { }
    public class EPropH<T> : PropertyH<T> { }
    public class FPropH<T> : PropertyH<T> { }

    public class PropertyContainer
    {
        private List<BaseProperty> m_properties = new List<BaseProperty>();

        public BaseProperty this[int idx]
        {
            get
            {
                Debug.Assert(idx >= 0 && idx < m_properties.Count);
                if (idx < 0 || idx >= m_properties.Count)
                    return null;
                return m_properties[idx];
            }
        }
        
        public void Clear()
        {
            foreach (var v in m_properties)
                if (v != null) v.Clear();
        }

        public void Clean()
        {
            Clear();
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
            m_properties[idx] = new Property<T>();
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

        public T[] GetPropertyArray<T>(PropertyH<T> ph) where T : new()
        {
            var p = GetProperty(ph);
            if (p != null)
                return p.InternalArray;
            return null;
        }
    }
}
