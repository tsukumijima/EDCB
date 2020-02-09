#pragma once

#include "../Common/StructDef.h"
#include "../Common/PathUtil.h"
#include "../Common/StringUtil.h"
#include "../Common/ErrDef.h"
#include "../Common/EpgDataCap3Util.h"
#include "../Common/TSPacketUtil.h"
#include "../Common/ThreadUtil.h"

#include "BonCtrlDef.h"
#include "ScrambleDecoderUtil.h"
#include "ServiceFilter.h"
#include "OneServiceUtil.h"
#include <functional>

class CTSOut
{
public:
	CTSOut(void);
	~CTSOut(void);

	void SetChChangeEvent(BOOL resetEpgUtil = FALSE);
	BOOL IsChUnknown(DWORD* elapsedTime = NULL);

	//���݂̃X�g���[����ID���擾����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// ONID		[OUT]originalNetworkID
	// TSID		[OUT]transportStreamID
	BOOL GetStreamID(
		WORD* ONID,
		WORD* TSID
		);

	void AddTSBuff(BYTE* data, DWORD dataSize);

	//EMM�����̓���ݒ�
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// enable		[IN] TRUE�i��������j�AFALSE�i�������Ȃ��j
	BOOL SetEmm(
		BOOL enable
		);

	//EMM�������s������
	//�߂�l�F
	// ������
	DWORD GetEmmCount();

	//DLL�̃��[�h��Ԃ��擾
	//�߂�l�F
	// TRUE�i���[�h�ɐ������Ă���j�AFALSE�i���[�h�Ɏ��s���Ă���j
	//�����F
	// loadErrDll		[OUT]���[�h�Ɏ��s����DLL�t�@�C����
	BOOL GetLoadStatus(
		wstring& loadErrDll
		);

	//EPG�f�[�^�̕ۑ����J�n����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	BOOL StartSaveEPG(
		const wstring& epgFilePath_
		);

	//EPG�f�[�^�̕ۑ����I������
	//�����F
	// copy			[IN]tmp����R�s�[�����s�����ǂ���
	void StopSaveEPG(
		BOOL copy
		);

	//EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
	//�߂�l�F
	// �X�e�[�^�X
	//�����F
	// l_eitFlag		[IN]L-EIT�̃X�e�[�^�X���擾
	EPG_SECTION_STATUS GetSectionStatus(
		BOOL l_eitFlag
		);

	//�w��T�[�r�X��EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
	//�߂�l�F
	// �X�e�[�^�X,�擾�ł������ǂ���
	//�����F
	// originalNetworkID		[IN]�擾�Ώۂ�OriginalNetworkID
	// transportStreamID		[IN]�擾�Ώۂ�TransportStreamID
	// serviceID				[IN]�擾�Ώۂ�ServiceID
	// l_eitFlag				[IN]L-EIT�̃X�e�[�^�X���擾
	pair<EPG_SECTION_STATUS, BOOL> GetSectionStatusService(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		BOOL l_eitFlag
		);

	//���X�g���[���̃T�[�r�X�ꗗ���擾����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// funcGetList		[IN]�߂�l��NO_ERR�̂Ƃ��T�[�r�X���̌��Ƃ��̃��X�g�������Ƃ��ČĂяo�����֐�
	DWORD GetServiceListActual(
		const std::function<void(DWORD, SERVICE_INFO*)>& funcGetList
		);

	//TS�X�g���[������p�R���g���[�����쐬����
	//�߂�l�F
	// ���䎯��ID
	//�����F
	// sendUdpTcp	[IN]UDP/TCP���M�p�ɂ���
	DWORD CreateServiceCtrl(
		BOOL sendUdpTcp
		);

	//TS�X�g���[������p�R���g���[�����폜����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s
	//�����F
	// id			[IN]���䎯��ID
	BOOL DeleteServiceCtrl(
		DWORD id
		);

	//����Ώۂ̃T�[�r�X��ݒ肷��
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s
	//�����F
	// id			[IN]���䎯��ID
	// serviceID	[IN]�ΏۃT�[�r�XID�A0xFFFF�őS�T�[�r�X�Ώ�
	BOOL SetServiceID(
		DWORD id,
		WORD serviceID
		);

	BOOL GetServiceID(
		DWORD id,
		WORD* serviceID
		);

	//UDP�ő��M���s��
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// id			[IN]���䎯��ID
	// sendList		[IN/OUT]���M�惊�X�g�BNULL�Œ�~�BPort�͎��ۂɑ��M�Ɏg�p����Port���Ԃ�B
	BOOL SendUdp(
		DWORD id,
		vector<NW_SEND_INFO>* sendList
		);

	//TCP�ő��M���s��
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// id			[IN]���䎯��ID
	// sendList		[IN/OUT]���M�惊�X�g�BNULL�Œ�~�BPort�͎��ۂɑ��M�Ɏg�p����Port���Ԃ�B
	BOOL SendTcp(
		DWORD id,
		vector<NW_SEND_INFO>* sendList
		);

	//�t�@�C���ۑ����J�n����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// recParam				[IN]�ۑ��p�����[�^
	// saveFolderSub		[IN]HDD�̋󂫂��Ȃ��Ȃ����ꍇ�Ɉꎞ�I�Ɏg�p����t�H���_
	// maxBuffCount			[IN]�o�̓o�b�t�@���
	BOOL StartSave(
		const SET_CTRL_REC_PARAM& recParam,
		const vector<wstring>& saveFolderSub,
		int maxBuffCount
	);

	//�t�@�C���ۑ����I������
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// id			[IN]���䎯��ID
	// subRecFlag	[OUT]�����̂Ƃ��A�T�u�^�悪�����������ǂ���
	BOOL EndSave(
		DWORD id,
		BOOL* subRecFlag = NULL
		);

	//�X�N�����u�����������̓���ݒ�
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// enable		[IN] TRUE�i��������j�AFALSE�i�������Ȃ��j
	BOOL SetScramble(
		DWORD id,
		BOOL enable
		);

	//�����ƃf�[�^�����܂߂邩�ǂ���
	//�����F
	// id					[IN]���䎯��ID
	// enableCaption		[IN]������ TRUE�i�܂߂�j�AFALSE�i�܂߂Ȃ��j
	// enableData			[IN]�f�[�^������ TRUE�i�܂߂�j�AFALSE�i�܂߂Ȃ��j
	void SetServiceMode(
		DWORD id,
		BOOL enableCaption,
		BOOL enableData
		);

	//�G���[�J�E���g���N���A����
	void ClearErrCount(
		DWORD id
		);

	//�h���b�v�ƃX�N�����u���̃J�E���g���擾����
	//�����F
	// drop				[OUT]�h���b�v��
	// scramble			[OUT]�X�N�����u����
	void GetErrCount(
		DWORD id,
		ULONGLONG* drop,
		ULONGLONG* scramble
		);

	//�^�撆�̃t�@�C���̏o�̓T�C�Y���擾����
	//�����F
	// id					[IN]���䎯��ID
	// writeSize			[OUT]�o�̓T�C�Y
	void GetRecWriteSize(
		DWORD id,
		__int64* writeSize
		);

	//�w��T�[�r�X�̌���or����EPG�����擾����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
	// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
	// serviceID				[IN]�擾�Ώۂ�ServiceID
	// nextFlag					[IN]TRUE�i���̔ԑg�j�AFALSE�i���݂̔ԑg�j
	// epgInfo					[OUT]EPG���
	DWORD GetEpgInfo(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		BOOL nextFlag,
		EPGDB_EVENT_INFO* epgInfo
		);
	
	//�w��C�x���g��EPG�����擾����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
	// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
	// serviceID				[IN]�擾�Ώۂ�ServiceID
	// eventID					[IN]�擾�Ώۂ�EventID
	// pfOnlyFlag				[IN]p/f����̂݌������邩�ǂ���
	// epgInfo					[OUT]EPG���
	DWORD SearchEpgInfo(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		WORD eventID,
		BYTE pfOnlyFlag,
		EPGDB_EVENT_INFO* epgInfo
		);

	//PC���v�����Ƃ����X�g���[�����ԂƂ̍����擾����
	//�߂�l�F
	// ���̕b��
	int GetTimeDelay(
		);
	
	//�^�撆���ǂ���
	//�߂�l�F
	// TRUE�i�^�撆�j�AFALSE�i���Ă��Ȃ��j
	BOOL IsRec();

	//�^�撆�̃t�@�C���̃t�@�C���p�X���擾����
	//�߂�l�F
	// �t�@�C���p�X
	//�����F
	// id					[IN]���䎯��ID
	wstring GetSaveFilePath(
		DWORD id
		);

	//�h���b�v�ƃX�N�����u���̃J�E���g��ۑ�����
	//�����F
	// id					[IN]���䎯��ID
	// filePath				[IN]�ۑ��t�@�C����
	// asUtf8				[IN]UTF-8�ŕۑ����邩
	// dropSaveThresh		[IN]�h���b�v��������ȏ�Ȃ�ۑ�����
	// drop					[OUT]�h���b�v��
	void SaveErrCount(
		DWORD id,
		const wstring& filePath,
		BOOL asUtf8,
		int dropSaveThresh,
		int scrambleSaveThresh,
		ULONGLONG& drop,
		ULONGLONG& scramble
		);

	void SetSignalLevel(
		float signalLv
		);

	void SetBonDriver(
		const wstring& bonDriver
		);

	void SetNoLogScramble(
		BOOL noLog
		);

	void SetParseEpgPostProcess(
		BOOL parsePost
		);
protected:
	//objLock->epgUtilLock�̏��Ƀ��b�N����
	recursive_mutex_ objLock;
	recursive_mutex_ epgUtilLock;

	CEpgDataCap3Util epgUtil;
	CScrambleDecoderUtil decodeUtil;

	enum { CH_ST_INIT, CH_ST_WAIT_PAT, CH_ST_WAIT_PAT2, CH_ST_WAIT_ID, CH_ST_DONE } chChangeState;
	DWORD chChangeTime;
	WORD lastONID;
	WORD lastTSID;

	vector<BYTE> decodeBuff;

	BOOL enableDecodeFlag;
	BOOL emmEnableFlag;

	map<DWORD, std::unique_ptr<COneServiceUtil>> serviceUtilMap; //�L�[����ID
	CServiceFilter serviceFilter;

	DWORD nextCtrlID;

	std::unique_ptr<FILE, decltype(&fclose)> epgFile;
	enum { EPG_FILE_ST_NONE, EPG_FILE_ST_PAT, EPG_FILE_ST_TOT, EPG_FILE_ST_ALL } epgFileState;
	__int64 epgFileTotPos;
	wstring epgFilePath;
	wstring epgTempFilePath;

	wstring bonFile;
	BOOL noLogScramble;
	BOOL parseEpgPostProcess;
protected:
	void ParseEpgPacket(BYTE* data, const CTSPacketUtil& packet);

	void UpdateServiceUtil(BOOL updateFilterSID);

	DWORD GetNextID();

	BOOL UpdateEnableDecodeFlag();

	void ResetErrCount();

	void OnChChanged(WORD onid, WORD tsid);
};

