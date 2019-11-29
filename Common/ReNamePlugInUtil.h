#pragma once

#include "EpgDataCap3Def.h"

class CReNamePlugInUtil
{
public:
	CReNamePlugInUtil() : hModuleConvert(NULL) {}
	~CReNamePlugInUtil() { CloseConvert(); }

#ifdef _WIN32
	//PlugIn�Őݒ肪�K�v�ȏꍇ�A�ݒ�p�̃_�C�A���O�Ȃǂ�\������
	//�߂�l
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// dllPath					[IN]�v���O�C��DLL�p�X
	// parentWnd				[IN]�e�E�C���h�E
	static BOOL ShowSetting(
		const WCHAR* dllPath,
		HWND parentWnd
		);
#endif

	//���͂��ꂽ�\����ƕϊ��p�^�[�������ɁA�^�掞�̃t�@�C�������쐬����i�g���q�܂ށj
	//recName��NULL���͕K�v�ȃT�C�Y��recNamesize�ŕԂ�
	//�ʏ�recNamesize=256�ŌĂяo��
	//�v���O�C���̃o�[�W�����ɉ�����ConvertRecName3��2��1�̏��Ɍ݊��Ăяo�����s��
	//�X���b�h�Z�[�t�ł͂Ȃ�
	//�߂�l
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// info						[IN]�\����
	// dllPattern				[IN]�v���O�C��DLL���A����эŏ���'?'�ɑ����ĕϊ��p�^�[��
	// dllFolder				[IN]�v���O�C��DLL�t�H���_�p�X(dllPattern�����̂܂ܘA�������)
	// recName					[OUT]����
	// recNamesize				[IN/OUT]name�̃T�C�Y(WCHAR�P��)
	BOOL Convert(
		PLUGIN_RESERVE_INFO* info,
		const WCHAR* dllPattern,
		const WCHAR* dllFolder,
		WCHAR* recName,
		DWORD* recNamesize
		);

	//Convert()�ŊJ�������\�[�X������Ε���
	void CloseConvert();

private:
#ifdef _WIN32
	typedef void (WINAPI* SettingRNP)(HWND parentWnd);
#endif
	typedef BOOL (WINAPI* ConvertRecNameRNP)(PLUGIN_RESERVE_INFO* info, WCHAR* recName, DWORD* recNamesize);
	typedef BOOL (WINAPI* ConvertRecName2RNP)(PLUGIN_RESERVE_INFO* info, EPG_EVENT_INFO* epgInfo, WCHAR* recName, DWORD* recNamesize);
	typedef BOOL (WINAPI* ConvertRecName3RNP)(PLUGIN_RESERVE_INFO* info, const WCHAR* pattern, WCHAR* recName, DWORD* recNamesize);

	CReNamePlugInUtil(const CReNamePlugInUtil&);
	CReNamePlugInUtil& operator=(const CReNamePlugInUtil&);
	void* hModuleConvert;
};
