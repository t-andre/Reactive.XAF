﻿using System.Data;
using System.Diagnostics;
using System.Linq;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Xpo;
using Fasterflect;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.XAF.Xpo.Xpo;

namespace Xpand.Extensions.XAF.Xpo.ObjectSpaceExtensions{
    public static partial class ObjectSpaceExtensions{
        [DebuggerStepThrough]
        public static IXpoDataStoreProvider DataStoreProvider(this IObjectSpaceProvider objectSpaceProvider) 
            => (IXpoDataStoreProvider) objectSpaceProvider.GetPropertyValue("DataStoreProvider");
            
        [DebuggerStepThrough]
        public static UnitOfWork UnitOfWork(this IObjectSpace objectSpace) 
            => (UnitOfWork)(objectSpace is XPObjectSpace xpObjectSpace ? xpObjectSpace.Session
                : (UnitOfWork)objectSpace.To<CompositeObjectSpace>().AdditionalObjectSpaces.OfType<XPObjectSpace>().First().Session);

        [DebuggerStepThrough]
        public static IDbConnection Connection(this IObjectSpace objectSpace) => objectSpace.UnitOfWork().Connection();
    }
}