#pragma once

#include "../../Common/EpgTimerUtil.h"
#include "../../Common/ParseTextInstances.h"
#include "../../Common/ThreadUtil.h"
#include "EpgDBManager.h"
#include "NotifyManager.h"
#include "TunerBankCtrl.h"
#include "BatManager.h"

//�\����Ǘ����`���[�i�Ɋ��蓖�Ă�
//�K���I�u�W�F�N�g������Initialize()���c��Finalize()���j���̏��Ԃŗ��p���Ȃ���΂Ȃ�Ȃ�
class CReserveManager
{
public:
	enum CHECK_STATUS {
		CHECK_NO_ACTION,
		CHECK_EPGCAP_END,		//EPG�擾����������
		CHECK_NEED_SHUTDOWN,	//�V�X�e���V���b�g�_�E�������݂�K�v������
		CHECK_RESERVE_MODIFIED,	//�\��ɂȂ�炩�̕ω���������
	};
	CReserveManager(CNotifyManager& notifyManager_, CEpgDBManager& epgDBManager_);
	void Initialize(const CEpgTimerSrvSetting::SETTING& s);
	void Finalize();
	void ReloadSetting(const CEpgTimerSrvSetting::SETTING& s);
	//�\����ꗗ���擾����
	vector<RESERVE_DATA> GetReserveDataAll(bool getRecFileName = false) const;
	//�`���[�i���̗\������擾����
	vector<TUNER_RESERVE_INFO> GetTunerReserveAll() const;
	//�\������擾����
	bool GetReserveData(DWORD id, RESERVE_DATA* reserveData, bool getRecFileName = false, CReNamePlugInUtil* util = NULL) const;
	//�\�����ǉ�����
	bool AddReserveData(const vector<RESERVE_DATA>& reserveList, bool setReserveStatus = false);
	//�\�����ύX����
	bool ChgReserveData(const vector<RESERVE_DATA>& reserveList, bool setReserveStatus = false);
	//�\������폜����
	void DelReserveData(const vector<DWORD>& idList);
	//�^��ςݏ��ꗗ���擾����
	vector<REC_FILE_INFO> GetRecFileInfoAll(bool getExtraInfo = true) const;
	//�^��ςݏ����擾����
	bool GetRecFileInfo(DWORD id, REC_FILE_INFO* recInfo, bool getExtraInfo = true) const;
	//�^��ςݏ����폜����
	void DelRecFileInfo(const vector<DWORD>& idList);
	//�^��ςݏ��̃t�@�C���p�X��ύX����
	//infoList: �^��ςݏ��ꗗ(id��recFilePath�̂ݎQ��)
	void ChgPathRecFileInfo(const vector<REC_FILE_INFO>& infoList);
	//�^��ςݏ��̃v���e�N�g��ύX����
	//infoList: �^��ςݏ��ꗗ(id��protectFlag�̂ݎQ��)
	void ChgProtectRecFileInfo(const vector<REC_FILE_INFO>& infoList);
	//EIT[schedule]�����ƂɒǏ]��������
	void CheckTuijyu();
	//�\��Ǘ�����
	//�T��1�b���ƂɌĂ�
	pair<CHECK_STATUS, int> Check();
	//EPG�擾�J�n��v������
	bool RequestStartEpgCap();
	//�`���[�i�N����EPG�擾��o�b�`�������s���Ă��邩
	bool IsActive() const;
	//baseTime�Ȍ�ɘ^��܂���EPG�擾���J�n����ŏ��������擾����
	//reserveData: �ŏ������̗\����(�Ȃ��Ƃ�reserveID==0)
	__int64 GetSleepReturnTime(__int64 baseTime, RESERVE_DATA* reserveData = NULL) const;
	//�w��C�x���g�̗\�񂪑��݂��邩�ǂ���
	bool IsFindReserve(WORD onid, WORD tsid, WORD sid, WORD eid, DWORD tunerID) const;
	//�w��T�[�r�X�̃v���O�����\��𒊏o���Č�������
	template<class P>
	bool FindProgramReserve(WORD onid, WORD tsid, WORD sid, P findProc) const {
		CBlockLock lock(&this->managerLock);
		const vector<pair<ULONGLONG, DWORD>>& sortList = this->reserveText.GetSortByEventList();
		auto itr = std::lower_bound(sortList.begin(), sortList.end(), pair<ULONGLONG, DWORD>(Create64PgKey(onid, tsid, sid, 0xFFFF), 0));
		for( ; itr != sortList.end() && itr->first == Create64PgKey(onid, tsid, sid, 0xFFFF); itr++ ){
			if( findProc(this->reserveText.GetMap().find(itr->second)->second) ){
				return true;
			}
		}
		return false;
	}
	//�w��T�[�r�X�𗘗p�ł���`���[�iID�ꗗ���擾����
	vector<DWORD> GetSupportServiceTuner(WORD onid, WORD tsid, WORD sid) const;
	bool GetTunerCh(DWORD tunerID, WORD onid, WORD tsid, WORD sid, DWORD* space, DWORD* ch) const;
	wstring GetTunerBonFileName(DWORD tunerID) const;
	bool IsOpenTuner(DWORD tunerID) const;
	//�l�b�g���[�N���[�h�Ń`���[�i���N�����`�����l���ݒ肷��
	//tunerIDList: �N��������Ƃ��͂��̃��X�g�ɂ���`���[�i�����ɂ���
	pair<bool, int> OpenNWTV(int id, bool nwUdp, bool nwTcp, const SET_CH_INFO& chInfo, const vector<DWORD>& tunerIDList);
	//�l�b�g���[�N���[�h�Ń`���[�i���N�����Ă��邩
	pair<bool, int> IsOpenNWTV(int id) const;
	//�l�b�g���[�N���[�h�̃`���[�i�����
	bool CloseNWTV(int id);
	//�\�񂪘^�撆�ł���΂��̘^��t�@�C�������擾����
	bool GetRecFilePath(DWORD reserveID, wstring& filePath) const;
	//�w��EPG�C�x���g�͘^��ς݂��ǂ���
	bool IsFindRecEventInfo(const EPGDB_EVENT_INFO& info, WORD chkDay) const;
	//�����\��ɂ���č쐬���ꂽ�w��C�x���g�̗\��𖳌��ɂ���
	bool ChgAutoAddNoRec(WORD onid, WORD tsid, WORD sid, WORD eid, DWORD tunerID);
	//�`�����l�������擾����
	bool GetChData(WORD onid, WORD tsid, WORD sid, CH_DATA5* chData) const;
	//�`�����l�����ꗗ���擾����
	vector<CH_DATA5> GetChDataList() const;
	//�p�����[�^�Ȃ��̒ʒm��ǉ�����
	void AddNotifyAndPostBat(DWORD notifyID);
	//�o�b�`�̃J�X�^���n���h����ݒ肷��
	void SetBatCustomHandler(LPCWSTR ext, const std::function<void(CBatManager::BAT_WORK_INFO&, vector<char>&)>& handler);
private:
	struct CHK_RESERVE_DATA {
		__int64 cutStartTime;
		__int64 cutEndTime;
		__int64 startOrder;
		__int64 effectivePriority;
		bool started;
		const RESERVE_DATA* r;
	};
	//�`���[�i�Ɋ��蓖�Ă��Ă��Ȃ��\��ꗗ���擾����
	vector<DWORD> GetNoTunerReserveAll() const;
	//�\����`���[�i�Ɋ��蓖�Ă�
	//reloadTime: �Ȃ�炩�̕ύX���������ŏ��\��ʒu
	void ReloadBankMap(__int64 reloadTime = 0);
	//����\����o���N�ɒǉ������Ƃ��ɔ�������R�X�g(�P��:10�b)���v�Z����
	//�߂�l: �d�Ȃ肪�������0�A�ʃ`�����l���̏d�Ȃ肪����Ώd�Ȃ�̕b���������Z�A����`�����l���݂̂̏d�Ȃ肪�����-1
	__int64 ChkInsertStatus(vector<CHK_RESERVE_DATA>& bank, CHK_RESERVE_DATA& inItem, bool modifyBank) const;
	//�}�[�W�����l�������\�񎞍����v�Z����(���endTime>=startTime)
	void CalcEntireReserveTime(__int64* startTime, __int64* endTime, const RESERVE_DATA& data) const;
	//�Ǐ]�ʒm�p���b�Z�[�W���擾����
	static wstring GetNotifyChgReserveMessage(const RESERVE_DATA& oldInfo, const RESERVE_DATA& newInfo);
	//�ŐVEPG(�`���[�i����̏��)�����ƂɒǏ]��������
	void CheckTuijyuTuner();
	//�f�B�X�N�̋󂫗e�ʂ𒲂ׂĕK�v�Ȃ玩���폜����
	void CheckAutoDel() const;
	//�`���[�i���蓖�Ă���Ă��Ȃ��Â��\����I����������
	void CheckOverTimeReserve();
	//�\��I������������
	//tunerID: �I�������`���[�iID
	//shutdownMode: �Ō�ɏ��������\��̘^��㓮����L�^
	void ProcessRecEnd(const vector<CTunerBankCtrl::CHECK_RESULT>& retList, DWORD tunerID = 0, int* shutdownMode = NULL);
	//EPG�擾�\�ȃ`���[�i�̃��X�g���擾����
	vector<CTunerBankCtrl*> GetEpgCapTunerList(__int64 now) const;
	//EPG�擾�������Ǘ�����
	//isEpgCap: EPG�擾���̃`���[�i���������false
	//�߂�l: EPG�擾�����������u�Ԃ�true
	bool CheckEpgCap(bool isEpgCap);
	//�\��J�n(����������)�̍ŏ��������擾����
	__int64 GetNearestRecReserveTime() const;
	//����EPG�擾�������擾����
	__int64 GetNextEpgCapTime(__int64 now, int* basicOnlyFlags = NULL) const;
	//�o���N���Ď����ĕK�v�Ȃ�`���[�i�������I������X���b�h
	static void WatchdogThread(CReserveManager* sys);
	//batPostManager�Ƀo�b�`��ǉ�����
	void AddPostBatWork(vector<CBatManager::BAT_WORK_INFO>& workList, LPCWSTR baseName);
	//�o�b�`�ɓn�������}�N����ǉ�����
	static void AddTimeMacro(vector<pair<string, wstring>>& macroList, const SYSTEMTIME& startTime, DWORD durationSecond, LPCSTR suffix);
	//�o�b�`�ɓn���\����}�N����ǉ�����
	static void AddReserveDataMacro(vector<pair<string, wstring>>& macroList, const RESERVE_DATA& data, LPCSTR suffix);
	//�o�b�`�ɓn���^��ςݏ��}�N����ǉ�����
	static void AddRecInfoMacro(vector<pair<string, wstring>>& macroList, const REC_FILE_INFO& recInfo);

	mutable recursive_mutex_ managerLock;

	CNotifyManager& notifyManager;
	CEpgDBManager& epgDBManager;

	CParseReserveText reserveText;
	CParseRecInfoText recInfoText;
	CParseRecInfo2Text recInfo2Text;
	CParseChText5 chUtil;

	CBatManager batManager;
	CBatManager batPostManager;

	map<DWORD, std::unique_ptr<CTunerBankCtrl>> tunerBankMap;

	CEpgTimerSrvSetting::SETTING setting;
	DWORD checkCount;
	__int64 lastCheckEpgCap;
	bool epgCapRequested;
	bool epgCapWork;
	bool epgCapSetTimeSync;
	__int64 epgCapTimeSyncBase;
	__int64 epgCapTimeSyncDelayMin;
	__int64 epgCapTimeSyncDelayMax;
	DWORD epgCapTimeSyncTick;
	DWORD epgCapTimeSyncQuality;
	int epgCapBasicOnlyFlags;
	int shutdownModePending;
	bool reserveModified;

	CAutoResetEvent watchdogStopEvent;
	thread_ watchdogThread;
};
