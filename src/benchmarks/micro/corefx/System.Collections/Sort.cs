﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnostics.Windows.Configs;
using BenchmarkDotNet.Extensions;
using MicroBenchmarks;

namespace System.Collections
{
    // TODO: Where should the below types reside?

    public readonly struct IntStruct : IComparable<IntStruct>
    {
        readonly int _value;

        public IntStruct(int value) => _value = value;

        public int CompareTo(IntStruct other) => _value.CompareTo(other._value);
    }

    public class IntClass : IComparable<IntClass>
    {
        readonly int _value;

        public IntClass(int value) => _value = value;

        public int CompareTo(IntClass other) => _value.CompareTo(other._value);
    }

    [BenchmarkCategory(Categories.CoreCLR, Categories.Collections, Categories.GenericCollections)]
    [GenericTypeArguments(typeof(int))] // value type, Array sort in native code
    [GenericTypeArguments(typeof(IntStruct))] // custom value type, sort in managed code
    [GenericTypeArguments(typeof(IntClass))] // custom reference type, sort in managed code, compare fast
    //[GenericTypeArguments(typeof(string))] // reference type, compare slow
    [InvocationCount(InvocationsPerIteration)]
    [DisassemblyDiagnoser(recursiveDepth: 3)]
    [InliningDiagnoser]
    public class Sort<T>
    {
        private const int InvocationsPerIteration = 40000;

        [Params(Utils.DefaultCollectionSize)]
        public int Size;

        private int _iterationIndex = 0;
        private T[] _values;

        private T[][] _arrays;
        private List<T>[] _lists;

        [GlobalSetup]
        public void Setup() => _values = GenerateValues();

        [IterationCleanup]
        public void CleanupIteration() => _iterationIndex = 0; // after every iteration end we set the index to 0

        [IterationSetup(Target = nameof(Array))]
        public void SetupArrayIteration() => Utils.FillArrays(ref _arrays, InvocationsPerIteration, _values);

        [Benchmark]
        public void Array() => System.Array.Sort(_arrays[_iterationIndex++], 0, Size);

        //[IterationSetup(Target = nameof(List))]
        public void SetupListIteration() => Utils.FillCollections(ref _lists, InvocationsPerIteration, _values);

        //[Benchmark]
        public void List() => _lists[_iterationIndex++].Sort();

        //[BenchmarkCategory(Categories.LINQ)]
        //[Benchmark]
        public int LinqQuery()
        {
            int count = 0;
            foreach (var _ in (from value in _values orderby value ascending select value))
                count++;
            return count;
        }

        //[BenchmarkCategory(Categories.LINQ)]
        //[Benchmark]
        public int LinqOrderByExtension()
        {
            int count = 0;
            foreach (var _ in _values.OrderBy(value => value)) // we can't use .Count here because it has been optimized for icollection.OrderBy().Count()
                count++;
            return count;
        }

        T[] GenerateValues()
        {
            if (typeof(T) == typeof(IntStruct))
            {
                var values = ValuesGenerator.ArrayOfUniqueValues<int>(Size);
                return (T[])(object)values.Select(v => new IntStruct(v)).ToArray();
            }
            else if (typeof(T) == typeof(IntClass))
            {
                var values = ValuesGenerator.ArrayOfUniqueValues<int>(Size);
                return (T[])(object)values.Select(v => new IntClass(v)).ToArray();
            }
            else
            {
                return ValuesGenerator.ArrayOfUniqueValues<T>(Size);
            }
        }
    }
}