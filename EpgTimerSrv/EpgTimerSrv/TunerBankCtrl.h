#pragma once

#include "NotifyManager.h"
#include "EpgDBManager.h"
#include "EpgTimerSrvSetting.h"
#include "../../Common/ReNamePlugInUtil.h"
#include "../../Common/ThreadUtil.h"

//1�̃`���[�i(EpgDataCap_Bon.exe)���Ǘ�����
//�K���I�u�W�F�N�g������ReloadSetting()���c���j���̏��Ԃŗ��p���Ȃ���΂Ȃ�Ȃ�
//�X���b�h�Z�[�t�ł͂Ȃ�
class CTunerBankCtrl
{
public:
	//�^��J�n�O�ɘ^�搧����쐬����^�C�~���O(�b)
	static const int READY_MARGIN = 20;

	enum TR_STATE {
		TR_IDLE,
		TR_OPEN,	//�`���[�i�N���ς�(GetState()�̂ݎg�p)
		TR_READY,	//�^�搧��쐬�ς�
		TR_REC,		//�^�撆
		TR_EPGCAP,	//EPG�擾��(GetState()��specialState�Ŏg�p)
		TR_NWTV,	//�l�b�g���[�N���[�h�ŋN����(GetState()��specialState�Ŏg�p)
	};
	enum {
		CHECK_END = 1,				//����I��
		CHECK_END_CANCEL,			//�L�����Z���ɂ��^�悪���f����
		CHECK_END_NOT_FIND_PF,		//p/f�ɔԑg���m�F�ł��Ȃ�����
		CHECK_END_NEXT_START_END,	//���̗\��J�n�̂��ߏI��
		CHECK_END_END_SUBREC,		//�T�u�t�H���_�ւ̘^�悪��������
		CHECK_END_NOT_START_HEAD,	//�ꕔ�̂ݘ^�悳�ꂽ
		CHECK_ERR_RECEND,			//�^��I�������Ɏ��s����
		CHECK_ERR_REC,				//�\�������^�悪���f����
		CHECK_ERR_RECSTART,			//�^��J�n�Ɏ��s����
		CHECK_ERR_CTRL,				//�^�搧��̍쐬�Ɏ��s����
		CHECK_ERR_OPEN,				//�`���[�i�̃I�[�v�����ł��Ȃ�����
		CHECK_ERR_PASS,				//�I�����Ԃ��߂��Ă���
	};
	struct CHECK_RESULT {
		DWORD type;
		DWORD reserveID;
		//�ȉ���type<=CHECK_END_NOT_START_HEAD�̂Ƃ��L��
		wstring recFilePath;
		bool continueRec;
		//continueRec(�A���^��J�n�ɂ��I��)�̂Ƃ�drops��scrambles�͏��0
		__int64 drops;
		__int64 scrambles;
		//�ȉ���type==CHECK_END�̂Ƃ��L��
		SYSTEMTIME epgStartTime;
		wstring epgEventName;
	};
	struct TUNER_RESERVE {
		DWORD reserveID;
		wstring title;
		__int64 startTime;
		DWORD durationSecond;
		wstring stationName;
		WORD onid;
		WORD tsid;
		WORD sid;
		WORD eid;
		BYTE recMode; //RECMODE_ALL�`RECMODE_VIEW
		BYTE priority;
		bool enableCaption;
		bool enableData;
		bool pittari;
		BYTE partialRecMode;
		bool continueRecFlag;
		//�}�[�W���̓f�t�H���g�l�K�p�ς݂Ƃ��邱��
		__int64 startMargin;
		__int64 endMargin;
		vector<REC_FILE_SET_INFO> recFolder;
		vector<REC_FILE_SET_INFO> partialRecFolder;
	};

	CTunerBankCtrl(DWORD tunerID_, LPCWSTR bonFileName_, WORD epgCapMax, const vector<CH_DATA4>& chList_, CNotifyManager& notifyManager_, CEpgDBManager& epgDBManager_);
	~CTunerBankCtrl();
	void ReloadSetting(const CEpgTimerSrvSetting::SETTING& s);

	//�h���C�o�t�@�C�������擾(�s�ϒl)
	const wstring& GetBonFileName() const { return this->bonFileName; }
	//���̃`���[�i�Ɠ����h���C�o�̃`���[�i��EPG�擾�Ɏg����ő吔���擾(�s�ϒl)
	WORD GetEpgCapMaxOfThisBon() const { return this->epgCapMaxOfThisBon; }
	//�T�[�r�X�����擾(�s�ϒl)
	const CH_DATA4* GetCh(WORD onid, WORD tsid, WORD sid) const {
		auto itr = std::find_if(this->chList.begin(), this->chList.end(), [=](const CH_DATA4& a) {
			return a.originalNetworkID == onid && a.transportStreamID == tsid && a.serviceID == sid; });
		return itr == this->chList.end() ? NULL : &*itr;
	}
	//�\���ǉ�����
	bool AddReserve(const TUNER_RESERVE& reserve);
	//�ҋ@��Ԃɓ����Ă���\���ύX����
	//�ύX�ł��Ȃ��t�B�[���h�͓K�X�C�������
	//�J�n���Ԃ̌���ړ��͒��ӂ��K�v�B����ړ��̌��ʑҋ@��Ԃ𖾂炩�ɔ����Ă��܂��ꍇ��startTime��startMargin���C�������
	bool ChgCtrlReserve(TUNER_RESERVE* reserve);
	//�\����폜����
	//retList: �^�撆�ł���ΏI�����ʂ�ǉ�
	bool DelReserve(DWORD reserveID, vector<CHECK_RESULT>* retList = NULL);
	//�J�n���Ԃ�startTime�ȏ�̑ҋ@��Ԃɓ����Ă��Ȃ����ׂĂ̗\����N���A����
	void ClearNoCtrl(__int64 startTime = 0);
	//�\��ID�ꗗ���擾����(�\�[�g�ς�)
	vector<DWORD> GetReserveIDList() const;
	//�`���[�i�̏�ԑJ�ڂ������Ȃ��A�I�������\����擾����
	//�T��1�b���ƂɌĂ�
	//startedReserveIDList: TR_REC�ɑJ�ڂ����\��ID�ꗗ
	vector<CHECK_RESULT> Check(vector<DWORD>* startedReserveIDList = NULL);
	//�`���[�i�S�̂Ƃ��Ă̏�Ԃ��擾����
	TR_STATE GetState() const;
	//�v���Z�XID���擾����(GetState()��TR_IDLE�̂Ƃ��s��)
	int GetProcessID() const { return this->tunerPid; }
	//�l�b�g���[�N���[�hID���擾����(GetState()��TR_NWTV�łȂ��Ƃ��s��)
	int GetNWTVID() const { return this->nwtvID; }
	//�\��J�n�̍ŏ��������擾����
	__int64 GetNearestReserveTime() const;
	//EPG�擾���J�n����
	bool StartEpgCap(const vector<SET_CH_INFO>& setChList);
	//�N�����̃`���[�i�̃`�����l�����擾����
	bool GetCurrentChID(WORD* onid, WORD* tsid) const;
	//�N�����̃`���[�i����EPG�f�[�^�̌���
	bool SearchEpgInfo(WORD sid, WORD eid, EPGDB_EVENT_INFO* resVal) const;
	//�N�����̃`���[�i���猻��or���̔ԑg�����擾����
	//�߂�l: 0=����,1=���s(�ԑg���͂Ȃ�),2=���s(�擾�ł��Ȃ�)
	int GetEventPF(WORD sid, bool pfNextFlag, EPGDB_EVENT_INFO* resVal) const;
	//�����g�����ɑ΂���V�X�e�������̒x�����Ԃ��擾����
	__int64 DelayTime() const;
	//�l�b�g���[�N���[�h�Ń`���[�i���N�����`�����l���ݒ肷��
	bool OpenNWTV(int id, bool nwUdp, bool nwTcp, const SET_CH_INFO& chInfo);
	//�l�b�g���[�N���[�h�̃`���[�i�����
	void CloseNWTV();
	//�\�񂪘^�撆�ł���΂��̘^��t�@�C�������擾����
	bool GetRecFilePath(DWORD reserveID, wstring& filePath) const;
	//�\��������ƂɃt�@�C�����𐶐�����
	static wstring ConvertRecName(
		LPCWSTR recNamePlugIn, const SYSTEMTIME& startTime, DWORD durationSec, LPCWSTR eventName, WORD onid, WORD tsid, WORD sid, WORD eid,
		LPCWSTR serviceName, LPCWSTR bonDriverName, DWORD tunerID, DWORD reserveID, CEpgDBManager& epgDBManager_,
		const SYSTEMTIME& startTimeForDefault, DWORD ctrlID, LPCWSTR ext, bool noChkYen, CReNamePlugInUtil& util);
	//�o���N���Ď����ĕK�v�Ȃ�`���[�i�������I������
	//�T��2�b���ƂɃ��[�J�X���b�h����Ă�
	void Watch();
private:
	struct TUNER_RESERVE_WORK : TUNER_RESERVE {
		__int64 startOrder; //�J�n��(�\��̑O��֌W�����߂�)
		__int64 effectivePriority; //�����D��x(�\��̗D��x�����߂�B�������ق������D��x)
		TR_STATE state;
		int retryOpenCount;
		//�ȉ���state!=TR_IDLE�̂Ƃ��L��
		DWORD ctrlID[2]; //�v�f1�͕�����M�^�搧��
		//�ȉ���state==TR_REC�̂Ƃ��L��
		wstring recFilePath[2];
		bool notStartHead;
		bool appendPgInfo;
		bool savedPgInfo;
		SYSTEMTIME epgStartTime;
		wstring epgEventName;
	};
	//�`���[�i����Ă͂����Ȃ���Ԃ��ǂ���
	bool IsNeedOpenTuner() const;
	//������M�T�[�r�X��T��
	bool FindPartialService(WORD onid, WORD tsid, WORD sid, WORD* partialSID, wstring* serviceName) const;
	//�`���[�i�ɘ^�搧����쐬����
	bool CreateCtrl(DWORD* ctrlID, DWORD* partialCtrlID, const TUNER_RESERVE& reserve) const;
	//�^��t�@�C���ɑΉ�����ԑg���t�@�C����ۑ�����
	void SaveProgramInfo(LPCWSTR recPath, const EPGDB_EVENT_INFO& info, bool append) const;
	//�`���[�i�ɘ^����J�n������
	bool RecStart(const TUNER_RESERVE_WORK& reserve, __int64 now) const;
	//�`���[�i���N������
	bool OpenTuner(bool minWake, bool noView, bool nwUdp, bool nwTcp, bool standbyRec, const SET_CH_INFO* initCh);
	//�`���[�i�����
	void CloseTuner();
	//���̃o���N��BonDriver���g�p���Ă���v���Z�X��1��������
	bool CloseOtherTuner();

	const DWORD tunerID;
	const wstring bonFileName;
	const WORD epgCapMaxOfThisBon;
	const vector<CH_DATA4> chList;
	CNotifyManager& notifyManager;
	CEpgDBManager& epgDBManager;
	map<DWORD, TUNER_RESERVE_WORK> reserveMap;
	DWORD tunerPid;
#ifdef _WIN32
	//tunerPid����0�ŗL���A0�Ŗ���
	HANDLE hTunerProcess;
#endif
	WORD tunerONID;
	WORD tunerTSID;
	bool tunerChLocked;
	bool tunerResetLock;
	DWORD tunerChChgTick;
	//EPG�擾�����l�b�g���[�N���[�h���ۂ�
	TR_STATE specialState;
	//�����g�����ɑ΂���V�X�e�������̒x������
	__int64 delayTime;
	__int64 epgCapDelayTime;
	//�l�b�g���[�N���[�hID
	int nwtvID;

	__int64 recWakeTime;
	bool recMinWake;
	bool recView;
	bool recNW;
	bool backPriority;
	bool saveProgramInfo;
	bool saveProgramInfoAsUtf8;
	bool saveErrLog;
	bool recOverWrite;
	int processPriority;
	bool keepDisk;
	bool recNameNoChkYen;
	wstring recNamePlugInFileName;
	wstring tsExt;

	mutable struct WATCH_CONTEXT {
		recursive_mutex_ lock;
		DWORD count;
		DWORD tick;
	} watchContext;

	class CWatchBlock {
	public:
		CWatchBlock(WATCH_CONTEXT* context_);
		~CWatchBlock();
	private:
		WATCH_CONTEXT* context;
	};
};
