#pragma once

// TVTest�̃t�H���_��SendTSTCP.dll������΁AEpgDataCap_Bon�ő��M���"0.0.0.1:0"��ݒ肵���Ƃ��Ǝ����悤�ȓ��������
//#define SEND_PIPE_TEST

#define TVTEST_PLUGIN_CLASS_IMPLEMENT
#define TVTEST_PLUGIN_VERSION TVTEST_PLUGIN_VERSION_(0,0,14)
#include "../../Common/TVTestPlugin.h"
#include "../../Common/PipeServer.h"
#include "../../Common/EpgDataCap3Util.h"
#include "../../BonCtrl/DropCount.h"
#include "../../Common/PathUtil.h"
#include "../../Common/ThreadUtil.h"
#include "../../BonCtrl/BonCtrlDef.h"
#ifdef SEND_PIPE_TEST
#include "../../BonCtrl/ServiceFilter.h"
#include "../../BonCtrl/SendTCP.h"
#endif

class CEdcbPlugIn : public TVTest::CTVTestPlugin
{
public:
	CEdcbPlugIn();
	// �v���O�C���̏���Ԃ�
	bool GetPluginInfo(TVTest::PluginInfo *pInfo);
	// ����������
	bool Initialize();
	// �I������
	bool Finalize();
private:
	class CMyEventHandler : public TVTest::CTVTestEventHandler
	{
	public:
		CMyEventHandler(CEdcbPlugIn &outer) : m_outer(outer) {}
		static LRESULT CALLBACK EventCallback(UINT ev, LPARAM lp1, LPARAM lp2, void *pc) { return static_cast<CMyEventHandler*>(pc)->HandleEvent(ev, lp1, lp2, pc); }
		// �`�����l�����ύX���ꂽ
		bool OnChannelChange();
		// �T�[�r�X���ύX���ꂽ
		bool OnServiceChange();
		// �T�[�r�X�̍\�����ω�����
		bool OnServiceUpdate();
		// �h���C�o���ύX���ꂽ
		bool OnDriverChange();
		// �^���Ԃ��ω�����
		bool OnRecordStatusChange(int Status);
		// �N���������I������
		void OnStartupDone();
	private:
		CEdcbPlugIn &m_outer;
	};
	struct REC_CTRL
	{
		wstring filePath;
		WORD sid;
		DWORD duplicateTargetID;
		CDropCount dropCount;
	};

	// EPG�擾�Ώۂ̃T�[�r�X�ꗗ���擾����
	vector<CH_DATA5> GetEpgCheckList(WORD onid, WORD tsid, int sid, bool basicFlag) const;
	static LRESULT CALLBACK WndProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
	LRESULT WndProc_(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
	void CtrlCmdCallback(CMD_STREAM *cmdParam, CMD_STREAM *resParam);
	void CtrlCmdCallbackInvoked(CMD_STREAM *cmdParam, CMD_STREAM *resParam);
	// EDCB�̐ݒ�֌W�ۑ��t�H���_�̃p�X���擾����
	fs_path GetEdcbSettingPath() const;
	// �^���~�����ǂ������ׂ�
	bool IsNotRecording() const;
	// EDCB�̐��䉺�Ř^�撆���ǂ������ׂ�
	bool IsEdcbRecording() const;
	// ���݂�BonDriver�̓`���[�i���ǂ������ׂ�
	bool IsTunerBonDriver() const;
	// EpgTimerSrv��EPG�ēǂݍ��݂�v������X���b�h
	static void ReloadEpgThread(int param);
	// �X�g���[���R�[���o�b�N(�ʃX���b�h)
	static BOOL CALLBACK StreamCallback(BYTE *pData, void *pClientData);

	CMyEventHandler m_handler;
	recursive_mutex_ m_streamLock;
	recursive_mutex_ m_statusLock;
	HWND m_hwnd;
	CPipeServer m_pipeServer;
	vector<CH_DATA5> m_chSet5;
	CEpgDataCap3Util m_epgUtil;
	wstring m_epgUtilPath;
	int m_outCtrlID;
	wstring m_edcbDir;
	wstring m_nonTunerDrivers;
	wstring m_currentBonDriver;
	wstring m_recNamePrefix;
	int m_dropSaveThresh;
	int m_scrambleSaveThresh;
	bool m_noLogScramble;
	DWORD m_statusCode;
	SET_CH_INFO m_lastSetCh;
	bool m_chChangedAfterSetCh;
	DWORD m_chChangeID;
	DWORD m_chChangeTick;
	std::unique_ptr<FILE, decltype(&fclose)> m_epgFile;
	enum { EPG_FILE_ST_NONE, EPG_FILE_ST_PAT, EPG_FILE_ST_TOT, EPG_FILE_ST_ALL } m_epgFileState;
	__int64 m_epgFileTotPos;
	wstring m_epgFilePath;
	thread_ m_epgReloadThread;
	DWORD m_epgCapTimeout;
	bool m_epgCapSaveTimeout;
	vector<SET_CH_INFO> m_epgCapChList;
	bool m_epgCapBasicOnlyONIDs[16];
	bool m_epgCapChkONIDs[16];
	bool m_epgCapChkNext;
	DWORD m_epgCapStartTick;
	bool m_epgCapBack;
	bool m_epgCapBackBasicOnlyONIDs[16];
	DWORD m_epgCapBackStartWaitSec;
	DWORD m_epgCapBackStartTick;
	DWORD m_recCtrlCount;
	map<DWORD, REC_CTRL> m_recCtrlMap;
	wstring m_duplicateOriginalPath;
#ifdef SEND_PIPE_TEST
	CSendTCP m_sendPipe;
	HANDLE m_sendPipeMutex;
	vector<BYTE> m_sendPipeBuf;
	CServiceFilter m_serviceFilter;
#endif
};
