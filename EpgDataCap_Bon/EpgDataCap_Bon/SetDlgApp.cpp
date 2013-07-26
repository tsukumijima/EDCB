// SetDlgApp.cpp : 実装ファイル
//

#include "stdafx.h"
#include "EpgDataCap_Bon.h"
#include "SetDlgApp.h"


// CSetDlgApp ダイアログ

CSetDlgApp::CSetDlgApp()
	: m_hWnd(NULL)
{

}

CSetDlgApp::~CSetDlgApp()
{
}

BOOL CSetDlgApp::Create(LPCTSTR lpszTemplateName, HWND hWndParent)
{
	return CreateDialogParam(GetModuleHandle(NULL), lpszTemplateName, hWndParent, DlgProc, (LPARAM)this) != NULL;
}


// CSetDlgApp メッセージ ハンドラー


BOOL CSetDlgApp::OnInitDialog()
{
	// TODO:  ここに初期化を追加してください

	Button_SetCheck(GetDlgItem(IDC_CHECK_ALL_SERVICE), GetPrivateProfileInt(L"SET", L"AllService", 0, appIniPath.c_str()));
	Button_SetCheck(GetDlgItem(IDC_CHECK_ENABLE_DECODE), GetPrivateProfileInt(L"SET", L"Scramble", 1, appIniPath.c_str()));
	Button_SetCheck(GetDlgItem(IDC_CHECK_EMM), GetPrivateProfileInt(L"SET", L"EMM", 0, appIniPath.c_str()));
	Button_SetCheck(GetDlgItem(IDC_CHECK_NEED_CAPTION), GetPrivateProfileInt(L"SET", L"Caption", 1, appIniPath.c_str()));
	Button_SetCheck(GetDlgItem(IDC_CHECK_NEED_DATA), GetPrivateProfileInt(L"SET", L"Data", 0, appIniPath.c_str()));

	SetDlgItemInt(m_hWnd, IDC_EDIT_START_MARGINE, GetPrivateProfileInt(L"SET", L"StartMargine", 5, appIniPath.c_str()), FALSE);
	SetDlgItemInt(m_hWnd, IDC_EDIT_END_MARGINE, GetPrivateProfileInt(L"SET", L"EndMargine", 5, appIniPath.c_str()), FALSE);
	Button_SetCheck(GetDlgItem(IDC_CHECK_OVER_WRITE), GetPrivateProfileInt(L"SET", L"OverWrite", 0, appIniPath.c_str()));
	
	Button_SetCheck(GetDlgItem(IDC_CHECK_EPGCAP_LIVE), GetPrivateProfileInt(L"SET", L"EpgCapLive", 1, appIniPath.c_str()));
	Button_SetCheck(GetDlgItem(IDC_CHECK_EPGCAP_REC), GetPrivateProfileInt(L"SET", L"EpgCapRec", 1, appIniPath.c_str()));
	Button_SetCheck(GetDlgItem(IDC_CHECK_TASKMIN), GetPrivateProfileInt(L"SET", L"MinTask", 0, appIniPath.c_str()));
	Button_SetCheck(GetDlgItem(IDC_CHECK_OPENLAST), GetPrivateProfileInt(L"SET", L"OpenLast", 1, appIniPath.c_str()));

	SetDlgItemInt(m_hWnd, IDC_EDIT_BACKSTART_WAITSEC, GetPrivateProfileInt(L"SET", L"EpgCapBackStartWaitSec", 30, appIniPath.c_str()), TRUE);

	return TRUE;  // return TRUE unless you set the focus to a control
	// 例外 : OCX プロパティ ページは必ず FALSE を返します。
}

void CSetDlgApp::SaveIni(void)
{
	if( m_hWnd == NULL ){
		return;
	}

	WritePrivateProfileInt( L"SET", L"AllService", Button_GetCheck(GetDlgItem(IDC_CHECK_ALL_SERVICE)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"Scramble", Button_GetCheck(GetDlgItem(IDC_CHECK_ENABLE_DECODE)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EMM", Button_GetCheck(GetDlgItem(IDC_CHECK_EMM)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"Caption", Button_GetCheck(GetDlgItem(IDC_CHECK_NEED_CAPTION)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"Data", Button_GetCheck(GetDlgItem(IDC_CHECK_NEED_DATA)), appIniPath.c_str() );

	WritePrivateProfileInt( L"SET", L"StartMargine", GetDlgItemInt(m_hWnd, IDC_EDIT_START_MARGINE, NULL, FALSE), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EndMargine", GetDlgItemInt(m_hWnd, IDC_EDIT_END_MARGINE, NULL, FALSE), appIniPath.c_str() );

	WritePrivateProfileInt( L"SET", L"OverWrite", Button_GetCheck(GetDlgItem(IDC_CHECK_OVER_WRITE)), appIniPath.c_str() );

	WritePrivateProfileInt( L"SET", L"EpgCapLive", Button_GetCheck(GetDlgItem(IDC_CHECK_EPGCAP_LIVE)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EpgCapRec", Button_GetCheck(GetDlgItem(IDC_CHECK_EPGCAP_REC)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"MinTask", Button_GetCheck(GetDlgItem(IDC_CHECK_TASKMIN)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"OpenLast", Button_GetCheck(GetDlgItem(IDC_CHECK_OPENLAST)), appIniPath.c_str() );

	WritePrivateProfileInt( L"SET", L"EpgCapBackStartWaitSec", GetDlgItemInt(m_hWnd, IDC_EDIT_BACKSTART_WAITSEC, NULL, TRUE), appIniPath.c_str() );

}


INT_PTR CALLBACK CSetDlgApp::DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	CSetDlgApp* pSys = (CSetDlgApp*)GetWindowLongPtr(hDlg, GWLP_USERDATA);
	if( pSys == NULL && uMsg != WM_INITDIALOG ){
		return FALSE;
	}
	switch( uMsg ){
	case WM_INITDIALOG:
		SetWindowLongPtr(hDlg, GWLP_USERDATA, lParam);
		pSys = (CSetDlgApp*)lParam;
		pSys->m_hWnd = hDlg;
		return pSys->OnInitDialog();
	case WM_NCDESTROY:
		pSys->m_hWnd = NULL;
		break;
	}
	return FALSE;
}
