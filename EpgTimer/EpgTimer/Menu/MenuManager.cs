using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Reflection;

namespace EpgTimer
{
    public class MenuManager
    {
        private Dictionary<CtxmCode, CtxmData> DefCtxmData;//デフォルトのコンテキストメニュー
        private Dictionary<CtxmCode, List<ICommand>> DefCtxmCmdList;//デフォルトのコンテキストメニューのコマンドリスト
        private CtxmData AddInfoSearchMenu;//簡易検索用の追加メニュー

        private Dictionary<CtxmCode, CtxmData> WorkCtxmData;//現在のコンテキストメニュー
//        private Dictionary<CtxmCode, List<ICommand>> WorCtxmCmdList;//各ビューのコンテキストメニューのコマンドリスト
        private Dictionary<CtxmCode, List<ICommand>> WorkGestureCmdList;//各ビューのショートカット管理用のコマンドリスト
        private Dictionary<CtxmCode, List<ICommand>> WorkDelGesCmdList;//メニューから削除され、shortcut無効なコマンド

        public MenuCmds MC { get; private set; } //コマンドオプション関係

        public MenuManager()
        {
            MC = new MenuCmds();
            SetDefCtxmData();
            SetDefGestureCmdList();
        }
        //コンテキストメニューの定義
        private void SetDefCtxmData()
        {
            //各画面のコンテキストメニュー。空にならないようとりあえず全部作っておく。
            DefCtxmData = new Dictionary<CtxmCode, CtxmData>();
            foreach (CtxmCode code in Enum.GetValues(typeof(CtxmCode)))
            {
                DefCtxmData.Add(code, new CtxmData(code));
            }

            //サブメニューなど設定のあるものは、情報固定のためいったん定義する。
            var cm_Separator = new CtxmItemData(EpgCmdsEx.SeparatorString, EpgCmdsEx.Separator);

            //予約追加サブメニュー 実行時、セパレータ以降にプリセットを展開する。
            var cm_AddMenu = new CtxmItemData("予約追加(仮)", EpgCmdsEx.AddMenu);
            cm_AddMenu.Items.Add(new CtxmItemData("ダイアログ表示(_X)...", EpgCmds.ShowDialog, 1));
            cm_AddMenu.Items.Add(new CtxmItemData(cm_Separator));
            cm_AddMenu.Items.Add(new CtxmItemData("デフォルト", EpgCmds.AddOnPreset, 0));//仮

            //予約変更サブメニューの各サブメニュー
            ////自動登録の有効/無効
            var cm_ChgKeyEnabledMenu = new CtxmItemData("自動登録有効(仮)", EpgCmdsEx.ChgKeyEnabledMenu);
            cm_ChgKeyEnabledMenu.Items.Add(new CtxmItemData(CommonManager.ConvertIsEnableText(true) + "(_0)", EpgCmds.ChgKeyEnabled, 0));
            cm_ChgKeyEnabledMenu.Items.Add(new CtxmItemData(CommonManager.ConvertIsEnableText(false) + "(_1)", EpgCmds.ChgKeyEnabled, 1));

            ////プリセット変更 実行時、サブメニューにプリセットを展開する。
            var cm_ChgOnPresetMenu = new CtxmItemData("プリセットへ変更(仮)", EpgCmdsEx.ChgOnPresetMenu);
            cm_ChgOnPresetMenu.Items.Add(new CtxmItemData("デフォルト(_0)", EpgCmds.ChgOnPreset, 0));//仮

            ////予約モード変更
            var cm_ChgResModeMenu = new CtxmItemData("予約モード変更(仮)", EpgCmdsEx.ChgResModeMenu);
            cm_ChgResModeMenu.Items.Add(new CtxmItemData("EPG予約(_E)", EpgCmds.ChgResMode, 0));
            cm_ChgResModeMenu.Items.Add(new CtxmItemData("プログラム予約(_P)", EpgCmds.ChgResMode, 1));
            cm_ChgResModeMenu.Items.Add(new CtxmItemData(cm_Separator));

            ////録画有効
            var cm_ChgRecEnableMenu = new CtxmItemData("録画有効(仮)", EpgCmdsEx.ChgRecEnableMenu);
            cm_ChgRecEnableMenu.Items.Add(new CtxmItemData(CommonManager.ConvertIsEnableText(true) + "(_0)", EpgCmds.ChgRecEnabled, 0));
            cm_ChgRecEnableMenu.Items.Add(new CtxmItemData(CommonManager.ConvertIsEnableText(false) + "(_1)", EpgCmds.ChgRecEnabled, 1));

            ////録画モード
            var cm_ChgRecmodeMenu = new CtxmItemData("録画モード(仮)", EpgCmdsEx.ChgRecmodeMenu);
            for (int i = 0; i <= 4; i++)
            {
                cm_ChgRecmodeMenu.Items.Add(new CtxmItemData(string.Format("{0}(_{1})"
                    , CommonManager.ConvertRecModeText(i), i), EpgCmds.ChgRecmode, i));
            }

            ////優先度
            var cm_ChgPriorityMenu = new CtxmItemData("優先度(仮)", EpgCmdsEx.ChgPriorityMenu);
            for (int i = 1; i <= 5; i++)
            {
                cm_ChgPriorityMenu.Items.Add(new CtxmItemData(
                    CommonManager.ConvertPriorityText(i).Insert(1, string.Format("(_{0})", i)), EpgCmds.ChgPriority, i));
            }

            ////イベントリレー変更
            var cm_ChgRelayMenu = new CtxmItemData("イベントリレー追従(仮)", EpgCmdsEx.ChgRelayMenu);
            for (int i = 0; i <= 1; i++)
            {
                cm_ChgRelayMenu.Items.Add(new CtxmItemData(string.Format("{0}(_{1})"
                    , CommonManager.ConvertYesNoText(i), i), EpgCmds.ChgRelay, i));
            }

            ////ぴったり変更
            var cm_ChgPittariMenu = new CtxmItemData("ぴったり録画(仮)", EpgCmdsEx.ChgPittariMenu);
            for (int i = 0; i <= 1; i++)
            {
                cm_ChgPittariMenu.Items.Add(new CtxmItemData(string.Format("{0}(_{1})"
                    , CommonManager.ConvertYesNoText(i), i), EpgCmds.ChgPittari, i));
            }

            ////チューナー変更、実行時、セパレータ以降に一覧を展開する。
            var cm_ChgTunerMenu = new CtxmItemData("チューナー(仮)", EpgCmdsEx.ChgTunerMenu);
            cm_ChgTunerMenu.Items.Add(new CtxmItemData("自動(_0)", EpgCmds.ChgTuner, 0));
            cm_ChgTunerMenu.Items.Add(new CtxmItemData(cm_Separator));
            
            ////開始マージン
            var cm_ChgMarginStartMenu = new CtxmItemData("開始マージン(仮)", EpgCmdsEx.ChgMarginStartMenu);
            cm_ChgMarginStartMenu.Items.Add(new CtxmItemData("デフォルトに変更(_I)", EpgCmds.ChgMarginStart, 0));
            cm_ChgMarginStartMenu.Items.Add(new CtxmItemData("指定値へ変更(_S)...", EpgCmds.ChgMarginValue, 1));
            cm_ChgMarginStartMenu.Items.Add(new CtxmItemData(cm_Separator));
            int idx = 0;
            var vals = new int[] { 1, 5, 30, 60 };
            foreach (int val in vals.Concat(vals.Select(val => -val)))
            {
                cm_ChgMarginStartMenu.Items.Add(new CtxmItemData(string.Format(
                    "{0:増やす;減らす}(_{1}) : {0:+0;-0} 秒", val, idx++), EpgCmds.ChgMarginStart, val));
            }
            cm_ChgMarginStartMenu.Items.Insert(cm_ChgMarginStartMenu.Items.Count - vals.Length, new CtxmItemData(cm_Separator));

            ////終了マージン、複製してコマンドだけ差し替える。
            var cm_ChgMarginEndMenu = new CtxmItemData("終了マージン(仮)", cm_ChgMarginStartMenu);
            cm_ChgMarginEndMenu.Command = EpgCmdsEx.ChgMarginEndMenu;
            cm_ChgMarginEndMenu.Items = cm_ChgMarginStartMenu.Items.DeepClone();
            cm_ChgMarginEndMenu.Items.ForEach(menu => { if (menu.Command == EpgCmds.ChgMarginStart) menu.Command = EpgCmds.ChgMarginEnd; });
            cm_ChgMarginEndMenu.Items.ForEach(menu => { if (menu.Command == EpgCmds.ChgMarginValue) menu.ID = 2; });

            ////録画後動作
            var cm_ChgRecEndMenu = new CtxmItemData("録画後動作(仮)", EpgCmdsEx.ChgRecEndMenu);
            cm_ChgRecEndMenu.Items.Add(new CtxmItemData("デフォルトに変更(_I)", EpgCmds.ChgRecEndMode, -1));
            cm_ChgRecEndMenu.Items.Add(new CtxmItemData(cm_Separator));
            for (int i = 0; i <= 3; i++)
            {
                cm_ChgRecEndMenu.Items.Add(new CtxmItemData(string.Format("{0}(_{1})"
                    , CommonManager.ConvertRecEndModeText(i), i), EpgCmds.ChgRecEndMode, i));
            }
            cm_ChgRecEndMenu.Items.Add(new CtxmItemData(cm_Separator));
            for (int i = 0; i <= 1; i++)
            {
                cm_ChgRecEndMenu.Items.Add(new CtxmItemData(string.Format("復帰後再起動{0}(_{1})"
                    , CommonManager.ConvertYesNoText(i), i + 4), EpgCmds.ChgRecEndReboot, i));
            }

            //予約変更サブメニュー登録
            var cm_ChangeMenu = new CtxmItemData("変更(仮)", EpgCmdsEx.ChgMenu);
            cm_ChangeMenu.Items.Add(new CtxmItemData("ダイアログ表示...", EpgCmds.ShowDialog));
            cm_ChangeMenu.Items.Add(new CtxmItemData(cm_Separator));
            cm_ChangeMenu.Items.Add(new CtxmItemData("自動登録有効", cm_ChgKeyEnabledMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("プリセットへ変更", cm_ChgOnPresetMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("予約モード変更", cm_ChgResModeMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("まとめて録画設定を変更...", EpgCmds.ChgBulkRecSet));
            cm_ChangeMenu.Items.Add(new CtxmItemData("まとめてジャンル絞り込みを変更...", EpgCmds.ChgGenre));
            cm_ChangeMenu.Items.Add(new CtxmItemData(cm_Separator));
            cm_ChangeMenu.Items.Add(new CtxmItemData("録画有効", cm_ChgRecEnableMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("録画モード", cm_ChgRecmodeMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("優先度", cm_ChgPriorityMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("イベントリレー追従", cm_ChgRelayMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("ぴったり（？）録画", cm_ChgPittariMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("チューナー", cm_ChgTunerMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("開始マージン", cm_ChgMarginStartMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("終了マージン", cm_ChgMarginEndMenu));
            cm_ChangeMenu.Items.Add(new CtxmItemData("録画後動作", cm_ChgRecEndMenu));

            CtxmData ctmd = DefCtxmData[CtxmCode.EditChgMenu];
            ctmd.Items = cm_ChangeMenu.Items;

            //アイテムの復元サブメニュー
            var cm_RestoreMenu = new CtxmItemData("アイテムの復元(仮)", EpgCmdsEx.RestoreMenu);
            cm_RestoreMenu.Items.Add(new CtxmItemData("履歴をクリア(_R)", EpgCmds.RestoreClear));

            //フォルダを開くサブメニュー登録
            var cm_OpenFolderMenu = new CtxmItemData("録画フォルダを開く(仮)", EpgCmdsEx.OpenFolderMenu);
            cm_OpenFolderMenu.Items.Add(new CtxmItemData("録画フォルダを開く(仮)", EpgCmds.OpenFolder));

            //ビューモードサブメニュー
            var cm_ViewMenu = new CtxmItemData("表示モード(仮)", EpgCmdsEx.ViewMenu);
            for (int i = 0; i <= 2; i++)
            {
                cm_ViewMenu.Items.Add(new CtxmItemData(CommonManager.ConvertViewModeText(i)
                    + string.Format("(_{0})", i + 1), EpgCmds.ViewChgMode, i));
            }
            cm_ViewMenu.Items.Add(new CtxmItemData(cm_Separator));
            cm_ViewMenu.Items.Add(new CtxmItemData("表示設定(_S)...", EpgCmds.ViewChgSet));
            cm_ViewMenu.Items.Add(new CtxmItemData("一時的な変更をクリア(_R)", EpgCmds.ViewChgReSet));

            //共通メニューの追加用リスト
            var AddAppendTagMenus = new List<CtxmItemData>();
            AddAppendTagMenus.Add(new CtxmItemData("録画タグで予約情報検索", EpgCmds.InfoSearchRecTag));
            AddAppendTagMenus.Add(new CtxmItemData("録画タグをネットで検索", EpgCmds.SearchRecTag));
            AddAppendTagMenus.Add(new CtxmItemData("録画タグをコピー", EpgCmds.CopyRecTag));
            AddAppendTagMenus.Add(new CtxmItemData("録画タグに貼り付け", EpgCmds.SetRecTag));

            var AddAppendMenus = new List<CtxmItemData>();
            AddAppendMenus.Add(new CtxmItemData(cm_Separator));
            AddAppendMenus.Add(new CtxmItemData("番組名をコピー", EpgCmds.CopyTitle));
            AddAppendMenus.Add(new CtxmItemData("番組情報をコピー", EpgCmds.CopyContent));
            AddAppendMenus.Add(new CtxmItemData("番組名で予約情報検索", EpgCmds.InfoSearchTitle));
            AddAppendMenus.Add(new CtxmItemData("番組名をネットで検索", EpgCmds.SearchTitle));

            var AddMenuSetting = new List<CtxmItemData>();
            AddMenuSetting.Add(new CtxmItemData(cm_Separator));
            AddMenuSetting.Add(new CtxmItemData("右クリックメニューの設定...", EpgCmds.MenuSetting));


            //メニューアイテム:予約一覧
            ctmd = DefCtxmData[CtxmCode.ReserveView];
            ctmd.Items.Add(new CtxmItemData("予約←→無効", EpgCmds.ChgOnOff));
            ctmd.Items.Add(new CtxmItemData("変更(_C)", cm_ChangeMenu));
            ctmd.Items.Add(new CtxmItemData("コピーを追加", EpgCmds.CopyItem));
            ctmd.Items.Add(new CtxmItemData("削除", EpgCmds.Delete));
            ctmd.Items.Add(new CtxmItemData("アイテムの復元", cm_RestoreMenu));
            ctmd.Items.Add(new CtxmItemData("新規プログラム予約...", EpgCmds.ShowAddDialog));
            ctmd.Items.Add(new CtxmItemData("チューナー画面へジャンプ", EpgCmds.JumpTuner));
            ctmd.Items.Add(new CtxmItemData("番組表へジャンプ", EpgCmds.JumpTable));
            ctmd.Items.Add(new CtxmItemData("自動予約登録変更", EpgCmdsEx.ShowAutoAddDialogMenu));
            ctmd.Items.Add(new CtxmItemData("番組名でキーワード予約作成...", EpgCmds.ToAutoadd));
            ctmd.Items.Add(new CtxmItemData("追っかけ再生", EpgCmds.Play));
            ctmd.Items.Add(new CtxmItemData("録画フォルダを開く", cm_OpenFolderMenu));
            ctmd.Items.AddRange(AddAppendMenus.DeepClone());
            ctmd.Items.AddRange(AddAppendTagMenus.DeepClone());
            ctmd.Items.AddRange(AddMenuSetting.DeepClone());

            //メニューアイテム:使用予定チューナー
            ctmd = DefCtxmData[CtxmCode.TunerReserveView];
            ctmd.Items.Add(new CtxmItemData("予約←→無効", EpgCmds.ChgOnOff));
            ctmd.Items.Add(new CtxmItemData("変更(_C)", cm_ChangeMenu));
            ctmd.Items.Add(new CtxmItemData("コピーを追加", EpgCmds.CopyItem));
            ctmd.Items.Add(new CtxmItemData("削除", EpgCmds.Delete));
            ctmd.Items.Add(new CtxmItemData("アイテムの復元", cm_RestoreMenu));
            ctmd.Items.Add(new CtxmItemData("新規プログラム予約...", EpgCmds.ShowAddDialog));
            ctmd.Items.Add(new CtxmItemData("予約一覧へジャンプ", EpgCmds.JumpReserve));
            ctmd.Items.Add(new CtxmItemData("番組表へジャンプ", EpgCmds.JumpTable));
            ctmd.Items.Add(new CtxmItemData("自動予約登録変更", EpgCmdsEx.ShowAutoAddDialogMenu));
            ctmd.Items.Add(new CtxmItemData("番組名でキーワード予約作成...", EpgCmds.ToAutoadd));
            ctmd.Items.Add(new CtxmItemData("追っかけ再生", EpgCmds.Play));
            ctmd.Items.Add(new CtxmItemData("録画フォルダを開く", cm_OpenFolderMenu));
            ctmd.Items.AddRange(AddAppendMenus.DeepClone());
            ctmd.Items.AddRange(AddAppendTagMenus.DeepClone());
            ctmd.Items.AddRange(AddMenuSetting.DeepClone());

            //メニューアイテム:録画済み一覧
            ctmd = DefCtxmData[CtxmCode.RecInfoView];
            ctmd.Items.Add(new CtxmItemData("録画情報...", EpgCmds.ShowDialog));
            ctmd.Items.Add(new CtxmItemData("削除", EpgCmds.Delete));
            ctmd.Items.Add(new CtxmItemData("プロテクト←→解除", EpgCmds.ProtectChange));
            ctmd.Items.Add(new CtxmItemData("番組表へジャンプ", EpgCmds.JumpTable));
            ctmd.Items.Add(new CtxmItemData("自動予約登録変更", EpgCmdsEx.ShowAutoAddDialogMenu));
            ctmd.Items.Add(new CtxmItemData("番組名でキーワード予約作成...", EpgCmds.ToAutoadd));
            ctmd.Items.Add(new CtxmItemData("再生", EpgCmds.Play));
            ctmd.Items.Add(new CtxmItemData("録画フォルダを開く", EpgCmds.OpenFolder));//他の画面と違う
            ctmd.Items.AddRange(AddAppendMenus.DeepClone());
            ctmd.Items.AddRange(AddMenuSetting.DeepClone());

            //メニューアイテム:キーワード自動予約登録
            ctmd = DefCtxmData[CtxmCode.EpgAutoAddView];
            ctmd.Items.Add(new CtxmItemData("予約一覧(_L)", EpgCmdsEx.ShowReserveDialogMenu));
            ctmd.Items.Add(new CtxmItemData("変更(_C)", cm_ChangeMenu));
            ctmd.Items.Add(new CtxmItemData("コピーを追加", EpgCmds.CopyItem));
            ctmd.Items.Add(new CtxmItemData("削除", EpgCmds.Delete));
            ctmd.Items.Add(new CtxmItemData("予約ごと削除", EpgCmds.Delete2));
            ctmd.Items.Add(new CtxmItemData("予約を自動登録に合わせる", EpgCmds.AdjustReserve));
            ctmd.Items.Add(new CtxmItemData("アイテムの復元", cm_RestoreMenu));
            ctmd.Items.Add(new CtxmItemData("次の予約(予約一覧)へジャンプ", EpgCmds.JumpReserve));
            ctmd.Items.Add(new CtxmItemData("次の予約(チューナー画面)へジャンプ", EpgCmds.JumpTuner));
            ctmd.Items.Add(new CtxmItemData("次の予約(番組表)へジャンプ", EpgCmds.JumpTable));
            ctmd.Items.Add(new CtxmItemData("新規自動予約登録...", EpgCmds.ShowAddDialog));
            ctmd.Items.Add(new CtxmItemData("録画フォルダを開く", cm_OpenFolderMenu));
            ctmd.Items.Add(new CtxmItemData(cm_Separator));
            ctmd.Items.Add(new CtxmItemData("Andキーワードをコピー", EpgCmds.CopyTitle));
            ctmd.Items.Add(new CtxmItemData("Andキーワードで予約情報検索", EpgCmds.InfoSearchTitle));
            ctmd.Items.Add(new CtxmItemData("Andキーワードをネットで検索", EpgCmds.SearchTitle));
            ctmd.Items.Add(new CtxmItemData("Notキーワードをコピー", EpgCmds.CopyNotKey));
            ctmd.Items.Add(new CtxmItemData("Notキーワードに貼り付け", EpgCmds.SetNotKey));
            ctmd.Items.AddRange(AddAppendTagMenus.DeepClone());
            ctmd.Items.AddRange(AddMenuSetting.DeepClone());

            //メニューアイテム:プログラム自動予約登録
            ctmd = DefCtxmData[CtxmCode.ManualAutoAddView];
            ctmd.Items.Add(new CtxmItemData("予約一覧(_L)", EpgCmdsEx.ShowReserveDialogMenu));
            ctmd.Items.Add(new CtxmItemData("変更(_C)", cm_ChangeMenu));
            ctmd.Items.Add(new CtxmItemData("コピーを追加", EpgCmds.CopyItem));
            ctmd.Items.Add(new CtxmItemData("削除", EpgCmds.Delete));
            ctmd.Items.Add(new CtxmItemData("予約ごと削除", EpgCmds.Delete2));
            ctmd.Items.Add(new CtxmItemData("予約を自動登録に合わせる", EpgCmds.AdjustReserve));
            ctmd.Items.Add(new CtxmItemData("アイテムの復元", cm_RestoreMenu));
            ctmd.Items.Add(new CtxmItemData("次の予約(予約一覧)へジャンプ", EpgCmds.JumpReserve));
            ctmd.Items.Add(new CtxmItemData("次の予約(チューナー画面)へジャンプ", EpgCmds.JumpTuner));
            ctmd.Items.Add(new CtxmItemData("次の予約(番組表)へジャンプ", EpgCmds.JumpTable));
            ctmd.Items.Add(new CtxmItemData("番組名でキーワード予約作成...", EpgCmds.ToAutoadd));
            ctmd.Items.Add(new CtxmItemData("新規自動予約登録...", EpgCmds.ShowAddDialog));
            ctmd.Items.Add(new CtxmItemData("録画フォルダを開く", cm_OpenFolderMenu));
            ctmd.Items.Add(new CtxmItemData(cm_Separator));
            ctmd.Items.Add(new CtxmItemData("番組名をコピー", EpgCmds.CopyTitle));
            ctmd.Items.Add(new CtxmItemData("番組名で予約情報検索", EpgCmds.InfoSearchTitle));
            ctmd.Items.Add(new CtxmItemData("番組名をネットで検索", EpgCmds.SearchTitle));
            ctmd.Items.AddRange(AddAppendTagMenus.DeepClone());
            ctmd.Items.AddRange(AddMenuSetting.DeepClone());

            //メニューアイテム:番組表
            ctmd = DefCtxmData[CtxmCode.EpgView];
            ctmd.Items.Add(new CtxmItemData("簡易予約/予約←→無効", EpgCmds.ChgOnOff));
            ctmd.Items.Add(new CtxmItemData("予約追加(_A)", cm_AddMenu));
            ctmd.Items.Add(new CtxmItemData("変更(_C)", cm_ChangeMenu));
            ctmd.Items.Add(new CtxmItemData("コピーを追加", EpgCmds.CopyItem));
            ctmd.Items.Add(new CtxmItemData("削除", EpgCmds.Delete));
            ctmd.Items.Add(new CtxmItemData("アイテムの復元", cm_RestoreMenu));
            ctmd.Items.Add(new CtxmItemData("予約一覧へジャンプ", EpgCmds.JumpReserve));
            ctmd.Items.Add(new CtxmItemData("録画済み一覧へジャンプ", EpgCmds.JumpRecInfo));
            ctmd.Items.Add(new CtxmItemData("チューナー画面へジャンプ", EpgCmds.JumpTuner));
            ctmd.Items.Add(new CtxmItemData("番組表(標準モード)へジャンプ", EpgCmds.JumpTable));
            ctmd.Items.Add(new CtxmItemData("自動予約登録変更", EpgCmdsEx.ShowAutoAddDialogMenu));
            ctmd.Items.Add(new CtxmItemData("番組名でキーワード予約作成...", EpgCmds.ToAutoadd));
            ctmd.Items.Add(new CtxmItemData("追っかけ再生", EpgCmds.Play, 0));
            ctmd.Items.Add(new CtxmItemData("録画済み再生", EpgCmds.Play, 1));
            ctmd.Items.Add(new CtxmItemData("録画フォルダを開く", cm_OpenFolderMenu));
            ctmd.Items.AddRange(AddAppendMenus.DeepClone());
            ctmd.Items.AddRange(AddAppendTagMenus.DeepClone());
            ctmd.Items.Add(new CtxmItemData(cm_Separator));
            ctmd.Items.Add(new CtxmItemData("表示モード(_V)", cm_ViewMenu));
            ctmd.Items.AddRange(AddMenuSetting.DeepClone());

            //メニューアイテム:検索ダイアログ、キーワード予約ダイアログ
            ctmd = DefCtxmData[CtxmCode.SearchWindow];
            ctmd.Items.Add(new CtxmItemData("簡易予約/予約←→無効", EpgCmds.ChgOnOff));
            ctmd.Items.Add(new CtxmItemData("予約追加(_A)", cm_AddMenu));
            ctmd.Items.Add(new CtxmItemData("変更(_C)", cm_ChangeMenu));
            ctmd.Items.Add(new CtxmItemData("コピーを追加", EpgCmds.CopyItem));
            ctmd.Items.Add(new CtxmItemData("削除", EpgCmds.Delete));
            ctmd.Items.Add(new CtxmItemData("アイテムの復元", cm_RestoreMenu));
            ctmd.Items.Add(new CtxmItemData("予約一覧へジャンプ", EpgCmds.JumpReserve));
            ctmd.Items.Add(new CtxmItemData("録画済み一覧へジャンプ", EpgCmds.JumpRecInfo));
            ctmd.Items.Add(new CtxmItemData("チューナー画面へジャンプ", EpgCmds.JumpTuner));
            ctmd.Items.Add(new CtxmItemData("番組表へジャンプ", EpgCmds.JumpTable));
            ctmd.Items.Add(new CtxmItemData("自動予約登録変更", EpgCmdsEx.ShowAutoAddDialogMenu));
            ctmd.Items.Add(new CtxmItemData("番組名で再検索", EpgCmds.ReSearch));
            ctmd.Items.Add(new CtxmItemData("番組名で再検索(別ウィンドウ)", EpgCmds.ReSearch2));
            ctmd.Items.Add(new CtxmItemData("追っかけ再生", EpgCmds.Play, 0));
            ctmd.Items.Add(new CtxmItemData("録画済み再生", EpgCmds.Play, 1));
            ctmd.Items.Add(new CtxmItemData("録画フォルダを開く", cm_OpenFolderMenu));
            ctmd.Items.AddRange(AddAppendMenus.DeepClone());
            ctmd.Items.AddRange(AddAppendTagMenus.DeepClone());
            ctmd.Items.AddRange(AddMenuSetting.DeepClone());

            //メニューアイテム:予約情報検索(デフォルト・複数選択)
            ctmd = DefCtxmData[CtxmCode.InfoSearchWindow];
            ctmd.Items.Add(new CtxmItemData("一覧へジャンプ", EpgCmds.JumpListView));
            ctmd.Items.Add(new CtxmItemData("番組名/ANDキーワードで再検索", EpgCmds.ReSearch));
            ctmd.Items.Add(new CtxmItemData("番組名/ANDキーワードで再検索(別ウィンドウ)", EpgCmds.ReSearch2));
            ctmd.Items.Add(new CtxmItemData(cm_Separator));
            ctmd.Items.Add(new CtxmItemData("ダイアログ表示...", EpgCmds.ShowDialog));
            ctmd.Items.Add(new CtxmItemData("有効・無効/プロテクト切替え", EpgCmds.ChgOnOff));
            ctmd.Items.Add(new CtxmItemData("削除", EpgCmds.Delete));
            ctmd.Items.Add(new CtxmItemData("アイテムの復元", cm_RestoreMenu));
            ctmd.Items.Add(new CtxmItemData(cm_Separator));
            ctmd.Items.Add(new CtxmItemData("番組名/ANDキーワードをコピー", EpgCmds.CopyTitle));
            ctmd.Items.Add(new CtxmItemData("番組名/ANDキーワードをネットで検索", EpgCmds.SearchTitle));
            ctmd.Items.AddRange(AddAppendTagMenus.DeepClone());
            ctmd.Items.AddRange(AddMenuSetting.DeepClone());

            //メニューアイテム:予約情報検索(個別選択の追加メニュー)
            AddInfoSearchMenu = new CtxmData(CtxmCode.InfoSearchWindow);
            AddInfoSearchMenu.Items.AddRange(ctmd.Items.Take(3));
        }
        private void SetDefGestureCmdList()
        {
            DefCtxmCmdList = new Dictionary<CtxmCode, List<ICommand>>();
            foreach (var ctxm in DefCtxmData)
            {
                DefCtxmCmdList.Add(ctxm.Key, GetCmdFromMenuItem(ctxm.Value.Items).Distinct().ToList());
            }
        }
        private List<ICommand> GetCmdFromMenuItem(List<CtxmItemData> items)
        {
            var clist = new List<ICommand>();
            items.ForEach(item =>
            {
                if (item != null && EpgCmdsEx.IsDummyCmd(item.Command) == false)
                {
                    clist.Add(item.Command);
                }
                clist.AddRange(GetCmdFromMenuItem(item.Items));
            });
            return clist;
        }

        public void ReloadWorkData()
        {
            MC.SetWorkCmdOptions();
            SetWorkCxtmData();
            SetWorkGestureCmdList();

            //簡易設定側の、修正済みデータの書き戻し
            Settings.Instance.MenuSet = GetWorkMenuSettingData();
        }
        private void SetWorkCxtmData()
        {
            WorkCtxmData = new Dictionary<CtxmCode, CtxmData>();
            foreach (var data in DefCtxmData.Values) { WorkCtxmData.Add(data.ctxmCode, GetWorkCtxmDataView(data.ctxmCode)); }

            //編集メニューの差し替え
            foreach (CtxmData ctxm in WorkCtxmData.Values)
            {
                foreach (var chgMenu in ctxm.Items.Where(item => item.Command == EpgCmdsEx.ChgMenu))
                {
                    chgMenu.Items = WorkCtxmData[CtxmCode.EditChgMenu].Items.DeepClone();
                }
            }
        }
        private CtxmData GetWorkCtxmDataView(CtxmCode code)
        {
            CtxmData ctxm = new CtxmData(code);
            CtxmData ctxmDef = DefCtxmData[code];

            //存在するものをコピーしていく。編集メニューは常に個別設定が有効になる。
            if (Settings.Instance.MenuSet.IsManualAssign.Contains(code) == true || code == CtxmCode.EditChgMenu)
            {
                CtxmSetting ctxmEdited = Settings.Instance.MenuSet.ManualMenuItems.FindData(code);
                if (ctxmEdited == null)
                {
                    //編集サブメニューの場合は、初期無効アイテムを削除したデフォルトセッティングを返す。
                    return code != CtxmCode.EditChgMenu ? ctxmDef.DeepClone() : GetDefaultChgSubMenuCtxmData();
                }

                ctxmEdited.Items.ForEach(setMenuString =>
                {
                    CtxmItemData item1 = ctxmDef.Items.Find(item => item.Header == setMenuString);
                    if (item1 != null)
                    {
                        ctxm.Items.Add(item1.DeepClone());
                    }
                });
            }
            else
            {
                ctxmDef.Items.ForEach(item1 =>
                {
                    if (MC.WorkCmdOptions[item1.Command].IsMenuEnabled == true)
                    {
                        ctxm.Items.Add(item1.DeepClone());
                    }
                });
            }

            //セパレータの整理
            SweepSeparators(ctxm);

            return ctxm;
        }
        private void SweepSeparators(CtxmData ctxm)
        {
            //・連続したセパレータの除去
            for (int i = ctxm.Items.Count - 1; i >= 1; i--)
            {
                if (ctxm.Items[i].Command == EpgCmdsEx.Separator && ctxm.Items[i - 1].Command == EpgCmdsEx.Separator)
                {
                    ctxm.Items.RemoveAt(i);
                }
            }
            //・先頭と最後のセパレータ除去
            if (ctxm.Items.Count != 0 && ctxm.Items[ctxm.Items.Count - 1].Command == EpgCmdsEx.Separator)
            {
                ctxm.Items.RemoveAt(ctxm.Items.Count - 1);
            }
            if (ctxm.Items.Count != 0 && ctxm.Items[0].Command == EpgCmdsEx.Separator)
            {
                ctxm.Items.RemoveAt(0);
            }
        }
        private void SetWorkGestureCmdList()
        {
            //WorCtxmCmdList = new Dictionary<CtxmCode, List<ICommand>>();
            WorkGestureCmdList = new Dictionary<CtxmCode, List<ICommand>>();
            WorkDelGesCmdList = new Dictionary<CtxmCode, List<ICommand>>();

            foreach (var ctxm in WorkCtxmData)
            {
                var cmdlist = GetCmdFromMenuItem(ctxm.Value.Items);
                //var cmdlist = GetCmdFromMenuItem(ctxm.Value.Items).Distinct().ToList();

                //コンテキストメニューのコマンドリスト
                //WorCtxmCmdList.Add(ctxm.Key, cmdlist.ToList());

                //常時有効なショートカットのあるコマンドを追加
                cmdlist.AddRange(DefCtxmCmdList[ctxm.Key].Where(command => MC.WorkCmdOptions[command].IsGesNeedMenu == false));
                cmdlist = cmdlist.Distinct().ToList();

                //無効なコマンドを除外
                cmdlist = cmdlist.Where(command => MC.WorkCmdOptions[command].IsGestureEnabled == true).ToList();

                WorkGestureCmdList.Add(ctxm.Key, cmdlist);

                //ショートカット無効なコマンドリスト
                WorkDelGesCmdList.Add(ctxm.Key, DefCtxmCmdList[ctxm.Key].Except(cmdlist).ToList());
            }
        }

        //ショートカットをデフォルト値に戻す
        public void SetDefaultGestures(MenuSettingData info)
        {
            foreach (MenuCmds.CmdData item in MC.DefCmdOptions.Values.Where(item => item.IsSaveSetting == true))
            {
                MenuSettingData.CmdSaveData data = info.EasyMenuItems.Find(d => d.GetCommand() == item.Command);
                if (data != null) data.SetGestuers(item.Gestures);
            }
        }

        //設定画面へデフォルト値を送る
        public MenuSettingData GetDefaultMenuSettingData()
        {
            var set = new MenuSettingData();
            set.EasyMenuItems = MC.GetDefEasyMenuSetting();
            set.ManualMenuItems = GetManualMenuSetting(DefCtxmData);
            //編集メニュー初期値の設定、差し替え
            set.ManualMenuItems.RemoveAll(item => item.ctxmCode == CtxmCode.EditChgMenu);
            set.ManualMenuItems.Add(new CtxmSetting(GetDefaultChgSubMenuCtxmData()));
            return set;
        }
        //設定画面へメニュー編集選択画面用の全てを含む初期値を送る
        public List<CtxmSetting> GetDefaultCtxmSettingForEditor()
        {
            return GetManualMenuSetting(DefCtxmData);
        }
        private CtxmData GetDefaultChgSubMenuCtxmData()
        {
            var set = new CtxmData(CtxmCode.EditChgMenu);
            DefCtxmData[CtxmCode.EditChgMenu].Items.ForEach(item =>
            {
                //初期無効データは入れない
                if (MC.DefCmdOptions[item.Command].IsMenuEnabled == true)
                {
                    set.Items.Add(item.DeepClone());
                }
            });
            return set;
        }
        private MenuSettingData GetWorkMenuSettingData()
        {
            var set = Settings.Instance.MenuSet.DeepClone();
            set.EasyMenuItems = MC.GetWorkEasyMenuSetting();

            foreach (CtxmCode code in Enum.GetValues(typeof(CtxmCode)))
            {
                //編集メニューは常にマニュアルモードと同等
                if (set.IsManualAssign.Contains(code) == true || code == CtxmCode.EditChgMenu)
                {
                    set.ManualMenuItems.RemoveAll(item => item.ctxmCode == code);
                    set.ManualMenuItems.Add(new CtxmSetting(WorkCtxmData[code]));
                }
                else if (set.ManualMenuItems.Find(item => item.ctxmCode == code) == null)
                {
                    set.ManualMenuItems.Add(new CtxmSetting(DefCtxmData[code]));
                }
            }

            return set;
        }
        private List<CtxmSetting> GetManualMenuSetting(Dictionary<CtxmCode, CtxmData> dic)
        {
            var cmManualMenuSetting = new List<CtxmSetting>();
            foreach (CtxmCode code in Enum.GetValues(typeof(CtxmCode)))
            {
                cmManualMenuSetting.Add(new CtxmSetting(dic[code]));
            }
            return cmManualMenuSetting;
        }

        public bool IsGestureDisableOnView(ICommand icmd, CtxmCode code)
        {
            if (icmd == null) return false;

            MenuSettingData.CmdSaveData cmdData = Settings.Instance.MenuSet.EasyMenuItems.Find(data => data.GetCommand() == icmd);
            return cmdData != null && cmdData.IsGestureEnabled == false || WorkDelGesCmdList[code].Contains(icmd);
        }

        public List<ICommand> GetViewMenuCmdList(CtxmCode code)
        {
            return DefCtxmCmdList[code].ToList();
        }
        public List<ICommand> GetWorkGestureCmdList(CtxmCode code)
        {
            return WorkGestureCmdList[code].ToList();
        }

        public void CtxmGenerateContextMenuInfoSearch(ContextMenu ctxm, CtxmCode code)
        {
            CtxmData data = WorkCtxmData[code].DeepClone();
            if (code != CtxmCode.InfoSearchWindow)
            {
                //他画面用の簡易検索メニューを削除
                data.Items.Remove(data.Items.FirstOrDefault(d => d.Command == EpgCmds.InfoSearchTitle));

                //簡易検索用のメニューを先頭に追加
                var ISData = WorkCtxmData[CtxmCode.InfoSearchWindow].Items.Select(d => d.Command).ToList();
                data.Items.Insert(0, new CtxmItemData(EpgCmdsEx.SeparatorString, EpgCmdsEx.Separator));
                data.Items.InsertRange(0, AddInfoSearchMenu.Items.Where(item => ISData.Contains(item.Command)));
                SweepSeparators(data);
            }
            CtxmGenerateContextMenu(data, ctxm, code, true);
        }
        public void CtxmGenerateContextMenuEpgView(ContextMenu ctxm)
        {
            var ctmd = new CtxmData(CtxmCode.EpgView, DefCtxmData[CtxmCode.EpgView].Items.Find(item => item.Command == EpgCmdsEx.ViewMenu).Items);
            CtxmGenerateContextMenu(ctmd, ctxm, CtxmCode.EpgView, false);
            foreach (var item in ctxm.Items.OfType<MenuItem>().Where(item => item.Tag == EpgCmds.ViewChgMode))
            {
                item.IsChecked = ((item.CommandParameter as EpgCmdParam).ID == (int)ctxm.Tag);
            }
        }
        public void CtxmGenerateContextMenu(ContextMenu ctxm, CtxmCode code, bool shortcutTextforListType)
        {
            CtxmGenerateContextMenu(WorkCtxmData[code], ctxm, code, shortcutTextforListType);
        }
        private void CtxmGenerateContextMenu(CtxmData data, ContextMenu ctxm, CtxmCode code, bool shortcutTextforListType)
        {
            try
            {
                ctxm.Name = code.ToString();
                CtxmConvertToMenuItems(data.Items, ctxm.Items, code, shortcutTextforListType);
            }
            catch (Exception ex) { MessageBox.Show(ex.ToString()); }
        }
        private void CtxmConvertToMenuItems(List<CtxmItemData> src, ItemCollection dest, CtxmCode code, bool shortcutTextforListType)
        {
            dest.Clear();

            src.ForEach(data =>
            {
                Control item;
                if (data.Command == EpgCmdsEx.Separator)
                {
                    item = new Separator();
                }
                else
                {
                    var menu = new MenuItem();
                    menu.Header = data.Header;
                    menu.Command = (EpgCmdsEx.IsDummyCmd(data.Command) ? null : data.Command);
                    if (menu.Command != null)
                    {
                        if ((shortcutTextforListType == true || (MC.WorkCmdOptions[data.Command].GesTrg & MenuCmds.GestureTrg.ToView) == MenuCmds.GestureTrg.ToView) 
                            && (MC.WorkCmdOptions.ContainsKey(data.Command) == false || MC.WorkCmdOptions[data.Command].IsGestureEnabled == true)
                            && data.ID == 0)
                        {
                            menu.InputGestureText = MenuBinds.GetInputGestureText(data.Command);
                        }
                    }
                    menu.CommandParameter = new EpgCmdParam(typeof(MenuItem), code, data.ID);
                    if (data.Items.Count != 0) CtxmConvertToMenuItems(data.Items, menu.Items, code, shortcutTextforListType);
                    item = menu;
                }
                item.Tag = data.Command;

                dest.Add(item);
            });
        }

        public static char ToAccessKey(double n, uint divisor = 36)
        {
            return "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ"[(int)((uint)n % Math.Min(36, divisor))];
        }

        public void CtxmGenerateAddOnPresetItems(MenuItem menu) { CtxmGenerateOnPresetItems(menu, EpgCmds.AddOnPreset); }
        public void CtxmGenerateChgOnPresetItems(MenuItem menu) { CtxmGenerateOnPresetItems(menu, EpgCmds.ChgOnPreset); }
        public void CtxmGenerateOnPresetItems(MenuItem menu, ICommand icmd)
        {
            var delList = menu.Items.OfType<MenuItem>().Where(item => item.Command == icmd).ToList();
            delList.ForEach(item => menu.Items.Remove(item));

            if (menu.IsEnabled == false) return;

            foreach (var item in Settings.Instance.RecPresetList)
            {
                var menuItem = new MenuItem();
                menuItem.Header = string.Format("プリセット(_{0}) : " + MenuUtil.ToAccessKeyForm(item.ToString()), ToAccessKey(item.ID));
                menuItem.Command = icmd;
                menuItem.CommandParameter = new EpgCmdParam(menu.CommandParameter as EpgCmdParam);
                (menuItem.CommandParameter as EpgCmdParam).ID = item.ID;
                menuItem.Tag = menuItem.Command;
                menu.Items.Add(menuItem);
            }
        }

        public void CtxmGenerateChgResModeAutoAddItems(MenuItem menu, IAutoAddTargetData info)
        {
            //クリア
            int sPos = 2;//セパレータ位置
            for (int i = menu.Items.Count - 1; i > sPos; i--) menu.Items.RemoveAt(i);

            if (menu.IsEnabled == false || info == null) return;

            CtxmGenerateChgAutoAddMenuItem(menu, info, EpgCmds.ChgResMode, true, false);

            (menu.Items[sPos] as UIElement).Visibility = menu.Items.Count - 1 > sPos ? Visibility.Visible : Visibility.Collapsed;
        }

        public void CtxmGenerateRestoreMenuItems(MenuItem menu)
        {
            //クリア
            object menuReset = menu.Items[menu.Items.Count - 1];
            menu.Items.Clear();

            for (int i = 0; i < CmdHistorys.Count; i++)
            {
                List<IRecWorkMainData> list = CmdHistorys.Historys[i].Items;
                var menuItem = new MenuItem();
                var s = string.Format((CmdHistorys.Historys[i].Command == EpgCmds.Delete ? "削除した{0}を復元" : "変更前の{0}を新規追加") + "(_{1}) : {2}"
                            , new InfoSearchItem(list[0]).ViewItemName, ToAccessKey(i, 16), MenuUtil.ToAccessKeyForm(ToMenuString(list[0])))
                            + (list.Count > 1 ? " ほか" + (list.Count - 1) : "");
                menuItem.Header = CommonUtil.LimitLenString(s, 45, 31); // 長すぎる場合は省略
                if (list.Count >= 2 || (menuItem.Header as string).Length != s.Length)
                {
                    s = string.Join("\r\n", list.Take(10).Select(item => ToMenuString(item)))
                                    + (list.Count > 10 ? "\r\nほか" + (list.Count - 10) : "");
                    menuItem.ToolTip = ViewUtil.GetTooltipBlockStandard(s);
                }
                menuItem.Command = EpgCmds.RestoreItem;
                menuItem.CommandParameter = new EpgCmdParam(menu.CommandParameter as EpgCmdParam);
                (menuItem.CommandParameter as EpgCmdParam).ID = i;
                menuItem.Tag = menuItem.Command;
                menu.Items.Add(menuItem);
            }
            menu.IsEnabled = menu.Items.Count > 0;
            menu.Items.Add(new Separator());
            menu.Items.Add(menuReset);
        }

        public void CtxmGenerateTunerMenuItems(MenuItem menu)
        {
            var delList = menu.Items.OfType<MenuItem>().Where(item => (item.CommandParameter as EpgCmdParam).ID != 0).ToList();
            delList.ForEach(item => menu.Items.Remove(item));

            if (menu.IsEnabled == false) return;

            int idx = 1; //0は自動
            foreach (var info in CommonManager.Instance.DB.TunerReserveList.Values.Where(info => info.tunerID != 0xFFFFFFFF)
                .Select(info => new TunerSelectInfo(info.tunerName, info.tunerID)))
            {
                var menuItem = new MenuItem();
                var s = MenuUtil.ToAccessKeyForm(info.ToString());
                menuItem.Header = s.Insert(Math.Min(11, s.Length), "(_" + ToAccessKey(idx++) + ")");
                menuItem.Command = EpgCmds.ChgTuner;
                menuItem.CommandParameter = new EpgCmdParam(menu.CommandParameter as EpgCmdParam);
                (menuItem.CommandParameter as EpgCmdParam).ID = (int)info.ID;
                menuItem.Tag = menuItem.Command;
                menu.Items.Add(menuItem);
            }
        }

        public bool CtxmGenerateChgAutoAdd(MenuItem menu, IAutoAddTargetData info)
        {
            bool skipMenu = Settings.Instance.MenuSet.AutoAddSearchSkipSubMenu;
            CtxmClearItemMenu(menu, skipMenu);

            if (menu.IsEnabled == false) return false;

            CtxmGenerateChgAutoAddMenuItem(menu, info, EpgCmds.ShowAutoAddDialog, null, Settings.Instance.MenuSet.AutoAddFazySearch);

            if (menu.Items.Count == 0) return false;

            //候補が一つの時は直接メニューを実行出来るようにする
            if (skipMenu == true) CtxmPullUpSubMenu(menu, true);
            return true;
        }

        private void CtxmGenerateChgAutoAddMenuItem(MenuItem menu, IAutoAddTargetData info, ICommand cmd, bool? IsAutoAddEnabled, bool ByFazy)
        {
            if (info != null)
            {
                var addList = new List<AutoAddData>();
                addList.AddRange(info.SearchEpgAutoAddList(IsAutoAddEnabled, ByFazy));
                addList.AddRange(info.SearchManualAutoAddList(IsAutoAddEnabled));

                var chkList = new List<AutoAddData>();
                chkList.AddRange(info.GetEpgAutoAddList(true));
                chkList.AddRange(info.GetManualAutoAddList(true));

                int idx = 0;
                addList.ForEach(autoAdd =>
                {
                    var menuItem = new MenuItem();
                    menuItem.IsChecked = chkList.Contains(autoAdd) && (info is ReserveData ? (info as ReserveData).IsAutoAdded : true);

                    menuItem.Header = new InfoSearchItem(autoAdd).ViewItemName + " : " + ToMenuString(autoAdd);
                    SetLimitLenHeader(menuItem, null, false, 42, 28);
                    var header = menuItem.Header as Label;
                    header.Content = MenuUtil.ToAccessKeyForm(header.Content as string).Insert(7, "(_" + ToAccessKey(idx++) + ")");

                    if (Settings.Instance.MenuSet.AutoAddSearchToolTip == true)
                    {
                        menuItem.ToolTip = AutoAddDataItemEx.CreateIncetance(autoAdd).ToolTipViewAlways;
                    }
                    menuItem.Command = cmd;
                    menuItem.CommandParameter = new EpgCmdParam(menu.CommandParameter as EpgCmdParam);
                    (menuItem.CommandParameter as EpgCmdParam).Data = autoAdd.GetType();//オブジェクト入れると残るので
                    (menuItem.CommandParameter as EpgCmdParam).ID = (int)(autoAdd.DataID);
                    menuItem.Tag = menuItem.Command;

                    menu.Items.Add(menuItem);
                });
            }
        }
        private string ToMenuString(IRecWorkMainData data)
        {
            string s = (data.DataTitle == "" ? "(空白)" : data.DataTitle);
            if (data is ManualAutoAddData)
            {
                var view = new ManualAutoAddDataItem(data as ManualAutoAddData);
                s = string.Format("({0}){1} {2}", view.DayOfWeek, view.StartTimeShort, s);
            }
            return s;
        }

        public bool CtxmGenerateShowReserveDialogMenuItems(MenuItem menu, IEnumerable<AutoAddData> list)
        {
            menu.Items.Clear();

            if (menu.IsEnabled == true && list != null)
            {
                var chkList = new HashSet<ReserveData>(list.GetAutoAddList(true).GetReserveList());
                var addList = list.GetReserveList().FindAll(info => info.IsOver() == false);//FindAll()は通常無くても同じはず
                var hasStatus = addList.Any(data => string.IsNullOrWhiteSpace(new ReserveItem(data).Status) == false);

                foreach (var data in addList.OrderBy(info => info.StartTimeActual))
                {
                    var resItem = new ReserveItem(data);
                    var menuItem = new MenuItem();

                    menuItem.IsChecked = chkList.Contains(data) && data.IsAutoAdded;
                    SetLimitLenHeader(menuItem, resItem.StartTimeShort + " " + data.Title, null, 42, 28);

                    //ステータスがあれば表示する
                    var headBlock = new StackPanel { Orientation = Orientation.Horizontal };
                    if (hasStatus == true)
                    {
                        headBlock.Children.Add(new TextBlock { Text = resItem.Status, Foreground = resItem.StatusColor, Width = 25 });
                    }
                    headBlock.Children.Add(new TextBlock { Text = menuItem.Header as string });//折り返しも可能だがいまいちな感じ。
                    menuItem.Header = headBlock;

                    if (Settings.Instance.MenuSet.ReserveSearchToolTip == true)
                    {
                        menuItem.ToolTip = resItem.ToolTipViewAlways;
                    }
                    menuItem.Command = EpgCmds.ShowReserveDialog;
                    menuItem.CommandParameter = new EpgCmdParam(menu.CommandParameter as EpgCmdParam);
                    (menuItem.CommandParameter as EpgCmdParam).ID = (int)(data.ReserveID);
                    menuItem.Tag = menuItem.Command;
                    menu.Items.Add(menuItem);
                }
            }
            if (menu.Items.Count == 0)
            {
                menu.Items.Add(new object());//メニューに「>」を表示するためのダミー
                return false;
            }

            return true;
        }

        public void CtxmGenerateOpenFolderItems(MenuItem menu, RecSettingData recSetting = null, string additionalPath = null, bool isGestureString = true)
        {
            CtxmClearItemMenu(menu); //ツールチップのクリアがあるので先

            if (menu.IsEnabled == false) return;

            bool defOutPutted = false;
            recSetting = recSetting == null ? new RecSettingData() : recSetting.DeepClone();
            if (string.IsNullOrEmpty(additionalPath) == false) recSetting.RecFolderList.Add(new RecFileSetInfo { RecFolder = additionalPath });

            var addFolderList = new Action<List<RecFileSetInfo>, bool, string>((fldrs, recflg, header_exp) =>
            {
                //ワンセグ出力未チェックでも、フォルダ設定があれば表示する。
                //ただし、デフォルトフォルダは1回だけ展開して表示する。
                if (defOutPutted == false && (recflg && fldrs.Count == 0 || fldrs.Any(info => info.RecFolder == "!Default")))
                {
                    defOutPutted = true;
                    Settings.Instance.DefRecFolders.ForEach(folder => CtxmGenerateOpenFolderItem(menu, folder, header_exp + "(デフォルト) "));
                }

                foreach (var info in fldrs.Where(info => info.RecFolder != "!Default"))
                {
                    CtxmGenerateOpenFolderItem(menu, info.RecFolder, header_exp);
                }
            });

            addFolderList(recSetting.RecFolderList, true, "");
            addFolderList(recSetting.PartialRecFolder, recSetting.PartialRecFlag != 0, "(ワンセグ) ");

            //候補が一つの時は直接メニューを実行出来るようにする
            CtxmPullUpSubMenu(menu);

            if (isGestureString && MC.WorkCmdOptions[EpgCmds.OpenFolder].IsGestureEnabled)
            {
                menu.InputGestureText = MenuBinds.GetInputGestureText(EpgCmds.OpenFolder);
            }
        }
        private void CtxmGenerateOpenFolderItem(MenuItem menu, string path, string header_exp = "")
        {
            var menuItem = new MenuItem();
            SetLimitLenHeader(menuItem, header_exp + path, true);
            menuItem.Command = EpgCmds.OpenFolder;
            menuItem.CommandParameter = new EpgCmdParam(menu.CommandParameter as EpgCmdParam);
            (menuItem.CommandParameter as EpgCmdParam).Data = path;
            menuItem.Tag = menuItem.Command;
            menu.Items.Add(menuItem);
        }

        /// <summary>長すぎるとき省略してツールチップを追加する</summary>
        public void SetLimitLenHeader(MenuItem menu, string s, bool? useTextBlock, int max = 45, int pos = -1)
        {
            s = s ?? menu.Header as string;
            if (s != null && s.Length > max)
            {
                menu.ToolTip = ViewUtil.GetTooltipBlockStandard(s);
                s = CommonUtil.LimitLenString(s, max, pos); // 長すぎる場合は省略
            }
            menu.Header = useTextBlock == null ? s : 
                            useTextBlock == true ? (object)new TextBlock { Text = s, Tag = s }
                                : new Label { Content = s, Padding = new Thickness(), Tag = s };
        }

        private void CtxmClearItemMenu(MenuItem menu, bool? isEndDot = null)
        {
            menu.ToolTip = null;
            menu.Command = null;
            (menu.CommandParameter as EpgCmdParam).Data = null;
            (menu.CommandParameter as EpgCmdParam).ID = 0;
            menu.Items.Clear();
            if (isEndDot != null) CtxmPullUpSubMenuSwitchEndDot(menu, (bool)isEndDot);
        }
        private void CtxmPullUpSubMenu(MenuItem menu, bool? isEndDot = null)
        {
            if (menu.Items.Count == 1)
            {
                var submenu = (menu.Items[0] as MenuItem);
                menu.ToolTip = submenu.ToolTip ?? (submenu.Header is FrameworkElement ?
                    (submenu.Header as FrameworkElement).Tag : MenuUtil.DeleteAccessKey(submenu.Header as string, true));
                menu.Command = submenu.Command;
                (menu.CommandParameter as EpgCmdParam).Data = (submenu.CommandParameter as EpgCmdParam).Data;
                (menu.CommandParameter as EpgCmdParam).ID = (submenu.CommandParameter as EpgCmdParam).ID;
                menu.Items.Clear();
            }
            if (isEndDot != null) CtxmPullUpSubMenuSwitchEndDot(menu, (bool)isEndDot);
        }
        private void CtxmPullUpSubMenuSwitchEndDot(MenuItem menu, bool isEndDot = true)
        {
            var header = menu.Header as string;
            if (header != null)
            {
                if (header.EndsWith("...", StringComparison.Ordinal) == true)
                {
                    menu.Header = header.Substring(0, header.Length - 3);
                }
                if (isEndDot == true && menu.Items.Count == 0)
                {
                    menu.Header += "...";
                }
            }
        }
    }
}
