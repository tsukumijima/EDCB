#pragma once

class CWritePlugInUtil
{
public:
	CWritePlugInUtil(void);
	~CWritePlugInUtil(void);

	//DLL�̏�����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// loadDllFilePath		[IN]���[�h����DLL�p�X
	BOOL Initialize(
		LPCWSTR loadDllFilePath
		);

	//DLL�̉��
	void UnInitialize(
		);

	//PlugIn�̖��O���擾����
	//name��NULL���͕K�v�ȃT�C�Y��nameSize�ŕԂ�
	//�ʏ�nameSize=256�ŌĂяo��
	//�߂�l
	// TRUE�i�����j�AFALSE�i���s�j�AERR_NOT_INIT�i���������j
	//�����F
	// name						[OUT]����
	// nameSize					[IN/OUT]name�̃T�C�Y(WCHAR�P��)
	BOOL GetName(
		WCHAR* name,
		DWORD* nameSize
		);

	//PlugIn�Őݒ肪�K�v�ȏꍇ�A�ݒ�p�̃_�C�A���O�Ȃǂ�\������
	//�����F
	// parentWnd				[IN]�e�E�C���h�E
	void ShowSetting(
		HWND parentWnd
		);

	//�t�@�C���ۑ����J�n����
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j�AERR_NOT_INIT�i���������j
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
	// TRUE�i�����j�AFALSE�i���s�j�AERR_NOT_INIT�i���������j
	BOOL Stop(
		);

	//���ۂɕۑ����Ă���t�@�C���p�X���擾����i�Đ���o�b�`�����ɗ��p�����j
	//filePath��NULL���͕K�v�ȃT�C�Y��filePathSize�ŕԂ�
	//�ʏ�filePathSize=512�ŌĂяo��
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j�AERR_NOT_INIT�i���������j
	//�����F
	// filePath				[OUT]�ۑ��t�@�C���t���p�X
	// filePathSize			[IN/OUT]filePath�̃T�C�Y(WCHAR�P��)
	BOOL GetSavePath(
		WCHAR* filePath,
		DWORD* filePathSize
		);

	//�ۑ��pTS�f�[�^�𑗂�
	//�󂫗e�ʕs���Ȃǂŏ����o�����s�����ꍇ�AwriteSize�̒l������
	//�ēx�ۑ���������Ƃ��̑��M�J�n�n�_�����߂�
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j�AERR_NOT_INIT�i���������j
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
	typedef BOOL (WINAPI *GetPlugInNameWP)(WCHAR* name, DWORD* nameSize);
	typedef void (WINAPI *SettingWP)(HWND parentWnd);
	typedef BOOL (WINAPI *CreateCtrlWP)(DWORD* id);
	typedef BOOL (WINAPI *DeleteCtrlWP)(DWORD id);
	typedef BOOL (WINAPI *StartSaveWP)(DWORD id, LPCWSTR fileName, BOOL overWriteFlag, ULONGLONG createSize);
	typedef BOOL (WINAPI *StopSaveWP)(DWORD id);
	typedef BOOL (WINAPI *GetSaveFilePathWP)(DWORD id, WCHAR* filePath, DWORD* filePathSize);
	typedef BOOL (WINAPI *AddTSBuffWP)(DWORD id, BYTE* data, DWORD size, DWORD* writeSize);

	void* module;
	DWORD id;

	GetPlugInNameWP				pfnGetPlugInNameWP;
	SettingWP					pfnSettingWP;
	DeleteCtrlWP				pfnDeleteCtrlWP;
	StartSaveWP					pfnStartSaveWP;
	StopSaveWP					pfnStopSaveWP;
	GetSaveFilePathWP			pfnGetSaveFilePathWP;
	AddTSBuffWP					pfnAddTSBuffWP;

	CWritePlugInUtil(const CWritePlugInUtil&);
	CWritePlugInUtil& operator=(const CWritePlugInUtil&);
};

