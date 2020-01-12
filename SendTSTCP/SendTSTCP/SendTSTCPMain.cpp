#include "stdafx.h"
#include "SendTSTCPMain.h"

//SendTSTCP�v���g�R���̃w�b�_�̑��M��}���������̃|�[�g�͈�
#define SEND_TS_TCP_NOHEAD_PORT_MIN 22000
#define SEND_TS_TCP_NOHEAD_PORT_MAX 22999
//���M�悪0.0.0.1�̂Ƃ��҂��󂯂閼�O�t���p�C�v��
#define SEND_TS_TCP_0001_PIPE_NAME L"\\\\.\\pipe\\SendTSTCP_%d_%u"
//���M�悪0.0.0.2�̂Ƃ��J�����O�t���p�C�v��
#define SEND_TS_TCP_0002_PIPE_NAME L"\\\\.\\pipe\\BonDriver_Pipe%02d"
//���M�o�b�t�@�̍ő吔(�T�C�Y��AddSendData()�̓��͂Ɉˑ�)
#define SEND_TS_TCP_BUFF_MAX 500
//���M��(�T�[�o)�ڑ��̂��߂̃|�[�����O�Ԋu
#define SEND_TS_TCP_CONNECT_INTERVAL_MSEC 2000

CSendTSTCPMain::CSendTSTCPMain(void)
{
	WSAData wsaData;
	WSAStartup(MAKEWORD(2, 2), &wsaData);
}

CSendTSTCPMain::~CSendTSTCPMain(void)
{
	StopSend();

	WSACleanup();
}

//���M���ǉ�
//�߂�l�F�G���[�R�[�h
DWORD CSendTSTCPMain::AddSendAddr(
	LPCWSTR lpcwszIP,
	DWORD dwPort
	)
{
	if( lpcwszIP == NULL ){
		return FALSE;
	}
	SEND_INFO Item;
	WtoUTF8(lpcwszIP, Item.strIP);
	Item.dwPort = dwPort;
	if( SEND_TS_TCP_NOHEAD_PORT_MIN <= dwPort && dwPort <= SEND_TS_TCP_NOHEAD_PORT_MAX ){
		//��ʃ��[�h��1�̂Ƃ��̓w�b�_�̑��M���}�������
		Item.dwPort |= 0x10000;
	}
	Item.sock = INVALID_SOCKET;
	Item.pipe = INVALID_HANDLE_VALUE;
	Item.olEvent = NULL;
	Item.bConnect = FALSE;

	CBlockLock lock(&m_sendLock);
	if( std::find_if(m_SendList.begin(), m_SendList.end(), [&Item](const SEND_INFO& a) {
	        return a.strIP == Item.strIP && (WORD)a.dwPort == (WORD)Item.dwPort; }) == m_SendList.end() ){
		m_SendList.push_back(Item);
	}

	return TRUE;
}

//���M��N���A
//�߂�l�F�G���[�R�[�h
DWORD CSendTSTCPMain::ClearSendAddr(
	)
{
	if( m_sendThread.joinable() ){
		StopSend();
		m_SendList.clear();
		StartSend();
	}else{
		m_SendList.clear();
	}

	return TRUE;
}

//�f�[�^���M���J�n
//�߂�l�F�G���[�R�[�h
DWORD CSendTSTCPMain::StartSend(
	)
{
	if( m_sendThread.joinable() ){
		return FALSE;
	}

	m_stopSendEvent.Reset();
	m_sendThread = thread_(SendThread, this);

	return TRUE;
}

//�f�[�^���M���~
//�߂�l�F�G���[�R�[�h
DWORD CSendTSTCPMain::StopSend(
	)
{
	if( m_sendThread.joinable() ){
		m_stopSendEvent.Set();
		m_sendThread.join();
	}

	return TRUE;
}

//�f�[�^���M���J�n
//�߂�l�F�G���[�R�[�h
DWORD CSendTSTCPMain::AddSendData(
	BYTE* pbData,
	DWORD dwSize
	)
{
	if( m_sendThread.joinable() ){
		CBlockLock lock(&m_sendLock);
		m_TSBuff.push_back(vector<BYTE>());
		m_TSBuff.back().reserve(sizeof(DWORD) * 2 + dwSize);
		m_TSBuff.back().resize(sizeof(DWORD) * 2);
		m_TSBuff.back().insert(m_TSBuff.back().end(), pbData, pbData + dwSize);
		if( m_TSBuff.size() > SEND_TS_TCP_BUFF_MAX ){
			for( ; m_TSBuff.size() > SEND_TS_TCP_BUFF_MAX / 2; m_TSBuff.pop_front() );
		}
	}
	return TRUE;
}

//���M�o�b�t�@���N���A
//�߂�l�F�G���[�R�[�h
DWORD CSendTSTCPMain::ClearSendBuff(
	)
{
	CBlockLock lock(&m_sendLock);
	m_TSBuff.clear();

	return TRUE;
}

void CSendTSTCPMain::SendThread(CSendTSTCPMain* pSys)
{
	DWORD dwCount = 0;
	DWORD dwCheckConnectTick = GetTickCount();
	for(;;){
		DWORD tick = GetTickCount();
		if( tick - dwCheckConnectTick > SEND_TS_TCP_CONNECT_INTERVAL_MSEC ){
			dwCheckConnectTick = tick;
			std::list<SEND_INFO>::iterator itr;
			for( size_t i = 0;; i++ ){
				{
					CBlockLock lock(&pSys->m_sendLock);
					if( i == 0 ){
						itr = pSys->m_SendList.begin();
					}else{
						itr++;
					}
					if( itr == pSys->m_SendList.end() ){
						break;
					}
				}
				if( itr->strIP == "0.0.0.1" ){
					//�T�[�o�Ƃ��Ė��O�t���p�C�v�ő҂���
					if( itr->olEvent == NULL ){
						itr->olEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
					}
					if( itr->olEvent ){
						if( itr->pipe == INVALID_HANDLE_VALUE ){
							wstring strPipe;
							Format(strPipe, SEND_TS_TCP_0001_PIPE_NAME, (WORD)itr->dwPort, GetCurrentProcessId());
							itr->pipe = CreateNamedPipe(strPipe.c_str(), PIPE_ACCESS_OUTBOUND | FILE_FLAG_OVERLAPPED, 0, 1, 48128, 0, 0, NULL);
							if( itr->pipe != INVALID_HANDLE_VALUE ){
								OVERLAPPED olZero = {};
								itr->ol = olZero;
								itr->ol.hEvent = itr->olEvent;
								if( ConnectNamedPipe(itr->pipe, &itr->ol) == FALSE ){
									DWORD err = GetLastError();
									if( err == ERROR_PIPE_CONNECTED ){
										itr->bConnect = TRUE;
									}else if( err != ERROR_IO_PENDING ){
										CloseHandle(itr->pipe);
										itr->pipe = INVALID_HANDLE_VALUE;
									}
								}
							}
						}else if( itr->bConnect == FALSE ){
							if( WaitForSingleObject(itr->olEvent, 0) == WAIT_OBJECT_0 ){
								itr->bConnect = TRUE;
							}
						}
					}
				}else if( itr->strIP == "0.0.0.2" ){
					//�N���C�A���g�Ƃ��Ė��O�t���p�C�v���J��
					if( itr->olEvent == NULL ){
						itr->olEvent = CreateEvent(NULL, TRUE, FALSE, NULL);
					}
					if( itr->olEvent && itr->pipe == INVALID_HANDLE_VALUE ){
						wstring strPipe;
						Format(strPipe, SEND_TS_TCP_0002_PIPE_NAME, (WORD)itr->dwPort);
						itr->pipe = CreateFile(strPipe.c_str(), GENERIC_WRITE, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED, NULL);
						if( itr->pipe != INVALID_HANDLE_VALUE ){
							itr->bConnect = TRUE;
						}
					}
				}else{
					//�N���C�A���g�Ƃ���TCP�Őڑ�
					if( itr->sock != INVALID_SOCKET && itr->bConnect == FALSE ){
						fd_set wmask;
						FD_ZERO(&wmask);
						FD_SET(itr->sock, &wmask);
						struct timeval tv = {0, 0};
						if( select((int)itr->sock + 1, NULL, &wmask, NULL, &tv) == 1 ){
							itr->bConnect = TRUE;
						}else{
							closesocket(itr->sock);
							itr->sock = INVALID_SOCKET;
						}
					}
					if( itr->sock == INVALID_SOCKET ){
						char szPort[16];
						sprintf_s(szPort, "%d", (WORD)itr->dwPort);
						struct addrinfo hints = {};
						hints.ai_flags = AI_NUMERICHOST;
						hints.ai_socktype = SOCK_STREAM;
						hints.ai_protocol = IPPROTO_TCP;
						struct addrinfo* result;
						if( getaddrinfo(itr->strIP.c_str(), szPort, &hints, &result) == 0 ){
							itr->sock = socket(result->ai_family, result->ai_socktype, result->ai_protocol);
							if( itr->sock != INVALID_SOCKET ){
								//�m���u���b�L���O���[�h��
								unsigned long x = 1;
								if( ioctlsocket(itr->sock, FIONBIO, &x) == SOCKET_ERROR ){
									closesocket(itr->sock);
									itr->sock = INVALID_SOCKET;
								}else if( connect(itr->sock, result->ai_addr, (int)result->ai_addrlen) != SOCKET_ERROR ){
									itr->bConnect = TRUE;
								}else if( WSAGetLastError() != WSAEWOULDBLOCK ){
									closesocket(itr->sock);
									itr->sock = INVALID_SOCKET;
								}
							}
							freeaddrinfo(result);
						}
					}
				}
			}
		}

		std::list<vector<BYTE>> item;
		size_t sendListSizeOrStop;
		{
			CBlockLock lock(&pSys->m_sendLock);

			if( pSys->m_TSBuff.empty() == false ){
				item.splice(item.end(), pSys->m_TSBuff, pSys->m_TSBuff.begin());
				DWORD dwCmd[2] = { dwCount, (DWORD)(item.back().size() - sizeof(DWORD) * 2) };
				memcpy(&item.back().front(), dwCmd, sizeof(dwCmd));
			}
			//�r���Ō��邱�Ƃ͂Ȃ�
			sendListSizeOrStop = pSys->m_SendList.size();
		}

		if( item.empty() || sendListSizeOrStop == 0 ){
			if( pSys->m_stopSendEvent.WaitOne(item.empty() ? 100 : 0) ){
				//�L�����Z�����ꂽ
				break;
			}
		}else{
			std::list<SEND_INFO>::iterator itr;
			for( size_t i = 0; i < sendListSizeOrStop; i++ ){
				{
					CBlockLock lock(&pSys->m_sendLock);
					if( i == 0 ){
						itr = pSys->m_SendList.begin();
					}else{
						itr++;
					}
				}
				size_t adjust = item.back().size();
				if( itr->pipe != INVALID_HANDLE_VALUE || itr->dwPort >> 16 == 1 ){
					adjust -= sizeof(DWORD) * 2;
				}
				if( itr->pipe != INVALID_HANDLE_VALUE && itr->bConnect && adjust != 0 ){
					//���O�t���p�C�v�ɏ�������
					OVERLAPPED olZero = {};
					itr->ol = olZero;
					itr->ol.hEvent = itr->olEvent;
					HANDLE olEvents[] = { pSys->m_stopSendEvent.Handle(), itr->olEvent };
					BOOL bClose = FALSE;
					DWORD xferred;
					if( WriteFile(itr->pipe, item.back().data() + item.back().size() - adjust, (DWORD)adjust, NULL, &itr->ol) == FALSE &&
					    GetLastError() != ERROR_IO_PENDING ){
						bClose = TRUE;
					}else if( WaitForMultipleObjects(2, olEvents, FALSE, INFINITE) != WAIT_OBJECT_0 + 1 ){
						//�L�����Z�����ꂽ
						CancelIo(itr->pipe);
						WaitForSingleObject(itr->olEvent, INFINITE);
						sendListSizeOrStop = 0;
					}else if( GetOverlappedResult(itr->pipe, &itr->ol, &xferred, FALSE) == FALSE || xferred < adjust ){
						bClose = TRUE;
					}
					if( bClose ){
						if( itr->strIP == "0.0.0.1" ){
							//�Ăё҂���
							DisconnectNamedPipe(itr->pipe);
							itr->bConnect = FALSE;
							itr->ol = olZero;
							itr->ol.hEvent = itr->olEvent;
							if( ConnectNamedPipe(itr->pipe, &itr->ol) == FALSE ){
								DWORD err = GetLastError();
								if( err == ERROR_PIPE_CONNECTED ){
									itr->bConnect = TRUE;
								}else if( err != ERROR_IO_PENDING ){
									CloseHandle(itr->pipe);
									itr->pipe = INVALID_HANDLE_VALUE;
								}
							}
						}else{
							CloseHandle(itr->pipe);
							itr->pipe = INVALID_HANDLE_VALUE;
							itr->bConnect = FALSE;
						}
					}
				}
				for(;;){
					if( pSys->m_stopSendEvent.WaitOne(0) ){
						//�L�����Z�����ꂽ
						sendListSizeOrStop = 0;
						break;
					}
					if( itr->sock == INVALID_SOCKET || itr->bConnect == FALSE ){
						break;
					}
					if( adjust != 0 ){
						int ret = send(itr->sock, (char*)(item.back().data() + item.back().size() - adjust), (int)adjust, 0);
						if( ret == 0 || (ret == SOCKET_ERROR && WSAGetLastError() != WSAEWOULDBLOCK) ){
							closesocket(itr->sock);
							itr->sock = INVALID_SOCKET;
							itr->bConnect = FALSE;
							break;
						}else if( ret != SOCKET_ERROR ){
							adjust -= ret;
						}
					}
					if( adjust == 0 ){
						dwCount++;
						break;
					}
					//�������҂�
					fd_set wmask;
					FD_ZERO(&wmask);
					FD_SET(itr->sock, &wmask);
					struct timeval tv10msec = {0, 10000};
					select((int)itr->sock + 1, NULL, &wmask, NULL, &tv10msec);
				}
			}
			if( sendListSizeOrStop == 0 ){
				break;
			}
		}
	}

	CBlockLock lock(&pSys->m_sendLock);
	for( auto itr = pSys->m_SendList.begin(); itr != pSys->m_SendList.end(); itr++ ){
		if( itr->sock != INVALID_SOCKET ){
			//�����M�f�[�^���̂Ă��Ă����Ȃ��̂�shutdown()�͏ȗ�
			closesocket(itr->sock);
			itr->sock = INVALID_SOCKET;
		}
		if( itr->pipe != INVALID_HANDLE_VALUE ){
			if( itr->strIP == "0.0.0.1" ){
				if( itr->bConnect ){
					DisconnectNamedPipe(itr->pipe);
				}else{
					//�҂��󂯂��L�����Z��
					CancelIo(itr->pipe);
					WaitForSingleObject(itr->olEvent, INFINITE);
				}
			}
			CloseHandle(itr->pipe);
			itr->pipe = INVALID_HANDLE_VALUE;
		}
		if( itr->olEvent ){
			CloseHandle(itr->olEvent);
			itr->olEvent = NULL;
		}
		itr->bConnect = FALSE;
	}
}
