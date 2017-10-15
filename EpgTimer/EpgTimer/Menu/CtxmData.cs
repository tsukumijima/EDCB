using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EpgTimer
{
    public enum CtxmCode
    {
        ReserveView,
        TunerReserveView,
        RecInfoView,
        EpgAutoAddView,
        ManualAutoAddView,
        EpgView,
        SearchWindow,
        InfoSearchWindow,
        EditChgMenu,    //編集サブメニュー編集用
        EtcWindow       //ショートカットメニューがないダイアログなど用のダミー
    }

    public class CtxmData : IDeepCloneObj
    {
        public CtxmCode ctxmCode { set; get; }
        public List<CtxmItemData> Items { set; get; }
        public object DeepCloneObj() { return new CtxmData(ctxmCode, Items); }

        public CtxmData(CtxmCode code, List<CtxmItemData> items = null)
        {
            ctxmCode = code;
            Items = items.DeepClone() ?? new List<CtxmItemData>();
        }
    }

    public class CtxmItemData : IDeepCloneObj
    {
        public string Header { set; get; }
        public int ID { set; get; }
        public ICommand Command { set; get; }
        public List<CtxmItemData> Items { set; get; }
        public object DeepCloneObj() { return new CtxmItemData(this); }

        public CtxmItemData(string header, ICommand icmd, int id = 0, List<CtxmItemData> items = null)
        {
            Header = header;
            ID = id;
            Command = icmd;
            Items = items.DeepClone() ?? new List<CtxmItemData>();
        }
        public CtxmItemData(CtxmItemData data) : this(data.Header, data.Command, data.ID, data.Items) { }
        //デフォルト定義用、ヘッダ以外コピー
        public CtxmItemData(string header, CtxmItemData refdata) : this(refdata) { Header = header; }
    }
}
