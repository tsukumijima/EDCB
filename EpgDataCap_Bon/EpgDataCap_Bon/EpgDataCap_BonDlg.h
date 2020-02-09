
// EpgDataCap_BonDlg.h : �w�b�_�[ �t�@�C��
//

#pragma once

#include "../../BonCtrl/BonCtrl.h"
#include "../../Common/PipeServer.h"
#include "EpgDataCap_BonDef.h"
#include "SettingDlg.h"

// CEpgDataCap_BonDlg �_�C�A���O
class CEpgDataCap_BonDlg
{
// �R���X�g���N�V����
public:
	CEpgDataCap_BonDlg();	// �W���R���X�g���N�^�[
	INT_PTR DoModal();

	void SetInitBon(LPCWSTR bonFile){ iniBonDriver = bonFile; }
	void SetIniMin(BOOL minFlag){ iniMin = minFlag; };
	void SetIniNW(BOOL networkFlag){ iniNetwork = networkFlag; };
	void SetIniView(BOOL viewFlag){ iniView = viewFlag; };
	void SetIniNWUDP(BOOL udpFlag){ iniUDP = udpFlag; };
	void SetIniNWTCP(BOOL tcpFlag){ iniTCP = tcpFlag; };

// �_�C�A���O �f�[�^
	enum { IDD = IDD_EPGDATACAP_BON_DIALOG };

protected:
	static UINT taskbarCreated;
	static BOOL disableKeyboardHook;
protected:
	void ReloadSetting();
	void BtnUpdate(DWORD guiMode);
	//�^�X�N�g���C
	BOOL DeleteTaskBar(HWND wnd, UINT id);
	BOOL AddTaskBar(HWND wnd, UINT msg, UINT id, HICON icon, wstring tips);
	BOOL ChgTipsTaskBar(HWND wnd, UINT id, HICON icon, wstring tips);
	void ChgIconStatus();

	void SetOverlayIcon(HICON icon);
	void UpdateTitleBarText();
	int ReloadServiceList(int selONID = -1, int selTSID = -1, int selSID = -1);
	void ReloadNWSet();
	BOOL SelectBonDriver(LPCWSTR fileName);
	BOOL SelectService(const CH_DATA4& chData);

	void StartPipeServer();
	void CtrlCmdCallbackInvoked();
// ����
protected:
	HWND m_hWnd;
	HHOOK m_hKeyboardHook;
	HICON m_hIcon;
	HICON m_hIcon2;

	HICON iconRed;
	HICON iconBlue;
	HICON iconGreen;
	HICON iconGray;
	HICON iconOlRec;
	HICON iconOlEpg;
	BOOL modifyTitleBarText;
	BOOL overlayTaskIcon;
	BOOL minTask;
	wstring recFileName;
	BOOL overWriteFlag;
	wstring viewPath;
	wstring viewOpt;
	int dropSaveThresh;
	int scrambleSaveThresh;
	BOOL dropLogAsUtf8;
	DWORD tsBuffMaxCount;
	int writeBuffMaxCount;
	int openWait;
	vector<wstring> recFolderList;
	vector<NW_SEND_INFO> setUdpSendList;
	vector<NW_SEND_INFO> setTcpSendList;

	wstring iniBonDriver;
	BOOL iniMin;
	BOOL iniView;
	BOOL iniNetwork;
	BOOL iniUDP;
	BOOL iniTCP;

	CBonCtrl bonCtrl;
	CPipeServer pipeServer;
	int outCtrlID;
	vector<DWORD> cmdCtrlList;
	CMD_STREAM* cmdCapture;
	CMD_STREAM* resCapture;

	vector<CH_DATA4> serviceList;
	WORD lastONID;
	WORD lastTSID;
	DWORD recCtrlID;
	vector<NW_SEND_INFO> udpSendList;
	vector<NW_SEND_INFO> tcpSendList;
	BOOL chScanWorking;
	BOOL epgCapWorking;

	// �������ꂽ�A���b�Z�[�W���蓖�Ċ֐�
	BOOL OnInitDialog();
	afx_msg void OnSysCommand(UINT nID, LPARAM lParam, BOOL* pbProcessed);
	afx_msg void OnDestroy();
	afx_msg void OnTimer(UINT_PTR nIDEvent);
	afx_msg void OnSize(UINT nType, int cx, int cy);
	LRESULT WindowProc(UINT message, WPARAM wParam, LPARAM lParam);
	afx_msg LRESULT OnTaskbarCreated(WPARAM, LPARAM);
	afx_msg void OnCbnSelchangeComboTuner();
	afx_msg void OnCbnSelchangeComboService();
	afx_msg void OnBnClickedButtonSet();
	afx_msg void OnBnClickedButtonChscan();
	afx_msg void OnBnClickedButtonEpg();
	afx_msg void OnBnClickedButtonRec();
	afx_msg void OnBnClickedButtonCancel();
	afx_msg void OnBnClickedButtonView();
	afx_msg void OnBnClickedCheckUdp();
	afx_msg void OnBnClickedCheckTcp();
	afx_msg void OnBnClickedCheckRecSet();
	afx_msg void OnBnClickedCheckNextpg();
	afx_msg BOOL OnQueryEndSession();
	afx_msg void OnEndSession(BOOL bEnding);
	static LRESULT CALLBACK KeyboardProc(int nCode, WPARAM wParam, LPARAM lParam);
	static INT_PTR CALLBACK DlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
	HWND GetDlgItem(int nID) const{ return ::GetDlgItem(m_hWnd, nID); }
	UINT_PTR SetTimer(UINT_PTR nIDEvent, UINT uElapse, TIMERPROC lpTimerFunc){ return ::SetTimer(m_hWnd, nIDEvent, uElapse, lpTimerFunc); }
	BOOL KillTimer(UINT_PTR uIDEvent){ return ::KillTimer(m_hWnd, uIDEvent); }
};
