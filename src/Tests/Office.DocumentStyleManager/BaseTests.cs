﻿using DevExpress.Data.Filtering;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Office.Win;
using DevExpress.XtraRichEdit;
using DevExpress.XtraRichEdit.API.Native;
using JetBrains.Annotations;
using NUnit.Framework;
using Xpand.Extensions.XAF.XafApplicationExtensions;
using Xpand.TestsLib;
using Xpand.XAF.Modules.Office.DocumentStyleManager.Extensions;
using Xpand.XAF.Modules.Reactive;

namespace Xpand.XAF.Modules.Office.DocumentStyleManager.Tests{
    public abstract class BaseTests:BaseTest{
        [UsedImplicitly] protected const string NotImplmemented = "not implemented";
        protected RichEditDocumentServer RichEditDocumentServer;
        protected Document Document;

        [SetUp]
        public override void Setup(){
            base.Setup();
	        RichEditDocumentServer = new RichEditDocumentServer();
	        RichEditDocumentServer.CreateNewDocument();
	        Document = RichEditDocumentServer.Document;
        }
        protected DocumentStyleManagerModule DocumentStyleManagerModule(params ModuleBase[] modules){
            var application = Platform.Win.NewApplication<DocumentStyleManagerModule>();
            application.Modules.Add(new OfficeWindowsFormsModule());
            var documentStyleManagerModule = application.AddModule<DocumentStyleManagerModule>(typeof(DataObject),typeof(DataObjectParent));
            var xafApplication = documentStyleManagerModule.Application;
            xafApplication.Modules.AddRange(modules);
            
            var modelOffieModule = xafApplication.Model.ToReactiveModule<IModelReactiveModuleOffice>().Office.DocumentStyleManager();
            modelOffieModule.DefaultPropertiesProvider = xafApplication.Model.BOModel.GetClass(typeof(DataObject));
            using var objectSpace = xafApplication.CreateObjectSpace();
            var dataObject = objectSpace.CreateObject<DataObject>();
            dataObject.Content = Document.ToByteArray(DocumentFormat.OpenXml);
            objectSpace.CommitChanges();
            modelOffieModule.DefaultPropertiesProviderCriteria =
                CriteriaOperator.Parse("Oid=?", dataObject.Oid).ToString();
            
            xafApplication.Logon();
            xafApplication.CreateObjectSpace();
            return documentStyleManagerModule;
        }

        public override void Dispose(){
            base.Dispose();
            RichEditDocumentServer.Dispose();
        }

    }
}