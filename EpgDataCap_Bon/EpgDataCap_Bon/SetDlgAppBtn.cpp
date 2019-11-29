// SetDlgAppBtn.cpp : �����t�@�C��
//

#include "stdafx.h"
#include "EpgDataCap_Bon.h"
#include "SetDlgAppBtn.h"
#include <commdlg.h>


// CSetDlgAppBtn �_�C�A���O

CSetDlgAppBtn::CSetDlgAppBtn()
	: m_hWnd(NULL)
{

}

CSetDlgAppBtn::~CSetDlgAppBtn()
{
}

BOOL CSetDlgAppBtn::Create(LPCWSTR lpszTemplateName, HWND hWndParent)
{
	return CreateDialogParam(GetModuleHandle(NULL), lpszTemplateName, hWndParent, DlgProc, (LPARAM)this) != NULL;
}


// CSetDlgAppBtn ���b�Z�[�W �n���h���[


BOOL CSetDlgAppBtn::OnInitDialog()
{
	// TODO:  �����ɏ�������ǉ����Ă�������
	fs_path appIniPath = GetModuleIniPath();
	SetDlgItemText(m_hWnd, IDC_EDIT_VIEW_EXE, GetPrivateProfileToString( L"SET", L"ViewPath", L"", appIniPath.c_str() ).c_str());
	SetDlgItemText(m_hWnd, IDC_EDIT_VIEW_OPT, GetPrivateProfileToString( L"SET", L"ViewOption", L"", appIniPath.c_str() ).c_str());

	return TRUE;  // return TRUE unless you set the focus to a control
	// ��O : OCX �v���p�e�B �y�[�W�͕K�� FALSE ��Ԃ��܂��B
}

void CSetDlgAppBtn::SaveIni(void)
{
	if( m_hWnd == NULL ){
		return;
	}
	fs_path appIniPath = GetModuleIniPath();

	WCHAR buff[512]=L"";
	GetDlgItemText(m_hWnd, IDC_EDIT_VIEW_EXE, buff, 512);
	WritePrivateProfileString( L"SET", L"ViewPath", buff, appIniPath.c_str() );
	GetDlgItemText(m_hWnd, IDC_EDIT_VIEW_OPT, buff, 512);
	WritePrivateProfileString( L"SET", L"ViewOption", buff, appIniPath.c_str() );
}

void CSetDlgAppBtn::OnBnClickedButtonViewExe()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	WCHAR strFile[MAX_PATH]=L"";
	OPENFILENAME ofn = {};
	ofn.lStructSize = sizeof (OPENFILENAME);
	ofn.hwndOwner = m_hWnd;
	ofn.lpstrFilter = L"exe files (*.exe)\0*.exe\0All files (*.*)\0*.*\0";
	ofn.lpstrFile = strFile;
	ofn.nMaxFile = MAX_PATH;
	ofn.Flags = OFN_FILEMUSTEXIST;
	if (GetOpenFileName(&ofn) == 0) {
		return ;
	}
	SetDlgItemText(m_hWnd, IDC_EDIT_VIEW_EXE, strFile);
}

INT_PTR CALLBACK CSetDlgAppBtn::DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	CSetDlgAppBtn* pSys = (CSetDlgAppBtn*)GetWindowLongPtr(hDlg, GWLP_USERDATA);
	if( pSys == NULL && uMsg != WM_INITDIALOG ){
		return FALSE;
	}
	switch( uMsg ){
	case WM_INITDIALOG:
		SetWindowLongPtr(hDlg, GWLP_USERDATA, lParam);
		pSys = (CSetDlgAppBtn*)lParam;
		pSys->m_hWnd = hDlg;
		return pSys->OnInitDialog();
	case WM_NCDESTROY:
		pSys->m_hWnd = NULL;
		break;
	case WM_COMMAND:
		switch( LOWORD(wParam) ){
		case IDC_BUTTON_VIEW_EXE:
			pSys->OnBnClickedButtonViewExe();
			break;
		}
		break;
	}
	return FALSE;
}
