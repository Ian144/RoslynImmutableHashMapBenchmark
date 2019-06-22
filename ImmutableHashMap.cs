using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using System.Text;
using Contract = System.Diagnostics.Contracts.Contract;

namespace RoslynImmutableHashMapConsoleApp
{
    internal sealed partial class ImmutableHashMap<TKey, TValue> : IImmutableDictionary<TKey, TValue>
    {
        private readonly Bucket _root;

        private readonly IEqualityComparer<TKey> _keyComparer;

        private readonly IEqualityComparer<TValue> _valueComparer;

        private ImmutableHashMap(Bucket root, IEqualityComparer<TKey> comparer, IEqualityComparer<TValue> valueComparer)
            : this(comparer, valueComparer)
        {
            Debug.Assert(root != null);
            Debug.Assert(comparer != null);
            Debug.Assert(valueComparer != null);

            _root = root;
        }

        internal ImmutableHashMap(IEqualityComparer<TKey> comparer = null, IEqualityComparer<TValue> valueComparer = null)
        {
            _keyComparer = comparer ?? EqualityComparer<TKey>.Default;
            _valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
        }

        public static ImmutableHashMap<TKey, TValue> Empty { get; } = new ImmutableHashMap<TKey, TValue>();

        public ImmutableHashMap<TKey, TValue> Clear()
        {
            return this.IsEmpty ? this : Empty.WithComparers(_keyComparer, _valueComparer);
        }

        #region Public methods

        [Pure]
        public ImmutableHashMap<TKey, TValue> Add(TKey key, TValue value)
        {
            var vb = new ValueBucket(key, value, _keyComparer.GetHashCode(key));

            if (_root == null)
            {
                return this.Wrap(vb);
            }

            var bucket = _root.Add(0, vb, _keyComparer, _valueComparer, false);
            return this.Wrap(bucket);
        }

        [Pure]
        public ImmutableHashMap<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs)
        {
            return this.AddRange(pairs, false, false);
        }

        [Pure]
        public ImmutableHashMap<TKey, TValue> SetItem(TKey key, TValue value)
        {
            var vb = new ValueBucket(key, value, _keyComparer.GetHashCode(key));
            var bucket = _root == null 
                            ? vb 
                            : _root.Add(0, vb, _keyComparer, _valueComparer, true);

            return this.Wrap(bucket);
        }

        [Pure]
        public ImmutableHashMap<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items)
        {
            return this.AddRange(items, true, false);
        }

        [Pure]
        public ImmutableHashMap<TKey, TValue> Remove(TKey key)
        {
            return _root != null 
                ? this.Wrap(_root.Remove(_keyComparer.GetHashCode(key), key, _keyComparer)) 
                : this;
        }

        [Pure]
        public ImmutableHashMap<TKey, TValue> RemoveRange(IEnumerable<TKey> keys)
        {
            var map = _root;
            if (map != null)
            {
                foreach (var key in keys)
                {
                    map = map.Remove(_keyComparer.GetHashCode(key), key, _keyComparer);
                    if (map == null)
                    {
                        break;
                    }
                }
            }

            return this.Wrap(map);
        }

        [Pure]
        public ImmutableHashMap<TKey, TValue> WithComparers(IEqualityComparer<TKey> keyComparer, IEqualityComparer<TValue> valueComparer)
        {
            if (keyComparer == null)
            {
                keyComparer = EqualityComparer<TKey>.Default;
            }

            if (valueComparer == null)
            {
                valueComparer = EqualityComparer<TValue>.Default;
            }

            if (_keyComparer == keyComparer)
            {
                if (_valueComparer == valueComparer)
                {
                    return this;
                }
                else
                {
                    return new ImmutableHashMap<TKey, TValue>(_root, _keyComparer, valueComparer);
                }
            }
            else
            {
                var set = new ImmutableHashMap<TKey, TValue>(keyComparer, valueComparer);
                set = set.AddRange(this, false, true);
                return set;
            }
        }

        [Pure]
        public ImmutableHashMap<TKey, TValue> WithComparers(IEqualityComparer<TKey> keyComparer)
        {
            return this.WithComparers(keyComparer, _valueComparer);
        }

        [Pure]
        public bool ContainsValue(TValue value) => this.Values.Contains(value, _valueComparer);

        #endregion

        #region IImmutableDictionary<TKey, TValue> Members

        public int Count => _root?.Count ?? 0;

        public bool IsEmpty => this.Count == 0;

        public IEnumerable<TKey> Keys
        {
            get
            {
                if (_root == null)
                {
                    yield break;
                }

                var stack = new Stack<IEnumerator<Bucket>>();
                stack.Push(_root.GetAll().GetEnumerator());
                while (stack.Count > 0)
                {
                    var en = stack.Peek();
                    if (en.MoveNext())
                    {
                        if (en.Current is ValueBucket vb)
                        {
                            yield return vb.Key;
                        }
                        else
                        {
                            stack.Push(en.Current.GetAll().GetEnumerator());
                        }
                    }
                    else
                    {
                        stack.Pop();
                    }
                }
            }
        }

        public IEnumerable<TValue> Values
        {
#pragma warning disable 618
            get { return this.GetValueBuckets().Select(vb => vb.Value); }
#pragma warning restore 618
        }

        public TValue this[TKey key]
        {
            get
            {
                if (this.TryGetValue(key, out var value))
                {
                    return value;
                }

                throw new KeyNotFoundException();
            }
        }

        public bool ContainsKey(TKey key)
        {
            if (_root != null)
            {
                var vb = _root.Get(_keyComparer.GetHashCode(key), key, _keyComparer);
                return vb != null;
            }

            return false;
        }

        public bool Contains( KeyValuePair<TKey, TValue> keyValuePair )
        {
            if (_root != null)
            {
                var vb = _root.Get(_keyComparer.GetHashCode(keyValuePair.Key), keyValuePair.Key, _keyComparer);
                return vb != null && _valueComparer.Equals(vb.Value, keyValuePair.Value);
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_root != null)
            {
                var vb = _root.Get(_keyComparer.GetHashCode(key), key, _keyComparer);
                if (vb != null)
                {
                    value = vb.Value;
                    return true;
                }
            }

            value = default;
            return false;
        }

        public bool TryGetKey(TKey equalKey, out TKey actualKey)
        {
            if (_root != null)
            {
                var vb = _root.Get(_keyComparer.GetHashCode(equalKey), equalKey, _keyComparer);
                if (vb != null)
                {
                    actualKey = vb.Key;
                    return true;
                }
            }

            actualKey = equalKey;
            return false;
        }

        #endregion

        #region IEnumerable<KeyValuePair<TKey, TValue>> Members

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return this.GetValueBuckets().Select(vb => new KeyValuePair<TKey, TValue>(vb.Key, vb.Value))
                .GetEnumerator();
        }

        #endregion

        #region IEnumerable Members

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        #endregion

        internal bool TryExchangeKey(TKey key, out TKey existingKey)
        {
            var vb = _root?.Get(_keyComparer.GetHashCode(key), key, _keyComparer);
            if (vb != null)
            {
                existingKey = vb.Key;
                return true;
            }
            else
            {
                existingKey = default;
                return false;
            }
        }

        private static bool TryCastToImmutableMap( IEnumerable<KeyValuePair<TKey, TValue>> sequence, out ImmutableHashMap<TKey, TValue> other)
        {
            other = sequence as ImmutableHashMap<TKey, TValue>;
            if (other != null)
            {
                return true;
            }

            return false;
        }

        private ImmutableHashMap<TKey, TValue> Wrap(Bucket root)
        {
            if (root == null)
            {
                return this.Clear();
            }

            if (_root != root)
            {
                return root.Count == 0
                    ? this.Clear()
                    : new ImmutableHashMap<TKey, TValue>(root, _keyComparer, _valueComparer);
            }

            return this;
        }

        [Pure]
        private ImmutableHashMap<TKey, TValue> AddRange( IEnumerable<KeyValuePair<TKey, TValue>> pairs, bool overwriteOnCollision, bool avoidToHashMap)
        {
            Debug.Assert(pairs != null);
            if (this.IsEmpty && !avoidToHashMap)
            {
                if (TryCastToImmutableMap(pairs, out var other))
                {
                    return other.WithComparers(_keyComparer, _valueComparer);
                }
            }

            var map = this;
            foreach (var pair in pairs)
            {
                map = overwriteOnCollision
                    ? map.SetItem(pair.Key, pair.Value)
                    : map.Add(pair.Key, pair.Value);
            }


            return map;
        }

        private IEnumerable<ValueBucket> GetValueBuckets()
        {
            if (_root == null)
            {
                yield break;
            }

            var stack = new Stack<IEnumerator<Bucket>>();
            stack.Push(_root.GetAll().GetEnumerator());
            while (stack.Count > 0)
            {
                var en = stack.Peek();
                if (en.MoveNext())
                {
                    if (en.Current is ValueBucket vb)
                    {
                        yield return vb;
                    }
                    else
                    {
                        stack.Push(en.Current.GetAll().GetEnumerator());
                    }
                }
                else
                {
                    stack.Pop();
                }
            }
        }

        #region IImmutableDictionary<TKey,TValue> Members

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Clear()
        {
            return this.Clear();
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Add(TKey key, TValue value)
        {
            return this.Add(key, value);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItem(TKey key, TValue value)
        {
            return this.SetItem(key, value);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.SetItems( IEnumerable<KeyValuePair<TKey, TValue>> items )
        {
            return this.SetItems(items);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.AddRange( IEnumerable<KeyValuePair<TKey, TValue>> pairs )
        {
            return this.AddRange(pairs);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.RemoveRange(IEnumerable<TKey> keys)
        {
            return this.RemoveRange(keys);
        }

        IImmutableDictionary<TKey, TValue> IImmutableDictionary<TKey, TValue>.Remove(TKey key)
        {
            return this.Remove(key);
        }

        #endregion

        private static class Strings
        {
            public static string DuplicateKey => "An_element_with_the_same_key_but_a_different_value_already_exists";
        }
    }


}