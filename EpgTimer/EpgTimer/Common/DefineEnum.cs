﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EpgTimer
{
    public enum ErrCode : uint
    {
        CMD_SUCCESS = 1, //成功
        CMD_ERR = 0, //汎用エラー
        CMD_NON_SUPPORT = 203, //未サポートのコマンド
        CMD_ERR_INVALID_ARG = 204, //引数エラー
        CMD_ERR_CONNECT = 205, //サーバーにコネクトできなかった
        CMD_ERR_DISCONNECT = 206, //サーバーから切断された
        CMD_ERR_TIMEOUT = 207, //タイムアウト発生
        CMD_ERR_BUSY = 208, //ビジー状態で現在処理できない（EPGデータ読み込み中、録画中など）
    };

    public enum UpdateNotifyItem : uint
    {
        No = 0, //なし
        EpgData = 1, //EPGデータ更新
        EpgDatabaseInfo = 0x10000000 | EpgData, //EPGアーカイブの情報更新、EpgTimer内部でのみ使用。
        EpgDataAdd = 0x10010000 | EpgData, //EPGの過去データ追加、EpgTimer内部でのみ使用。
        ReserveInfo = 2, //予約情報更新
        ReserveName = 0x10000000 | ReserveInfo, //予約名のみ更新、EpgTimer内部でのみ使用。
        RecInfo = 3, //録画結果更新
        AutoAddEpgInfo = 4, //EPG自動予約登録更新
        AutoAddManualInfo = 5, //プログラム自動予約登録更新
        PlugInFile = 6, //PlugIn系のファイル一覧更新
        IniFile = 51,//iniファイルの更新。
        SrvStatus = 100,
        PreRecStart = 101,
        RecStart = 102,
        RecEnd = 103,
        RecTuijyu = 104,
        ChgTuijyu = 105,
        PreEpgCapStart = 106,
        EpgCapStart = 107,
        EpgCapEnd = 108,
    };

    //MainWindowへ処理を依頼
    public enum MainProcItem : uint
    {
        EpgDataLoaded, //EPGデータ更新完了
        EpgDataSearch, //EPG表示用データの一部更新
        EpgDataAddLoaded, //EPGデータの追加更新
        ReserveInfo, //予約情報更新
        ChSet5Logo, //ロゴの更新
    };

    public enum EventInfoTextMode : uint
    {
        BasicInfo,
        BasicText,
        ExtendedText,
        PropertyInfo,
    };

    //StructDef.hより
    public enum RecEndStatus : uint
    {
        NORMAL = 1,         //終了・録画終了
        OPEN_ERR = 2,       //チューナーのオープンに失敗しました
        ERR_END = 3,        //録画中にキャンセルされた可能性があります
        NEXT_START_END = 4, //次の予約開始のためにキャンセルされました
        START_ERR = 5,      //録画時間に起動していなかった可能性があります
        CHG_TIME = 6,       //開始時間が変更されました
        NO_TUNER = 7,       //チューナー不足のため失敗しました
        NO_RECMODE = 8,     //無効扱いでした
        NOT_FIND_PF = 9,    //録画中に番組情報を確認できませんでした
        NOT_FIND_6H = 10,   //指定時間番組情報が見つかりませんでした
        END_SUBREC = 11,    //録画終了（空き容量不足で別フォルダへの保存が発生）
        ERR_RECSTART = 12,  //録画開始処理に失敗しました（空き容量不足の可能性あり）
        NOT_START_HEAD = 13,//一部のみ録画が実行された可能性があります
        ERR_CH_CHG = 14,    //指定チャンネルのデータがBonDriverから出力されなかった可能性があります
        ERR_END2 = 15       //ファイル保存で致命的なエラーが発生した可能性があります
    };
    //EpgTimer内部用、集約した簡易ステータス
    public enum RecEndStatusBasic : uint
    {
        DEFAULT = 32,//エラー無し
        WARN = 64,	//軽微なエラー
        ERR = 128   //重度なエラー
    };

    //EpgTimer内部用、予約モード
    public enum ReserveMode : uint
    {
        KeywordAuto = 0,        //キーワード予約
        ManualAuto = 1,	    //プログラム自動予約
        EPG = 2,            //個別予約(EPG)
        Program = 3         //個別予約(プログラム)
    };
}
