﻿#pragma once

#include "ParseText.h"
#include "StructDef.h"

//チャンネル情報ファイル「ChSet4.txt」の読み込みと保存処理を行う
//キーは読み込み順番号
class CParseChText4 : CParseText<DWORD, CH_DATA4>
{
public:
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	//チャンネル情報を追加する(失敗しない)。戻り値は追加されたキー
	DWORD AddCh(const CH_DATA4& item);
	//チャンネル情報を削除する
	void DelCh(DWORD key);
	//useViewFlagを設定する
	void SetUseViewFlag(DWORD key, BOOL useViewFlag);
private:
	bool ParseLine(LPCWSTR parseLine, pair<DWORD, CH_DATA4>& item);
	bool SaveLine(const pair<DWORD, CH_DATA4>& item, wstring& saveLine) const;
};

//チャンネル情報ファイル「ChSet5.txt」の読み込みと保存処理を行う
//キーはONID<<32|TSID<<16|SID
class CParseChText5 : CParseText<LONGLONG, CH_DATA5>
{
public:
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	LONGLONG AddCh(const CH_DATA5& item);
	//EPGデータの取得対象かを設定する
	bool SetEpgCapMode(WORD originalNetworkID, WORD transportStreamID, WORD serviceID, BOOL epgCapFlag);
private:
	bool ParseLine(LPCWSTR parseLine, pair<LONGLONG, CH_DATA5>& item);
	bool SaveLine(const pair<LONGLONG, CH_DATA5>& item, wstring& saveLine) const;
	bool SelectItemToSave(vector<map<LONGLONG, CH_DATA5>::const_iterator>& itemList) const;
	vector<LONGLONG> parsedOrder;
};

//拡張子とContent-Typeの対応ファイル「ContentTypeText.txt」の読み込みを行う
class CParseContentTypeText : CParseText<wstring, wstring>
{
public:
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	void GetMimeType(wstring ext, wstring& mimeType) const;
private:
	bool ParseLine(LPCWSTR parseLine, pair<wstring, wstring>& item);
};

//サービス名としょぼいカレンダー放送局名の対応ファイル「SyoboiCh.txt」の読み込みを行う
class CParseServiceChgText : CParseText<wstring, wstring>
{
public:
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	void ChgText(wstring& chgText) const;
private:
	bool ParseLine(LPCWSTR parseLine, pair<wstring, wstring>& item);
};

//録画済み情報ファイル「RecInfo.txt」の読み込みと保存処理を行う
//キーはREC_FILE_INFO::id(非0,永続的)
class CParseRecInfoText : CParseText<DWORD, REC_FILE_INFO>
{
public:
	CParseRecInfoText() : nextID(1), saveNextID(1), keepCount(UINT_MAX), recInfoDelFile(false), customizeDelExt(false) {}
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	//録画済み情報を追加する
	DWORD AddRecInfo(const REC_FILE_INFO& item);
	//録画済み情報を削除する
	bool DelRecInfo(DWORD id);
	//ファイルパスを変更する
	bool ChgPathRecInfo(DWORD id, LPCWSTR recFilePath);
	//プロテクト情報を変更する
	bool ChgProtectRecInfo(DWORD id, BYTE flag);
	//AddRecInfo直後に残しておく非プロテクトの録画済み情報の個数を設定する
	void SetKeepCount(DWORD n = UINT_MAX) { this->keepCount = n; }
	void SetRecInfoDelFile(bool delFile) { this->recInfoDelFile = delFile; }
	void CustomizeDelExt(bool customize) { this->customizeDelExt = customize; }
	void SetCustomDelExt(const vector<wstring>& list) { this->customDelExt = list; }
	void SetRecInfoFolder(LPCWSTR folder);
	wstring GetRecInfoFolder() const { return this->recInfoFolder; }
	//補足の録画情報を取得する
	static wstring GetExtraInfo(LPCWSTR recFilePath, LPCWSTR extension, const wstring& resultOfGetRecInfoFolder, bool recInfoFolderOnly);
private:
	bool ParseLine(LPCWSTR parseLine, pair<DWORD, REC_FILE_INFO>& item);
	bool SaveLine(const pair<DWORD, REC_FILE_INFO>& item, wstring& saveLine) const;
	bool SaveFooterLine(wstring& saveLine) const;
	bool SelectItemToSave(vector<map<DWORD, REC_FILE_INFO>::const_iterator>& itemList) const;
	bool IsUtf8Default() const { return true; }
	//情報が削除される直前の補足作業
	void OnDelRecInfo(const REC_FILE_INFO& item);
	//過去に追加したIDよりも大きな値。100000000(1億)IDで巡回する(ただし1日に1000ID消費しても200年以上かかるので考えるだけ無駄)
	DWORD nextID;
	DWORD saveNextID;
	DWORD keepCount;
	bool recInfoDelFile;
	bool customizeDelExt;
	vector<wstring> customDelExt;
	wstring recInfoFolder;
};

struct PARSE_REC_INFO2_ITEM
{
	WORD originalNetworkID;
	WORD transportStreamID;
	WORD serviceID;
	SYSTEMTIME startTime;
	wstring eventName;
};

//録画済みイベント情報ファイル「RecInfo2.txt」の読み込みと保存処理を行う
//キーは読み込み順番号
class CParseRecInfo2Text : CParseText<DWORD, PARSE_REC_INFO2_ITEM>
{
public:
	CParseRecInfo2Text() : keepCount(UINT_MAX) {}
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	DWORD Add(const PARSE_REC_INFO2_ITEM& item);
	void SetKeepCount(DWORD n = UINT_MAX) { this->keepCount = n; }
private:
	bool ParseLine(LPCWSTR parseLine, pair<DWORD, PARSE_REC_INFO2_ITEM>& item);
	bool SaveLine(const pair<DWORD, PARSE_REC_INFO2_ITEM>& item, wstring& saveLine) const;
	bool SelectItemToSave(vector<map<DWORD, PARSE_REC_INFO2_ITEM>::const_iterator>& itemList) const;
	bool IsUtf8Default() const { return true; }
	DWORD keepCount;
};

//予約情報ファイル「Reserve.txt」の読み込みと保存処理を行う
//キーはreserveID(非0,永続的)
class CParseReserveText : CParseText<DWORD, RESERVE_DATA>
{
public:
	CParseReserveText() : nextID(1), saveNextID(1) {}
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	//予約情報を追加する
	DWORD AddReserve(const RESERVE_DATA& item);
	//予約情報を変更する
	bool ChgReserve(const RESERVE_DATA& item);
	//presentFlagを変更する(イテレータに影響しない)
	bool SetPresentFlag(DWORD id, BYTE presentFlag);
	//overlapModeを変更する(イテレータに影響しない)
	bool SetOverlapMode(DWORD id, BYTE overlapMode);
	//ngTunerIDListに追加する(イテレータに影響しない)
	bool AddNGTunerID(DWORD id, DWORD tunerID);
	//予約情報を削除する
	bool DelReserve(DWORD id);
	//録画開始日時でソートされた予約一覧を取得する
	vector<pair<LONGLONG, const RESERVE_DATA*>> GetReserveList(BOOL calcMargin = FALSE, int defStartMargin = 0) const;
	//ONID<<48|TSID<<32|SID<<16|EID,予約IDでソートされた予約一覧を取得する。戻り値は次の非const操作まで有効
	const vector<pair<ULONGLONG, DWORD>>& GetSortByEventList() const;
private:
	bool ParseLine(LPCWSTR parseLine, pair<DWORD, RESERVE_DATA>& item);
	bool SaveLine(const pair<DWORD, RESERVE_DATA>& item, wstring& saveLine) const;
	bool SaveFooterLine(wstring& saveLine) const;
	bool SelectItemToSave(vector<map<DWORD, RESERVE_DATA>::const_iterator>& itemList) const;
	bool IsUtf8Default() const { return true; }
	DWORD nextID;
	DWORD saveNextID;
	mutable vector<pair<ULONGLONG, DWORD>> sortByEventCache;
};

//予約情報ファイル「EpgAutoAdd.txt」の読み込みと保存処理を行う
//キーはdataID(非0,永続的)
class CParseEpgAutoAddText : CParseText<DWORD, EPG_AUTO_ADD_DATA>
{
public:
	CParseEpgAutoAddText() : nextID(1), saveNextID(1) {}
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	DWORD AddData(const EPG_AUTO_ADD_DATA& item);
	bool ChgData(const EPG_AUTO_ADD_DATA& item);
	//予約登録数を変更する(イテレータに影響しない)
	bool SetAddCount(DWORD id, DWORD addCount);
	bool DelData(DWORD id);
private:
	bool ParseLine(LPCWSTR parseLine, pair<DWORD, EPG_AUTO_ADD_DATA>& item);
	bool SaveLine(const pair<DWORD, EPG_AUTO_ADD_DATA>& item, wstring& saveLine) const;
	bool SaveFooterLine(wstring& saveLine) const;
	bool SelectItemToSave(vector<map<DWORD, EPG_AUTO_ADD_DATA>::const_iterator>& itemList) const;
	bool IsUtf8Default() const { return true; }
	DWORD nextID;
	DWORD saveNextID;
};

//予約情報ファイル「ManualAutoAdd.txt」の読み込みと保存処理を行う
//キーはdataID(非0,永続的)
class CParseManualAutoAddText : CParseText<DWORD, MANUAL_AUTO_ADD_DATA>
{
public:
	CParseManualAutoAddText() : nextID(1), saveNextID(1) {}
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	DWORD AddData(const MANUAL_AUTO_ADD_DATA& item);
	bool ChgData(const MANUAL_AUTO_ADD_DATA& item);
	bool DelData(DWORD id);
private:
	bool ParseLine(LPCWSTR parseLine, pair<DWORD, MANUAL_AUTO_ADD_DATA>& item);
	bool SaveLine(const pair<DWORD, MANUAL_AUTO_ADD_DATA>& item, wstring& saveLine) const;
	bool SaveFooterLine(wstring& saveLine) const;
	bool SelectItemToSave(vector<map<DWORD, MANUAL_AUTO_ADD_DATA>::const_iterator>& itemList) const;
	bool IsUtf8Default() const { return true; }
	DWORD nextID;
	DWORD saveNextID;
};
