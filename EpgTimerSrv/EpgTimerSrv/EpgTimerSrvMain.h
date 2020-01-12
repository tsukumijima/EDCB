#pragma once

#include "EpgDBManager.h"
#include "ReserveManager.h"
#include "NotifyManager.h"
#include "HttpServer.h"
#include "../../Common/ParseTextInstances.h"
#include "../../Common/TimeShiftUtil.h"
#include "../../Common/InstanceManager.h"

//�e��T�[�o�Ǝ����\��̊Ǘ��������Ȃ�
//�K���I�u�W�F�N�g������Main()���c���j���̏��Ԃŗ��p���Ȃ���΂Ȃ�Ȃ�
class CEpgTimerSrvMain
{
public:
	CEpgTimerSrvMain();
	//���C�����[�v����(Task���[�h)
	static bool TaskMain();
	//���C�����[�v����
	//serviceFlag_: �T�[�r�X�Ƃ��Ă̋N�����ǂ���
	bool Main(bool serviceFlag_);
	//���C��������~
	void StopMain();
	//�x�~�^�X�^���o�C�Ɉڍs���č\��Ȃ��󋵂��ǂ���
	bool IsSuspendOK() const;
private:
	//���C���E�B���h�E(Task���[�h)
	static LRESULT CALLBACK TaskMainWndProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
	//���C���E�B���h�E
	static LRESULT CALLBACK MainWndProc(HWND hwnd, UINT uMsg, WPARAM wParam, LPARAM lParam);
	//�V���b�g�_�E���₢���킹�_�C�A���O
	static INT_PTR CALLBACK QueryShutdownDlgProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam);
	//�A�C�R����ǂݍ���
	static HICON LoadSmallIcon(int iconID);
	//GUI(EpgTimer)���N������
	static void OpenGUI();
	//�u�\��폜�v�|�b�v�A�b�v���쐬����
	static void InitReserveMenuPopup(HMENU hMenu, vector<RESERVE_DATA>& list);
	void ReloadNetworkSetting();
	void ReloadSetting(bool initialize = false);
	//�f�t�H���g�w��\�ȃt�B�[���h�̃f�t�H���g�l����ʂȗ\����(ID=0x7FFFFFFF)�Ƃ��Ď擾����
	RESERVE_DATA GetDefaultReserveData(__int64 startTime) const;
	//���݂̗\���Ԃɉ��������A�^�C�}���Z�b�g����
	bool SetResumeTimer(HANDLE* resumeTimer, __int64* resumeTime, DWORD marginSec);
	//�V�X�e�����V���b�g�_�E������
	static void SetShutdown(BYTE shutdownMode);
	//GUI�ɃV���b�g�_�E���\���ǂ����̖₢���킹���J�n������
	//suspendMode==0:�ċN��(���rebootFlag==1�Ƃ���)
	//suspendMode!=0:�X�^���o�C�x�~�܂��͓d���f
	bool QueryShutdown(BYTE rebootFlag, BYTE suspendMode);
	//���[�U�[��PC���g�p�����ǂ���
	bool IsUserWorking() const;
	//���L�t�H���_��TS�t�@�C���ɃA�N�Z�X�����邩�ǂ���
	bool IsFindShareTSFile() const;
	//�}�������̃v���Z�X���N�����Ă��邩�ǂ���
	bool IsFindNoSuspendExe() const;
	//�ύX���O�̗\��𒲐�����
	vector<RESERVE_DATA>& PreChgReserveData(vector<RESERVE_DATA>& reserveList) const;
	void AutoAddReserveEPG(const EPG_AUTO_ADD_DATA& data, vector<RESERVE_DATA>& setList);
	void AutoAddReserveProgram(const MANUAL_AUTO_ADD_DATA& data, vector<RESERVE_DATA>& setList) const;
	//�O������R�}���h�֌W
	static void CtrlCmdCallback(CEpgTimerSrvMain* sys, CMD_STREAM* cmdParam, CMD_STREAM* resParam, bool tcpFlag, LPCWSTR clientIP);
	bool CtrlCmdProcessCompatible(CMD_STREAM& cmdParam, CMD_STREAM& resParam, LPCWSTR clientIP);
	void InitLuaCallback(lua_State* L, LPCSTR serverRandom);
	void DoLuaBat(CBatManager::BAT_WORK_INFO& work, vector<char>& buff);
	//Lua-edcb��Ԃ̃R�[���o�b�N
	class CLuaWorkspace
	{
	public:
		CLuaWorkspace(lua_State* L_);
		const char* WtoUTF8(const wstring& strIn);
		lua_State* const L;
		CEpgTimerSrvMain* const sys;
		int htmlEscape;
	private:
		vector<char> strOut;
	};
	static int LuaGetGenreName(lua_State* L);
	static int LuaGetComponentTypeName(lua_State* L);
	static int LuaSleep(lua_State* L);
	static int LuaConvert(lua_State* L);
	static int LuaGetPrivateProfile(lua_State* L);
	static int LuaWritePrivateProfile(lua_State* L);
	static int LuaReloadEpg(lua_State* L);
	static int LuaReloadSetting(lua_State* L);
	static int LuaEpgCapNow(lua_State* L);
	static int LuaGetChDataList(lua_State* L);
	static int LuaGetServiceList(lua_State* L);
	static int LuaGetEventMinMaxTime(lua_State* L);
	static int LuaGetEventMinMaxTimeArchive(lua_State* L);
	static int LuaGetEventMinMaxTimeProc(lua_State* L, bool archive);
	static int LuaEnumEventInfo(lua_State* L);
	static int LuaEnumEventInfoArchive(lua_State* L);
	static int LuaEnumEventInfoProc(lua_State* L, bool archive);
	static int LuaSearchEpg(lua_State* L);
	static int LuaSearchEpgArchive(lua_State* L);
	static int LuaSearchEpgProc(lua_State* L, bool archive);
	static int LuaAddReserveData(lua_State* L);
	static int LuaChgReserveData(lua_State* L);
	static int LuaDelReserveData(lua_State* L);
	static int LuaGetReserveData(lua_State* L);
	static int LuaGetRecFilePath(lua_State* L);
	static int LuaGetRecFileInfo(lua_State* L);
	static int LuaGetRecFileInfoBasic(lua_State* L);
	static int LuaGetRecFileInfoProc(lua_State* L, bool getExtraInfo);
	static int LuaChgPathRecFileInfo(lua_State* L);
	static int LuaChgProtectRecFileInfo(lua_State* L);
	static int LuaDelRecFileInfo(lua_State* L);
	static int LuaGetTunerReserveAll(lua_State* L);
	static int LuaEnumAutoAdd(lua_State* L);
	static int LuaEnumManuAdd(lua_State* L);
	static int LuaDelAutoAdd(lua_State* L);
	static int LuaDelManuAdd(lua_State* L);
	static int LuaAddOrChgAutoAdd(lua_State* L);
	static int LuaAddOrChgManuAdd(lua_State* L);
	static int LuaGetNotifyUpdateCount(lua_State* L);
	static int LuaFindFile(lua_State* L);
	static int LuaOpenNetworkTV(lua_State* L);
	static int LuaIsOpenNetworkTV(lua_State* L);
	static int LuaCloseNetworkTV(lua_State* L);
	static void PushEpgEventInfo(CLuaWorkspace& ws, const EPGDB_EVENT_INFO& e);
	static void PushReserveData(CLuaWorkspace& ws, const RESERVE_DATA& r);
	static void PushRecSettingData(CLuaWorkspace& ws, const REC_SETTING_DATA& rs);
	static void PushEpgSearchKeyInfo(CLuaWorkspace& ws, const EPGDB_SEARCH_KEY_INFO& k);
	static bool FetchReserveData(CLuaWorkspace& ws, RESERVE_DATA& r);
	static void FetchRecSettingData(CLuaWorkspace& ws, REC_SETTING_DATA& rs);
	static void FetchEpgSearchKeyInfo(CLuaWorkspace& ws, EPGDB_SEARCH_KEY_INFO& k);

	CNotifyManager notifyManager;
	CEpgDBManager epgDB;
	//reserveManager��notifyManager��epgDB�Ɉˑ�����̂ŁA���������ւ��Ă͂����Ȃ�
	CReserveManager reserveManager;
	CInstanceManager<CTimeShiftUtil> streamingManager;

	CParseEpgAutoAddText epgAutoAdd;
	CParseManualAutoAddText manualAutoAdd;
	map<DWORD, EPG_AUTO_ADD_DATA>::const_iterator autoAddCheckItr;

	//autoAddLock->settingLock�̏��Ƀ��b�N����
	mutable recursive_mutex_ autoAddLock;
	mutable recursive_mutex_ settingLock;
	HWND hwndMain;
#ifdef LUA_BUILD_AS_DLL
	HMODULE hLuaDll;
#endif
	atomic_bool_ stoppingFlag;

	atomic_bool_ residentFlag;
	CEpgTimerSrvSetting::SETTING setting;
	unsigned short tcpPort;
	bool tcpIPv6;
	DWORD tcpResponseTimeoutSec;
	wstring tcpAccessControlList;
	CHttpServer::SERVER_OPTIONS httpOptions;
	string httpServerRandom;
	atomic_bool_ useSyoboi;
	bool nwtvUdp;
	bool nwtvTcp;
	DWORD compatFlags;

	vector<EPGDB_EVENT_INFO> oldSearchList[2];
};
