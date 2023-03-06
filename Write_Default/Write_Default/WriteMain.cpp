#include "stdafx.h"
#include "WriteMain.h"

#ifndef _WIN32
#include <codecvt>
#include <fcntl.h>
#include <locale>
#endif

extern HINSTANCE g_instance;

CWriteMain::CWriteMain(void)
{
#ifdef _WIN32
	this->file = INVALID_HANDLE_VALUE;
	this->teeFile = INVALID_HANDLE_VALUE;
#else
	this->file = NULL;
	this->teeFile = NULL;
#endif
	{
#ifdef _WIN32
		fs_path iniPath = GetModuleIniPath(g_instance);
#else
		fs_path iniPath = GetModuleIniPath();
#endif
		this->writeBuffSize = GetPrivateProfileInt(L"SET", L"Size", 770048, iniPath.c_str());
		this->writeBuff.reserve(this->writeBuffSize);
		this->teeCmd = GetPrivateProfileToString(L"SET", L"TeeCmd", L"", iniPath.c_str());
		if( this->teeCmd.empty() == false ){
			this->teeBuff.resize(GetPrivateProfileInt(L"SET", L"TeeSize", 770048, iniPath.c_str()));
			this->teeBuff.resize(max<size_t>(this->teeBuff.size(), 1));
			this->teeDelay = GetPrivateProfileInt(L"SET", L"TeeDelay", 0, iniPath.c_str());
		}
	}
}


CWriteMain::~CWriteMain(void)
{
	Stop();
}

BOOL CWriteMain::Start(
	LPCWSTR fileName,
	BOOL overWriteFlag,
	ULONGLONG createSize
	)
{
	Stop();

	this->savePath = fileName;
	AddDebugLogFormat(L"★CWriteMain::Start CreateFile:%ls", this->savePath.c_str());
	UtilCreateDirectories(fs_path(this->savePath).parent_path());
#ifdef _WIN32
	this->file = CreateFile(this->savePath.c_str(), GENERIC_WRITE, FILE_SHARE_READ, NULL, overWriteFlag ? CREATE_ALWAYS : CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
	if( this->file == INVALID_HANDLE_VALUE ){
		AddDebugLogFormat(L"★CWriteMain::Start Err:0x%08X", GetLastError());
		fs_path pathWoExt = this->savePath;
		fs_path ext = pathWoExt.extension();
		pathWoExt.replace_extension();
		for( int i = 1; ; i++ ){
			Format(this->savePath, L"%ls-(%d)%ls", pathWoExt.c_str(), i, ext.c_str());
			this->file = CreateFile(this->savePath.c_str(), GENERIC_WRITE, FILE_SHARE_READ, NULL, overWriteFlag ? CREATE_ALWAYS : CREATE_NEW, FILE_ATTRIBUTE_NORMAL, NULL);
			if( this->file != INVALID_HANDLE_VALUE || i >= 999 ){
				DWORD err = GetLastError();
				AddDebugLogFormat(L"★CWriteMain::Start CreateFile:%ls", this->savePath.c_str());
				if( this->file != INVALID_HANDLE_VALUE ){
					break;
				}
				AddDebugLogFormat(L"★CWriteMain::Start Err:0x%08X", err);
				this->savePath = L"";
				return FALSE;
			}
		}
	}
#else
	std::wstring_convert<std::codecvt_utf8<wchar_t>> converter;
	std::string savePathStdString = converter.to_bytes(this->savePath);

	this->file = fopen(savePathStdString.c_str(), "wb");
	if( this->file == NULL ){
		AddDebugLogFormat(L"★CWriteMain::Start Err:0x%08X", errno);
		fs_path pathWoExt = this->savePath;
		fs_path ext = pathWoExt.extension();
		pathWoExt.replace_extension();
		for( int i = 1; ; i++ ){
			Format(this->savePath, L"%ls-(%d)%ls", pathWoExt.c_str(), i, ext.c_str());
			this->file = fopen(savePathStdString.c_str(), "wb");
			if( this->file != NULL || i >= 999 ){
				DWORD err = errno;
				AddDebugLogFormat(L"★CWriteMain::Start CreateFile:%ls", this->savePath.c_str());
				if( this->file != NULL ){
					break;
				}
				AddDebugLogFormat(L"★CWriteMain::Start Err:0x%08X", err);
				this->savePath = L"";
				savePathStdString = "";
				return FALSE;
			}
		}
	}
#endif

	//ディスクに容量を確保
	if( createSize > 0 ){
		LARGE_INTEGER stPos;
		stPos.QuadPart = createSize;
#ifdef _WIN32
		SetFilePointerEx( this->file, stPos, NULL, FILE_BEGIN );
		SetEndOfFile( this->file );
		SetFilePointer( this->file, 0, NULL, FILE_BEGIN );
#else
		fseeko(this->file, stPos.QuadPart, SEEK_SET);
		ftruncate(fileno(this->file), stPos.QuadPart);
		fseeko(this->file, 0, SEEK_SET);
#endif
	}
	this->wrotePos = 0;

	//コマンドに分岐出力
	if( this->teeCmd.empty() == false ){
#ifdef _WIN32
		this->teeFile = CreateFile(this->savePath.c_str(), GENERIC_READ, FILE_SHARE_READ | FILE_SHARE_WRITE, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_SEQUENTIAL_SCAN, NULL);
#else
		this->teeFile = fopen(savePathStdString.c_str(), "rb");
#endif
		if( this->teeFile != INVALID_HANDLE_VALUE ){
			this->teeThreadStopEvent.Reset();
			this->teeThread = thread_(TeeThread, this);
		}
	}

	return TRUE;
}

BOOL CWriteMain::Stop(
	)
{
#ifdef _WIN32
	if( this->file != INVALID_HANDLE_VALUE ){
		if( this->writeBuff.empty() == false ){
			DWORD write;
			if( WriteFile(this->file, &this->writeBuff.front(), (DWORD)this->writeBuff.size(), &write, NULL) == FALSE ){
				AddDebugLogFormat(L"★WriteFile Err:0x%08X", GetLastError());
			}else{
				this->writeBuff.erase(this->writeBuff.begin(), this->writeBuff.begin() + write);
				lock_recursive_mutex lock(this->wroteLock);
				this->wrotePos += write;
			}
			//未出力のバッファは再Start()に備えて繰り越す
		}
		SetEndOfFile(this->file);
		CloseHandle(this->file);
		this->file = INVALID_HANDLE_VALUE;
	}
#else
	if( this->file != NULL ){
		if( this->writeBuff.empty() == false ){
			size_t write = fwrite(&this->writeBuff.front(), 1, this->writeBuff.size(), this->file);
			if( write != this->writeBuff.size() ){
				AddDebugLogFormat(L"★fwrite Err:0x%08X", errno);
			}else{
				this->writeBuff.erase(this->writeBuff.begin(), this->writeBuff.begin() + write);
				lock_recursive_mutex lock(this->wroteLock);
				this->wrotePos += write;
			}
			//未出力のバッファは再Start()に備えて繰り越す
		}
		fflush(this->file);
		ftruncate(fileno(this->file), this->wrotePos);
		fclose(this->file);
		this->file = NULL;
	}
#endif
	if( this->teeThread.joinable() ){
		this->teeThreadStopEvent.Set();
		this->teeThread.join();
	}
#ifdef _WIN32
	if( this->teeFile != INVALID_HANDLE_VALUE ){
		CloseHandle(this->teeFile);
		this->teeFile = INVALID_HANDLE_VALUE;
	}
#else
	if( this->teeFile != NULL ){
		fclose(this->teeFile);
		this->teeFile = NULL;
	}
#endif

	return TRUE;
}

wstring CWriteMain::GetSavePath(
	)
{
	return this->savePath;
}

BOOL CWriteMain::Write(
	BYTE* data,
	DWORD size,
	DWORD* writeSize
	)
{
	if( this->file != INVALID_HANDLE_VALUE && data != NULL && size > 0 ){
		*writeSize = 0;
		if( this->writeBuff.empty() == false ){
			//できるだけバッファにコピー。コピー済みデータは呼び出し側にとっては「保存済み」となる
			*writeSize = min(size, this->writeBuffSize - (DWORD)this->writeBuff.size());
			this->writeBuff.insert(this->writeBuff.end(), data, data + *writeSize);
			data += *writeSize;
			size -= *writeSize;
			if( this->writeBuff.size() >= this->writeBuffSize ){
				//バッファが埋まったので出力
				DWORD write;
#ifdef _WIN32
				if( WriteFile(this->file, &this->writeBuff.front(), (DWORD)this->writeBuff.size(), &write, NULL) == FALSE ){
					AddDebugLogFormat(L"★WriteFile Err:0x%08X", GetLastError());
					SetEndOfFile(this->file);
					CloseHandle(this->file);
					this->file = INVALID_HANDLE_VALUE;
					return FALSE;
				}
#else
				write = fwrite(&this->writeBuff.front(), 1, this->writeBuff.size(), this->file);
				if( write != this->writeBuff.size() ){
					AddDebugLogFormat(L"★fwrite Err:0x%08X", errno);
					ftruncate(fileno(this->file), this->wrotePos);
					fclose(this->file);
					this->file = NULL;
					return FALSE;
				}
#endif
				this->writeBuff.erase(this->writeBuff.begin(), this->writeBuff.begin() + write);
				lock_recursive_mutex lock(this->wroteLock);
				this->wrotePos += write;
			}
			if( this->writeBuff.empty() == false || size == 0 ){
				return TRUE;
			}
		}
		if( size > this->writeBuffSize ){
			//バッファサイズより大きいのでそのまま出力
			DWORD write;
#ifdef _WIN32
			if( WriteFile(this->file, data, size, &write, NULL) == FALSE ){
				AddDebugLogFormat(L"★WriteFile Err:0x%08X", GetLastError());
				SetEndOfFile(this->file);
				CloseHandle(this->file);
				this->file = INVALID_HANDLE_VALUE;
				return FALSE;
			}
#else
			write = fwrite(data, 1, size, this->file);
			if( write != size ){
				AddDebugLogFormat(L"★fwrite Err:0x%08X", errno);
				ftruncate(fileno(this->file), this->wrotePos);
				fclose(this->file);
				this->file = NULL;
				return FALSE;
			}
#endif
			*writeSize += write;
			lock_recursive_mutex lock(this->wroteLock);
			this->wrotePos += write;
		}else{
			//バッファにコピー
			*writeSize += size;
			this->writeBuff.insert(this->writeBuff.end(), data, data + size);
		}
		return TRUE;
	}
	return FALSE;
}

void CWriteMain::TeeThread(CWriteMain* sys)
{
	wstring cmd = sys->teeCmd;
	Replace(cmd, L"$FilePath$", sys->savePath);
	vector<WCHAR> cmdBuff(cmd.c_str(), cmd.c_str() + cmd.size() + 1);
	//カレントは実行ファイルのあるフォルダ
	fs_path currentDir = GetModulePath().parent_path();

#ifdef _WIN32
	HANDLE olEvents[] = { sys->teeThreadStopEvent.Handle(), CreateEvent(NULL, TRUE, FALSE, NULL) };
	if( olEvents[1] ){
		WCHAR pipeName[64];
		swprintf_s(pipeName, L"\\\\.\\pipe\\anon_%08x_%08x", GetCurrentProcessId(), GetCurrentThreadId());
		SECURITY_ATTRIBUTES sa = {};
		sa.nLength = sizeof(sa);
		sa.bInheritHandle = TRUE;

		//出力を速やかに打ち切るために非同期書き込みのパイプを作成する。CreatePipe()は非同期にできない
		HANDLE readPipe = CreateNamedPipe(pipeName, PIPE_ACCESS_INBOUND, 0, 1, 8192, 8192, 0, &sa);
		if( readPipe != INVALID_HANDLE_VALUE ){
			HANDLE writePipe = CreateFile(pipeName, GENERIC_WRITE, 0, NULL, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL | FILE_FLAG_OVERLAPPED, NULL);
			if( writePipe != INVALID_HANDLE_VALUE ){
				//標準入力にパイプしたプロセスを起動する
				STARTUPINFO si = {};
				si.cb = sizeof(si);
				si.dwFlags = STARTF_USESTDHANDLES;
				si.hStdInput = readPipe;
				//標準(エラー)出力はnulデバイスに捨てる
				si.hStdOutput = CreateFile(L"nul", GENERIC_WRITE, 0, &sa, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
				si.hStdError = CreateFile(L"nul", GENERIC_WRITE, 0, &sa, OPEN_EXISTING, FILE_ATTRIBUTE_NORMAL, NULL);
				PROCESS_INFORMATION pi;
				BOOL bRet = CreateProcess(NULL, cmdBuff.data(), NULL, NULL, TRUE, BELOW_NORMAL_PRIORITY_CLASS | CREATE_NO_WINDOW, NULL, currentDir.c_str(), &si, &pi);
				CloseHandle(readPipe);
				if( si.hStdOutput != INVALID_HANDLE_VALUE ){
					CloseHandle(si.hStdOutput);
				}
				if( si.hStdError != INVALID_HANDLE_VALUE ){
					CloseHandle(si.hStdError);
				}
				if( bRet ){
					CloseHandle(pi.hThread);
					CloseHandle(pi.hProcess);
					for(;;){
						__int64 readablePos;
						{
							lock_recursive_mutex lock(sys->wroteLock);
							readablePos = sys->wrotePos - sys->teeDelay;
						}
						LARGE_INTEGER liPos = {};
						DWORD read;
						if( SetFilePointerEx(sys->teeFile, liPos, &liPos, FILE_CURRENT) &&
						    readablePos - liPos.QuadPart >= (__int64)sys->teeBuff.size() &&
						    ReadFile(sys->teeFile, sys->teeBuff.data(), (DWORD)sys->teeBuff.size(), &read, NULL) && read > 0 ){
							OVERLAPPED ol = {};
							ol.hEvent = olEvents[1];
							if( WriteFile(writePipe, sys->teeBuff.data(), read, NULL, &ol) == FALSE && GetLastError() != ERROR_IO_PENDING ){
								//出力完了
								break;
							}
							if( WaitForMultipleObjects(2, olEvents, FALSE, INFINITE) != WAIT_OBJECT_0 + 1 ){
								//打ち切り
								CancelIo(writePipe);
								WaitForSingleObject(olEvents[1], INFINITE);
								break;
							}
							DWORD xferred;
							if( GetOverlappedResult(writePipe, &ol, &xferred, FALSE) == FALSE || xferred < read ){
								//出力完了
								break;
							}
						}else{
							if( WaitForSingleObject(olEvents[0], 200) != WAIT_TIMEOUT ){
								//打ち切り
								break;
							}
						}
					}
					//プロセスは回収しない(標準入力が閉じられた後にどうするかはプロセスの判断に任せる)
				}
				CloseHandle(writePipe);
			}else{
				CloseHandle(readPipe);
			}
		}
		CloseHandle(olEvents[1]);
	}
#else

	// Linux 版の Tee コマンド出力の実装 written by GitHub Copilot
	// 本当に動くかは知らない… (Windows 版でもほとんど誰も使っていないのでは)

	int pipefd[2];

	if( pipe(pipefd) == 0 ){
		pid_t pid = fork();
		if( pid == 0 ){
			//子プロセス
			close(pipefd[1]);
			dup2(pipefd[0], STDIN_FILENO);
			close(pipefd[0]);
			//標準(エラー)出力はnulデバイスに捨てる
			int fd = open("/dev/null", O_WRONLY);
			dup2(fd, STDOUT_FILENO);
			dup2(fd, STDERR_FILENO);
			close(fd);
			//カレントディレクトリを変更する
			std::wstring_convert<std::codecvt_utf8<wchar_t>> converter;
			chdir(converter.to_bytes(currentDir.native()).c_str());
			//コマンドを起動する
			execl("/bin/sh", "sh", "-c", cmdBuff.data(), NULL);
			_exit(1);
		}else if( pid > 0 ){
			//親プロセス
			close(pipefd[0]);
			//標準入力にパイプしたプロセスを起動する
			for(;;){
				__int64 readablePos;
				{
					lock_recursive_mutex lock(sys->wroteLock);
					readablePos = sys->wrotePos - sys->teeDelay;
				}
				LARGE_INTEGER liPos = {};
				DWORD read_;

				// POSIX API では SetFilePointerEx と ReadFile の代わりに lseek と read を使う
				if( lseek(fileno(sys->teeFile), 0, SEEK_CUR) != -1 &&
				    readablePos - lseek(fileno(sys->teeFile), 0, SEEK_CUR) >= (__int64)sys->teeBuff.size() &&
				    (read_ = read(fileno(sys->teeFile), sys->teeBuff.data(), sys->teeBuff.size())) > 0 ){
					if( write(pipefd[1], sys->teeBuff.data(), read_) != read_ ){
						//出力完了
						break;
					}
				}else{
					// POSIX API では WaitForSingleObject と WaitForMultipleObjects の代わりに select を使う
					fd_set fds;
					FD_ZERO(&fds);
					FD_SET(fileno(sys->teeFile), &fds);
					struct timeval tv = {0, 200000};
					if( select(fileno(sys->teeFile) + 1, &fds, NULL, NULL, &tv) <= 0 ){
						//打ち切り
						break;
					}
				}
			}
			//プロセスは回収しない(標準入力が閉じられた後にどうするかはプロセスの判断に任せる)
			close(pipefd[1]);
		}
	}
#endif
}
