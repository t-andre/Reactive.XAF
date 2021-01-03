﻿#if !NETCOREAPP3_1
using System.Linq;
using DevExpress.ExpressApp;
using Fasterflect;
using Shouldly;
using Xpand.TestsLib;
using Xpand.TestsLib.Common;
using Xpand.XAF.Modules.Reactive;
using ALL.Win.Tests;
#endif
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ALL.Tests;
using DevExpress.EasyTest.Framework;
using NUnit.Framework;
using Xpand.Extensions.AppDomainExtensions;
using Xpand.TestsLib.Common.Attributes;
using Xpand.TestsLib.Common.EasyTest;
using Xpand.TestsLib.Common.EasyTest.Commands.ActionCommands;
using Xpand.TestsLib.Common.EasyTest.Commands.Automation;
using Xpand.XAF.Modules.Reactive.Logger;
using CommonTest = ALL.Tests.CommonTest;

namespace Web.Tests{
    
    [NonParallelizable]
    public class AllWebTests : CommonTest{
#if !NETCOREAPP3_1
        [Test()]
        [TestCaseSource(nameof(AgnosticModules))]
        [TestCaseSource(nameof(WebModules))]
        [XpandTest]
        public void UnloadWebModules(Type moduleType){
            ReactiveModuleBase.Unload(moduleType);
            using var application = new TestWebApplication(moduleType, false);
            application.AddModule((ModuleBase) moduleType.CreateInstance(), nameof(UnloadWebModules));

            application.Modules.FirstOrDefault(m => m.GetType()==moduleType).ShouldBeNull();
        }

        [Test]
        [XpandTest(LongTimeout,3)]
        [Apartment(ApartmentState.STA)]
        public async Task Web_EasyTest_InLocalDb(){
            var connectionString = $"Integrated Security=SSPI;Pooling=false;Data Source=(localdb)\\mssqllocaldb;Initial Catalog=TestApplicationWeb{AppDomain.CurrentDomain.UseNetFramework()}";
            await EasyTest(NewWebAdapter, RunWebApplication,  adapter => {
                adapter.TestSequenceGeneratorService();
                return Task.CompletedTask;
            
            },connectionString);
            
        }
#endif
        private TestApplication RunWebApplication(IApplicationAdapter adapter, string connectionString){

#if !NETCOREAPP3_1
            var physicalPath = $@"{AppDomain.CurrentDomain.ApplicationPath()}..\TestWebApplication\";
#else
            var physicalPath = $@"{AppDomain.CurrentDomain.ApplicationPath()}..\..\TestBlazorApplication\";
#endif
            LogPaths.Clear();
            LogPaths.Add(Path.Combine(Path.GetDirectoryName(physicalPath)!,"eXpressAppFramework.log"));
            LogPaths.Add(Path.Combine(@$"{Path.GetDirectoryName(physicalPath)}\bin",Path.GetFileName(ReactiveLoggerService.RXLoggerLogPath)!));
#if !NETCOREAPP3_1
            return ((DevExpress.ExpressApp.EasyTest.WebAdapter.WebAdapter) adapter).RunWebApplication(physicalPath, 65477, connectionString);
#else
            
            return ((DevExpress.ExpressApp.EasyTest.BlazorAdapter.BlazorAdapter) adapter).RunBlazorApplication(physicalPath,44318,connectionString);
#endif
            
        }

        [XpandTest(LongTimeout,3)]
        [Test][Apartment(ApartmentState.STA)]
        public async Task Web_EasyTest_InMemory(){
            await EasyTest(NewWebAdapter, RunWebApplication, async adapter => {
                var autoTestCommand = new AutoTestCommand("Event|Task|Reports");
                adapter.Execute(autoTestCommand);
                await Task.CompletedTask;
#if NETCOREAPP3_1
                await adapter.TestJobScheduler();
#endif

            });
        }

        private static IApplicationAdapter NewWebAdapter(){
#if !NETCOREAPP3_1
            return new DevExpress.ExpressApp.EasyTest.WebAdapter.WebAdapter();
#else
            return new DevExpress.ExpressApp.EasyTest.BlazorAdapter.BlazorAdapter();
#endif
            
        }

#if !NETCOREAPP3_1
        [XpandTest(LongTimeout,3)]
        [Test][Apartment(ApartmentState.STA)]
        public async Task Web_MicrosoftCloud_EasyTest(){
            DeleteBrowserFiles.Execute();
            await EasyTest(NewWebAdapter, RunWebApplication, async adapter => {
                await adapter.TestMicrosoftService(async () => {
                    await adapter.TestMicrosoftCalendarService();
                    await adapter.TestMicrosoftTodoService();
                });
            });
        }
#endif

        [XpandTest(LongTimeout,3)]
        [Test][Apartment(ApartmentState.STA)]
        public async Task Web_GoogleCloud_EasyTest(){
#if !NETCOREAPP3_1
            DeleteBrowserFiles.Execute();
#endif
            await EasyTest(NewWebAdapter, RunWebApplication, async adapter => {
                await adapter.TestGoogleService(async () => {
                    await adapter.TestGoogleCalendarService();
                    await adapter.TestGoogleTasksService();
                });
            });
        }



    }

}