#pragma once

#include "../Common/ThreadUtil.h"
#include <functional>

class IBonDriver2;

class CBonDriverUtil
{
public:
	CBonDriverUtil(void);
	~CBonDriverUtil(void);

	//BonDriver�����[�h���ă`�����l�����Ȃǂ��擾�i�t�@�C�����Ŏw��j
	//�����F
	// bonDriverFolder	[IN]BonDriver�̃t�H���_�p�X
	// bonDriverFile	[IN]BonDriver�̃t�@�C����
	// recvFunc_		[IN]�X�g���[����M���̃R�[���o�b�N�֐�
	// statusFunc_		[IN]�X�e�[�^�X(�V�O�i�����x��,Space,Ch)�擾���̃R�[���o�b�N�֐�
	bool OpenBonDriver(
		LPCWSTR bonDriverFolder,
		LPCWSTR bonDriverFile,
		const std::function<void(BYTE*, DWORD, DWORD)>& recvFunc_,
		const std::function<void(float, int, int)>& statusFunc_,
		int openWait
		);

	//���[�h���Ă���BonDriver�̊J��
	void CloseBonDriver();

	//���[�h����BonDriver�̏��擾
	//Space��Ch�̈ꗗ���擾����
	//�߂�l�F
	// Space��Ch�̈ꗗ�i���X�g�̓Y���������̂܂܃`���[�i�[��Ԃ�`�����l���̔ԍ��ɂȂ�j
	// ������̓`�����l��������̂��̂��X�L�b�v����d�l�Ȃ̂ŁA���p���͂���ɏ]�����ق����ǂ���������Ȃ�
	const vector<pair<wstring, vector<wstring>>>& GetOriginalChList() { return this->loadChList; }

	//BonDriver�̃`���[�i�[�����擾
	//�߂�l�F
	// �`���[�i�[��
	const wstring& GetTunerName() { return this->loadTunerName; }

	//�`�����l���ύX
	//�����F
	// space			[IN]�ύX�`�����l����Space
	// ch				[IN]�ύX�`�����l���̕���Ch
	bool SetCh(
		DWORD space,
		DWORD ch
		);

	//���݂̃`�����l���擾
	//�����F
	// space			[IN]���݂̃`�����l����Space
	// ch				[IN]���݂̃`�����l���̕���Ch
	bool GetNowCh(
		DWORD* space,
		DWORD* ch
		);

	//Open����BonDriver�̃t�@�C�������擾
	//���X���b�h�Z�[�t
	//�߂�l�F
	// BonDriver�̃t�@�C�����i�g���q�܂ށj�iempty�Ŗ�Open�j
	wstring GetOpenBonDriverFileName();

private:
	//BonDriver�ɃA�N�Z�X���郏�[�J�[�X���b�h
	static void DriverThread(CBonDriverUtil* sys);
	//���[�J�[�X���b�h�̃��b�Z�[�W��p�E�B���h�E�v���V�[�W��
	static LRESULT CALLBACK DriverWindowProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);

	static class CInit { public: CInit(); } s_init;
	recursive_mutex_ utilLock;
	wstring loadDllFolder;
	wstring loadDllFileName;
	wstring loadTunerName;
	vector<pair<wstring, vector<wstring>>> loadChList;
	bool initChSetFlag;
	std::function<void(BYTE*, DWORD, DWORD)> recvFunc;
	std::function<void(float, int, int)> statusFunc;
	int statusTimeout;
	IBonDriver2* bon2IF;
	thread_ driverThread;
	HWND hwndDriver;
};

