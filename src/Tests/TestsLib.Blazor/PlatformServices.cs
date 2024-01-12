﻿using System;
using DevExpress.ExpressApp;
using Xpand.TestsLib.Common;

namespace Xpand.TestsLib.Blazor {
    public class ObjectSelector<T> : IObjectSelector<T> where T : class{
        public IObservable<T> SelectObject(ListView view, params T[] objects) 
            => view.SelectObject(objects);
    }

}