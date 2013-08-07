using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace System.Collections.Generic
{
    public class LinkedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>
    {
        private Dictionary<TKey, TValue> _Self;
        private IDictionary<TKey, TValue> _Parent;

        public LinkedDictionary(IDictionary<TKey, TValue> Parent)
        {
            _Parent = Parent;
            _Self = new Dictionary<TKey, TValue>();
        }

        public void Add(TKey key, TValue value)
        {
            _Self.Add(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            return _Self.ContainsKey(key) || _Parent.ContainsKey(key);
        }

        public ICollection<TKey> Keys
        {
            get { return _Self.Keys.Concat(_Parent.Keys).ToList(); }
        }

        public bool Remove(TKey key)
        {
            return _Self.Remove(key);
        }

        public bool RemoveRecursive(TKey key)
        {
            return _Self.ContainsKey(key) ? _Self.Remove(key) : _Parent.Remove(key);
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (!_Self.TryGetValue(key, out value))
                return _Parent.TryGetValue(key, out value);
            return true;
        }

        public ICollection<TValue> Values
        {
            get { return _Self.Values.Concat(_Parent.Values).ToList(); }
        }

        public TValue this[TKey key]
        {
            get
            {
                return _Self.ContainsKey(key) ? _Self[key] : _Parent[key];
            }
            set
            {
                _Self[key] = value;
            }
        }

        public void Add(KeyValuePair<TKey, TValue> item)
        {
            this.Add(item.Key, item.Value);
        }

        public void Clear()
        {
            _Self.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> item)
        {
            return _Self.Contains(item) || _Parent.Contains(item);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public int Count
        {
            get { return _Self.Count + _Parent.Count; }
        }

        public bool IsReadOnly
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new System.NotImplementedException();
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return _Self.GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return _Self.GetEnumerator();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
        {
            throw new System.NotImplementedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.Clear()
        {
            throw new System.NotImplementedException();
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)
        {
            throw new System.NotImplementedException();
        }

        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        int ICollection<KeyValuePair<TKey, TValue>>.Count
        {
            get { throw new System.NotImplementedException(); }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly
        {
            get { throw new System.NotImplementedException(); }
        }

        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
        {
            throw new System.NotImplementedException();
        }

        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        void IDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            _Self.Add(key, value);
        }

        bool IDictionary<TKey, TValue>.ContainsKey(TKey key)
        {
            return this.ContainsKey(key);
        }

        ICollection<TKey> IDictionary<TKey, TValue>.Keys
        {
            get { return this.Keys; }
        }

        bool IDictionary<TKey, TValue>.Remove(TKey key)
        {
            return this.Remove(key);
        }

        bool IDictionary<TKey, TValue>.TryGetValue(TKey key, out TValue value)
        {
            return this.TryGetValue(key, out value);
        }

        ICollection<TValue> IDictionary<TKey, TValue>.Values
        {
            get { return this.Values; }
        }

        TValue IDictionary<TKey, TValue>.this[TKey key]
        {
            get
            {
                return this[key];
            }
            set
            {
                this[key] = value;
            }
        }
    }
}