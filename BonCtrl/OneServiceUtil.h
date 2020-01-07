#pragma once

#include "../Common/ErrDef.h"
#include "../Common/TSPacketUtil.h"
#include "../Common/StringUtil.h"

#include "BonCtrlDef.h"
#include "SendUDP.h"
#include "SendTCP.h"
#include "WriteTSFile.h"
#include "PMTUtil.h"
#include "CATUtil.h"
#include "CreatePMTPacket.h"
#include "CreatePATPacket.h"
#include "DropCount.h"
#include <functional>

class COneServiceUtil
{
public:
	COneServiceUtil(BOOL sendUdpTcp_);
	~COneServiceUtil(void);

	//UDP/TCP���M�p���ǂ����i�s�ϒl�j
	BOOL GetSendUdpTcp() {
		return this->sendUdpTcp;
	}

	//�����Ώ�ServiceID��ݒ�
	//�����F
	// SID			[IN]ServiceID�B0xFFFF�őS�T�[�r�X�ΏہB
	void SetSID(
		WORD SID_
	);

	//�ݒ肳��Ă鏈���Ώۂ�ServiceID���擾
	//�߂�l�F
	// ServiceID
	WORD GetSID();

	//UDP�ő��M���s��
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// sendList		[IN/OUT]���M�惊�X�g�BNULL�Œ�~�BPort�͎��ۂɑ��M�Ɏg�p����Port���Ԃ�B
	BOOL SendUdp(
		vector<NW_SEND_INFO>* sendList
		) {
		return SendUdpTcp(sendList, this->sendUdp, this->udpPortMutex, MUTEX_UDP_PORT_NAME);
	}

	//TCP�ő��M���s��
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// sendList		[IN/OUT]���M�惊�X�g�BNULL�Œ�~�BPort�͎��ۂɑ��M�Ɏg�p����Port���Ԃ�B
	BOOL SendTcp(
		vector<NW_SEND_INFO>* sendList
		) {
		return SendUdpTcp(sendList, this->sendTcp, this->tcpPortMutex, MUTEX_TCP_PORT_NAME);
	}

	//�o�͗pTS�f�[�^�𑗂�
	//�����F
	// data		[IN]TS�f�[�^
	// size		[IN]data�̃T�C�Y
	// funcGetPresent	[IN]EPG�̌��ݔԑgID�𒲂ׂ�֐�
	void AddTSBuff(
		BYTE* data,
		DWORD size,
		const std::function<int(WORD, WORD, WORD)>& funcGetPresent
		);

	void SetPmtPID(
		WORD TSID,
		WORD pmtPID_
		);

	void SetEmmPID(
		const vector<WORD>& pidList
		);

	//�t�@�C���ۑ����J�n����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// recParam				[IN]�ۑ��p�����[�^�ictrlID�t�B�[���h�͖����j
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
	// subRecFlag	[OUT]�����̂Ƃ��A�T�u�^�悪�����������ǂ���
	BOOL EndSave(BOOL* subRecFlag = NULL);

	//�^�撆���ǂ���
	//�߂�l�F
	// TRUE�i�^�撆�j�AFALSE�i���Ă��Ȃ��j
	BOOL IsRec();

	//�X�N�����u�����������̓���ݒ�
	//�����F
	// enable		[IN] TRUE�i��������j�AFALSE�i�������Ȃ��j
	void SetScramble(
		BOOL enable
		) {
		this->enableScramble = enable != FALSE;
	}

	//�X�N�����u�������������s�����ǂ���
	//�߂�l�F
	// ���i��������j�A0�i�������Ȃ��j�A���i���ݒ�j
	int GetScramble() {
		return this->enableScramble;
	}

	//�����ƃf�[�^�����܂߂邩�ǂ���
	//�����F
	// enableCaption		[IN]������ TRUE�i�܂߂�j�AFALSE�i�܂߂Ȃ��j
	// enableData			[IN]�f�[�^������ TRUE�i�܂߂�j�AFALSE�i�܂߂Ȃ��j
	void SetServiceMode(
		BOOL enableCaption,
		BOOL enableData
		);

	//�G���[�J�E���g���N���A����
	void ClearErrCount();

	//�h���b�v�ƃX�N�����u���̃J�E���g���擾����
	//�����F
	// drop				[OUT]�h���b�v��
	// scramble			[OUT]�X�N�����u����
	void GetErrCount(ULONGLONG* drop, ULONGLONG* scramble);


	//�^�撆�̃t�@�C���̃t�@�C���p�X���擾����
	//�߂�l�F
	// �t�@�C���p�X
	wstring GetSaveFilePath();

	//�h���b�v�ƃX�N�����u���̃J�E���g��ۑ�����
	//�����F
	// filePath			[IN]�ۑ��t�@�C����
	// asUtf8			[IN]UTF-8�ŕۑ����邩
	// dropSaveThresh	[IN]�h���b�v��������ȏ�Ȃ�ۑ�����
	// drop				[OUT]�h���b�v��
	void SaveErrCount(
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

	//�^�撆�̃t�@�C���̏o�̓T�C�Y���擾����
	//�����F
	// writeSize			[OUT]�o�̓T�C�Y
	void GetRecWriteSize(
		__int64* writeSize
		);

	void SetBonDriver(
		const wstring& bonDriver
		);
	void SetPIDName(
		WORD pid,
		const wstring& name
		);
	void SetNoLogScramble(
		BOOL noLog
		);
protected:
	BOOL sendUdpTcp;
	WORD SID;

	int enableScramble;

	vector<HANDLE> udpPortMutex;
	vector<HANDLE> tcpPortMutex;

	CSendUDP sendUdp;
	CSendTCP sendTcp;
	CWriteTSFile writeFile;

	vector<BYTE> buff;

	CCreatePATPacket createPat;
	CCreatePMTPacket createPmt;

	WORD pmtPID;
	vector<WORD> emmPIDList;

	CDropCount dropCount;

	WORD lastPMTVer;

	enum { PITTARI_NONE, PITTARI_START, PITTARI_END_CHK, PITTARI_END } pittariState;
	BOOL pittariSubRec;
	SET_CTRL_REC_PARAM pittariRecParam;
	vector<wstring> pittariSaveFolderSub;
	int pittariMaxBuffCount;

protected:
	static BOOL SendUdpTcp(
		vector<NW_SEND_INFO>* sendList,
		CSendNW& sendNW,
		vector<HANDLE>& portMutexList,
		LPCWSTR mutexName
		);
	void StratPittariRec();
	void StopPittariRec();
};

