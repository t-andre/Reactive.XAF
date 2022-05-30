﻿using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xpand.Extensions.Reactive.Transform;

namespace Xpand.Extensions.Reactive.Combine{
    public static partial class Combine{
        public static IObservable<TC> MergeOrCombineLatest<TA, TB, TC>(this IObservable<TA> a, IObservable<TB> b, Func<TA, TC> aStartsFirst, Func<TB, TC> bStartFirst, Func<TA, TB, TC> bothStart)
            => a.Publish(aa => b.Publish(bb => aa.CombineLatest(bb, bothStart)
                    .Publish(xs => aa.Select(aStartsFirst).Merge(bb.Select(bStartFirst)).TakeUntil(xs).SkipLast(1).Merge(xs))));
        public static IObservable<T> MergeOrdered<T>(this IObservable<IObservable<T>> source, int maximumConcurrency = Int32.MaxValue) 
            => Observable.Defer(() => {
                var semaphore = new SemaphoreSlim(maximumConcurrency);
                return source.Select(inner => {
                        var published = inner.Replay();
                        _ = semaphore.WaitAsync().ContinueWith(_ => published.Connect(), TaskScheduler.Default);
                        return published.Finally(() => semaphore.Release());
                    })
                    .Concat();
            });
        
        public static IObservable<TValue> MergeWith<TSource, TValue>(this IObservable<TSource> source, TValue value, IScheduler scheduler = null) 
            => source.Merge(default(TSource).ReturnObservable(scheduler ?? CurrentThreadScheduler.Instance)).Select(_ => value);

        public static IObservable<Unit> MergeWith<TSource, TValue>(this IObservable<TSource> source, IObservable<TValue> value, IScheduler scheduler = null) 
            => source.ToUnit().Merge(value.ToUnit());
    }
}