﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System;
using System.Collections.Generic;

namespace Roslyn.Utilities
{
    internal partial class SpecializedCollections
    {
        private static partial class ReadOnly
        {
            //internal class Collection<TUnderlying, T> : Enumerable<TUnderlying, T>, ICollection<T>
            //    where TUnderlying : ICollection<T>
            //{
            //    public Collection(TUnderlying underlying)
            //        : base(underlying)
            //    {
            //    }

            //    public void Add(T item)
            //    {
            //        throw new NotSupportedException();
            //    }

            //    public void Clear()
            //    {
            //        throw new NotSupportedException();
            //    }

            //    public bool Contains(T item)
            //    {
            //        return this.Underlying.Contains(item);
            //    }

            //    public void CopyTo(T[] array, int arrayIndex)
            //    {
            //        this.Underlying.CopyTo(array, arrayIndex);
            //    }

            //    public int Count => this.Underlying.Count;

            //    public bool IsReadOnly => true;

            //    public bool Remove(T item) => throw new NotSupportedException();
            //}
        }
    }
}
