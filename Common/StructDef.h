#ifndef INCLUDE_STRUCT_DEF_H
#define INCLUDE_STRUCT_DEF_H

#include "EpgDataCap3Def.h"

//�]���t�@�C���f�[�^
struct FILE_DATA {
	wstring Name;				//�t�@�C����
	vector<BYTE> Data;			//�t�@�C���f�[�^
};

//�^��t�H���_���
struct REC_FILE_SET_INFO {
	wstring recFolder;			//�^��t�H���_
	wstring writePlugIn;		//�o��PlugIn
	wstring recNamePlugIn;		//�t�@�C�����ϊ�PlugIn�̎g�p
	wstring recFileName;		//�t�@�C�����ʑΉ� �^��J�n�������ɓ����Ŏg�p�B�\����Ƃ��Ă͕K�v�Ȃ�
};

//�^��ݒ���
struct REC_SETTING_DATA {
	BYTE recMode;				//�^�惂�[�h
	BYTE priority;				//�D��x
	BYTE tuijyuuFlag;			//�C�x���g�����[�Ǐ]���邩�ǂ���
	DWORD serviceMode;			//�����Ώۃf�[�^���[�h
	BYTE pittariFlag;			//�҂�����H�^��
	wstring batFilePath;		//�^���BAT�t�@�C���p�X
	vector<REC_FILE_SET_INFO> recFolderList;		//�^��t�H���_�p�X
	BYTE suspendMode;			//�x�~���[�h
	BYTE rebootFlag;			//�^���ċN������
	BYTE useMargineFlag;		//�^��}�[�W�����ʎw��
	int startMargine;			//�^��J�n���̃}�[�W��
	int endMargine;				//�^��I�����̃}�[�W��
	BYTE continueRecFlag;		//�㑱����T�[�r�X���A����t�@�C���Ř^��
	BYTE partialRecFlag;		//����CH�ɕ�����M�T�[�r�X������ꍇ�A�����^�悷�邩�ǂ���
	DWORD tunerID;				//�����I�Ɏg�pTuner���Œ�
	//CMD_VER 2�ȍ~
	vector<REC_FILE_SET_INFO> partialRecFolder;	//������M�T�[�r�X�^��̃t�H���_
};

//�o�^�\����
struct RESERVE_DATA {
	wstring title;					//�ԑg��
	SYSTEMTIME startTime;			//�^��J�n����
	DWORD durationSecond;			//�^�摍����
	wstring stationName;			//�T�[�r�X��
	WORD originalNetworkID;			//ONID
	WORD transportStreamID;			//TSID
	WORD serviceID;					//SID
	WORD eventID;					//EventID
	wstring comment;				//�R�����g
	DWORD reserveID;				//�\�񎯕�ID �\��o�^����0
	//BYTE recWaitFlag;				//�\��ҋ@�������H �����Ŏg�p�i�p�~�j
	BYTE presentFlag;				//EIT[present]�Ń`�F�b�N�ς݁H �����ɓ����Ŏg�p
	BYTE overlapMode;				//���Ԃ��� 1:���Ԃ��ă`���[�i�[����Ȃ��\�񂠂� 2:�`���[�i�[����Ȃ��ė\��ł��Ȃ�
	//wstring recFilePath;			//�^��t�@�C���p�X ���o�[�W�����݊��p ���g�p�i�p�~�j
	SYSTEMTIME startTimeEpg;		//�\�񎞂̊J�n����
	REC_SETTING_DATA recSetting;	//�^��ݒ�
	DWORD reserveStatus;			//�\��ǉ���� �����Ŏg�p
	vector<DWORD> ngTunerIDList;	//���s����TunerID�̃��X�g�B�����ɓ����Ŏg�p
	//CMD_VER 5�ȍ~
	vector<wstring> recFileNameList;	//�^��\��t�@�C����
	//DWORD param1;					//�����p
};

enum REC_END_STATUS {
	REC_END_STATUS_NORMAL = 1,		//�I���E�^��I��
	REC_END_STATUS_OPEN_ERR,		//�`���[�i�[�̃I�[�v���Ɏ��s���܂���
	REC_END_STATUS_ERR_END,			//�^�撆�ɃL�����Z�����ꂽ�\��������܂�
	REC_END_STATUS_NEXT_START_END,	//���̗\��J�n�̂��߂ɃL�����Z������܂���
	REC_END_STATUS_START_ERR,		//�^�掞�ԂɋN�����Ă��Ȃ������\��������܂�
	REC_END_STATUS_CHG_TIME,		//�J�n���Ԃ��ύX����܂���
	REC_END_STATUS_NO_TUNER,		//�`���[�i�[�s���̂��ߎ��s���܂���
	REC_END_STATUS_NO_RECMODE,		//���������ł���
	REC_END_STATUS_NOT_FIND_PF,		//�^�撆�ɔԑg�����m�F�ł��܂���ł���
	REC_END_STATUS_NOT_FIND_6H,		//�w�莞�Ԕԑg��񂪌�����܂���ł���
	REC_END_STATUS_END_SUBREC,		//�^��I���i�󂫗e�ʕs���ŕʃt�H���_�ւ̕ۑ��������j
	REC_END_STATUS_ERR_RECSTART,	//�^��J�n�����Ɏ��s���܂���
	REC_END_STATUS_NOT_START_HEAD,	//�ꕔ�̂ݘ^�悪���s���ꂽ�\��������܂�
	REC_END_STATUS_ERR_CH_CHG,		//�w��`�����l���̃f�[�^��BonDriver����o�͂���Ȃ������\��������܂�
	REC_END_STATUS_ERR_END2,		//�t�@�C���ۑ��Œv���I�ȃG���[�����������\��������܂�
};

struct REC_FILE_INFO {
	DWORD id;					//ID
	wstring recFilePath;		//�^��t�@�C���p�X
	wstring title;				//�ԑg��
	SYSTEMTIME startTime;		//�J�n����
	DWORD durationSecond;		//�^�掞��
	wstring serviceName;		//�T�[�r�X��
	WORD originalNetworkID;		//ONID
	WORD transportStreamID;		//TSID
	WORD serviceID;				//SID
	WORD eventID;				//EventID
	__int64 drops;				//�h���b�v��
	__int64 scrambles;			//�X�N�����u����
	DWORD recStatus;			//�^�挋�ʂ̃X�e�[�^�X
	SYSTEMTIME startTimeEpg;	//�\�񎞂̊J�n����
	wstring programInfo;		//.program.txt�t�@�C���̓��e
	wstring errInfo;			//.err�t�@�C���̓��e
	//CMD_VER 4�ȍ~
	BYTE protectFlag;
	REC_FILE_INFO & operator= (const RESERVE_DATA & o) {
		id = 0;
		recFilePath = L"";
		title = o.title;
		startTime = o.startTime;
		durationSecond = o.durationSecond;
		serviceName = o.stationName;
		originalNetworkID = o.originalNetworkID;
		transportStreamID = o.transportStreamID;
		serviceID = o.serviceID;
		eventID = o.eventID;
		drops = 0;
		scrambles = 0;
		recStatus = 0;
		startTimeEpg = o.startTimeEpg;
		programInfo = L"";
		errInfo = L"";
		protectFlag = 0;
		return *this;
	};
	LPCWSTR GetComment() const {
		return recStatus == REC_END_STATUS_NORMAL ? (recFilePath.empty() ? L"�I��" : L"�^��I��") :
			recStatus == REC_END_STATUS_OPEN_ERR ? L"�`���[�i�[�̃I�[�v���Ɏ��s���܂���" :
			recStatus == REC_END_STATUS_ERR_END ? L"�^�撆�ɃL�����Z�����ꂽ�\��������܂�" :
			recStatus == REC_END_STATUS_NEXT_START_END ? L"���̗\��J�n�̂��߂ɃL�����Z������܂���" :
			recStatus == REC_END_STATUS_START_ERR ? L"�^�掞�ԂɋN�����Ă��Ȃ������\��������܂�" :
			recStatus == REC_END_STATUS_CHG_TIME ? L"�J�n���Ԃ��ύX����܂���" :
			recStatus == REC_END_STATUS_NO_TUNER ? L"�`���[�i�[�s���̂��ߎ��s���܂���" :
			recStatus == REC_END_STATUS_NO_RECMODE ? L"���������ł���" :
			recStatus == REC_END_STATUS_NOT_FIND_PF ? L"�^�撆�ɔԑg�����m�F�ł��܂���ł���" :
			recStatus == REC_END_STATUS_NOT_FIND_6H ? L"�w�莞�Ԕԑg��񂪌�����܂���ł���" :
			recStatus == REC_END_STATUS_END_SUBREC ? L"�^��I���i�󂫗e�ʕs���ŕʃt�H���_�ւ̕ۑ��������j" :
			recStatus == REC_END_STATUS_ERR_RECSTART ? L"�^��J�n�����Ɏ��s���܂���" :
			recStatus == REC_END_STATUS_NOT_START_HEAD ? L"�ꕔ�̂ݘ^�悪���s���ꂽ�\��������܂�" :
			recStatus == REC_END_STATUS_ERR_CH_CHG ? L"�w��`�����l���̃f�[�^��BonDriver����o�͂���Ȃ������\��������܂�" :
			recStatus == REC_END_STATUS_ERR_END2 ? L"�t�@�C���ۑ��Œv���I�ȃG���[�����������\��������܂�" : L"";
	}
};

struct TUNER_RESERVE_INFO {
	DWORD tunerID;
	wstring tunerName;
	vector<DWORD> reserveList;
};

//�`���[�i�[���T�[�r�X���
struct CH_DATA4 {
	int space;						//�`���[�i�[���
	int ch;							//�����`�����l��
	WORD originalNetworkID;			//ONID
	WORD transportStreamID;			//TSID
	WORD serviceID;					//�T�[�r�XID
	WORD serviceType;				//�T�[�r�X�^�C�v
	BOOL partialFlag;				//������M�T�[�r�X�i�����Z�O�j���ǂ���
	BOOL useViewFlag;				//�ꗗ�\���Ɏg�p���邩�ǂ���
	wstring serviceName;			//�T�[�r�X��
	wstring chName;					//�`�����l����
	wstring networkName;			//ts_name or network_name
	BYTE remoconID;					//�����R��ID
};

//�S�`���[�i�[�ŔF�������T�[�r�X�ꗗ
struct CH_DATA5 {
	WORD originalNetworkID;			//ONID
	WORD transportStreamID;			//TSID
	WORD serviceID;					//�T�[�r�XID
	WORD serviceType;				//�T�[�r�X�^�C�v
	BOOL partialFlag;				//������M�T�[�r�X�i�����Z�O�j���ǂ���
	wstring serviceName;			//�T�[�r�X��
	wstring networkName;			//ts_name or network_name
	BOOL epgCapFlag;				//EPG�f�[�^�擾�Ώۂ��ǂ���
	BOOL searchFlag;				//�������̃f�t�H���g�����ΏۃT�[�r�X���ǂ���
};

//�R�}���h����M�X�g���[��
struct CMD_STREAM {
	DWORD param;	//���M���R�}���h�A��M���G���[�R�[�h
	DWORD dataSize;	//data�̃T�C�Y�iBYTE�P�ʁj
	std::unique_ptr<BYTE[]> data;	//����M����o�C�i���f�[�^�idataSize>0�̂Ƃ��K����NULL�j
	CMD_STREAM(void) {
		param = 0;
		dataSize = 0;
	}
};

//EPG��{���
struct EPGDB_SHORT_EVENT_INFO {
	wstring event_name;			//�C�x���g��
	wstring text_char;			//���
};

//EPG�g�����
struct EPGDB_EXTENDED_EVENT_INFO {
	wstring text_char;			//�ڍ׏��
};

//EPG�W�������f�[�^
typedef EPG_CONTENT EPGDB_CONTENT_DATA;

//EPG�W���������
struct EPGDB_CONTEN_INFO {
	vector<EPGDB_CONTENT_DATA> nibbleList;
};

//EPG�f�����
struct EPGDB_COMPONENT_INFO {
	BYTE stream_content;
	BYTE component_type;
	BYTE component_tag;
	wstring text_char;			//���
};

//EPG�������f�[�^
struct EPGDB_AUDIO_COMPONENT_INFO_DATA {
	BYTE stream_content;
	BYTE component_type;
	BYTE component_tag;
	BYTE stream_type;
	BYTE simulcast_group_tag;
	BYTE ES_multi_lingual_flag;
	BYTE main_component_flag;
	BYTE quality_indicator;
	BYTE sampling_rate;
	wstring text_char;			//�ڍ׏��
};

//EPG�������
struct EPGDB_AUDIO_COMPONENT_INFO {
	vector<EPGDB_AUDIO_COMPONENT_INFO_DATA> componentList;
};

//EPG�C�x���g�f�[�^
typedef EPG_EVENT_DATA EPGDB_EVENT_DATA;

//EPG�C�x���g�O���[�v���
struct EPGDB_EVENTGROUP_INFO {
	vector<EPGDB_EVENT_DATA> eventDataList;
};

struct EPGDB_EVENT_INFO {
	WORD original_network_id;
	WORD transport_stream_id;
	WORD service_id;
	WORD event_id;							//�C�x���gID
	BYTE StartTimeFlag;						//start_time�̒l���L�����ǂ���
	SYSTEMTIME start_time;					//�J�n����
	BYTE DurationFlag;						//duration�̒l���L�����ǂ���
	DWORD durationSec;						//�����ԁi�P�ʁF�b�j
	BYTE freeCAFlag;						//�m���X�N�����u���t���O
	bool hasShortInfo;
	bool hasExtInfo;
	bool hasContentInfo;
	bool hasComponentInfo;
	bool hasAudioInfo;
	BYTE eventGroupInfoGroupType;
	BYTE eventRelayInfoGroupType;
	EPGDB_SHORT_EVENT_INFO shortInfo;		//��{���
	EPGDB_EXTENDED_EVENT_INFO extInfo;		//�g�����
	EPGDB_CONTEN_INFO contentInfo;			//�W���������
	EPGDB_COMPONENT_INFO componentInfo;		//�f�����
	EPGDB_AUDIO_COMPONENT_INFO audioInfo;	//�������
	EPGDB_EVENTGROUP_INFO eventGroupInfo;	//�C�x���g�O���[�v���
	EPGDB_EVENTGROUP_INFO eventRelayInfo;	//�C�x���g�����[���
	EPGDB_EVENT_INFO(void) {
		hasShortInfo = false;
		hasExtInfo = false;
		hasContentInfo = false;
		hasComponentInfo = false;
		hasAudioInfo = false;
		eventGroupInfoGroupType = 0;
		eventRelayInfoGroupType = 0;
	};
};

struct EPGDB_SERVICE_INFO {
	WORD ONID;
	WORD TSID;
	WORD SID;
	BYTE service_type;
	BYTE partialReceptionFlag;
	wstring service_provider_name;
	wstring service_name;
	wstring network_name;
	wstring ts_name;
	BYTE remote_control_key_id;
};

struct EPGDB_SERVICE_EVENT_INFO {
	EPGDB_SERVICE_INFO serviceInfo;
	vector<EPGDB_EVENT_INFO> eventList;
};

struct EPGDB_SERVICE_EVENT_INFO_PTR {
	const EPGDB_SERVICE_INFO* serviceInfo;
	vector<const EPGDB_EVENT_INFO*> eventList;
};

struct EPGDB_SEARCH_DATE_INFO {
	BYTE startDayOfWeek;
	WORD startHour;
	WORD startMin;
	BYTE endDayOfWeek;
	WORD endHour;
	WORD endMin;
};

//��������
struct EPGDB_SEARCH_KEY_INFO {
	wstring andKey;
	wstring notKey;
	BOOL regExpFlag;
	BOOL titleOnlyFlag;
	vector<EPGDB_CONTENT_DATA> contentList;
	vector<EPGDB_SEARCH_DATE_INFO> dateList;
	vector<__int64> serviceList;
	vector<WORD> videoList;
	vector<WORD> audioList;
	BYTE aimaiFlag;
	BYTE notContetFlag;
	BYTE notDateFlag;
	BYTE freeCAFlag;
	//CMD_VER 3�ȍ~
	//�����\��o�^�̏�����p
	BYTE chkRecEnd;					//�^��ς��̃`�F�b�N����
	WORD chkRecDay;					//�^��ς��̃`�F�b�N�Ώۊ��ԁi+20000=SID����,+30000=TS|SID����,+40000=ON|TS|SID�����j
};

struct SEARCH_PG_PARAM {
	vector<EPGDB_SEARCH_KEY_INFO> keyList;
	__int64 enumStart;
	__int64 enumEnd;
};

//�����\��o�^���
struct EPG_AUTO_ADD_DATA {
	DWORD dataID;
	EPGDB_SEARCH_KEY_INFO searchInfo;	//�����L�[
	REC_SETTING_DATA recSetting;	//�^��ݒ�
	DWORD addCount;		//�\��o�^��
};

struct MANUAL_AUTO_ADD_DATA {
	DWORD dataID;
	BYTE dayOfWeekFlag;				//�Ώۗj��
	DWORD startTime;				//�^��J�n���ԁi00:00��0�Ƃ��ĕb�P�ʁj
	DWORD durationSecond;			//�^�摍����
	wstring title;					//�ԑg��
	wstring stationName;			//�T�[�r�X��
	WORD originalNetworkID;			//ONID
	WORD transportStreamID;			//TSID
	WORD serviceID;					//SID
	REC_SETTING_DATA recSetting;	//�^��ݒ�
};

//�R�}���h���M�p
//�`�����l���ύX���
struct SET_CH_INFO {
	BOOL useSID;//wONID��wTSID��wSID�̒l���g�p�ł��邩�ǂ���
	WORD ONID;
	WORD TSID;
	WORD SID;
	BOOL useBonCh;//dwSpace��dwCh�̒l���g�p�ł��邩�ǂ���
	DWORD space;
	DWORD ch;
};

struct SET_CTRL_MODE {
	DWORD ctrlID;
	WORD SID;
	BYTE enableScramble;
	BYTE enableCaption;
	BYTE enableData;
};

struct SET_CTRL_REC_PARAM {
	DWORD ctrlID;
	wstring fileName;
	BYTE overWriteFlag;
	ULONGLONG createSize;
	vector<REC_FILE_SET_INFO> saveFolder;
	BYTE pittariFlag;
	WORD pittariONID;
	WORD pittariTSID;
	WORD pittariSID;
	WORD pittariEventID;
};

struct SET_CTRL_REC_STOP_PARAM {
	DWORD ctrlID;
	BOOL saveErrLog;
};

struct SET_CTRL_REC_STOP_RES_PARAM {
	wstring recFilePath;
	ULONGLONG drop;
	ULONGLONG scramble;
	BYTE subRecFlag;
};

struct SEARCH_EPG_INFO_PARAM {
	WORD ONID;
	WORD TSID;
	WORD SID;
	WORD eventID;
	BYTE pfOnlyFlag;
};

struct GET_EPG_PF_INFO_PARAM {
	WORD ONID;
	WORD TSID;
	WORD SID;
	BYTE pfNextFlag;
};

struct TVTEST_CH_CHG_INFO {
	wstring bonDriver;
	SET_CH_INFO chInfo;
};


struct TVTEST_STREAMING_INFO {
	BOOL enableMode;
	DWORD ctrlID;
	DWORD serverIP;
	DWORD serverPort;
	wstring filePath;
	BOOL udpSend;
	BOOL tcpSend;
	BOOL timeShiftMode;
};

struct NWPLAY_PLAY_INFO {
	DWORD ctrlID;
	DWORD ip;
	BYTE udp;
	BYTE tcp;
	DWORD udpPort;//out�Ŏ��ۂ̊J�n�|�[�g
	DWORD tcpPort;//out�Ŏ��ۂ̊J�n�|�[�g
};

struct NWPLAY_POS_CMD {
	DWORD ctrlID;
	__int64 currentPos;
	__int64 totalPos;//CMD2_EPG_SRV_NWPLAY_SET_POS���͖���
};

struct NWPLAY_TIMESHIFT_INFO {
	DWORD ctrlID;
	wstring filePath;
};

//���ʒm�p�p�����[�^�[
struct NOTIFY_SRV_INFO {
	DWORD notifyID;		//�ʒm���̎��
	SYSTEMTIME time;	//�ʒm��Ԃ̔�����������
	DWORD param1;		//�p�����[�^�[�P�i��ނɂ���ē��e�ύX�j
	DWORD param2;		//�p�����[�^�[�Q�i��ނɂ���ē��e�ύX�j
	DWORD param3;		//�p�����[�^�[�R�i�ʒm�̏���J�E���^�j
	wstring param4;		//�p�����[�^�[�S�i��ނɂ���ē��e�ύX�j
	wstring param5;		//�p�����[�^�[�T�i��ނɂ���ē��e�ύX�j
	wstring param6;		//�p�����[�^�[�U�i��ނɂ���ē��e�ύX�j
};

#endif
