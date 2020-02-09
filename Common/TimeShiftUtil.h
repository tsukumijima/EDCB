#pragma once
#include "../BonCtrl/SendUDP.h"
#include "../BonCtrl/SendTCP.h"
#include "StructDef.h"
#include "ThreadUtil.h"

class CTimeShiftUtil
{
public:
	CTimeShiftUtil(void);
	~CTimeShiftUtil(void);

	//UDP/TCP���M���s��
	//�߂�l�F
	// �����Fval�ɊJ�n�|�[�g�ԍ��i�I��or���s�F�l�͕s�ρj
	//�����F
	// val		[IN/OUT]���M����
	void Send(
		NWPLAY_PLAY_INFO* val
		);

	//�^�C���V�t�g�p�t�@�C�����J��
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// filePath		[IN]�^�C���V�t�g�p�o�b�t�@�t�@�C���̃p�X
	// fileMode		[IN]�^��ς݃t�@�C���Đ����[�h
	BOOL OpenTimeShift(
		LPCWSTR filePath_,
		BOOL fileMode_
		);

	//�^�C���V�t�g���M���J�n����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	BOOL StartTimeShift();

	//�^�C���V�t�g���M���~����
	void StopTimeShift();

	//���݂̑��M�ʒu�ƗL���ȃt�@�C���T�C�Y���擾����
	//�����F
	// filePos		[OUT]�t�@�C���ʒu
	// fileSize		[OUT]�t�@�C���T�C�Y
	void GetFilePos(__int64* filePos, __int64* fileSize);

	//���M�J�n�ʒu��ύX����
	//�����F
	// filePos		[IN]�t�@�C���ʒu
	void SetFilePos(__int64 filePos);

protected:
	recursive_mutex_ utilLock;
	recursive_mutex_ ioLock;
	CSendUDP sendUdp;
	CSendTCP sendTcp;
	struct SEND_INFO {
		wstring ip;
		DWORD port;
		wstring key;
#ifdef _WIN32
		HANDLE mutex;
#else
		FILE* mutex;
#endif
	} sendInfo[2];

	wstring filePath;
	WORD PCR_PID;

	BOOL fileMode;
	int seekJitter;
	__int64 currentFilePos;

	thread_ readThread;
	atomic_bool_ readStopFlag;
	std::unique_ptr<FILE, decltype(&fclose)> readFile;
	std::unique_ptr<FILE, decltype(&fclose)> seekFile;
protected:
	static void ReadThread(CTimeShiftUtil* sys);
	__int64 GetAvailableFileSize() const;
};

