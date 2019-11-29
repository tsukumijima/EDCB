#pragma once

#include "ParseText.h"
#include "StructDef.h"

//�`�����l�����t�@�C���uChSet4.txt�v�̓ǂݍ��݂ƕۑ��������s��
//�L�[�͓ǂݍ��ݏ��ԍ�
class CParseChText4 : CParseText<DWORD, CH_DATA4>
{
public:
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	//�`�����l������ǉ�����(���s���Ȃ�)�B�߂�l�͒ǉ����ꂽ�L�[
	DWORD AddCh(const CH_DATA4& item);
	//�`�����l�������폜����
	void DelCh(DWORD key);
	//useViewFlag��ݒ肷��
	void SetUseViewFlag(DWORD key, BOOL useViewFlag);
private:
	bool ParseLine(LPCWSTR parseLine, pair<DWORD, CH_DATA4>& item);
	bool SaveLine(const pair<DWORD, CH_DATA4>& item, wstring& saveLine) const;
};

//�`�����l�����t�@�C���uChSet5.txt�v�̓ǂݍ��݂ƕۑ��������s��
//�L�[��ONID<<32|TSID<<16|SID
class CParseChText5 : CParseText<LONGLONG, CH_DATA5>
{
public:
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	LONGLONG AddCh(const CH_DATA5& item);
	//EPG�f�[�^�̎擾�Ώۂ���ݒ肷��
	bool SetEpgCapMode(WORD originalNetworkID, WORD transportStreamID, WORD serviceID, BOOL epgCapFlag);
private:
	bool ParseLine(LPCWSTR parseLine, pair<LONGLONG, CH_DATA5>& item);
	bool SaveLine(const pair<LONGLONG, CH_DATA5>& item, wstring& saveLine) const;
	bool SelectItemToSave(vector<map<LONGLONG, CH_DATA5>::const_iterator>& itemList) const;
	vector<LONGLONG> parsedOrder;
};

//�g���q��Content-Type�̑Ή��t�@�C���uContentTypeText.txt�v�̓ǂݍ��݂��s��
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

//�T�[�r�X���Ƃ���ڂ��J�����_�[�����ǖ��̑Ή��t�@�C���uSyoboiCh.txt�v�̓ǂݍ��݂��s��
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

//�^��ςݏ��t�@�C���uRecInfo.txt�v�̓ǂݍ��݂ƕۑ��������s��
//�L�[��REC_FILE_INFO::id(��0,�i���I)
class CParseRecInfoText : CParseText<DWORD, REC_FILE_INFO>
{
public:
	CParseRecInfoText() : nextID(1), saveNextID(1), keepCount(UINT_MAX), recInfoDelFile(false), customizeDelExt(false) {}
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	//�^��ςݏ���ǉ�����
	DWORD AddRecInfo(const REC_FILE_INFO& item);
	//�^��ςݏ����폜����
	bool DelRecInfo(DWORD id);
	//�t�@�C���p�X��ύX����
	bool ChgPathRecInfo(DWORD id, LPCWSTR recFilePath);
	//�v���e�N�g����ύX����
	bool ChgProtectRecInfo(DWORD id, BYTE flag);
	//AddRecInfo����Ɏc���Ă�����v���e�N�g�̘^��ςݏ��̌���ݒ肷��
	void SetKeepCount(DWORD n = UINT_MAX) { this->keepCount = n; }
	void SetRecInfoDelFile(bool delFile) { this->recInfoDelFile = delFile; }
	void CustomizeDelExt(bool customize) { this->customizeDelExt = customize; }
	void SetCustomDelExt(const vector<wstring>& list) { this->customDelExt = list; }
	void SetRecInfoFolder(LPCWSTR folder);
	wstring GetRecInfoFolder() const { return this->recInfoFolder; }
	//�⑫�̘^������擾����
	static wstring GetExtraInfo(LPCWSTR recFilePath, LPCWSTR extension, const wstring& resultOfGetRecInfoFolder, bool recInfoFolderOnly);
private:
	bool ParseLine(LPCWSTR parseLine, pair<DWORD, REC_FILE_INFO>& item);
	bool SaveLine(const pair<DWORD, REC_FILE_INFO>& item, wstring& saveLine) const;
	bool SaveFooterLine(wstring& saveLine) const;
	bool SelectItemToSave(vector<map<DWORD, REC_FILE_INFO>::const_iterator>& itemList) const;
	bool IsUtf8Default() const { return true; }
	//��񂪍폜����钼�O�̕⑫���
	void OnDelRecInfo(const REC_FILE_INFO& item);
	//�ߋ��ɒǉ�����ID�����傫�Ȓl�B100000000(1��)ID�ŏ��񂷂�(������1����1000ID����Ă�200�N�ȏォ����̂ōl���邾������)
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

//�^��ς݃C�x���g���t�@�C���uRecInfo2.txt�v�̓ǂݍ��݂ƕۑ��������s��
//�L�[�͓ǂݍ��ݏ��ԍ�
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

//�\����t�@�C���uReserve.txt�v�̓ǂݍ��݂ƕۑ��������s��
//�L�[��reserveID(��0,�i���I)
class CParseReserveText : CParseText<DWORD, RESERVE_DATA>
{
public:
	CParseReserveText() : nextID(1), saveNextID(1) {}
	using Base::ParseText;
	using Base::GetMap;
	using Base::GetFilePath;
	using Base::SetFilePath;
	using Base::SaveText;
	//�\�����ǉ�����
	DWORD AddReserve(const RESERVE_DATA& item);
	//�\�����ύX����
	bool ChgReserve(const RESERVE_DATA& item);
	//presentFlag��ύX����(�C�e���[�^�ɉe�����Ȃ�)
	bool SetPresentFlag(DWORD id, BYTE presentFlag);
	//overlapMode��ύX����(�C�e���[�^�ɉe�����Ȃ�)
	bool SetOverlapMode(DWORD id, BYTE overlapMode);
	//ngTunerIDList�ɒǉ�����(�C�e���[�^�ɉe�����Ȃ�)
	bool AddNGTunerID(DWORD id, DWORD tunerID);
	//�\������폜����
	bool DelReserve(DWORD id);
	//�^��J�n�����Ń\�[�g���ꂽ�\��ꗗ���擾����
	vector<pair<LONGLONG, const RESERVE_DATA*>> GetReserveList(BOOL calcMargin = FALSE, int defStartMargin = 0) const;
	//ONID<<48|TSID<<32|SID<<16|EID,�\��ID�Ń\�[�g���ꂽ�\��ꗗ���擾����B�߂�l�͎��̔�const����܂ŗL��
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

//�\����t�@�C���uEpgAutoAdd.txt�v�̓ǂݍ��݂ƕۑ��������s��
//�L�[��dataID(��0,�i���I)
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
	//�\��o�^����ύX����(�C�e���[�^�ɉe�����Ȃ�)
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

//�\����t�@�C���uManualAutoAdd.txt�v�̓ǂݍ��݂ƕۑ��������s��
//�L�[��dataID(��0,�i���I)
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
