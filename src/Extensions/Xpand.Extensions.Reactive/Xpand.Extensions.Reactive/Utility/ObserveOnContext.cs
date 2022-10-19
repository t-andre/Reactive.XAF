﻿using System;
using System.Reactive.Linq;
using System.Threading;
using Xpand.Extensions.Reactive.Conditional;

namespace Xpand.Extensions.Reactive.Utility {
    public static partial class Utility {
        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source, SynchronizationContext synchronizationContext) 
            => source.ObserveOn(synchronizationContext);

        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source, bool throwIfNull) 
            => source.If(_ => throwIfNull && SynchronizationContext.Current == null,
                () => Observable.Throw<T>(new NullReferenceException(nameof(SynchronizationContext))), () => source);

        public static IObservable<T> ObserveOnContext<T>(this IObservable<T> source) {
            var synchronizationContext = SynchronizationContext.Current;
            return synchronizationContext != null ? source.ObserveOn(synchronizationContext) : source;
        }
    }
}