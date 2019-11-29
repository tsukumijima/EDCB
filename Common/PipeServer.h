#pragma once

#include "StructDef.h"
#include "ThreadUtil.h"
#include <functional>

class CPipeServer
{
public:
	CPipeServer(void);
	~CPipeServer(void);

	bool StartServer(
		const wstring& pipeName,
		const std::function<void(CMD_STREAM*, CMD_STREAM*)>& cmdProc_,
		bool insecureFlag = false
		);
	bool StopServer(bool checkOnlyFlag = false);

#ifdef _WIN32
	//SERVICE_NAME�̃T�[�r�X�Z�L�����e�B���ʎq(Service-specific SID)�ɑ΂���A�N�Z�X����ǉ�����
	static BOOL GrantServerAccessToKernelObject(HANDLE handle, DWORD permissions);
#endif

protected:
	std::function<void(CMD_STREAM*, CMD_STREAM*)> cmdProc;
	atomic_bool_ exitingFlag;
	CAutoResetEvent stopEvent;
	thread_ workThread;
#ifdef _WIN32
	HANDLE hEventOl;
	HANDLE hEventConnect;
	HANDLE hPipe;

	static BOOL GrantAccessToKernelObject(HANDLE handle, WCHAR* trusteeName, DWORD permissions);
#else
	int srvSock;
	string sockPath;
#endif
	static void ServerThread(CPipeServer* pSys);

};
