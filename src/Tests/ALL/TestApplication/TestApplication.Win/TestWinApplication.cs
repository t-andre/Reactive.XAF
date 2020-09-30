﻿using System;
using System.Configuration;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Win;
using DevExpress.ExpressApp.Win.Utils;
using DevExpress.ExpressApp.Xpo;
using Fasterflect;
using Xpand.XAF.Modules.Reactive.Services;

namespace TestApplication.Win{
    public class TestWinApplication:WinApplication{
        public TestWinApplication(){
            DevExpress.ExpressApp.Utils.ImageLoader.Instance.UseSvgImages = true;
            UseOldTemplates = false;
            LinkNewObjectToParentImmediately = false;
            OptimizedControllersCreation = true;
            UseLightStyle = true;
            ExecuteStartupLogicBeforeClosingLogonWindow = true;

            Modules.Add(new WinModule());
            var module = Modules.FindModule(typeof(ModuleBase));
            module.SetFieldValue("name", "Base");
            CheckCompatibilityType=CheckCompatibilityType.DatabaseSchema;
            this.AlwaysUpdateOnDatabaseVersionMismatch().Subscribe();    
        }

        protected override string GetModulesVersionInfoFilePath(){
            return null;
        }

        protected override void CreateDefaultObjectSpaceProvider(CreateCustomObjectSpaceProviderEventArgs args){
            args.ObjectSpaceProvider=new XPObjectSpaceProvider(new ConnectionStringDataStoreProvider(args.ConnectionString),true);
            args.ObjectSpaceProviders.Add(new NonPersistentObjectSpaceProvider(TypesInfo, null));
            // args.ObjectSpaceProvider = new XPObjectSpaceProvider(new ConnectionStringDataStoreProvider(ConfigurationManager.ConnectionStrings["ConnectionString"].ConnectionString), true);
        }
    }
}