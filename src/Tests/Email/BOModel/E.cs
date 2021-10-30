﻿using System.ComponentModel;
using DevExpress.Persistent.Base;
using DevExpress.Xpo;
using Xpand.XAF.Persistent.BaseImpl;

namespace Xpand.XAF.Modules.Email.Tests.BOModel{
    [DefaultProperty(nameof(Name))]
    public class E:CustomBaseObject{
        public E(Session session) : base(session){
        }

        string _name;

        public string Name{
            get => _name;
            set => SetPropertyValue(nameof(Name), ref _name, value);
        }
        
        int _order;

        [VisibleInListView(false)]
        public int Order{
            get => _order;
            set => SetPropertyValue(nameof(Order), ref _order, value);
        }
    }
}