#ifndef INCLUDE_BON_CTRL_DEF_H
#define INCLUDE_BON_CTRL_DEF_H

#define MUTEX_UDP_PORT_NAME			L"EpgDataCap_Bon_UDP_PORT_" //+IP_�|�[�g�ԍ�
#define MUTEX_TCP_PORT_NAME			L"EpgDataCap_Bon_TCP_PORT_" //+IP_�|�[�g�ԍ�
#define CHSET_SAVE_EVENT_WAIT		L"Global\\EpgTimer_ChSet"

//�l�b�g���[�N���M�̊���|�[�g�ԍ�
#define BON_UDP_PORT_BEGIN			1234
#define BON_TCP_PORT_BEGIN			2230

//�l�b�g���[�N���M�̃|�[�g�ԍ��̑����͈�
#define BON_NW_PORT_RANGE			100

//�Ԑڎw�肪�Ȃ���Βʏ�K�v�łȂ�PID�͈͂̉���
#define BON_SELECTIVE_PID			0x0030

//�l�b�g���[�N���M�p�ݒ�
typedef struct {
	wstring ipString;
	DWORD port;
	BOOL broadcastFlag;
}NW_SEND_INFO;

class CSendNW
{
public:
	CSendNW() {}
	virtual ~CSendNW() {}
	virtual bool Initialize() = 0;
	virtual void UnInitialize() = 0;
	virtual bool IsInitialized() const = 0;
	virtual bool AddSendAddr(LPCWSTR ip, DWORD dwPort, bool broadcastFlag) = 0;
	virtual void ClearSendAddr() = 0;
	virtual bool StartSend() = 0;
	virtual void StopSend() = 0;
	virtual bool AddSendData(BYTE* pbBuff, DWORD dwSize) = 0;
	virtual void ClearSendBuff() {}
private:
	CSendNW(const CSendNW&);
	CSendNW& operator=(const CSendNW&);
};

#endif
