// SettingDlg.cpp : �����t�@�C��
//

#include "stdafx.h"
#include "EpgDataCap_Bon.h"
#include "SettingDlg.h"
#include "../../Common/SendCtrlCmd.h"


// CSettingDlg �_�C�A���O

CSettingDlg::CSettingDlg(HWND hWndOwner)
	: m_hWnd(NULL)
	, m_hWndOwner(hWndOwner)
{

}

CSettingDlg::~CSettingDlg()
{
}

INT_PTR CSettingDlg::DoModal()
{
	return DialogBoxParam(GetModuleHandle(NULL), MAKEINTRESOURCE(IDD), m_hWndOwner, DlgProc, (LPARAM)this);
}


// CSettingDlg ���b�Z�[�W �n���h���[


BOOL CSettingDlg::OnInitDialog()
{
	// TODO:  �����ɏ�������ǉ����Ă�������
	TCITEM Item;
	Item.mask = TCIF_TEXT;

	Item.pszText = L"��{�ݒ�";
	TabCtrl_InsertItem(GetDlgItem(IDC_TAB), 0, &Item);

	Item.pszText = L"����ݒ�";
	TabCtrl_InsertItem(GetDlgItem(IDC_TAB), 1, &Item);

	Item.pszText = L"EPG�擾�ݒ�";
	TabCtrl_InsertItem(GetDlgItem(IDC_TAB), 2, &Item);

	Item.pszText = L"�T�[�r�X�\���ݒ�";
	TabCtrl_InsertItem(GetDlgItem(IDC_TAB), 3, &Item);

	Item.pszText = L"�l�b�g���[�N�ݒ�";
	TabCtrl_InsertItem(GetDlgItem(IDC_TAB), 4, &Item);

	Item.pszText = L"�O���A�v���P�[�V�����ݒ�";
	TabCtrl_InsertItem(GetDlgItem(IDC_TAB), 5, &Item);

	basicDlg.Create( MAKEINTRESOURCE(IDD_DIALOG_SET_BASIC), GetSafeHwnd() );
	appDlg.Create( MAKEINTRESOURCE(IDD_DIALOG_SET_APP), GetSafeHwnd() );
	epgDlg.Create( MAKEINTRESOURCE(IDD_DIALOG_SET_EPG), GetSafeHwnd() );
	networkDlg.Create( MAKEINTRESOURCE(IDD_DIALOG_SET_NW), GetSafeHwnd() );
	appBtnDlg.Create( MAKEINTRESOURCE(IDD_DIALOG_SET_APPBTN), GetSafeHwnd() );
	serviceDlg.Create( MAKEINTRESOURCE(IDD_DIALOG_SET_SERVICE), GetSafeHwnd() );

	RECT rc;
	GetWindowRect(GetDlgItem(IDC_TAB), &rc);
	TabCtrl_AdjustRect(GetDlgItem(IDC_TAB), FALSE, &rc);
	POINT pt;
	pt.x = rc.left;
	pt.y = rc.top;
	ScreenToClient(GetSafeHwnd(), &pt);

	rc.right -= rc.left;
	rc.bottom -= rc.top;
	MoveWindow(basicDlg.GetSafeHwnd(), pt.x, pt.y, rc.right, rc.bottom, TRUE);
	MoveWindow(appDlg.GetSafeHwnd(), pt.x, pt.y, rc.right, rc.bottom, TRUE);
	MoveWindow(epgDlg.GetSafeHwnd(), pt.x, pt.y, rc.right, rc.bottom, TRUE);
	MoveWindow(networkDlg.GetSafeHwnd(), pt.x, pt.y, rc.right, rc.bottom, TRUE);
	MoveWindow(appBtnDlg.GetSafeHwnd(), pt.x, pt.y, rc.right, rc.bottom, TRUE);
	MoveWindow(serviceDlg.GetSafeHwnd(), pt.x, pt.y, rc.right, rc.bottom, TRUE);

	TabCtrl_SetCurSel(GetDlgItem(IDC_TAB), 0);
	ShowWindow(basicDlg.GetSafeHwnd(), SW_SHOW);

	return TRUE;  // return TRUE unless you set the focus to a control
	// ��O : OCX �v���p�e�B �y�[�W�͕K�� FALSE ��Ԃ��܂��B
}


void CSettingDlg::OnBnClickedOk()
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	basicDlg.SaveIni();
	appDlg.SaveIni();
	epgDlg.SaveIni();
	networkDlg.SaveIni();
	appBtnDlg.SaveIni();
	serviceDlg.SaveIni();

	//�ݒ�X�V��EpgTimer�ɒʒm����
	CSendCtrlCmd ctrlCmd;
	ctrlCmd.SendProfileUpdate(L"EpgDataCap_Bon");
}


void CSettingDlg::OnTcnSelchangingTab(NMHDR *pNMHDR, LRESULT *pResult)
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	int index = TabCtrl_GetCurSel(GetDlgItem(IDC_TAB));
	switch(index){
		case 0:
			ShowWindow(basicDlg.GetSafeHwnd(), SW_HIDE);
			break;
		case 1:
			ShowWindow(appDlg.GetSafeHwnd(), SW_HIDE);
			break;
		case 2:
			ShowWindow(epgDlg.GetSafeHwnd(), SW_HIDE);
			break;
		case 3:
			ShowWindow(serviceDlg.GetSafeHwnd(), SW_HIDE);
			break;
		case 4:
			ShowWindow(networkDlg.GetSafeHwnd(), SW_HIDE);
			break;
		case 5:
			ShowWindow(appBtnDlg.GetSafeHwnd(), SW_HIDE);
			break;
		default:
			break;
	}
	*pResult = 0;
}


void CSettingDlg::OnTcnSelchangeTab(NMHDR *pNMHDR, LRESULT *pResult)
{
	// TODO: �����ɃR���g���[���ʒm�n���h���[ �R�[�h��ǉ����܂��B
	int index = TabCtrl_GetCurSel(GetDlgItem(IDC_TAB));
	switch(index){
		case 0:
			ShowWindow(basicDlg.GetSafeHwnd(), SW_SHOW);
			break;
		case 1:
			ShowWindow(appDlg.GetSafeHwnd(), SW_SHOW);
			break;
		case 2:
			ShowWindow(epgDlg.GetSafeHwnd(), SW_SHOW);
			break;
		case 3:
			ShowWindow(serviceDlg.GetSafeHwnd(), SW_SHOW);
			break;
		case 4:
			ShowWindow(networkDlg.GetSafeHwnd(), SW_SHOW);
			break;
		case 5:
			ShowWindow(appBtnDlg.GetSafeHwnd(), SW_SHOW);
			break;
		default:
			break;
	}

	*pResult = 0;
}


INT_PTR CALLBACK CSettingDlg::DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	CSettingDlg* pSys = (CSettingDlg*)GetWindowLongPtr(hDlg, GWLP_USERDATA);
	if( pSys == NULL && uMsg != WM_INITDIALOG ){
		return FALSE;
	}
	switch( uMsg ){
	case WM_INITDIALOG:
		SetWindowLongPtr(hDlg, GWLP_USERDATA, lParam);
		pSys = (CSettingDlg*)lParam;
		pSys->m_hWnd = hDlg;
		return pSys->OnInitDialog();
	case WM_NCDESTROY:
		pSys->m_hWnd = NULL;
		break;
	case WM_NOTIFY:
		{
			LPNMHDR pNMHDR = (LPNMHDR)lParam;
			if( pNMHDR->idFrom	== IDC_TAB ){
				LRESULT result = 0;
				if( pNMHDR->code == TCN_SELCHANGING ){
					pSys->OnTcnSelchangingTab(pNMHDR, &result);
					SetWindowLongPtr(hDlg, DWLP_MSGRESULT, result);
					return TRUE;
				}else if( pNMHDR->code == TCN_SELCHANGE ){
					pSys->OnTcnSelchangeTab(pNMHDR, &result);
					SetWindowLongPtr(hDlg, DWLP_MSGRESULT, result);
					return TRUE;
				}
			}
		}
		break;
	case WM_COMMAND:
		switch( LOWORD(wParam) ){
		case IDOK:
			pSys->OnBnClickedOk();
			//FALL THROUGH!
		case IDCANCEL:
			EndDialog(hDlg, LOWORD(wParam));
			SetWindowLongPtr(hDlg, DWLP_MSGRESULT, 0);
			return TRUE;
		}
		break;
	}
	return FALSE;
}
