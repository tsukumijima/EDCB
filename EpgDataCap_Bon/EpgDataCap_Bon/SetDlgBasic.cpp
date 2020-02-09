// SetDlgBasic.cpp : �����t�@�C��
//

#include "stdafx.h"
#include "EpgDataCap_Bon.h"
#include "SetDlgBasic.h"

#include "../../Common/PathUtil.h"
#include "../../Common/StringUtil.h"
#include <shlobj.h>

// CSetDlgBasic �_�C�A���O

CSetDlgBasic::CSetDlgBasic()
	: m_hWnd(NULL)
{

}

CSetDlgBasic::~CSetDlgBasic()
{
}

BOOL CSetDlgBasic::Create(LPCWSTR lpszTemplateName, HWND hWndParent)
{
	return CreateDialogParam(GetModuleHandle(NULL), lpszTemplateName, hWndParent, DlgProc, (LPARAM)this) != NULL;
}


// CSetDlgBasic ���b�Z�[�W �n���h���[
BOOL CSetDlgBasic::OnInitDialog()
{
	// TODO:  �����ɏ�������ǉ����Ă�������
	fs_path path = GetSettingPath();
	SetDlgItemText(m_hWnd, IDC_EDIT_SET_PATH, path.c_str());

	for( int i = 0; ; i++ ){
		fs_path recPath = GetRecFolderPath(i);
		if( recPath.empty() ){
			break;
		}
		ListBox_AddString(GetDlgItem(IDC_LIST_REC_FOLDER), recPath.c_str());
	}

	fs_path appIniPath = GetModuleIniPath();
	Button_SetCheck(GetDlgItem(IDC_CHECK_MODIFY_TITLE_BAR), GetPrivateProfileInt(L"SET", L"ModifyTitleBarText", 0, appIniPath.c_str()));
	Button_SetCheck(GetDlgItem(IDC_CHECK_OVERLAY_TASK_ICON), GetPrivateProfileInt(L"SET", L"OverlayTaskIcon", 1, appIniPath.c_str()));
	Button_SetCheck(GetDlgItem(IDC_CHECK_TASKMIN), GetPrivateProfileInt(L"SET", L"MinTask", 0, appIniPath.c_str()));
	ComboBox_AddString(GetDlgItem(IDC_COMBO_DIALOG_TEMPLATE), L"MS UI Gothic");
	ComboBox_AddString(GetDlgItem(IDC_COMBO_DIALOG_TEMPLATE), L"Meiryo UI");
	ComboBox_AddString(GetDlgItem(IDC_COMBO_DIALOG_TEMPLATE), L"Yu Gothic UI");
	int index = GetPrivateProfileInt(L"SET", L"DialogTemplate", 0, appIniPath.c_str());
	ComboBox_SetCurSel(GetDlgItem(IDC_COMBO_DIALOG_TEMPLATE), min(max(index, 0), 2));

	return TRUE;  // return TRUE unless you set the focus to a control
	// ��O : OCX �v���p�e�B �y�[�W�͕K�� FALSE ��Ԃ��܂��B
}

void CSetDlgBasic::SaveIni()
{
	if( m_hWnd == NULL ){
		return;
	}
	fs_path commonIniPath = GetCommonIniPath();

	WCHAR settingFolderPath[512] = L"";
	GetDlgItemText(m_hWnd, IDC_EDIT_SET_PATH, settingFolderPath, 512);
	WritePrivateProfileString(L"SET", L"DataSavePath", UtilComparePath(settingFolderPath, GetDefSettingPath().c_str()) ? settingFolderPath : NULL, commonIniPath.c_str());
	UtilCreateDirectories(settingFolderPath);

	int iNum = ListBox_GetCount(GetDlgItem(IDC_LIST_REC_FOLDER));
	if( iNum == 1 ){
		WCHAR folder[512] = L"";
		int len = ListBox_GetTextLen(GetDlgItem(IDC_LIST_REC_FOLDER), 0);
		if( 0 <= len && len < 512 ){
			ListBox_GetText(GetDlgItem(IDC_LIST_REC_FOLDER), 0, folder);
		}
		if( UtilComparePath(folder, settingFolderPath) == 0 ){
			iNum = 0;
		}
	}
	WritePrivateProfileInt(L"SET", L"RecFolderNum", iNum, commonIniPath.c_str());
	for( int i = 0; i < iNum; i++ ){
		WCHAR key[64];
		swprintf_s(key, L"RecFolderPath%d", i);
		WCHAR folder[512] = L"";
		int len = ListBox_GetTextLen(GetDlgItem(IDC_LIST_REC_FOLDER), i);
		if( 0 <= len && len < 512 ){
			ListBox_GetText(GetDlgItem(IDC_LIST_REC_FOLDER), i, folder);
		}
		WritePrivateProfileString(L"SET", key, folder, commonIniPath.c_str());
	}

	fs_path appIniPath = GetModuleIniPath();
	WritePrivateProfileInt(L"SET", L"ModifyTitleBarText", Button_GetCheck(GetDlgItem(IDC_CHECK_MODIFY_TITLE_BAR)), appIniPath.c_str());
	WritePrivateProfileInt(L"SET", L"OverlayTaskIcon", Button_GetCheck(GetDlgItem(IDC_CHECK_OVERLAY_TASK_ICON)), appIniPath.c_str());
	WritePrivateProfileInt(L"SET", L"MinTask", Button_GetCheck(GetDlgItem(IDC_CHECK_TASKMIN)), appIniPath.c_str());
	WritePrivateProfileInt(L"SET", L"DialogTemplate", ComboBox_GetCurSel(GetDlgItem(IDC_COMBO_DIALOG_TEMPLATE)), appIniPath.c_str());
}

void CSetDlgBasic::OnBnClickedButtonRecPath()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	BROWSEINFO bi = {};
	WCHAR buff[MAX_PATH] = {};
	LPITEMIDLIST pidlBrowse;

	bi.hwndOwner = m_hWnd;
	bi.pszDisplayName = buff;
	bi.lpszTitle = L"�^��t�@�C���ۑ��t�H���_��I�����Ă�������";
	bi.ulFlags = BIF_NEWDIALOGSTYLE;

	pidlBrowse = SHBrowseForFolder(&bi);
	if (pidlBrowse != NULL) {  
		if (SHGetPathFromIDList(pidlBrowse, buff)) {
			SetDlgItemText(m_hWnd, IDC_EDIT_REC_FOLDER, buff);
		}
		CoTaskMemFree(pidlBrowse);
	}
}


void CSetDlgBasic::OnBnClickedButtonRecAdd()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	WCHAR recFolderPath[512];
	if( GetDlgItemText(m_hWnd, IDC_EDIT_REC_FOLDER, recFolderPath, 512) <= 0 ){
		return ;
	}

	//����t�H���_�����łɂ��邩�`�F�b�N
	int iNum = ListBox_GetCount(GetDlgItem(IDC_LIST_REC_FOLDER));
	BOOL findFlag = FALSE;
	for( int i = 0; i < iNum; i++ ){
		WCHAR folder[512] = L"";
		int len = ListBox_GetTextLen(GetDlgItem(IDC_LIST_REC_FOLDER), i);
		if( 0 <= len && len < 512 ){
			ListBox_GetText(GetDlgItem(IDC_LIST_REC_FOLDER), i, folder);
		}

		if( UtilComparePath(recFolderPath, folder) == 0 ){
			findFlag = TRUE;
			break;
		}
	}
	if( findFlag == FALSE ){
		ListBox_AddString(GetDlgItem(IDC_LIST_REC_FOLDER), recFolderPath);
	}
}


void CSetDlgBasic::OnBnClickedButtonRecDel()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	int sel = ListBox_GetCurSel(GetDlgItem(IDC_LIST_REC_FOLDER));
	if( sel == LB_ERR ){
		return ;
	}
	ListBox_DeleteString(GetDlgItem(IDC_LIST_REC_FOLDER), sel);
}


void CSetDlgBasic::OnBnClickedButtonRecUp()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	HWND hItem = GetDlgItem(IDC_LIST_REC_FOLDER);
	int sel = ListBox_GetCurSel(hItem);
	if( sel == LB_ERR || sel == 0){
		return ;
	}
	WCHAR folder[512] = L"";
	int len = ListBox_GetTextLen(hItem, sel);
	if( 0 <= len && len < 512 ){
		ListBox_GetText(hItem, sel, folder);
		ListBox_DeleteString(hItem, sel);
		ListBox_InsertString(hItem, sel - 1, folder);
		ListBox_SetCurSel(hItem, sel - 1);
	}
}


void CSetDlgBasic::OnBnClickedButtonRecDown()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	HWND hItem = GetDlgItem(IDC_LIST_REC_FOLDER);
	int sel = ListBox_GetCurSel(hItem);
	if( sel == LB_ERR || sel == ListBox_GetCount(hItem) - 1 ){
		return ;
	}
	WCHAR folder[512] = L"";
	int len = ListBox_GetTextLen(hItem, sel);
	if( 0 <= len && len < 512 ){
		ListBox_GetText(hItem, sel, folder);
		ListBox_DeleteString(hItem, sel);
		ListBox_InsertString(hItem, sel + 1, folder);
		ListBox_SetCurSel(hItem, sel + 1);
	}
}


void CSetDlgBasic::OnBnClickedButtonSetPath()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	BROWSEINFO bi = {};
	WCHAR buff[MAX_PATH] = {};
	LPITEMIDLIST pidlBrowse;

	bi.hwndOwner = m_hWnd;
	bi.pszDisplayName = buff;
	bi.lpszTitle = L"�ݒ�֌W�ۑ��t�H���_��I�����Ă�������";
	bi.ulFlags = BIF_NEWDIALOGSTYLE;

	pidlBrowse = SHBrowseForFolder(&bi);
	if (pidlBrowse != NULL) {  
		if (SHGetPathFromIDList(pidlBrowse, buff)) {
			SetDlgItemText(m_hWnd, IDC_EDIT_SET_PATH, buff);
		}
		CoTaskMemFree(pidlBrowse);
	}
}


INT_PTR CALLBACK CSetDlgBasic::DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	CSetDlgBasic* pSys = (CSetDlgBasic*)GetWindowLongPtr(hDlg, GWLP_USERDATA);
	if( pSys == NULL && uMsg != WM_INITDIALOG ){
		return FALSE;
	}
	switch( uMsg ){
	case WM_INITDIALOG:
		SetWindowLongPtr(hDlg, GWLP_USERDATA, lParam);
		pSys = (CSetDlgBasic*)lParam;
		pSys->m_hWnd = hDlg;
		return pSys->OnInitDialog();
	case WM_NCDESTROY:
		pSys->m_hWnd = NULL;
		break;
	case WM_COMMAND:
		switch( LOWORD(wParam) ){
		case IDC_BUTTON_REC_PATH:
			pSys->OnBnClickedButtonRecPath();
			break;
		case IDC_BUTTON_REC_ADD:
			pSys->OnBnClickedButtonRecAdd();
			break;
		case IDC_BUTTON_REC_DEL:
			pSys->OnBnClickedButtonRecDel();
			break;
		case IDC_BUTTON_REC_UP:
			pSys->OnBnClickedButtonRecUp();
			break;
		case IDC_BUTTON_REC_DOWN:
			pSys->OnBnClickedButtonRecDown();
			break;
		case IDC_BUTTON_SET_PATH:
			pSys->OnBnClickedButtonSetPath();
			break;
		}
		break;
	}
	return FALSE;
}

