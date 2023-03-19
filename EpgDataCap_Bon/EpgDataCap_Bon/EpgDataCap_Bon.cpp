
// EpgDataCap_Bon.cpp : アプリケーションのクラス動作を定義します。
//

#include "stdafx.h"
#include "EpgDataCap_Bon.h"
#include "EpgDataCap_BonDlg.h"
#include "../../Common/StackTrace.h"
#include "../../Common/ThreadUtil.h"
#ifdef _WIN32
#include <objbase.h>
#include <shellapi.h>
#endif

namespace
{

FILE* g_debugLog;
recursive_mutex_ g_debugLogLock;

// 唯一の CEpgDataCap_BonApp オブジェクトです。

CEpgDataCap_BonApp theApp;

}

// CEpgDataCap_BonApp コンストラクション

CEpgDataCap_BonApp::CEpgDataCap_BonApp()
{
	// TODO: この位置に構築用コードを追加してください。
	// ここに InitInstance 中の重要な初期化処理をすべて記述してください。
}

// CEpgDataCap_BonApp 初期化

#ifdef _WIN32
BOOL CEpgDataCap_BonApp::InitInstance()
#else
BOOL CEpgDataCap_BonApp::InitInstance(int argc, char* argv_[])
#endif
{
#ifdef _WIN32
	// アプリケーション マニフェストが visual スタイルを有効にするために、
	// ComCtl32.dll Version 6 以降の使用を指定する場合は、
	// Windows XP に InitCommonControlsEx() が必要です。さもなければ、ウィンドウ作成はすべて失敗します。
	INITCOMMONCONTROLSEX InitCtrls;
	InitCtrls.dwSize = sizeof(InitCtrls);
	// アプリケーションで使用するすべてのコモン コントロール クラスを含めるには、
	// これを設定します。
	InitCtrls.dwICC = ICC_WIN95_CLASSES;
	InitCommonControlsEx(&InitCtrls);

	SetProcessShutdownParameters(0x300, 0);
#endif

#ifndef SUPPRESS_OUTPUT_STACK_TRACE
	SetOutputStackTraceOnUnhandledException(GetModulePath().concat(L".err").c_str());
#endif

	CEpgDataCap_BonDlg dlg;
	dlg.SetIniMin(FALSE);
	dlg.SetIniView(TRUE);
	dlg.SetIniNW(TRUE);

	// コマンドオプションを解析
#ifdef _WIN32
	int argc;
	LPWSTR *argv = CommandLineToArgvW(GetCommandLine(), &argc);
#else
	LPWSTR *argv = new LPWSTR[argc];
	for (int i = 0; i < argc; i++) {
		argv[i] = new WCHAR[strlen(argv_[i]) + 1];
		mbstowcs(argv[i], argv_[i], strlen(argv_[i]) + 1);
	}
#endif
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
			AddDebugLogFormat(L"%ls", optUpperD);
		}
		// 原作の挙動に合わせるため
		if (optLowerD) {
			dlg.SetInitBon(optLowerD);
			AddDebugLogFormat(L"%ls", optLowerD);
		}
#ifdef _WIN32
		LocalFree(argv);
#endif
	}


	INT_PTR nResponse = dlg.DoModal();
	if (nResponse == IDOK)
	{
		// TODO: ダイアログが <OK> で消された時のコードを
		//  記述してください。
	}
	else if (nResponse == IDCANCEL)
	{
		// TODO: ダイアログが <キャンセル> で消された時のコードを
		//  記述してください。
	}

	// ダイアログは閉じられました。アプリケーションのメッセージ ポンプを開始しないで
	//  アプリケーションを終了するために FALSE を返してください。
	return FALSE;
}

#ifndef _WIN32
int main(int argc, char* argv[])
{
#else
#ifdef __MINGW32__
__declspec(dllexport) //ASLRを無効にしないため(CVE-2018-5392)
#endif
int APIENTRY wWinMain(HINSTANCE, HINSTANCE, LPWSTR, int)
{
	SetDllDirectory(L"");
#endif
	SetSaveDebugLog(GetPrivateProfileInt(L"SET", L"SaveDebugLog", 0, GetModuleIniPath().c_str()) != 0);
#ifdef _WIN32
	//メインスレッドに対するCOMの初期化
	CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
	theApp.InitInstance();
	CoUninitialize();
#else
	// ロケールを UTF-8 に設定 (これを設定しておかないと文字化けする)
	setlocale(LC_ALL, "ja_JP.UTF-8");

	theApp.InitInstance(argc, argv);
#endif
	SetSaveDebugLog(false);
	return 0;
}

void AddDebugLogNoNewline(const wchar_t* lpOutputString, bool suppressDebugOutput)
{
	if( lpOutputString[0] ){
		//デバッグ出力ログ保存
		lock_recursive_mutex lock(g_debugLogLock);
		if( g_debugLog ){
			SYSTEMTIME st;
			GetLocalTime(&st);
#ifdef _WIN32
			WCHAR t[128];
			int n = swprintf_s(t, L"[%02d%02d%02d%02d%02d%02d.%03d] ",
			                   st.wYear % 100, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds);
			fwrite(t, sizeof(WCHAR), n, g_debugLog);
			fwrite(lpOutputString, sizeof(WCHAR), wcslen(lpOutputString), g_debugLog);
#else
			size_t len = 0;
			while( lpOutputString[len] != L'\0' ){
				len++;
			}
			char* buf = new char[len * 4 + 1];
			size_t n = wcstombs(buf, lpOutputString, len * 4 + 1);
			if( n != (size_t)-1 ){
				buf[n] = '\0';
				fprintf(g_debugLog, "[%02d%02d%02d%02d%02d%02d.%03d] %s",
				        st.wYear % 100, st.wMonth, st.wDay, st.wHour, st.wMinute, st.wSecond, st.wMilliseconds, buf);
			}
			delete[] buf;
#endif
			fflush(g_debugLog);
		}
	}
	if( suppressDebugOutput == false ){
		OutputDebugString(lpOutputString);
	}
}

void SetSaveDebugLog(bool saveDebugLog)
{
	lock_recursive_mutex lock(g_debugLogLock);
	if( g_debugLog == NULL && saveDebugLog ){
		for( int i = 0; i < 100; i++ ){
			//パスに添え字をつけて書き込み可能な最初のものに記録する
#ifdef _WIN32
			WCHAR logFileName[64];
			swprintf_s(logFileName, L"EpgDataCap_Bon_DebugLog-%d.txt", i);
#else
			WCHAR logFileName[128];
			swprintf(logFileName, 128, L"EpgDataCap_Bon_DebugLog-%d.txt", i);
#endif
			fs_path logPath = GetCommonIniPath().replace_filename(logFileName);
			g_debugLog = UtilOpenFile(logPath, UTIL_O_EXCL_CREAT_APPEND | UTIL_SH_READ);
			if( g_debugLog ){
#ifdef _WIN32
				fwrite(L"\xFEFF", sizeof(WCHAR), 1, g_debugLog);
#endif
			}else{
				g_debugLog = UtilOpenFile(logPath, UTIL_O_CREAT_APPEND | UTIL_SH_READ);
			}
			if( g_debugLog ){
				AddDebugLog(L"****** LOG START ******");
				break;
			}
		}
	}else if( g_debugLog && saveDebugLog == false ){
		AddDebugLog(L"****** LOG STOP ******");
		fclose(g_debugLog);
		g_debugLog = NULL;
	}
}
