#pragma once

#include "../../Common/PathUtil.h"
#include "../../Common/StringUtil.h"
#include "../../Common/WritePlugInUtil.h"
#include "../../BonCtrl/PacketInit.h"
#include "../../BonCtrl/ServiceFilter.h"

class CWriteMain
{
public:
	CWriteMain();
	~CWriteMain();

	//�t�@�C���ۑ����J�n����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// fileName				[IN]�ۑ��t�@�C���t���p�X�i�K�v�ɉ����Ċg���q�ς�����ȂǍs���j
	// overWriteFlag		[IN]����t�@�C�������ݎ��ɏ㏑�����邩�ǂ����iTRUE�F����AFALSE�F���Ȃ��j
	// createSize			[IN]���͗\�z�e�ʁi188�o�C�gTS�ł̗e�ʁB�����^�掞�ȂǑ����Ԗ���̏ꍇ��0�B�����Ȃǂ̉\��������̂Ŗڈ����x�j
	BOOL Start(
		LPCWSTR fileName,
		BOOL overWriteFlag,
		ULONGLONG createSize
		);

	//�t�@�C���ۑ����I������
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	BOOL Stop(
		);

	//���ۂɕۑ����Ă���t�@�C���p�X���擾����i�Đ���o�b�`�����ɗ��p�����j
	//�߂�l�F
	// �ۑ��t�@�C���t���p�X
	wstring GetSavePath(
		);

	//�ۑ��pTS�f�[�^�𑗂�
	//�󂫗e�ʕs���Ȃǂŏ����o�����s�����ꍇ�AwriteSize�̒l������
	//�ēx�ۑ���������Ƃ��̑��M�J�n�n�_�����߂�
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// data					[IN]TS�f�[�^
	// size					[IN]data�̃T�C�Y
	// writeSize			[OUT]�ۑ��ɗ��p�����T�C�Y
	BOOL Write(
		BYTE* data,
		DWORD size,
		DWORD* writeSize
		);

private:
	std::unique_ptr<FILE, decltype(&fclose)> file;
	std::unique_ptr<CWritePlugInUtil> writePlugin;
	wstring savePath;
	WORD targetSID;
	WORD lastTSID;
	CPacketInit packetInit;
	CServiceFilter serviceFilter;
	vector<BYTE> outBuff;
};

