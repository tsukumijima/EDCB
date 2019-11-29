// Write_OneService.cpp : DLL �A�v���P�[�V�����p�ɃG�N�X�|�[�g�����֐����`���܂��B
//

#include "stdafx.h"

#include "WriteMain.h"
#include "../../Common/InstanceManager.h"
#include <shellapi.h>

CInstanceManager<CWriteMain> g_instMng;

extern HINSTANCE g_instance;

#define PLUGIN_NAME L"�T�[�r�X�w��o�� PlugIn"
#define DLL_EXPORT extern "C" __declspec(dllexport)


//PlugIn�̖��O���擾����
//name��NULL���͕K�v�ȃT�C�Y��nameSize�ŕԂ�
//�ʏ�nameSize=256�ŌĂяo��
//�߂�l
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// name						[OUT]����
// nameSize					[IN/OUT]name�̃T�C�Y(WCHAR�P��)
DLL_EXPORT
BOOL WINAPI GetPlugInName(
	WCHAR* name,
	DWORD* nameSize
	)
{
	if( name == NULL ){
		if( nameSize == NULL ){
			return FALSE;
		}else{
			*nameSize = (DWORD)wcslen(PLUGIN_NAME)+1;
		}
	}else{
		if( nameSize == NULL ){
			return FALSE;
		}else{
			if( *nameSize < (DWORD)wcslen(PLUGIN_NAME)+1 ){
				*nameSize = (DWORD)wcslen(PLUGIN_NAME)+1;
				return FALSE;
			}else{
				wcscpy_s(name, *nameSize, PLUGIN_NAME);
			}
		}
	}
	return TRUE;
}

//PlugIn�Őݒ肪�K�v�ȏꍇ�A�ݒ�p�̃_�C�A���O�Ȃǂ�\������
//�����F
// parentWnd				[IN]�e�E�C���h�E
DLL_EXPORT
void WINAPI Setting(
	HWND parentWnd
	)
{
	{
		fs_path iniPath = GetModuleIniPath(g_instance);
		if( GetPrivateProfileToString(L"SET", L"WritePlugin", L"*", iniPath.c_str()) == L"*" ){
			WritePrivateProfileString(L"SET", L"WritePlugin", L";Write_Default.dll", iniPath.c_str());
		}
		ShellExecute(NULL, L"edit", iniPath.c_str(), NULL, NULL, SW_SHOWNORMAL);
	}
}

//////////////////////////////////////////////////////////
//��{�I�ȕۑ�����API�̌Ă΂��
//CreateCtrl
//��
//StartSave
//��
//GetSaveFilePath
//��
//AddTSBuff�i���[�v�j
//���i�^�掞�ԏI������j
//StopSave
//��
//DeleteCtrl
//
//AddTSBuff��FALSE���Ԃ��Ă����ꍇ�i�󂫗e�ʂȂ��Ȃ����Ȃǁj
//AddTSBuff
//���iFALSE�j
//StopSave
//��
//StartSave
//��
//GetSaveFilePath
//��
//AddTSBuff�i���[�v�j
//���i�^�掞�ԏI������j
//StopSave
//��
//DeleteCtrl

//�����ۑ��Ή��̂��߃C���X�^���X��V�K�ɍ쐬����
//�����Ή��ł��Ȃ��ꍇ�͂��̎��_�ŃG���[�Ƃ���
//�߂�l
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// id				[OUT]����ID
DLL_EXPORT
BOOL WINAPI CreateCtrl(
	DWORD* id
	)
{
	if( id == NULL ){
		return FALSE;
	}

	try{
		std::shared_ptr<CWriteMain> ptr = std::make_shared<CWriteMain>();
		*id = g_instMng.push(ptr);
	}catch( std::bad_alloc& ){
		return FALSE;
	}

	return TRUE;
}

//�C���X�^���X���폜����
//�߂�l
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// id				[IN]����ID
DLL_EXPORT
BOOL WINAPI DeleteCtrl(
	DWORD id
	)
{
	if( g_instMng.pop(id) == NULL ){
		return FALSE;
	}

	return TRUE;
}

//�t�@�C���ۑ����J�n����
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// id					[IN]����ID
// fileName				[IN]�ۑ��t�@�C���t���p�X�i�K�v�ɉ����Ċg���q�ς�����ȂǍs���j
// overWriteFlag		[IN]����t�@�C�������ݎ��ɏ㏑�����邩�ǂ����iTRUE�F����AFALSE�F���Ȃ��j
// createSize			[IN]���͗\�z�e�ʁi188�o�C�gTS�ł̗e�ʁB�����^�掞�ȂǑ����Ԗ���̏ꍇ��0�B�����Ȃǂ̉\��������̂Ŗڈ����x�j
DLL_EXPORT
BOOL WINAPI StartSave(
	DWORD id,
	LPCWSTR fileName,
	BOOL overWriteFlag,
	ULONGLONG createSize
	)
{
	std::shared_ptr<CWriteMain> ptr = g_instMng.find(id);
	if( ptr == NULL ){
		return FALSE;
	}

	return ptr->Start(fileName, overWriteFlag, createSize);
}

//�t�@�C���ۑ����I������
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// id					[IN]����ID
DLL_EXPORT
BOOL WINAPI StopSave(
	DWORD id
	)
{
	std::shared_ptr<CWriteMain> ptr = g_instMng.find(id);
	if( ptr == NULL ){
		return FALSE;
	}

	return ptr->Stop();
}

//���ۂɕۑ����Ă���t�@�C���p�X���擾����i�Đ���o�b�`�����ɗ��p�����j
//filePath��NULL���͕K�v�ȃT�C�Y��filePathSize�ŕԂ�
//�ʏ�filePathSize=512�ŌĂяo��
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// id					[IN]����ID
// filePath				[OUT]�ۑ��t�@�C���t���p�X
// filePathSize			[IN/OUT]filePath�̃T�C�Y(WCHAR�P��)
DLL_EXPORT
BOOL WINAPI GetSaveFilePath(
	DWORD id,
	WCHAR* filePath,
	DWORD* filePathSize
	)
{
	std::shared_ptr<CWriteMain> ptr = g_instMng.find(id);
	if( ptr == NULL ){
		return FALSE;
	}

	if( filePathSize == NULL ){
		return FALSE;
	}else if( filePath == NULL ){
		*filePathSize = (DWORD)ptr->GetSavePath().size() + 1;
	}else if( *filePathSize < (DWORD)ptr->GetSavePath().size() + 1 ){
		*filePathSize = (DWORD)ptr->GetSavePath().size() + 1;
		return FALSE;
	}else{
		wcscpy_s(filePath, *filePathSize, ptr->GetSavePath().c_str());
	}
	return TRUE;
}

//�ۑ��pTS�f�[�^�𑗂�
//�󂫗e�ʕs���Ȃǂŏ����o�����s�����ꍇ�AwriteSize�̒l������
//�ēx�ۑ���������Ƃ��̑��M�J�n�n�_�����߂�
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// id					[IN]����ID
// data					[IN]TS�f�[�^
// size					[IN]data�̃T�C�Y
// writeSize			[OUT]�ۑ��ɗ��p�����T�C�Y
DLL_EXPORT
BOOL WINAPI AddTSBuff(
	DWORD id,
	BYTE* data,
	DWORD size,
	DWORD* writeSize
	)
{
	std::shared_ptr<CWriteMain> ptr = g_instMng.find(id);
	if( ptr == NULL ){
		return FALSE;
	}

	return ptr->Write(data, size, writeSize);
}
