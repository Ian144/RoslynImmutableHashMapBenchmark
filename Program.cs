using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
// ReSharper disable ArrangeTypeMemberModifiers

// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable UnusedVariable

namespace RoslynImmutableHashMapConsoleApp
{
    [MemoryDiagnoser]
    [RyuJitX64Job]
    public class Benchmarks
    {
        private readonly (string, int n)[] _items = Enumerable.Range(0, 1000).Select(n => (n.ToString(), n)).ToArray();
        private readonly ImmutableDictionary<string, int> _sciDict;
        private readonly ImmutableHashMap<string, int> _roslynMap;

        public Benchmarks()
        {
            var kvps = _items.Select(t2 => new KeyValuePair<string, int>(t2.Item1, t2.Item2)).ToList();
            _sciDict = System.Collections.Immutable.ImmutableDictionary.Create<string, int>().AddRange(kvps);
            _roslynMap = ImmutableHashMap<string,int>.Empty.AddRange(kvps);
        }

        [Benchmark]
        public void SysColImmReadItems()
        {
            foreach (var (key, _) in _items)
            {
                int vvalue = _sciDict[key];
            }
        }

        [Benchmark]
        public void RoslynReadItems()
        {
            foreach (var (key, n) in _items)
            {
                int vvalue = _roslynMap[key];
            }
        }




        [Benchmark]
        public void SysColImmAddItems()
        {
            var map = ImmutableDictionary<string, int>.Empty;

            foreach (var (key, n) in _items)
            {
                map = map.Add(key, n);
            }
        }

        [Benchmark]
        public void SysColImmAddItemsBuilder()
        {
            var bldr = ImmutableDictionary.CreateBuilder<string, int>();

            foreach (var (key, n) in _items)
            {
                bldr.Add(key, n);
            }

            var map = bldr.ToImmutable();
        }



        [Benchmark]
        public void RoslynAddItems()
        {
            var map = ImmutableHashMap<string, int>.Empty;

            foreach (var (key, n) in _items)
            {
                map = map.Add(key, n);
            }
        }

        //[Benchmark]
        //public void SysColImmGetOrAddItems()
        //{
        //    var map = ImmutableDictionary<string, int>.Empty;

        //    foreach (var (key, n) in _items)
        //    {
        //        map = map.GetAdd(key, n);
        //    }
        //}


        //[Benchmark]
        //public void RoslynGetOrAddItems()
        //{
        //    var map = ImmutableHashMap<string, int>.Empty;

        //    foreach (var (key, n) in _items)
        //    {
        //        map = ImmutableHashMapExtensions.GetOrAdd(map, key, n);
        //    }
        //}

    }


    internal static class Program
    {
        private static void Main()
        {
            
            var summary = BenchmarkRunner.Run<Benchmarks>();
        }
    }
}
