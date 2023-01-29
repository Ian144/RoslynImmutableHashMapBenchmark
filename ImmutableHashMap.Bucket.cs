using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using Roslyn.Utilities;

namespace RoslynImmutableHashMapConsoleApp
{
    internal sealed partial class ImmutableHashMap<TKey, TValue>
    {
        private sealed class HashBucket : Bucket
        {
            private readonly int _hashRoll;
            private readonly uint _used;
            private readonly Bucket[] _buckets;
            private readonly int _count;

            private HashBucket(int hashRoll, uint used, Bucket[] buckets, int count)
            {
                Debug.Assert(buckets != null);
                Debug.Assert(buckets.Length == CountBits(used));

                _hashRoll = hashRoll & 31;
                _used = used;
                _buckets = buckets;
                _count = count;
            }

            internal HashBucket(int suggestedHashRoll, ValueOrListBucket bucket1, ValueOrListBucket bucket2)
            {
                Debug.Assert(bucket1 != null);
                Debug.Assert(bucket2 != null);
                Debug.Assert(bucket1.Hash != bucket2.Hash);

                int h1 = bucket1.Hash;
                int h2 = bucket2.Hash;
                for (int i = 0; i < 32; i++)
                {
                    _hashRoll = (suggestedHashRoll + i) & 31;
                    int s1 = this.ComputeLogicalSlot(h1);
                    int s2 = this.ComputeLogicalSlot(h2);

                    if (s1 != s2)
                    {
                        _count = 2;
                        _used = (1u << s1) | (1u << s2);
                        _buckets = new Bucket[2];
                        _buckets[this.ComputePhysicalSlot(s1)] = bucket1;
                        _buckets[this.ComputePhysicalSlot(s2)] = bucket2;
                        return;
                    }
                }

                throw new InvalidOperationException();
            }

            internal override int Count => _count;

            internal override Bucket Add(
                int suggestedHashRoll, 
                ValueBucket bucket, 
                IEqualityComparer<TKey> keyComparer,
                IEqualityComparer<TValue> valueComparer, 
                bool overwriteExistingValue)
            {
                int logicalSlot = ComputeLogicalSlot(bucket.Hash);
                if (IsInUse(logicalSlot))
                {
                    int physicalSlot = ComputePhysicalSlot(logicalSlot);
                    var existing = _buckets[physicalSlot];

                    var added = existing.Add(_hashRoll + 5, bucket, keyComparer, valueComparer, overwriteExistingValue);
                    if (added != existing)
                    {
                        var newBuckets = _buckets.ReplaceAt(physicalSlot, added);
                        return new HashBucket(_hashRoll, _used, newBuckets, _count - existing.Count + added.Count);
                    }
                    else
                    {
                        return this;
                    }
                }
                else
                {
                    int physicalSlot = ComputePhysicalSlot(logicalSlot);
                    var newBuckets = _buckets.InsertAt(physicalSlot, bucket);
                    var newUsed = InsertBit(logicalSlot, _used);
                    return new HashBucket(_hashRoll, newUsed, newBuckets, _count + bucket.Count);
                }
            }

            internal override Bucket Remove(int hash, TKey key, IEqualityComparer<TKey> comparer)
            {
                int logicalSlot = ComputeLogicalSlot(hash);
                if (IsInUse(logicalSlot))
                {
                    int physicalSlot = ComputePhysicalSlot(logicalSlot);
                    var existing = _buckets[physicalSlot];
                    Bucket result = existing.Remove(hash, key, comparer);
                    if (result == null)
                    {
                        if (_buckets.Length == 1)
                        {
                            return null;
                        }
                        else if (_buckets.Length == 2)
                        {
                            return physicalSlot == 0 ? _buckets[1] : _buckets[0];
                        }
                        else
                        {
                            return new HashBucket(_hashRoll, RemoveBit(logicalSlot, _used),
                                _buckets.RemoveAt(physicalSlot), _count - existing.Count);
                        }
                    }
                    else if (_buckets[physicalSlot] != result)
                    {
                        return new HashBucket(_hashRoll, _used, _buckets.ReplaceAt(physicalSlot, result),
                            _count - existing.Count + result.Count);
                    }
                }

                return this;
            }

            internal override ValueBucket Get(int hash, TKey key, IEqualityComparer<TKey> comparer)
            {
                int logicalSlot = ComputeLogicalSlot(hash);
                if (IsInUse(logicalSlot))
                {
                    int physicalSlot = ComputePhysicalSlot(logicalSlot);
                    return _buckets[physicalSlot].Get(hash, key, comparer);
                }

                return null;
            }

            internal override IEnumerable<Bucket> GetAll()
            {
                return _buckets;
            }

            private bool IsInUse(int logicalSlot)
            {
                return ((1 << logicalSlot) & _used) != 0;
            }

            private int ComputeLogicalSlot(int hc)
            {
                uint uc = unchecked((uint) hc);
                uint rotated = RotateRight(uc, _hashRoll);
                return unchecked((int) (rotated & 31));
            }

            [Pure]
            private static uint RotateRight(uint v, int n)
            {
                Debug.Assert(n >= 0 && n < 32);
                if (n == 0)
                {
                    return v;
                }

                return v >> n | (v << (32 - n));
            }

            private int ComputePhysicalSlot(int logicalSlot)
            {
                Debug.Assert(logicalSlot >= 0 && logicalSlot < 32);
                Contract.Ensures(Contract.Result<int>() >= 0);
                if (_buckets.Length == 32)
                {
                    return logicalSlot;
                }

                if (logicalSlot == 0)
                {
                    return 0;
                }

                uint mask = uint.MaxValue >> (32 - logicalSlot);
                return CountBits(_used & mask);
            }

            [Pure]
            private static int CountBits(uint v)
            {
                unchecked
                {
#pragma warning disable IDE0054    
                    v = v - ((v >> 1) & 0x55555555u);
#pragma warning restore IDE0054    
                    v = (v & 0x33333333u) + ((v >> 2) & 0x33333333u);
                    return (int) ((v + (v >> 4) & 0xF0F0F0Fu) * 0x1010101u) >> 24;
                }
            }

            [Pure]
            private static uint InsertBit(int position, uint bits)
            {
                Debug.Assert(0 == (bits & (1u << position)));
                return bits | (1u << position);
            }

            [Pure]
            private static uint RemoveBit(int position, uint bits)
            {
                Debug.Assert(0 != (bits & (1u << position)));
                return bits & ~(1u << position);
            }
        }

        private sealed class ListBucket : ValueOrListBucket
        {
            private readonly ValueBucket[] _buckets;

            internal ListBucket(ValueBucket[] buckets)
                : base(buckets[0].Hash)
            {
                Debug.Assert(buckets != null);
                Debug.Assert(buckets.Length >= 2);
                _buckets = buckets;
            }

            internal override int Count => _buckets.Length;

            internal override Bucket Add(int suggestedHashRoll, ValueBucket bucket, IEqualityComparer<TKey> comparer,
                IEqualityComparer<TValue> valueComparer, bool overwriteExistingValue)
            {
                if (this.Hash == bucket.Hash)
                {
                    int pos = this.Find(bucket.Key, comparer);
                    if (pos >= 0)
                    {
                        if (valueComparer.Equals(bucket.Value, _buckets[pos].Value))
                        {
                            return this;
                        }
                        else
                        {
                            if (overwriteExistingValue)
                            {
                                return new ListBucket(_buckets.ReplaceAt(pos, bucket));
                            }
                            else
                            {
                                throw new ArgumentException(Strings.DuplicateKey);
                            }
                        }
                    }
                    else
                    {
                        return new ListBucket(_buckets.InsertAt(_buckets.Length, bucket));
                    }
                }
                else
                {
                    return new HashBucket(suggestedHashRoll, this, bucket);
                }
            }

            internal override Bucket Remove(int hash, TKey key, IEqualityComparer<TKey> comparer)
            {
                if (this.Hash == hash)
                {
                    int pos = this.Find(key, comparer);
                    if (pos >= 0)
                    {
                        if (_buckets.Length == 1)
                        {
                            return null;
                        }
                        else if (_buckets.Length == 2)
                        {
                            return pos == 0 ? _buckets[1] : _buckets[0];
                        }
                        else
                        {
                            return new ListBucket(_buckets.RemoveAt(pos));
                        }
                    }
                }

                return this;
            }

            internal override ValueBucket Get(int hash, TKey key, IEqualityComparer<TKey> comparer)
            {
                if (this.Hash == hash)
                {
                    int pos = this.Find(key, comparer);
                    if (pos >= 0)
                    {
                        return _buckets[pos];
                    }
                }

                return null;
            }

            private int Find(TKey key, IEqualityComparer<TKey> comparer)
            {
                for (int i = 0; i < _buckets.Length; i++)
                {
                    if (comparer.Equals(key, _buckets[i].Key))
                    {
                        return i;
                    }
                }

                return -1;
            }

            internal override IEnumerable<Bucket> GetAll()
            {
                return _buckets;
            }
        }

        private sealed class ValueBucket : ValueOrListBucket
        {
            internal readonly TKey Key;
            internal readonly TValue Value;

            internal ValueBucket(TKey key, TValue value, int hashcode)
                : base(hashcode)
            {
                this.Key = key;
                this.Value = value;
            }

            internal override int Count => 1;

            internal override Bucket Add(int suggestedHashRoll, ValueBucket bucket, IEqualityComparer<TKey> comparer,
                IEqualityComparer<TValue> valueComparer, bool overwriteExistingValue)
            {
                if (this.Hash == bucket.Hash)
                {
                    if (comparer.Equals(this.Key, bucket.Key))
                    {
                        if (valueComparer.Equals(this.Value, bucket.Value))
                        {
                            return this;
                        }
                        else
                        {
                            if (overwriteExistingValue)
                            {
                                return bucket;
                            }
                            else
                            {
                                throw new ArgumentException(Strings.DuplicateKey);
                            }
                        }
                    }
                    else
                    {
                        return new ListBucket(new ValueBucket[] {this, bucket});
                    }
                }
                else
                {
                    return new HashBucket(suggestedHashRoll, this, bucket);
                }
            }

            internal override Bucket Remove(int hash, TKey key, IEqualityComparer<TKey> comparer)
            {
                if (this.Hash == hash && comparer.Equals(this.Key, key))
                {
                    return null;
                }

                return this;
            }

            internal override ValueBucket Get(int hash, TKey key, IEqualityComparer<TKey> comparer)
            {
                if (this.Hash == hash && comparer.Equals(this.Key, key))
                {
                    return this;
                }

                return null;
            }

            internal override IEnumerable<Bucket> GetAll()
            {
                //throw new NotImplementedException();
                //return SpecializedCollections.SingletonEnumerable(this);
                //return new Roslyn.Utilities.SpecializedCollections.Singleton.List<Bucket>(this);
                return new List<Bucket>{this};
            }
        }

        private abstract class ValueOrListBucket : Bucket
        {
            internal readonly int Hash;

            protected ValueOrListBucket(int hash)
            {
                this.Hash = hash;
            }
        }

        private abstract class Bucket
        {
            internal abstract int Count { get; }

            internal abstract Bucket Add(int suggestedHashRoll, ValueBucket bucket, IEqualityComparer<TKey> comparer,
                IEqualityComparer<TValue> valueComparer, bool overwriteExistingValue);

            internal abstract Bucket Remove(int hash, TKey key, IEqualityComparer<TKey> comparer);
            internal abstract ValueBucket Get(int hash, TKey key, IEqualityComparer<TKey> comparer);
            internal abstract IEnumerable<Bucket> GetAll();
        }
    }
}