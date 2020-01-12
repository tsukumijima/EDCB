
// EpgDataCap_Bon.cpp : �A�v���P�[�V�����̃N���X������`���܂��B
//

#include "stdafx.h"
#include "EpgDataCap_Bon.h"
#include "EpgDataCap_BonDlg.h"

#include "../../Common/ThreadUtil.h"
#include <objbase.h>
#include <shellapi.h>

#ifndef SUPPRESS_OUTPUT_STACK_TRACE
#include <tlhelp32.h>
#include <dbghelp.h>
#pragma comment(lib, "dbghelp.lib")
#endif

namespace
{

FILE* g_debugLog;
recursive_mutex_ g_debugLogLock;

#ifndef SUPPRESS_OUTPUT_STACK_TRACE
// ��O�ɂ���ăA�v���P�[�V�������I�����钼�O�ɃX�^�b�N�g���[�X��"���s�t�@�C����.exe.err"�ɏo�͂���
// �f�o�b�O���(.pdb�t�@�C��)�����݂���Ώo�͂͂��ڍׂɂȂ�

void OutputStackTrace(DWORD exceptionCode, const PVOID* addrOffsets)
{
	WCHAR path[MAX_PATH + 4];
	path[GetModuleFileName(NULL, path, MAX_PATH)] = L'\0';
	wcscat_s(path, L".err");
	HANDLE hFile = CreateFile(path, GENERIC_WRITE, 0, NULL, CREATE_ALWAYS, FILE_ATTRIBUTE_NORMAL, NULL);
	if( hFile != INVALID_HANDLE_VALUE ){
		char buff[384];
		DWORD written;
		int len = sprintf_s(buff, "ExceptionCode = 0x%08X\r\n", exceptionCode);
		WriteFile(hFile, buff, len, &written, NULL);
		for( int i = 0; addrOffsets[i]; i++ ){
			SYMBOL_INFO symbol[1 + (256 + sizeof(SYMBOL_INFO)) / sizeof(SYMBOL_INFO)];
			symbol->SizeOfStruct = sizeof(SYMBOL_INFO);
			symbol->MaxNameLen = 256;
			DWORD64 displacement;
			if( SymFromAddr(GetCurrentProcess(), (DWORD64)addrOffsets[i], &displacement, symbol) ){
				len = sprintf_s(buff, "Trace%02d 0x%p = 0x%p(%s) + 0x%X\r\n", i, addrOffsets[i], (PVOID)symbol->Address, symbol->Name, (DWORD)displacement);
			}else{
				len = sprintf_s(buff, "Trace%02d 0x%p = ?\r\n", i, addrOffsets[i]);
			}
			WriteFile(hFile, buff, len, &written, NULL);
		}
		HANDLE hSnapshot = CreateToolhelp32Snapshot(TH32CS_SNAPMODULE, 0);
		if( hSnapshot != INVALID_HANDLE_VALUE ){
			MODULEENTRY32W modent;
			modent.dwSize = sizeof(modent);
			if( Module32FirstW(hSnapshot, &modent) ){
				do{
					char moduleA[256] = {};
					for( int i = 0; i == 0 || i < 255 && moduleA[i - 1]; i++ ){
						//�����������Ă��\��Ȃ�
						moduleA[i] = (char)modent.szModule[i];
					}
					len = sprintf_s(buff, "0x%p - 0x%p = %s\r\n", modent.modBaseAddr, modent.modBaseAddr + modent.modBaseSize - 1, moduleA);
					WriteFile(hFile, buff, len, &written, NULL);
				}while( Module32NextW(hSnapshot, &modent) );
			}
			CloseHandle(hSnapshot);
		}
		CloseHandle(hFile);
	}
}

LONG WINAPI TopLevelExceptionFilter(_EXCEPTION_POINTERS* exceptionInfo)
{
	static struct {
		LONG used;
		CONTEXT contextRecord;
		STACKFRAME64 stackFrame;
		PVOID addrOffsets[32];
	} work;

	if( InterlockedExchange(&work.used, 1) == 0 ){
		SymSetOptions(SYMOPT_UNDNAME | SYMOPT_DEFERRED_LOADS);
		if( SymInitialize(GetCurrentProcess(), NULL, TRUE) ){
			work.addrOffsets[0] = exceptionInfo->ExceptionRecord->ExceptionAddress;
			work.contextRecord = *exceptionInfo->ContextRecord;
			work.stackFrame.AddrPC.Mode = AddrModeFlat;
			work.stackFrame.AddrFrame.Mode = AddrModeFlat;
			work.stackFrame.AddrStack.Mode = AddrModeFlat;
#if defined(_M_IX86) || defined(_M_X64)
#ifdef _M_X64
			work.stackFrame.AddrPC.Offset = work.contextRecord.Rip;
			work.stackFrame.AddrFrame.Offset = work.contextRecord.Rbp;
			work.stackFrame.AddrStack.Offset = work.contextRecord.Rsp;
#else
			work.stackFrame.AddrPC.Offset = work.contextRecord.Eip;
			work.stackFrame.AddrFrame.Offset = work.contextRecord.Ebp;
			work.stackFrame.AddrStack.Offset = work.contextRecord.Esp;
#endif
			for( int i = 1; i < _countof(work.addrOffsets) - 1 && StackWalk64(
#ifdef _M_X64
				IMAGE_FILE_MACHINE_AMD64,
#else
				IMAGE_FILE_MACHINE_I386,
#endif
				GetCurrentProcess(), GetCurrentThread(), &work.stackFrame, &work.contextRecord,
				NULL, SymFunctionTableAccess64, SymGetModuleBase64, NULL); i++ ){
				work.addrOffsets[i] = (PVOID)work.stackFrame.AddrPC.Offset;
			}
#endif
			OutputStackTrace(exceptionInfo->ExceptionRecord->ExceptionCode, work.addrOffsets);
			SymCleanup(GetCurrentProcess());
		}
	}
	return EXCEPTION_CONTINUE_SEARCH;
}

#endif // SUPPRESS_OUTPUT_STACK_TRACE

// �B��� CEpgDataCap_BonApp �I�u�W�F�N�g�ł��B

CEpgDataCap_BonApp theApp;

}

// CEpgDataCap_BonApp �R���X�g���N�V����

CEpgDataCap_BonApp::CEpgDataCap_BonApp()
{
	// TODO: ���̈ʒu�ɍ\�z�p�R�[�h��ǉ����Ă��������B
	// ������ InitInstance ���̏d�v�ȏ��������������ׂċL�q���Ă��������B
}

// CEpgDataCap_BonApp ������

BOOL CEpgDataCap_BonApp::InitInstance()
{
	// �A�v���P�[�V���� �}�j�t�F�X�g�� visual �X�^�C����L���ɂ��邽�߂ɁA
	// ComCtl32.dll Version 6 �ȍ~�̎g�p���w�肷��ꍇ�́A
	// Windows XP �� InitCommonControlsEx() ���K�v�ł��B�����Ȃ���΁A�E�B���h�E�쐬�͂��ׂĎ��s���܂��B
	INITCOMMONCONTROLSEX InitCtrls;
	InitCtrls.dwSize = sizeof(InitCtrls);
	// �A�v���P�[�V�����Ŏg�p���邷�ׂẴR���� �R���g���[�� �N���X���܂߂�ɂ́A
	// �����ݒ肵�܂��B
	InitCtrls.dwICC = ICC_WIN95_CLASSES;
	InitCommonControlsEx(&InitCtrls);

	SetProcessShutdownParameters(0x300, 0);

#ifndef SUPPRESS_OUTPUT_STACK_TRACE
	SetUnhandledExceptionFilter(TopLevelExceptionFilter);
#endif

	CEpgDataCap_BonDlg dlg;
	dlg.SetIniMin(FALSE);
	dlg.SetIniView(TRUE);
	dlg.SetIniNW(TRUE);

	// �R�}���h�I�v�V���������
	int argc;
	LPWSTR *argv = CommandLineToArgvW(GetCommandLine(), &argc);
	if (argv != NULL) {
		LPCWSTR curr = L"";
		LPCWSTR optUpperD = NULL;
		LPCWSTR optLowerD = NULL;
		for (int i = 1; i < argc; i++) {
			if (argv[i][0] == L'-' || argv[i][0] == L'/') {
				curr = L"";
				if (wcscmp(argv[i] + 1, L"D") == 0 && optUpperD == NULL) {
					curr = argv[i] + 1;
					optUpperD = L"";
				} else if (wcscmp(argv[i] + 1, L"d") == 0 && optLowerD == NULL) {
					curr = argv[i] + 1;
					optLowerD = L"";
				} else if (CompareNoCase(argv[i] + 1, L"min") == 0) {
					dlg.SetIniMin(TRUE);
				} else if (CompareNoCase(argv[i] + 1, L"noview") == 0) {
					dlg.SetIniView(FALSE);
				} else if (CompareNoCase(argv[i] + 1, L"nonw") == 0) {
					dlg.SetIniNW(FALSE);
				} else if (CompareNoCase(argv[i] + 1, L"nwudp") == 0) {
					dlg.SetIniNWUDP(TRUE);
				} else if (CompareNoCase(argv[i] + 1, L"nwtcp") == 0) {
					dlg.SetIniNWTCP(TRUE);
				}
			} else if (wcscmp(curr, L"D") == 0 && optUpperD && optUpperD[0] == L'\0') {
				optUpperD = argv[i];
			} else if (wcscmp(curr, L"d") == 0 && optLowerD && optLowerD[0] == L'\0') {
				optLowerD = argv[i];
			}
		}
		if (optUpperD) {
			dlg.SetInitBon(optUpperD);
			OutputDebugString(optUpperD);
		}
		// ����̋����ɍ��킹�邽��
		if (optLowerD) {
			dlg.SetInitBon(optLowerD);
			OutputDebugString(optLowerD);
		}
		LocalFree(argv);
	}


	INT_PTR nResponse = dlg.DoModal();
	if (nResponse == IDOK)
	{
		// TODO: �_�C�A���O�� <OK> �ŏ����ꂽ���̃R�[�h��
		//  �L�q���Ă��������B
	}
	else if (nResponse == IDCANCEL)
	{
		// TODO: �_�C�A���O�� <�L�����Z��> �ŏ����ꂽ���̃R�[�h��
		//  �L�q���Ă��������B
	}

	// �_�C�A���O�͕����܂����B�A�v���P�[�V�����̃��b�Z�[�W �|���v���J�n���Ȃ���
	//  �A�v���P�[�V�������I�����邽�߂� FALSE ��Ԃ��Ă��������B
	return FALSE;
}

#ifdef USE_WINMAIN_A
__declspec(dllexport) //ASLR�𖳌��ɂ��Ȃ�����(CVE-2018-5392)
int APIENTRY WinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPSTR lpCmdLine, int nCmdShow)
#else
int APIENTRY wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, LPWSTR lpCmdLine, int nCmdShow)
#endif
{
	SetDllDirectory(L"");
	SetSaveDebugLog(GetPrivateProfileInt(L"SET", L"SaveDebugLog", 0, GetModuleIniPath().c_str()) != 0);
	//���C���X���b�h�ɑ΂���COM�̏�����
	CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
	theApp.InitInstance();
	CoUninitialize();
	SetSaveDebugLog(false);
	return 0;
}

void OutputDebugStringWrapper(LPCWSTR lpOutputString)
{
	{
		//�f�o�b�O�o�̓��O�ۑ�
		CBlockLock lock(&g_debugLogLock);
		if( g_debugLog ){
			SYSTEMTIME st;
			GetLocalTime(&st);
			WCHAR t[128];
			int n = swprintf_s(t, L"[%02d%02d%02d%02d%02d%02d.%03d] ",
			                   st.wYear % 100, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
			fwrite(t, sizeof(WCHAR), n, g_debugLog);
			size_t m = lpOutputString ? wcslen(lpOutputString) : 0;
			if( m > 0 ){
				fwrite(lpOutputString, sizeof(WCHAR), m, g_debugLog);
			}
			if( m == 0 || lpOutputString[m - 1] != L'\n' ){
				fwrite(L"<NOBR>\r\n", sizeof(WCHAR), 8, g_debugLog);
			}
			fflush(g_debugLog);
		}
	}
	OutputDebugStringW(lpOutputString);
}

void SetSaveDebugLog(bool saveDebugLog)
{
	CBlockLock lock(&g_debugLogLock);
	if( g_debugLog == NULL && saveDebugLog ){
		for( int i = 0; i < 100; i++ ){
			//�p�X�ɓY���������ď������݉\�ȍŏ��̂��̂ɋL�^����
			WCHAR logFileName[64];
			swprintf_s(logFileName, L"EpgDataCap_Bon_DebugLog-%d.txt", i);
			fs_path logPath = GetCommonIniPath().replace_filename(logFileName);
			g_debugLog = UtilOpenFile(logPath, UTIL_O_EXCL_CREAT_APPEND | UTIL_SH_READ);
			if( g_debugLog ){
				fwrite(L"\xFEFF", sizeof(WCHAR), 1, g_debugLog);
			}else{
				g_debugLog = UtilOpenFile(logPath, UTIL_O_CREAT_APPEND | UTIL_SH_READ);
			}
			if( g_debugLog ){
				OutputDebugString(L"****** LOG START ******\r\n");
				break;
			}
		}
	}else if( g_debugLog && saveDebugLog == false ){
		OutputDebugString(L"****** LOG STOP ******\r\n");
		fclose(g_debugLog);
		g_debugLog = NULL;
	}
}
