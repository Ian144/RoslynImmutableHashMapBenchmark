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
    //[RyuJitX64Job]
    public class Benchmarks
    {
        private readonly Random _rnd;
        private (string, int n)[] _items;
        private (string, int n)[] _randItems;
        private ImmutableDictionary<string, int> _sciDict;
        private ImmutableHashMap<string, int> _roslynMap;
        private List<KeyValuePair<string, int>> _kvps;

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
            _items = Enumerable.Range(0, 1000000).Select(n => (n.ToString(), n)).ToArray();
            _randItems = _items.OrderBy(_ => _rnd.Next()).ToArray();
            _kvps = _items.Select(t2 => new KeyValuePair<string, int>(t2.Item1, t2.Item2)).ToList();
            _sciDict = ImmutableDictionary.Create<string, int>().AddRange(_kvps);
            _roslynMap = ImmutableHashMap<string, int>.Empty.AddRange(_kvps);
        }

        //[Setup]
        //public void Setup()
        //{
        //    _sciDict = ImmutableDictionary.Create<string, int>().AddRange(_kvps);
        //    _roslynMap = ImmutableHashMap<string, int>.Empty.AddRange(_kvps);
        //}

        //[Benchmark]
        //public void SysColImmTryGetValue()
        //{
        //    foreach (var (key, _) in _randItems)
        //    {
        //        bool ok = _sciDict.TryGetValue(key, out int vvalue);
        //    }
        //}

        //[Benchmark]
        //public void RoslynTryGetValue()
        //{
        //    foreach (var (key, n) in _randItems)
        //    {
        //        bool ok = _roslynMap.TryGetValue(key, out int vvalue);
        //    }
        //}

        [Benchmark]
        public void SysColImmTryGetSet()
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
            foreach (var (key, n) in _randItems)
            {
                bool _ = _roslynMap.TryGetValue(key, out int vvalue);
                _roslynMap = _roslynMap.SetItem(key, ++vvalue);
            }
        }


        //[Benchmark]
        //public void SysColImmSetItem()
        //{
        //    var map = ImmutableDictionary<string, int>.Empty;

        //    foreach (var (key, n) in _items)
        //    {
        //        map = map.SetItem(key, n);
        //    }
        //}

        //[Benchmark]
        //public void RoslynSetItem()
        //{
        //    var map = ImmutableHashMap<string, int>.Empty;

        //    foreach (var (key, n) in _items)
        //    {
        //        map = map.SetItem(key, n);
        //    }
        //}
    }


    internal static class Program
    {
        private static void Main()
        {

            var _ = BenchmarkRunner.Run<Benchmarks>(DefaultConfig.Instance.With(Job.RyuJitX64.WithGcServer(true)));
        }
    }
}
