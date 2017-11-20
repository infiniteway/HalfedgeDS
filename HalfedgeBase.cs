using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HalfedgeDS
{
    public class SectionList<T> : IEnumerable<T>, IDisposable
    {
        private int _sectionSize = 4096;
        private List<List<T>> _itemLists = new List<List<T>>();

        private int _sectionIndex = 0;
        private int _count = 0;
        private int _capacity = 0;
                
        public SectionList(int section = 4096)
        {
            _sectionSize = section;
        }

        public void Add(T v)
        {
            EnsureCapacity(_count + 1, _capacity);
            _itemLists[_sectionIndex].Add(v);
            _count++;
        }

        public T this[int index]
        {
            get
            {
                if (index < 0 || index >= this._count)
                {
                    throw new System.ArgumentOutOfRangeException();
                }
                var seg = index / _sectionSize;
                var mod = index % _sectionSize;
                return _itemLists[seg][mod];
            }
            set
            {
                if (index < 0 || index >= this._count)
                {
                    throw new System.ArgumentOutOfRangeException();
                }
                var seg = index / _sectionSize;
                var mod = index % _sectionSize;
                _itemLists[seg][mod] = value;
            }
        }

        public int Count { get { return _count; } }
        public int Capacity
        {
            get { return _capacity; }
            set
            {
                if (value < this._count)
                    return;
                if(value != this._capacity)
                {
                    var seg = value / _sectionSize;
                    var mod = value % _sectionSize;
                    if (mod > 0) seg++;
                    for (int i = _itemLists.Count; i < seg; i++)
                        _itemLists.Add(new List<T>(_sectionSize));
                    _capacity = seg * _sectionSize;
                }
            }
        }

        public void Clear()
        {
            if(this._count > 0)
            {
                foreach (var section in _itemLists)
                    section.Clear();
                _count = 0;
            }
            _sectionIndex = 0;
        }

        private void EnsureCapacity(int len, int cap)
        {
            if(len > cap)
            {
                _capacity += _sectionSize;
                _itemLists.Add(new List<T>(_sectionSize));
            }
            if (_itemLists[_sectionIndex].Count + 1 > _sectionSize)
                _sectionIndex++;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new Enumerator(this);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return new Enumerator(this);
        }

        public void Dispose()
        {
            _itemLists.Clear();
            _count = 0;
            _capacity = 0;
            _sectionIndex = 0;
        }

        public struct Enumerator : IEnumerator<T>, IDisposable, IEnumerator
        {
            private SectionList<T> list;
            private int index;
            private T current;

            internal Enumerator(SectionList<T> _list)
            {
                this.list = _list;
                this.index = 0;
                this.current = default(T);
            }

            public T Current { get { return current; } }

            object IEnumerator.Current { get { return this.Current; } }

            public bool MoveNext()
            {
                if(this.index < list._count)
                {
                    this.current = list[this.index];
                    this.index++;
                    return true;
                }
                else
                {
                    this.index = list._count + 1;
                    this.current = default(T);
                    return false;
                }
            }

            public void Reset()
            {
                this.index = 0;
                this.current = default(T);
            }

            public void Dispose()
            {
                
            }
        }
    }
}
