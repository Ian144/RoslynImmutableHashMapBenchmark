using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

// ReSharper disable ArrangeTypeMemberModifiers
// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable UnusedVariable

namespace RoslynImmutableHashMapConsoleApp
{
    [MemoryDiagnoser]
    public class Benchmarks
    {
        private readonly Random _rnd;
        private (string, int n)[] _items;
        private (string, int n)[] _randItems;
        private ImmutableDictionary<string, int> _sciDict;
        private ImmutableSortedDictionary<string, int> _sciSortedDict;
        private ImmutableHashMap<string, int> _roslynMap;
        private Dictionary<string, int> _dict;
        private List<KeyValuePair<string, int>> _kvps;

        //private readonly int _addCount = 10000;


        public Benchmarks()
        {
            _rnd = new Random();
            //_items = Enumerable.Range(0, 10000).Select(n => (n.ToString(), n)).ToArray();
            //_randItems = _items.OrderBy(_ => _rnd.Next()).ToArray();
            //_kvps = _items.Select(t2 => new KeyValuePair<string, int>(t2.Item1, t2.Item2)).ToList();
            //_sciDict = ImmutableDictionary.Create<string, int>().AddRange(_kvps);
            //_roslynMap = ImmutableHashMap<string, int>.Empty.AddRange(_kvps);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            _items = Enumerable.Range(0, 1000).Select(n => (n.ToString(), n)).ToArray();
            _randItems = _items.OrderBy(_ => _rnd.Next()).ToArray();
            _kvps = _items.Select(t2 => new KeyValuePair<string, int>(t2.Item1, t2.Item2)).ToList();
            _sciDict = ImmutableDictionary.Create<string, int>().AddRange(_kvps);
            _sciSortedDict = ImmutableSortedDictionary.Create<string, int>().AddRange(_kvps);
            _roslynMap = ImmutableHashMap<string, int>.Empty.AddRange(_kvps);
            _dict = new Dictionary<string, int>();
            foreach (var kvp in _kvps)
            {
                _dict.Add(kvp.Key, kvp.Value);
            }
        }


        //[GlobalSetup]
        //[Setup]
        //public void Setup()
        //{
        //    _sciDict = ImmutableDictionary.Create<string, int>().AddRange(_kvps);
        //    _roslynMap = ImmutableHashMap<string, int>.Empty.AddRange(_kvps);
        //}

        [Benchmark]
        public void SortedImmTryGetSet()
        {
            foreach (var (key, _) in _randItems)
            {
                bool _ = _sciSortedDict.TryGetValue(key, out int vvalue);
                _sciSortedDict = _sciSortedDict.SetItem(key, ++vvalue);
            }
        }

        [Benchmark]
        public void ImmTryGetSet()
        {
            foreach (var (key, _) in _randItems)
            {
                bool _ = _sciDict.TryGetValue(key, out int vvalue);
                _sciDict = _sciDict.SetItem(key, ++vvalue);
            }
        }

        [Benchmark]
        public void RoslynTryGetSet()
        {
            foreach (var (key, _) in _randItems)
            {
                bool exists = _roslynMap.TryGetValue(key, out int vvalue);
                _roslynMap = _roslynMap.SetItem(key, ++vvalue);
            }
        }

        //[Benchmark]
        //public void DictTryGetSetToImmDict()
        //{
        //    foreach (var (key, _) in _randItems)
        //    {
        //        bool exists = _dict.TryGetValue(key, out int vvalue);
        //        if(exists)
        //            _dict[key] = ++vvalue;
        //    }

        //    var immx = _dict.ToImmutableDictionary();
        //}

        //[Benchmark]
        //public void AddNDict()
        //{
        //    var map = new Dictionary<int,int>();
        //    for (int ctr = 0; ctr < _addCount; ++ctr)
        //    {
        //        map.Add(ctr, ctr);
        //    }
        //}

        //[Benchmark]
        //public void AddNSortedDict()
        //{
        //    var map = new SortedDictionary<int, int>();
        //    for (int ctr = 0; ctr < _addCount; ++ctr)
        //    {
        //        map.Add(ctr, ctr);
        //    }
        //}

        //[Benchmark]
        //public void AddNImmutableDict()
        //{
        //    var map = ImmutableDictionary.Create<int, int>();
        //    for (int ctr = 0; ctr < _addCount; ++ctr)
        //    {
        //        map = map.Add(ctr, ctr);
        //    }
        //}

        //[Benchmark]
        //public void AddNImmutableSortedDict()
        //{
        //    var map = ImmutableSortedDictionary.Create<int, int>();
        //    for (int ctr = 0; ctr < _addCount; ++ctr)
        //    {
        //        map = map.Add(ctr, ctr);
        //    }
        //}

        //[Benchmark]
        //public void AddNRoslynImmutableHashMap()
        //{
        //    var map = ImmutableHashMap<int, int>.Empty;
        //    for (int ctr = 0; ctr < _addCount; ++ctr)
        //    {
        //        map = map.Add(ctr, ctr);
        //    }
        //}
    }

    /*
     * ihm and imh3 display correctly
     *
     * displaying imh2 contents gives a NotImplementedException visible in the debugger
     * i suspect this is a debugger bug
     * try with updated VS
     *
     */
     
    internal static class Program
    {
        private static void Main()
        {
            //var ihm = ImmutableHashMap<string, int>.Empty;
            //var ihm2 = ihm.Add("one", 1);
            //var ihm3 = ihm2.Add("two", 2);
            //Console.WriteLine(ihm3);

            //            var _ = BenchmarkRunner.Run<Benchmarks>(DefaultConfig.Instance.With(Job.RyuJitX64.WithGcServer(true)));
            var _ = BenchmarkRunner.Run<Benchmarks>(DefaultConfig.Instance.With(Job.RyuJitX64));
        }
    }
}
