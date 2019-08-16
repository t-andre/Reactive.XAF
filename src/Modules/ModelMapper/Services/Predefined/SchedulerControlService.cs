﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reflection;
using Xpand.XAF.Modules.ModelMapper.Configuration;
using Xpand.XAF.Modules.ModelMapper.Services.TypeMapping;
using Xpand.XAF.Modules.Reactive.Extensions;

namespace Xpand.XAF.Modules.ModelMapper.Services.Predefined{
    public class SchedulerControlService{
        public const string PopupMenusMoelPropertyName = "PopupMenus";

        internal static IObservable<Unit> Connect(Type typeToMap, Assembly schedulerCoreAssembly){
            var storageData = new[] {
                (property: "Labels", typeName: "AppointmentLabel", assembly: typeToMap.Assembly),
                (property: "Mappings", typeName: "ResourceMappingInfo", assembly: schedulerCoreAssembly),
                (property: PopupMenusMoelPropertyName, typeName: "SchedulerPopupMenu", assembly: typeToMap.Assembly)
            };
            var types = storageData
                .Select(_ => (_.property,listType:typeof(IList<>).MakeGenericType(_.assembly.GetType($"DevExpress.XtraScheduler.{_.typeName}"))))
                .ToArray();
            types.Select(_ => _.listType).ToObservable(Scheduler.Immediate)
                .Do(type => TypeMappingService.AdditionalTypesList.Add(type))
                .Subscribe();
            
            TypeMappingService.PropertyMappingRules.Insert(0,(PredefinedMap.SchedulerControl.ToString(),data => SchedulerStorage(data,typeToMap, types)));
            return Unit.Default.AsObservable();
        }


        private static void SchedulerStorage((Type declaringType, List<ModelMapperPropertyInfo> propertyInfos) data,Type typeToMap, (string property, Type listType)[] propertyData){
            if (data.declaringType == typeToMap){
                var propertyInfo = data.propertyInfos.First(info => info.Name=="Storage");
                propertyInfo.RemoveAttribute(typeof(BrowsableAttribute));
                propertyInfo.RemoveAttribute(typeof(DesignerSerializationVisibilityAttribute));
                var last = propertyData.Last();
                data.propertyInfos.Add(new ModelMapperPropertyInfo(last.property,last.listType,propertyInfo.DeclaringType));
            }
            else if (data.declaringType.FullName == "DevExpress.XtraScheduler.AppointmentStorage"){
                foreach (var pData in propertyData.SkipLast(1)){
//                foreach (var pData in propertyData){
                    var propertyInfo = data.propertyInfos.First(info => info.Name==pData.property);
                    data.propertyInfos.Remove(propertyInfo);
                    var modelMapperPropertyInfo = new ModelMapperPropertyInfo(pData.property,pData.listType,propertyInfo.DeclaringType);
                    data.propertyInfos.Add(modelMapperPropertyInfo);    
                }
            }

        }
    }
}