﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using DevExpress.ExpressApp;
using Grpc.Core;
using MagicOnion.Client;
using MagicOnion.Server;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Transform.System.Net;
using Xpand.Extensions.Reactive.Utility;
using Xpand.XAF.Modules.Reactive.Services;
using ListView = DevExpress.ExpressApp.ListView;

namespace Xpand.XAF.Modules.Reactive.Logger.Hub{
    public static class ReactiveLoggerHubService{
        static readonly TraceEventReceiver Receiver = new();
        private static Server _server;


        internal static IObservable<Unit> Connect(this ApplicationModulesManager manager) 
	        => manager.WhenApplication(application => {
		        if (!(application is ILoggerHubClientApplication)){
			        TraceEventHub.Init();
		        }
		        else {
			        Observable.FromEventPattern<EventHandler, EventArgs>(h => GrpcEnvironment.ShuttingDown += h,
					        h => GrpcEnvironment.ShuttingDown -= h)
				        .Select(pattern => pattern).IgnoreElements().ToUnit().Subscribe();
		        }
		        var startServer = application.StartServer().Publish().RefCount();
		        var client = Observable.Start(application.ConnectClient).Merge().Publish().RefCount();
		        application.CleanUpHubResources( startServer);

                var saveServerTraceMessages = application.SaveServerTraceMessages().Publish().RefCount();
		        return startServer.ToUnit()
			        .Merge(client.ToUnit())
			        .Merge(saveServerTraceMessages.ToUnit())
			        .Merge(application.WhenViewOnFrame(typeof(TraceEvent))
				        .SelectMany(frame => saveServerTraceMessages.LoadTracesToListView(frame)));
	        });

        public static void CleanUpHubResources(this XafApplication application, IObservable<Server> startServer) 
            => application.WhenDisposed().Zip(startServer, (_, server) => server.ShutDownServer())
                .Concat()
                .FirstOrDefaultAsync()
                .Subscribe();

        private static IObservable<Unit> ShutDownServer(this Server server) 
	        => server.ShutdownAsync().ToObservable().TakeUntil(Observable.Timer(TimeSpan.FromSeconds(5)));

        private static IObservable<Unit> LoadTracesToListView(this IObservable<TraceEvent[]> source,Frame frame) 
	        => source.ObserveOn(SynchronizationContext.Current)
		        .Select(events => {
			        if (events.Any()){
				        ((ListView)frame?.View)?.RefreshDataSource();
			        }
			        return events;
		        }).ToUnit();

        private static IObservable<TraceEvent[]> SaveServerTraceMessages(this XafApplication application) 
            => application.BufferUntilCompatibilityChecked(TraceEventReceiver.TraceEvent)
		        .Buffer(TimeSpan.FromSeconds(2)).WhenNotEmpty()
		        .TakeUntil(application.WhenDisposed())
		        .Select(list => application.ObjectSpaceProvider.NewObjectSpace(space => {
			        var criteriaOperator = space.ParseCriteria(application.Model.ToReactiveModule<IModelReactiveModuleLogger>()
				        .ReactiveLogger.TraceSources.PersistStrategyCriteria);
			        return space.SaveTraceEvent(list, criteriaOperator);
		        }).ToEnumerable().ToArray());

        internal static IObservable<TSource> TraceRXLoggerHub<TSource>(this IObservable<TSource> source, Func<TSource,string> messageFactory=null,string name = null, Action<string> traceAction = null,
	        Func<Exception,string> errorMessageFactory=null, ObservableTraceStrategy traceStrategy = ObservableTraceStrategy.OnNextOrOnError,
	        [CallerMemberName] string memberName = "",[CallerFilePath] string sourceFilePath = "",[CallerLineNumber] int sourceLineNumber = 0) 
	        => source.Trace(name, ReactiveLoggerHubModule.TraceSource,messageFactory,errorMessageFactory, traceAction, traceStrategy, memberName,sourceFilePath,sourceLineNumber);


        private static IObservable<Server> StartServer(this  XafApplication application) 
	        => application is ILoggerHubClientApplication ? Observable.Empty<Server>() : application.ServerPortsList().FirstAsync()
			        .Select(modelServerPort => modelServerPort.ToServerPort().StartServer())
			        .TraceRXLoggerHub(server => string.Join(", ",server.Ports.Select(port => $"{port.Host}, {port.Port}")));

        private static IObservable<ITraceEventHub> ConnectClient(this XafApplication application) 
	        => !(application is ILoggerHubClientApplication)? Observable.Empty<ITraceEventHub>()
		        : application.WhenCompatibilityChecked().FirstAsync()
			        .SelectMany(_ => application.DetectServer().Select(point => point)
				        .ConnectClient())
			        ;

        public static IObservable<IPEndPoint> DetectServer(this XafApplication application)
	        => application.ClientPortsList().Listening()
		        .TraceRXLoggerHub(point => $"{point.Address}, {point.Port}");

        public static IObservable<ITraceEventHub> ConnectClient(this IObservable<IPEndPoint> source) 
	        => source.SelectMany(point => {
			        var newClient = point.ToServerPort().NewClient(Receiver);
			        return newClient.ConnectAsync().ToObservable()
				        .Merge(Unit.Default.ReturnObservable()).To(newClient)
				        .Select(hub => hub);
		        })
		        .TraceRXLoggerHub()
		        .Retry();

        public static IEnumerable<IPEndPoint> ClientPortsList(this XafApplication application) 
	        => application.ModelLoggerPorts().SelectMany(ports => ports.LoggerPorts).OfType<IModelLoggerClientRange>()
		        .TraceRXLoggerHub(range => $"{range.Host}, {range.StartPort}, {range.EndPort}")
		        .SelectMany(range => Enumerable.Range(range.StartPort, range.EndPort-range.StartPort)
			        .Select(port => IpEndPoint(range.Host, port))).Merge()
		        .ToEnumerable();

        private static IObservable<IPEndPoint> IpEndPoint(string host, int port) 
	        => Regex.IsMatch(host, @"\A\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b\z") ? new IPEndPoint(IPAddress.Parse(host), port).ReturnObservable()
		        : Dns.GetHostAddressesAsync(host).ToObservable()
			        .Select(addresses => new IPEndPoint(addresses.Last(), port));

        public static IObservable<IPEndPoint> ServerPortsList(this XafApplication application) 
	        => application.ModelLoggerPorts()
		        .SelectMany(ports => ports.LoggerPorts.OfType<IModelLoggerServerPort>()
			        .ToObservable().SelectMany(_ => IpEndPoint(_.Host,_.Port)));

        private static IObservable<IModelReactiveLoggerHub> ModelLoggerPorts(this XafApplication application) 
	        => application.ToReactiveModule<IModelReactiveModuleLogger>().Select(logger => logger.ReactiveLogger).Cast<IModelReactiveLoggerHub>()
		        .Where(ports => ports.LoggerPorts.Enabled)
		        .Select(logger => logger).Cast<IModelReactiveLoggerHub>();

        public static Server StartServer(this ServerPort serverPort){
	        var options = new MagicOnionOptions{IsReturnExceptionStackTraceInErrorDetail = true};
            var service = MagicOnionEngine.BuildServerServiceDefinition(new[]{typeof(ReactiveLoggerHubService).GetTypeInfo().Assembly},options);
            _server = new Server{
	            Services = {service.ServerServiceDefinition},
	            Ports = {serverPort}
            };
            _server.Start();
            return _server;
        }

        private static ServerPort ToServerPort(this IPEndPoint endPoint) => new(endPoint.Address.ToString(), endPoint.Port, ServerCredentials.Insecure);

        public static ITraceEventHub NewClient(this ServerPort serverPort,TraceEventReceiver receiver) 
	        => StreamingHubClient.Connect<ITraceEventHub, ITraceEventHubReceiver>(new Channel(serverPort.Host, serverPort.Port, ChannelCredentials.Insecure),receiver);
    }
}
