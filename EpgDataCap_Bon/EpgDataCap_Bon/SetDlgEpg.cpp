// SetDlgEpg.cpp : �����t�@�C��
//

#include "stdafx.h"
#include "EpgDataCap_Bon.h"
#include "SetDlgEpg.h"

#include "../../BonCtrl/ChSetUtil.h"
#include "../../Common/PathUtil.h"
// CSetDlgEpg �_�C�A���O

CSetDlgEpg::CSetDlgEpg()
	: m_hWnd(NULL)
{

}

CSetDlgEpg::~CSetDlgEpg()
{
}

BOOL CSetDlgEpg::Create(LPCWSTR lpszTemplateName, HWND hWndParent)
{
	return CreateDialogParam(GetModuleHandle(NULL), lpszTemplateName, hWndParent, DlgProc, (LPARAM)this) != NULL;
}


// CSetDlgEpg ���b�Z�[�W �n���h���[


BOOL CSetDlgEpg::OnInitDialog()
{
	// TODO:  �����ɏ�������ǉ����Ă�������
	fs_path commonIniPath = GetCommonIniPath();
	fs_path appIniPath = GetModuleIniPath();
	Button_SetCheck( GetDlgItem(IDC_CHECK_BS), GetPrivateProfileInt( L"SET", L"BSBasicOnly", 1, commonIniPath.c_str() ) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_CS1), GetPrivateProfileInt( L"SET", L"CS1BasicOnly", 1, commonIniPath.c_str() ) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_CS2), GetPrivateProfileInt( L"SET", L"CS2BasicOnly", 1, commonIniPath.c_str() ) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_CS3), GetPrivateProfileInt( L"SET", L"CS3BasicOnly", 0, commonIniPath.c_str() ) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_BACK_BS), GetPrivateProfileInt( L"SET", L"EpgCapBackBSBasicOnly", 1, appIniPath.c_str() ) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_BACK_CS1), GetPrivateProfileInt( L"SET", L"EpgCapBackCS1BasicOnly", 1, appIniPath.c_str() ) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_BACK_CS2), GetPrivateProfileInt( L"SET", L"EpgCapBackCS2BasicOnly", 1, appIniPath.c_str() ) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_BACK_CS3), GetPrivateProfileInt( L"SET", L"EpgCapBackCS3BasicOnly", 0, appIniPath.c_str() ) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_EPGCAP_LIVE), GetPrivateProfileInt(L"SET", L"EpgCapLive", 1, appIniPath.c_str()) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_EPGCAP_REC), GetPrivateProfileInt(L"SET", L"EpgCapRec", 1, appIniPath.c_str()) );
	Button_SetCheck( GetDlgItem(IDC_CHECK_PARSE_EPG_POST_PROC), GetPrivateProfileInt(L"SET", L"ParseEpgPostProcess", 0, appIniPath.c_str()) );
	SetDlgItemInt(m_hWnd, IDC_EDIT_BACKSTART_WAITSEC, GetPrivateProfileInt(L"SET", L"EpgCapBackStartWaitSec", 30, appIniPath.c_str()), FALSE);

	this->chSet.ParseText(GetSettingPath().append(L"ChSet5.txt").c_str());

	//���X�g�r���[�Ƀ`�F�b�N�{�b�N�X�Ɨ������
	HWND hItem = GetDlgItem(IDC_LIST_SERVICE);
	ListView_SetExtendedListViewStyleEx(hItem, LVS_EX_CHECKBOXES | LVS_EX_FULLROWSELECT, LVS_EX_CHECKBOXES | LVS_EX_FULLROWSELECT);
	RECT rc;
	GetClientRect(hItem, &rc);
	LVCOLUMN lvc;
	lvc.mask = LVCF_WIDTH;
	lvc.cx = rc.right - GetSystemMetrics(SM_CXVSCROLL) - 4;
	ListView_InsertColumn(hItem, 0, &lvc);

	map<LONGLONG, CH_DATA5>::const_iterator itr;
	for( itr = this->chSet.GetMap().begin(); itr != this->chSet.GetMap().end(); itr++ ){
		LVITEM lvi;
		lvi.mask = LVIF_TEXT;
		lvi.iItem = ListView_GetItemCount(hItem);
		lvi.iSubItem = 0;
		lvi.pszText = (LPWSTR)itr->second.serviceName.c_str();
		int index = ListView_InsertItem(hItem, &lvi);
		ListView_SetCheckState(hItem, index, itr->second.epgCapFlag);
	}

	return TRUE;  // return TRUE unless you set the focus to a control
	// ��O : OCX �v���p�e�B �y�[�W�͕K�� FALSE ��Ԃ��܂��B
}

void CSetDlgEpg::SaveIni(void)
{
	if( m_hWnd == NULL ){
		return;
	}
	fs_path commonIniPath = GetCommonIniPath();
	fs_path appIniPath = GetModuleIniPath();

	WritePrivateProfileInt( L"SET", L"BSBasicOnly", Button_GetCheck(GetDlgItem(IDC_CHECK_BS)), commonIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"CS1BasicOnly", Button_GetCheck(GetDlgItem(IDC_CHECK_CS1)), commonIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"CS2BasicOnly", Button_GetCheck(GetDlgItem(IDC_CHECK_CS2)), commonIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"CS3BasicOnly", Button_GetCheck(GetDlgItem(IDC_CHECK_CS3)), commonIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EpgCapBackBSBasicOnly", Button_GetCheck(GetDlgItem(IDC_CHECK_BACK_BS)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EpgCapBackCS1BasicOnly", Button_GetCheck(GetDlgItem(IDC_CHECK_BACK_CS1)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EpgCapBackCS2BasicOnly", Button_GetCheck(GetDlgItem(IDC_CHECK_BACK_CS2)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EpgCapBackCS3BasicOnly", Button_GetCheck(GetDlgItem(IDC_CHECK_BACK_CS3)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EpgCapLive", Button_GetCheck(GetDlgItem(IDC_CHECK_EPGCAP_LIVE)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EpgCapRec", Button_GetCheck(GetDlgItem(IDC_CHECK_EPGCAP_REC)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"ParseEpgPostProcess", Button_GetCheck(GetDlgItem(IDC_CHECK_PARSE_EPG_POST_PROC)), appIniPath.c_str() );
	WritePrivateProfileInt( L"SET", L"EpgCapBackStartWaitSec", GetDlgItemInt(m_hWnd, IDC_EDIT_BACKSTART_WAITSEC, NULL, FALSE), appIniPath.c_str() );

	BOOL chSetModified = FALSE;
	for( int i=0; i<ListView_GetItemCount(GetDlgItem(IDC_LIST_SERVICE)); i++ ){
		map<LONGLONG, CH_DATA5>::const_iterator itr;
		itr = this->chSet.GetMap().begin();
		advance(itr, i);
		if( (ListView_GetCheckState(GetDlgItem(IDC_LIST_SERVICE), i) != FALSE) != (itr->second.epgCapFlag != FALSE) ){
			chSetModified = TRUE;
			this->chSet.SetEpgCapMode(
				itr->second.originalNetworkID,
				itr->second.transportStreamID,
				itr->second.serviceID,
				itr->second.epgCapFlag == FALSE
				);
		}
	}
	//�`�F�b�N�𑀍삵���Ƃ������ۑ�����
	if( chSetModified ){
		this->chSet.SaveText();
	}
}


void CSetDlgEpg::OnBnClickedButtonAllChk()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	for( int i=0; i<ListView_GetItemCount(GetDlgItem(IDC_LIST_SERVICE)); i++ ){
		ListView_SetCheckState(GetDlgItem(IDC_LIST_SERVICE), i, TRUE);
	}
}


void CSetDlgEpg::OnBnClickedButtonVideoChk()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	for( int i=0; i<ListView_GetItemCount(GetDlgItem(IDC_LIST_SERVICE)); i++ ){
		map<LONGLONG, CH_DATA5>::const_iterator itr;
		itr = this->chSet.GetMap().begin();
		advance(itr, i);
		ListView_SetCheckState(GetDlgItem(IDC_LIST_SERVICE), i, CChSetUtil::IsVideoServiceType(itr->second.serviceType));
	}
}


void CSetDlgEpg::OnBnClickedButtonAllClear()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	for( int i=0; i<ListView_GetItemCount(GetDlgItem(IDC_LIST_SERVICE)); i++ ){
		ListView_SetCheckState(GetDlgItem(IDC_LIST_SERVICE), i, FALSE);
	}
}


INT_PTR CALLBACK CSetDlgEpg::DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	CSetDlgEpg* pSys = (CSetDlgEpg*)GetWindowLongPtr(hDlg, GWLP_USERDATA);
	if( pSys == NULL && uMsg != WM_INITDIALOG ){
		return FALSE;
	}
	switch( uMsg ){
	case WM_INITDIALOG:
		SetWindowLongPtr(hDlg, GWLP_USERDATA, lParam);
		pSys = (CSetDlgEpg*)lParam;
		pSys->m_hWnd = hDlg;
		return pSys->OnInitDialog();
	case WM_NCDESTROY:
		pSys->m_hWnd = NULL;
		break;
	case WM_COMMAND:
		switch( LOWORD(wParam) ){
		case IDC_BUTTON_ALL_CHK:
			pSys->OnBnClickedButtonAllChk();
			break;
		case IDC_BUTTON_VIDEO_CHK:
			pSys->OnBnClickedButtonVideoChk();
			break;
		case IDC_BUTTON_ALL_CLEAR:
			pSys->OnBnClickedButtonAllClear();
			break;
		}
		break;
	}
	return FALSE;
}
