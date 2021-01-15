﻿#pragma once

#include "DecodeUtil.h"
#include "EpgDBUtil.h"
#include "../../Common/ThreadUtil.h"

class CEpgDataCap3Main
{
public:
	CEpgDataCap3Main(void);
	~CEpgDataCap3Main(void);

	//解析対象のTSパケットを読み込ませる
	//引数：
	// data		[IN]TSパケット
	// size		[IN]dataのサイズ（188の整数倍であること）
	void AddTSPacket(
		BYTE* data,
		DWORD size
		);

	//解析データの現在のストリームＩＤを取得する
	//引数：
	// originalNetworkID		[OUT]現在のoriginalNetworkID
	// transportStreamID		[OUT]現在のtransportStreamID
	BOOL GetTSID(
		WORD* originalNetworkID,
		WORD* transportStreamID
		);

	//自ストリームのサービス一覧を取得する
	//引数：
	// serviceListSize			[OUT]serviceListの個数
	// serviceList				[OUT]サービス情報のリスト（DLL内で自動的にdeleteする。次に取得を行うまで有効）
	BOOL GetServiceListActual(
		DWORD* serviceListSize,
		SERVICE_INFO** serviceList
		);

	//蓄積されたEPG情報のあるサービス一覧を取得する
	//SERVICE_EXT_INFOの情報はない場合がある
	//引数：
	// serviceListSize			[OUT]serviceListの個数
	// serviceList				[OUT]サービス情報のリスト（DLL内で自動的にdeleteする。次に取得を行うまで有効）
	void GetServiceListEpgDB(
		DWORD* serviceListSize,
		SERVICE_INFO** serviceList
		);

	//指定サービスの全EPG情報を取得する
	//引数：
	// originalNetworkID		[IN]取得対象のoriginalNetworkID
	// transportStreamID		[IN]取得対象のtransportStreamID
	// serviceID				[IN]取得対象のServiceID
	// epgInfoListSize			[OUT]epgInfoListの個数
	// epgInfoList				[OUT]EPG情報のリスト（DLL内で自動的にdeleteする。次に取得を行うまで有効）
	BOOL GetEpgInfoList(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		DWORD* epgInfoListSize,
		EPG_EVENT_INFO** epgInfoList
		);

	//指定サービスの全EPG情報を列挙する
	BOOL EnumEpgInfoList(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		BOOL (CALLBACK *enumEpgInfoListProc)(DWORD, EPG_EVENT_INFO*, LPVOID),
		LPVOID param
		);

	//指定サービスの現在or次のEPG情報を取得する
	//引数：
	// originalNetworkID		[IN]取得対象のoriginalNetworkID
	// transportStreamID		[IN]取得対象のtransportStreamID
	// serviceID				[IN]取得対象のServiceID
	// nextFlag					[IN]TRUE（次の番組）、FALSE（現在の番組）
	// epgInfo					[OUT]EPG情報（DLL内で自動的にdeleteする。次に取得を行うまで有効）
	BOOL GetEpgInfo(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		BOOL nextFlag,
		EPG_EVENT_INFO** epgInfo
		);

	//指定イベントのEPG情報を取得する
	//引数：
	// originalNetworkID		[IN]取得対象のoriginalNetworkID
	// transportStreamID		[IN]取得対象のtransportStreamID
	// serviceID				[IN]取得対象のServiceID
	// EventID					[IN]取得対象のEventID
	// pfOnlyFlag				[IN]p/fからのみ検索するかどうか
	// epgInfo					[OUT]EPG情報（DLL内で自動的にdeleteする。次に取得を行うまで有効）
	BOOL SearchEpgInfo(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		WORD eventID,
		BYTE pfOnlyFlag,
		EPG_EVENT_INFO** epgInfo
		);

	//EPGデータの蓄積状態をリセットする
	void ClearSectionStatus();

	//EPGデータの蓄積状態を取得する
	//戻り値：
	// ステータス
	//引数：
	// l_eitFlag		[IN]L-EITのステータスを取得
	EPG_SECTION_STATUS GetSectionStatus(
		BOOL l_eitFlag
		);

	//指定サービスのEPGデータの蓄積状態を取得する
	//戻り値：
	// ステータス
	//引数：
	// originalNetworkID		[IN]取得対象のOriginalNetworkID
	// transportStreamID		[IN]取得対象のTransportStreamID
	// serviceID				[IN]取得対象のServiceID
	// l_eitFlag				[IN]L-EITのステータスを取得
	EPG_SECTION_STATUS GetSectionStatusService(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		BOOL l_eitFlag
		);

	//取得するロゴタイプをフラグで指定する
	void SetLogoTypeFlags(
		DWORD flags,
		const WORD** additionalNeededPids
		);

	//全ロゴを列挙する
	BOOL EnumLogoList(
		BOOL (CALLBACK *enumLogoListProc)(DWORD, const LOGO_INFO*, LPVOID),
		LPVOID param
		);

	//PC時計を元としたストリーム時間との差を取得する
	//戻り値：
	// 差の秒数
	int GetTimeDelay(
		);

protected:
	recursive_mutex_ utilLock;

	CDecodeUtil decodeUtilClass;
	CEpgDBUtil epgDBUtilClass;

protected:
};
