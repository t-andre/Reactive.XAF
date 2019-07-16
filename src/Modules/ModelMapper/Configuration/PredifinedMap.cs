﻿using System;
using Xpand.Source.Extensions.XAF.XafApplication;

namespace Xpand.XAF.Modules.ModelMapper.Configuration{
    [AttributeUsage(AttributeTargets.Field)]
    public class MapPlatformAttribute:Attribute{
        internal MapPlatformAttribute(Platform platform){
            Platform = platform.ToString();
        }

        public string Platform{ get; }
    }

    public enum PredifinedMap{
        None,

        [MapPlatform(Platform.Win)]
        GridView,
        [MapPlatform(Platform.Win)]
        GridColumn,
        [MapPlatform(Platform.Win)]
        TreeList,
        [MapPlatform(Platform.Win)]
        TreeListColumn,
        [MapPlatform(Platform.Win)]
        XafLayoutControl,
        [MapPlatform(Platform.Win)]
        SplitContainerControl,
        [MapPlatform(Platform.Win)]
        DashboardDesigner,
        [MapPlatform(Platform.Win)]
        DashboardViewer,
        [MapPlatform(Platform.Win)]
        RepositoryItem,
        [MapPlatform(Platform.Win)]
        RepositoryItemTextEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemButtonEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemComboBox,
        [MapPlatform(Platform.Win)]
        RepositoryItemDateEdit,
        [MapPlatform(Platform.Win)]
        RepositoryFieldPicker,
        [MapPlatform(Platform.Win)]
        RepositoryItemPopupExpressionEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemPopupCriteriaEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemImageComboBox,
        [MapPlatform(Platform.Win)]
        RepositoryItemBaseSpinEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemSpinEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemObjectEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemMemoEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemLookupEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemProtectedContentTextEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemBlobBaseEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemRtfEditEx,
        [MapPlatform(Platform.Win)]
        RepositoryItemHyperLinkEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemPictureEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemCalcEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemCheckedComboBoxEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemColorEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemFontEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemLookUpEditBase,
        [MapPlatform(Platform.Win)]
        RepositoryItemMemoExEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemMRUEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemBaseProgressBar,
        [MapPlatform(Platform.Win)]
        RepositoryItemMarqueeProgressBar,
        [MapPlatform(Platform.Win)]
        RepositoryItemProgressBar,
        [MapPlatform(Platform.Win)]
        RepositoryItemRadioGroup,
        [MapPlatform(Platform.Win)]
        RepositoryItemTrackBar,
        [MapPlatform(Platform.Win)]
        RepositoryItemRangeTrackBar,
        [MapPlatform(Platform.Win)]
        RepositoryItemTimeEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemZoomTrackBar,
        [MapPlatform(Platform.Win)]
        RepositoryItemImageEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemPopupContainerEdit,
        [MapPlatform(Platform.Win)]
        RepositoryItemPopupBase,
        [MapPlatform(Platform.Win)]
        RepositoryItemPopupBaseAutoSearchEdit,
        [MapPlatform(Platform.Win)]
        SchedulerControl,
        [MapPlatform(Platform.Win)]
        AdvBandedGridView,
        [MapPlatform(Platform.Win)]
        BandedGridColumn,
        [MapPlatform(Platform.Win)]
        LayoutView,
        [MapPlatform(Platform.Win)]
        LayoutViewColumn,
        [MapPlatform(Platform.Win)]
        PivotGridControl,
        [MapPlatform(Platform.Win)]
        PivotGridField,
        [MapPlatform(Platform.Win)]
        ChartControl,
        [MapPlatform(Platform.Win)]
        ChartControlDiagram,
        [MapPlatform(Platform.Win)]
        ChartControlDiagram3D,
        [MapPlatform(Platform.Win)]
        ChartControlFunnelDiagram3D,
        [MapPlatform(Platform.Win)]
        ChartControlXYDiagram3D,
        [MapPlatform(Platform.Win)]
        ChartControlRadarDiagram,
        [MapPlatform(Platform.Win)]
        ChartControlSimpleDiagram3D,
        [MapPlatform(Platform.Win)]
        ChartControlPolarDiagram,
        [MapPlatform(Platform.Win)]
        ChartControlXYDiagram2D,
        [MapPlatform(Platform.Win)]
        ChartControlSwiftPlotDiagram,
        [MapPlatform(Platform.Win)]
        ChartControlXYDiagram,
        [MapPlatform(Platform.Win)]
        ChartControlGanttDiagram,
        [MapPlatform(Platform.Web)]
        ASPxGridView,
        [MapPlatform(Platform.Web)]
        GridViewColumn,
        [MapPlatform(Platform.Web)]
        ASPxHtmlEditor,
        [MapPlatform(Platform.Web)]
        ASPxScheduler,
        [MapPlatform(Platform.Web)]
        ASPxUploadControl,
        [MapPlatform(Platform.Web)]
        ASPxPopupControl,
        [MapPlatform(Platform.Web)]
        ASPxDateEdit,
        [MapPlatform(Platform.Web)]
        ASPxHyperLink,
    }
}