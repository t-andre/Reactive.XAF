﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Xpo;
using NUnit.Framework;
using Xpand.Extensions.Reactive.Combine;
using Xpand.Extensions.Reactive.Conditional;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.XAF.NonPersistentObjects;
using Xpand.TestsLib.Common;
using Xpand.TestsLib.Common.Attributes;
using Xpand.XAF.Modules.Blazor.Services;
using Xpand.XAF.Modules.JobScheduler.Hangfire.BusinessObjects;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.BO;
using Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.Common;
using Xpand.XAF.Modules.Reactive.Services;

namespace Xpand.XAF.Modules.JobScheduler.Hangfire.Tests.NewDirectory1 {
    public class JobSchedulerTests:JobSchedulerCommonTest {
        
        [TestCase(typeof(TestJob),false)]
        [TestCase(typeof(TestJobDI),true)]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Inject_BlazorApplication_In_JobType_Ctor(Type jobType,bool provider) 
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(jobType,
                    nameof(TestJob.TestJobId), true).IgnoreElements()
                .MergeToUnit(TestJob.Jobs.Take(1).If(_ => provider,
                    job => job.Provider.Observe().WhenNotDefault(),
                    job => job.Provider.Observe().WhenDefault()).ToUnit()).ReplayFirstTake()
        );
        
        [Test()][Apartment(ApartmentState.MTA)]
        [XpandTest()][Order(200)]
        public async Task Inject_PerformContext_In_JobType_Method()
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(typeof(TestJob),
                    nameof(TestJob.TestJobId), true).IgnoreElements()
                .MergeToUnit(TestJob.Jobs.WhenNotDefault(job => job.Context).Take(1).ToUnit()).ReplayFirstTake());


        [Test()]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Commit_Objects_NonSecuredProvider()
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(typeof(TestJobDI),
                    nameof(TestJobDI.CreateObject),true).IgnoreElements()
                .MergeToUnit(application.WhenSetupComplete().SelectMany(_ => application.WhenProviderCommitted<JS>(emitUpdatingObjectSpace:true))
                    .Select(t => t).Take(1).ToUnit()).ReplayFirstTake()
                .ToUnit().Select(unit => unit), startupFactory: context => new TestStartup(context.Configuration,startup => startup.AddObjectSpaceProviders));
        
        
        [Test()]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Commit_Objects_SecuredProvider()
            => await StartJobSchedulerTest(application =>
                TestTracing.Handle<UserFriendlyObjectLayerSecurityException>().Take(1).IgnoreElements()
                    .MergeToUnit(application.AssertTriggerJob(typeof(TestJobDI), nameof(TestJobDI.CreateObject), false).IgnoreElements())
                    .MergeToUnit(application.WhenTabControl(typeof(Job))
                        .Do(model => model.ActiveTabIndex = 1).Take(1).IgnoreElements())
                    .MergeToUnit(application.AssertListViewHasObject<JobWorker>(worker
                        => worker.State == WorkerState.Failed && worker.LastState.Reason.Contains("object is prohibited by security")))
                    .ReplayFirstTake()
        );
        
        [TestCase(nameof(TestJobDI.CreateObjectAnonymous))]
        [TestCase(nameof(TestJobDI.CreateObjectNonSecured))]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Commit_Objects_SecuredProvider_ByPass(string method) 
            => await StartJobSchedulerTest(application =>
                application.AssertTriggerJob(typeof(TestJobDI), method, false).IgnoreElements()
                    .MergeToUnit(application.WhenTabControl(typeof(Job))
                        .Do(model => model.ActiveTabIndex = 1).Take(1).IgnoreElements())
                    .MergeToUnit(application.AssertListViewHasObject<JobWorker>(worker
                        => worker.State == WorkerState.Succeeded))
                    .ReplayFirstTake()
        );

        [Test()] 
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Customize_Job_Schedule()
            => await StartJobSchedulerTest(application => application.AssertJobListViewNavigation()
                .SelectMany(window => window.CreateJob(typeof(TestJobDI), nameof(TestJobDI.TestJobId))).ToUnit()
                .Zip(JobSchedulerService.CustomJobSchedule.Handle().SelectMany(args => args.Instance).Take(1)).ToSecond()
                .ToUnit().ReplayFirstTake());
        
        [Test]
        [XpandTest(state:ApartmentState.MTA)]
        public async Task Schedule_Successful_job() 
            => await StartJobSchedulerTest(application => application.AssertTriggerJob(typeof(TestJobDI), nameof(TestJobDI.TestJobId),true).IgnoreElements()
                .MergeToUnit(WorkerState.Succeeded.Executed().Where(state => state.JobWorker.State==WorkerState.Succeeded)
                    .Where(jobState => jobState.JobWorker.Executions.DistinctBy(state => state.State).Count() == 3 && jobState.JobWorker.Executions.Select(state => state.State)
                        .All(state => new[]{WorkerState.Enqueued,WorkerState.Processing, WorkerState.Succeeded}.Contains(state))).Take(1)
                    .Select(state => state))
                .ReplayFirstTake());
        
        [XpandTest(state:ApartmentState.MTA)]
        [Test]
        public async Task Pause_Job() {
            await StartJobSchedulerTest(application
                => application.WhenMainWindowCreated()
                    .SelectMany(_ => {
                        var objectSpace = application.CreateObjectSpace();
                        var job = objectSpace.CreateObject<Job>();
                        job.Id = nameof(Pause_Job);
                        job.JobType = new ObjectType(typeof(TestJobDI));
                        job.JobMethod = new ObjectString(nameof(TestJobDI.TestJobId));
                        job.IsPaused = true;
                        job.CommitChanges();
                        job.Trigger();
                        return application.Navigate(typeof(Job), ViewType.ListView)
                            .SelectMany(frame => frame.AssertListViewHasObject<Job>()
                                .SelectMany(_ => frame.ListViewProcessSelectedItem()))
                            .Zip(application.WhenTabControl(typeof(Job))).ToSecond().Do(model => model.ActiveTabIndex=1)
                            .Zip(application.AssertListViewHasObject<JobWorker>(worker => worker.State==WorkerState.Skipped))
                            .Assert();
                    }).ToUnit().ReplayFirstTake());
            
        }

    }
    
}