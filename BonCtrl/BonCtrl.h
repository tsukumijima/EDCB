#pragma once

#include "../Common/StructDef.h"
#include "../Common/EpgTimerUtil.h"
#include "../Common/StringUtil.h"
#include "../Common/ThreadUtil.h"

#include "BonDriverUtil.h"
#include "PacketInit.h"
#include "TSOut.h"
#include "ChSetUtil.h"
#include <list>

class CBonCtrl
{
public:
	//�`�����l���X�L�����AEPG�擾�̃X�e�[�^�X�p
	enum JOB_STATUS {
		ST_STOP = -4,		//��~��
		ST_COMPLETE = -3,	//����
		ST_CANCEL = -2,		//�L�����Z�����ꂽ
		ST_WORKING = -1,	//���s��
	};

	CBonCtrl(void);
	~CBonCtrl(void);

	void ReloadSetting(
		BOOL enableEmm,
		BOOL noLogScramble,
		BOOL parseEpgPostProcess,
		BOOL enableScramble,
		BOOL needCaption,
		BOOL needData,
		BOOL allService
		);

	//�l�b�g���[�N���M�Ɠ��v�̑ΏۃT�[�r�XID���擾����
	WORD GetNWCtrlServiceID() { return this->nwCtrlServiceID; }

	//�l�b�g���[�N���M�Ɠ��v�̑ΏۃT�[�r�XID��ݒ肷��
	//��GetStreamID()�Ŏ󓮓I�ȃ`�����l���ω������o�������ȂǂɎg��
	void SetNWCtrlServiceID(
		WORD serviceID
		);

	//EPG�擾�Ȃǂ̏�Ԃ��X�V����
	//���T��1�b���ƂɌĂ�
	void Check();

	//BonDriver�����[�h���ă`�����l�����Ȃǂ��擾�i�t�@�C�����Ŏw��j
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// bonDriverFile	[IN]BonDriver�̃t�@�C����
	BOOL OpenBonDriver(
		LPCWSTR bonDriverFile,
		int openWait,
		DWORD tsBuffMaxCount
		);

	//���[�h���Ă���BonDriver�̊J��
	void CloseBonDriver();

	//���[�h����BonDriver�̃t�@�C�������擾����i���[�h�������Ă��邩�̔���j
	//���X���b�h�Z�[�t
	//�߂�l�F
	// TRUE�i�����j�FFALSE�iOpen�Ɏ��s���Ă���j
	//�����F
	// bonDriverFile		[OUT]BonDriver�̃t�@�C����(NULL��)
	BOOL GetOpenBonDriver(
		wstring* bonDriverFile
		);

	//�`�����l���ύX
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// space			[IN]�ύX�`�����l����Space
	// ch				[IN]�ύX�`�����l���̕���Ch
	// serviceID		[IN]�ύX�`�����l���̃T�[�r�XID
	BOOL SetCh(
		DWORD space,
		DWORD ch,
		WORD serviceID
		);

	//�`�����l���ύX�����ǂ���
	//���X���b�h�Z�[�t
	//�߂�l�F
	// TRUE�i�ύX���j�AFALSE�i�����j
	BOOL IsChChanging(BOOL* chChgErr);

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

	//�T�[�r�X�ꗗ���擾����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// serviceList				[OUT]�T�[�r�X���̃��X�g
	DWORD GetServiceList(
		vector<CH_DATA4>* serviceList
		);

	//TS�X�g���[������p�R���g���[�����쐬����
	//�߂�l�F
	// ���䎯��ID
	//�����F
	// duplicateNWCtrl		[IN]�l�b�g���[�N���M�Ɠ��v�p�̂��̂Ɠ��������l��K�p���邩�ǂ���
	DWORD CreateServiceCtrl(
		BOOL duplicateNWCtrl
		);

	//TS�X�g���[������p�R���g���[�����쐬����
	//�߂�l�F
	// �G���[�R�[�h
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
	//�����F
	// sendList		[IN/OUT]���M�惊�X�g�BNULL�Œ�~�BPort�͎��ۂɑ��M�Ɏg�p����Port���Ԃ�B
	void SendUdp(
		vector<NW_SEND_INFO>* sendList
		);

	//TCP�ő��M���s��
	//�����F
	// sendList		[IN/OUT]���M�惊�X�g�BNULL�Œ�~�BPort�͎��ۂɑ��M�Ɏg�p����Port���Ԃ�B
	void SendTcp(
		vector<NW_SEND_INFO>* sendList
		);

	//�t�@�C���ۑ����J�n����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// recParam				[IN]�ۑ��p�����[�^
	// saveFolderSub		[IN]HDD�̋󂫂��Ȃ��Ȃ����ꍇ�Ɉꎞ�I�Ɏg�p����t�H���_
	// writeBuffMaxCount	[IN]�o�̓o�b�t�@���
	BOOL StartSave(
		const SET_CTRL_REC_PARAM& recParam,
		const vector<wstring>& saveFolderSub,
		int writeBuffMaxCount
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
	// id			[IN]���䎯��ID
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
	//�����F
	// id					[IN]���䎯��ID
	void ClearErrCount(
		DWORD id
		);

	//�h���b�v�ƃX�N�����u���̃J�E���g���擾����
	//�����F
	// id					[IN]���䎯��ID
	// drop					[OUT]�h���b�v��
	// scramble				[OUT]�X�N�����u����
	void GetErrCount(
		DWORD id,
		ULONGLONG* drop,
		ULONGLONG* scramble
		);

	//�^�撆�̃t�@�C���̃t�@�C���p�X���擾����
	//���X���b�h�Z�[�t
	//�߂�l�F
	// �t�@�C���p�X
	//�����F
	// id					[IN]���䎯��ID
	wstring GetSaveFilePath(
		DWORD id
		) {
		return this->tsOut.GetSaveFilePath(id);
	}

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

	//�^�撆�̃t�@�C���̏o�̓T�C�Y���擾����
	//�����F
	// id					[IN]���䎯��ID
	// writeSize			[OUT]�ۑ��t�@�C����
	void GetRecWriteSize(
		DWORD id,
		__int64* writeSize
		);

	//�w��T�[�r�X�̌���or����EPG�����擾����
	//���X���b�h�Z�[�t
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
		) {
		return this->tsOut.GetEpgInfo(originalNetworkID, transportStreamID, serviceID, nextFlag, epgInfo);
	}

	//�w��C�x���g��EPG�����擾����
	//���X���b�h�Z�[�t
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
		) {
		return this->tsOut.SearchEpgInfo(originalNetworkID, transportStreamID, serviceID, eventID, pfOnlyFlag, epgInfo);
	}
	
	//PC���v�����Ƃ����X�g���[�����ԂƂ̍����擾����
	//���X���b�h�Z�[�t
	//�߂�l�F
	// ���̕b��
	int GetTimeDelay() { return this->tsOut.GetTimeDelay(); }

	//�^�撆���ǂ������擾����
	//���X���b�h�Z�[�t
	// TRUE�i�^�撆�j�AFALSE�i�^�悵�Ă��Ȃ��j
	BOOL IsRec() { return this->tsOut.IsRec(); }

	//�`�����l���X�L�������J�n����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	BOOL StartChScan();

	//�`�����l���X�L�������L�����Z������
	void StopChScan();

	//�`�����l���X�L�����̏�Ԃ��擾����
	//�߂�l�F
	// �X�e�[�^�X
	//�����F
	// space		[OUT]�X�L�������̕���CH��space
	// ch			[OUT]�X�L�������̕���CH��ch
	// chName		[OUT]�X�L�������̕���CH�̖��O
	// chkNum		[OUT]�`�F�b�N�ς݂̐�
	// totalNum		[OUT]�`�F�b�N�Ώۂ̑���
	JOB_STATUS GetChScanStatus(
		DWORD* space,
		DWORD* ch,
		wstring* chName,
		DWORD* chkNum,
		DWORD* totalNum
		);

	//EPG�擾���J�n����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// chList		[IN]EPG�擾����`�����l���ꗗ(NULL��)
	BOOL StartEpgCap(
		const vector<SET_CH_INFO>* chList
		);

	//EPG�擾���~����
	void StopEpgCap(
		);

	//EPG�擾�̃X�e�[�^�X���擾����
	//��info==NULL�̏ꍇ�Ɍ���X���b�h�Z�[�t
	//�߂�l�F
	// �X�e�[�^�X
	//�����F
	// info			[OUT]�擾���̃T�[�r�X
	JOB_STATUS GetEpgCapStatus(
		SET_CH_INFO* info
		);

	//�o�b�N�O���E���h�ł�EPG�擾�ݒ�
	//�����F
	// enableLive	[IN]�������Ɏ擾����
	// enableRec	[IN]�^�撆�Ɏ擾����
	// enableRec	[IN]EPG�擾����`�����l���ꗗ
	// *Basic		[IN]�P�`�����l�������{���̂ݎ擾���邩�ǂ���
	// backStartWaitSec	[IN]Ch�؂�ւ��A�^��J�n��A�o�b�N�O���E���h�ł�EPG�擾���J�n����܂ł̕b��
	void SetBackGroundEpgCap(
		BOOL enableLive,
		BOOL enableRec,
		BOOL BSBasic,
		BOOL CS1Basic,
		BOOL CS2Basic,
		BOOL CS3Basic,
		DWORD backStartWaitSec
		);

	//���݂̃X�g���[���̕\���p�̃X�e�[�^�X���擾����
	//�����F
	// signalLv		[OUT]�V�O�i�����x��
	// space		[OUT]����CH��space(�s���̂Ƃ���)
	// ch			[OUT]����CH��ch(�s���̂Ƃ���)
	// drop			[OUT]�h���b�v��
	// scramble		[OUT]�X�N�����u����
	void GetViewStatusInfo(
		float* signalLv,
		int* space,
		int* ch,
		ULONGLONG* drop,
		ULONGLONG* scramble
		);

protected:
	CBonDriverUtil bonUtil;
	CPacketInit packetInit;
	CTSOut tsOut;
	CChSetUtil chUtil;

	recursive_mutex_ buffLock;
	std::list<vector<BYTE>> tsBuffList;
	std::list<vector<BYTE>> tsFreeList;
	float statusSignalLv;
	int viewSpace;
	int viewCh;

	DWORD nwCtrlID;
	BOOL nwCtrlEnableScramble;
	BOOL nwCtrlNeedCaption;
	BOOL nwCtrlNeedData;
	BOOL nwCtrlAllService;
	WORD nwCtrlServiceID;

	thread_ analyzeThread;
	CAutoResetEvent analyzeEvent;
	atomic_bool_ analyzeStopFlag;

	//�`�����l���X�L�����p
	struct CHK_CH_INFO {
		DWORD space;
		DWORD ch;
		wstring spaceName;
		wstring chName;
	};
	vector<CHK_CH_INFO> chScanChkList;
	int chScanIndexOrStatus;
	DWORD chScanChChgTimeOut;
	DWORD chScanServiceChkTimeOut;
	BOOL chScanChkNext;
	DWORD chScanTick;

	//EPG�擾�p
	//�擾����const����̂�
	vector<SET_CH_INFO> epgCapChList;
	atomic_int_ epgCapIndexOrStatus;
	BOOL epgCapBSBasic;
	BOOL epgCapCS1Basic;
	BOOL epgCapCS2Basic;
	BOOL epgCapCS3Basic;
	BOOL epgCapChkBS;
	BOOL epgCapChkCS1;
	BOOL epgCapChkCS2;
	BOOL epgCapChkCS3;
	BOOL epgCapChkNext;
	int epgCapSetChState;
	DWORD epgCapTimeOut;
	BOOL epgCapSaveTimeOut;
	DWORD epgCapTick;
	DWORD epgCapLastChkTick;

	int epgCapBackIndexOrStatus;
	BOOL enableLiveEpgCap;
	BOOL enableRecEpgCap;

	BOOL epgCapBackBSBasic;
	BOOL epgCapBackCS1Basic;
	BOOL epgCapBackCS2Basic;
	BOOL epgCapBackCS3Basic;
	DWORD epgCapBackStartWaitSec;
protected:
	BOOL ProcessSetCh(
		DWORD space,
		DWORD ch,
		BOOL chScan
		);

	static void GetEpgDataFilePath(WORD ONID, WORD TSID, wstring& epgDataFilePath);

	void RecvCallback(BYTE* data, DWORD size, DWORD remain, DWORD tsBuffMaxCount);
	void StatusCallback(float signalLv, int space, int ch);
	static void AnalyzeThread(CBonCtrl* sys);

	void CheckChScan();
	void CheckEpgCap();

	void StartBackgroundEpgCap();
	void StopBackgroundEpgCap();
	void CheckEpgCapBack();
};

