#pragma once

#include "StructDef.h"

#include "CtrlCmdDef.h"
#include "ErrDef.h"
#include "CtrlCmdUtil.h"

class CSendCtrlCmd
{
public:
	CSendCtrlCmd(void);
	~CSendCtrlCmd(void);

#if !defined(SEND_CTRL_CMD_NO_TCP) && defined(_WIN32)
	//����M�^�C���A�E�g�i�ڑ��悪�v������������̂ɂ����鎞�Ԃ����\���ɒ����j
	static const DWORD SND_RCV_TIMEOUT = 30000;

	//�R�}���h���M���@�̐ݒ�
	//�����F
	// tcpFlag		[IN] TRUE�FTCP/IP���[�h�AFALSE�F���O�t���p�C�v���[�h
	void SetSendMode(
		BOOL tcpFlag_
		);
#endif

	//���O�t���p�C�v���[�h���̐ڑ����ݒ�
	//EpgTimerSrv.exe�ɑ΂���R�}���h�͐ݒ肵�Ȃ��Ă��i�f�t�H���g�l�ɂȂ��Ă���j
	//�����F
	// pipeName		[IN]�ڑ��p�C�v�̖��O
	void SetPipeSetting(
		LPCWSTR pipeName_
		);

	//���O�t���p�C�v���[�h���̐ڑ����ݒ�i�ڔ��Ƀv���Z�XID�𔺂��^�C�v�j
	//�����F
	// pid			[IN]�v���Z�XID
	void SetPipeSetting(
		LPCWSTR pipeName_,
		DWORD pid
		);

	//�ڑ���p�C�v�����݂��邩���ׂ�
	bool PipeExists();

	//TCP/IP���[�h���̐ڑ����ݒ�
	//�����F
	// ip			[IN]�ڑ���IP
	// port			[IN]�ڑ���|�[�g
	void SetNWSetting(
		const wstring& ip,
		DWORD port
		);

	//�ڑ��������̃^�C���A�E�g�ݒ�
	// timeOut		[IN]�^�C���A�E�g�l�i�P�ʁFms�j
	void SetConnectTimeOut(
		DWORD timeOut
		);

	//EPG�f�[�^���ēǂݍ��݂���
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendReloadEpg(){
		return SendCmdWithoutData(CMD2_EPG_SRV_RELOAD_EPG);
	}

	//�ݒ�����ēǂݍ��݂���
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendReloadSetting(){
		return SendCmdWithoutData(CMD2_EPG_SRV_RELOAD_SETTING);
	}

	//EpgTimerSrv.exe�̃p�C�v�ڑ�GUI�Ƃ��ăv���Z�X��o�^����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// processID			[IN]�v���Z�XID
	DWORD SendRegistGUI(DWORD processID){
		return SendCmdData(CMD2_EPG_SRV_REGIST_GUI, processID);
	}

	//EpgTimerSrv.exe�̃p�C�v�ڑ�GUI�o�^����������
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// processID			[IN]�v���Z�XID
	DWORD SendUnRegistGUI(DWORD processID){
		return SendCmdData(CMD2_EPG_SRV_UNREGIST_GUI, processID);
	}

	//�\��ꗗ���擾����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val			[OUT]�\��ꗗ
	DWORD SendEnumReserve(
		vector<RESERVE_DATA>* val
		){
		return ReceiveCmdData(CMD2_EPG_SRV_ENUM_RESERVE, val);
	}

	//�\����폜����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val				[IN]�폜����\��ID�ꗗ
	DWORD SendDelReserve(const vector<DWORD>& val){
		return SendCmdData(CMD2_EPG_SRV_DEL_RESERVE, val);
	}

	DWORD SendChkSuspend(){
		return SendCmdWithoutData(CMD2_EPG_SRV_CHK_SUSPEND);
	}

	DWORD SendSuspend(
		WORD val
		){
		return SendCmdData(CMD2_EPG_SRV_SUSPEND, val);
	}

	DWORD SendReboot(){
		return SendCmdWithoutData(CMD2_EPG_SRV_REBOOT);
	}

	//�ݒ�t�@�C��(ini)�̍X�V��ʒm������
	//�߂�l�F
	//�����F
	// val			[IN]Sender
	// �G���[�R�[�h
	DWORD SendProfileUpdate(
		const wstring& val
		){
		return SendCmdData(CMD2_EPG_SRV_PROFILE_UPDATE, val);
	}

	//�X�g���[���z�M�p�t�@�C�������
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val				[IN]����pCtrlID
	DWORD SendNwPlayClose(
		DWORD val
		){
		return SendCmdData(CMD2_EPG_SRV_NWPLAY_CLOSE, val);
	}

	//�X�g���[���z�M�J�n
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val				[IN]����pCtrlID
	DWORD SendNwPlayStart(
		DWORD val
		){
		return SendCmdData(CMD2_EPG_SRV_NWPLAY_PLAY, val);
	}

	//�X�g���[���z�M��~
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val				[IN]����pCtrlID
	DWORD SendNwPlayStop(
		DWORD val
		){
		return SendCmdData(CMD2_EPG_SRV_NWPLAY_STOP, val);
	}

	//�X�g���[���z�M�Ō��݂̑��M�ʒu�Ƒ��t�@�C���T�C�Y���擾����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val				[IN/OUT]�T�C�Y���
	DWORD SendNwPlayGetPos(
		NWPLAY_POS_CMD* val
		){
		return SendAndReceiveCmdData(CMD2_EPG_SRV_NWPLAY_GET_POS, *val, val);
	}

	//�X�g���[���z�M�ő��M�ʒu���V�[�N����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val				[IN]�T�C�Y���
	DWORD SendNwPlaySetPos(
		const NWPLAY_POS_CMD* val
		){
		return SendCmdData(CMD2_EPG_SRV_NWPLAY_SET_POS, *val);
	}

	//�X�g���[���z�M�ő��M���ݒ肷��
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val				[IN/OUT]�T�C�Y���
	DWORD SendNwPlaySetIP(
		NWPLAY_PLAY_INFO* val
		){
		return SendAndReceiveCmdData(CMD2_EPG_SRV_NWPLAY_SET_IP, *val, val);
	}

//�^�C�}�[GUI�iEpgTimer_Bon.exe�j�p

	//�\��ꗗ�̏�񂪍X�V���ꂽ
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendGUIUpdateReserve(
		){
		return SendCmdWithoutData(CMD2_TIMER_GUI_UPDATE_RESERVE);
	}

	//EPG�f�[�^�̍ēǂݍ��݂���������
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendGUIUpdateEpgData(
		){
		return SendCmdWithoutData(CMD2_TIMER_GUI_UPDATE_EPGDATA);
	}

	//���X�V��ʒm����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val				[IN]�ʒm���
	DWORD SendGUINotifyInfo2(const NOTIFY_SRV_INFO& val){
		return SendCmdData2(CMD2_TIMER_GUI_SRV_STATUS_NOTIFY2, val);
	}

	//View�A�v���iEpgDataCap_Bon.exe�j���N��
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// exeCmd			[IN]�R�}���h���C��
	// PID				[OUT]�N������exe��PID
	DWORD SendGUIExecute(
		const wstring& exeCmd,
		DWORD* PID
		){
		return SendAndReceiveCmdData(CMD2_TIMER_GUI_VIEW_EXECUTE, exeCmd, PID);
	}

	//�X�^���o�C�A�x�~�A�V���b�g�_�E���ɓ����Ă������̊m�F�����[�U�[�ɍs��
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendGUIQuerySuspend(
		BYTE rebootFlag,
		BYTE suspendMode
		){
		return SendCmdData(CMD2_TIMER_GUI_QUERY_SUSPEND, (WORD)(rebootFlag<<8|suspendMode));
	}

	//PC�ċN���ɓ����Ă������̊m�F�����[�U�[�ɍs��
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendGUIQueryReboot(
		BYTE rebootFlag
		){
		return SendCmdData(CMD2_TIMER_GUI_QUERY_REBOOT, (WORD)(rebootFlag<<8));
	}

	//�T�[�o�[�̃X�e�[�^�X�ύX�ʒm
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// status			[IN]�X�e�[�^�X
	DWORD SendGUIStatusChg(
		WORD status
		){
		return SendCmdData(CMD2_TIMER_GUI_SRV_STATUS_CHG, status);
	}


//View�A�v���iEpgDataCap_Bon.exe�j�p

	//�g�p����BonDriver�̃t�@�C�������擾
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// bonDriver			[OUT]BonDriver�t�@�C����
	DWORD SendViewGetBonDrivere(
		wstring* bonDriver
		){
		return ReceiveCmdData(CMD2_VIEW_APP_GET_BONDRIVER, bonDriver);
	}

	//�`�����l���؂�ւ�
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// chInfo				[IN]�`�����l�����
	DWORD SendViewSetCh(
		const SET_CH_INFO& chInfo
		){
		return SendCmdData(CMD2_VIEW_APP_SET_CH, chInfo);
	}

	//�����g�̎��Ԃ�PC���Ԃ̌덷�擾
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// delaySec				[OUT]�덷�i�b�j
	DWORD SendViewGetDelay(
		int* delaySec
		){
		return ReceiveCmdData(CMD2_VIEW_APP_GET_DELAY, delaySec);
	}

	//���݂̏�Ԃ��擾
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// status				[OUT]���
	DWORD SendViewGetStatus(
		DWORD* status
		){
		return ReceiveCmdData(CMD2_VIEW_APP_GET_STATUS, status);
	}

	//�A�v���P�[�V�����̏I��
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendViewAppClose(
		){
		return SendCmdWithoutData(CMD2_VIEW_APP_CLOSE);
	}

	//���ʗpID�̐ݒ�
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// id				[IN]ID
	DWORD SendViewSetID(
		int id
		){
		return SendCmdData(CMD2_VIEW_APP_SET_ID, id);
	}

	//���ʗpID�̎擾
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// id				[OUT]ID
	DWORD SendViewGetID(
		int* id
		){
		return ReceiveCmdData(CMD2_VIEW_APP_GET_ID, id);
	}

	//�\��^��p��GUI�L�[�v
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendViewSetStandbyRec(
		DWORD keepFlag
		){
		return SendCmdData(CMD2_VIEW_APP_SET_STANDBY_REC, keepFlag);
	}

	//�X�g���[������p�R���g���[���쐬
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// ctrlID				[OUT]����ID
	DWORD SendViewCreateCtrl(
		DWORD* ctrlID
		){
		return ReceiveCmdData(CMD2_VIEW_APP_CREATE_CTRL, ctrlID);
	}

	//�X�g���[������p�R���g���[���폜
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// ctrlID				[IN]����ID
	DWORD SendViewDeleteCtrl(
		DWORD ctrlID
		){
		return SendCmdData(CMD2_VIEW_APP_DELETE_CTRL, ctrlID);
	}

	//����R���g���[���̐ݒ�
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val					[IN]�ݒ�l
	DWORD SendViewSetCtrlMode(
		const SET_CTRL_MODE& val
		){
		return SendCmdData(CMD2_VIEW_APP_SET_CTRLMODE, val);
	}

	//�^�揈���J�n
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val					[IN]�ݒ�l
	DWORD SendViewStartRec(
		const SET_CTRL_REC_PARAM& val
		){
		return SendCmdData(CMD2_VIEW_APP_REC_START_CTRL, val);
	}

	//�^�揈����~
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val					[IN]�ݒ�l
	// resVal				[OUT]�h���b�v��
	DWORD SendViewStopRec(
		const SET_CTRL_REC_STOP_PARAM& val,
		SET_CTRL_REC_STOP_RES_PARAM* resVal
		){
		return SendAndReceiveCmdData(CMD2_VIEW_APP_REC_STOP_CTRL, val, resVal);
	}

	//�^�撆�̃t�@�C���p�X���擾
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val					[OUT]�t�@�C���p�X
	DWORD SendViewGetRecFilePath(
		DWORD ctrlID,
		wstring* resVal
		){
		return SendAndReceiveCmdData(CMD2_VIEW_APP_REC_FILE_PATH, ctrlID, resVal);
	}

	//EPG�擾�J�n
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// val					[IN]�擾�`�����l�����X�g
	DWORD SendViewEpgCapStart(
		const vector<SET_CH_INFO>& val
		){
		return SendCmdData(CMD2_VIEW_APP_EPGCAP_START, val);
	}

	//EPG�擾�L�����Z��
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendViewEpgCapStop(
		){
		return SendCmdWithoutData(CMD2_VIEW_APP_EPGCAP_STOP);
	}

	//EPG�f�[�^�̌���
	//�߂�l�F
	// �G���[�R�[�h
	// val					[IN]�擾�ԑg
	// resVal				[OUT]�ԑg���
	DWORD SendViewSearchEvent(
		const SEARCH_EPG_INFO_PARAM& val,
		EPGDB_EVENT_INFO* resVal
		){
		return SendAndReceiveCmdData(CMD2_VIEW_APP_SEARCH_EVENT, val, resVal);
	}

	//����or���̔ԑg�����擾����
	//�߂�l�F
	// �G���[�R�[�h
	// val					[IN]�擾�ԑg
	// resVal				[OUT]�ԑg���
	DWORD SendViewGetEventPF(
		const GET_EPG_PF_INFO_PARAM& val,
		EPGDB_EVENT_INFO* resVal
		){
		return SendAndReceiveCmdData(CMD2_VIEW_APP_GET_EVENT_PF, val, resVal);
	}

	//View�{�^���o�^�A�v���N��
	//�߂�l�F
	// �G���[�R�[�h
	DWORD SendViewExecViewApp(
		){
		return SendCmdWithoutData(CMD2_VIEW_APP_EXEC_VIEW_APP);
	}

private:
	BOOL tcpFlag;
	DWORD connectTimeOut;
	wstring pipeName;
	wstring sendIP;
	DWORD sendPort;

	CSendCtrlCmd(const CSendCtrlCmd&);
	CSendCtrlCmd& operator=(const CSendCtrlCmd&);
	DWORD SendCmdStream(const CMD_STREAM* cmd, CMD_STREAM* res);
	DWORD SendCmdWithoutData(DWORD param, CMD_STREAM* res = NULL);
	DWORD SendCmdWithoutData2(DWORD param, CMD_STREAM* res = NULL);
	template<class T> DWORD SendCmdData(DWORD param, const T& val, CMD_STREAM* res = NULL);
	template<class T> DWORD SendCmdData2(DWORD param, const T& val, CMD_STREAM* res = NULL);
	template<class T> DWORD ReceiveCmdData(DWORD param, T* resVal);
	template<class T> DWORD ReceiveCmdData2(DWORD param, T* resVal);
	template<class T, class U> DWORD SendAndReceiveCmdData(DWORD param, const T& val, U* resVal);
	template<class T, class U> DWORD SendAndReceiveCmdData2(DWORD param, const T& val, U* resVal);
};

#if 1 //�C�����C��/�e���v���[�g��`

inline DWORD CSendCtrlCmd::SendCmdWithoutData(DWORD param, CMD_STREAM* res)
{
	CMD_STREAM cmd;
	cmd.param = param;
	return SendCmdStream(&cmd, res);
}

inline DWORD CSendCtrlCmd::SendCmdWithoutData2(DWORD param, CMD_STREAM* res)
{
	return SendCmdData(param, (WORD)CMD_VER, res);
}

template<class T>
DWORD CSendCtrlCmd::SendCmdData(DWORD param, const T& val, CMD_STREAM* res)
{
	CMD_STREAM cmd;
	cmd.param = param;
	cmd.data = NewWriteVALUE(val, cmd.dataSize);
	if( cmd.data == NULL ){
		return CMD_ERR;
	}
	return SendCmdStream(&cmd, res);
}

template<class T>
DWORD CSendCtrlCmd::SendCmdData2(DWORD param, const T& val, CMD_STREAM* res)
{
	WORD ver = CMD_VER;
	CMD_STREAM cmd;
	cmd.param = param;
	cmd.data = NewWriteVALUE2WithVersion(ver, val, cmd.dataSize);
	if( cmd.data == NULL ){
		return CMD_ERR;
	}
	return SendCmdStream(&cmd, res);
}

template<class T>
DWORD CSendCtrlCmd::ReceiveCmdData(DWORD param, T* resVal)
{
	CMD_STREAM res;
	DWORD ret = SendCmdWithoutData(param, &res);

	if( ret == CMD_SUCCESS ){
		if( ReadVALUE(resVal, res.data, res.dataSize, NULL) == FALSE ){
			ret = CMD_ERR;
		}
	}
	return ret;
}

template<class T>
DWORD CSendCtrlCmd::ReceiveCmdData2(DWORD param, T* resVal)
{
	CMD_STREAM res;
	DWORD ret = SendCmdWithoutData2(param, &res);

	if( ret == CMD_SUCCESS ){
		WORD ver = 0;
		DWORD readSize = 0;
		if( ReadVALUE(&ver, res.data, res.dataSize, &readSize) == FALSE ||
			ReadVALUE2(ver, resVal, res.data.get() + readSize, res.dataSize - readSize, NULL) == FALSE ){
			ret = CMD_ERR;
		}
	}
	return ret;
}

template<class T, class U>
DWORD CSendCtrlCmd::SendAndReceiveCmdData(DWORD param, const T& val, U* resVal)
{
	CMD_STREAM res;
	DWORD ret = SendCmdData(param, val, &res);

	if( ret == CMD_SUCCESS ){
		if( ReadVALUE(resVal, res.data, res.dataSize, NULL) == FALSE ){
			ret = CMD_ERR;
		}
	}
	return ret;
}

template<class T, class U>
DWORD CSendCtrlCmd::SendAndReceiveCmdData2(DWORD param, const T& val, U* resVal)
{
	CMD_STREAM res;
	DWORD ret = SendCmdData2(param, val, &res);

	if( ret == CMD_SUCCESS ){
		WORD ver = 0;
		DWORD readSize = 0;
		if( ReadVALUE(&ver, res.data, res.dataSize, &readSize) == FALSE ||
			ReadVALUE2(ver, resVal, res.data.get() + readSize, res.dataSize - readSize, NULL) == FALSE ){
			ret = CMD_ERR;
		}
	}
	return ret;
}

#endif
