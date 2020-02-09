#include "stdafx.h"
#include "BatManager.h"

#include "../../Common/SendCtrlCmd.h"
#include "../../Common/StringUtil.h"
#include "../../Common/PathUtil.h"
#ifndef _WIN32
#include <sys/wait.h>
#include <unistd.h>
#endif

CBatManager::CBatManager(CNotifyManager& notifyManager_, LPCWSTR name)
	: notifyManager(notifyManager_)
{
	this->managerName = name;
	this->idleMargin = MAXDWORD;
	this->nextBatMargin = 0;
}

CBatManager::~CBatManager()
{
	Finalize();
}

void CBatManager::Finalize()
{
	if( this->batWorkThread.joinable() ){
		this->batWorkStopFlag = true;
		this->batWorkEvent.Set();
		this->batWorkThread.join();
	}
}

void CBatManager::AddBatWork(const BAT_WORK_INFO& info)
{
	CBlockLock lock(&this->managerLock);

	this->workList.push_back(info);
	StartWork();
}

void CBatManager::SetIdleMargin(DWORD marginSec)
{
	CBlockLock lock(&this->managerLock);

	this->idleMargin = marginSec;
	StartWork();
}

void CBatManager::SetCustomHandler(LPCWSTR ext, const std::function<void(BAT_WORK_INFO&, vector<char>&)>& handler)
{
	CBlockLock lock(&this->managerLock);

	this->customHandler = handler;
	this->customExt = ext;
}

wstring CBatManager::FindExistingPath(LPCWSTR basePath) const
{
	fs_path path = basePath;
#ifdef _WIN32
	if( UtilFileExists(path.concat(L".bat")).first ||
	    UtilFileExists(path.replace_extension(L".ps1")).first ){
#else
	if( UtilFileExists(path.concat(L".sh")).first ){
#endif
		return path.native();
	}
	{
		CBlockLock lock(&this->managerLock);
		if( !this->customHandler ){
			return wstring();
		}
		path.replace_extension(this->customExt);
	}
	return UtilFileExists(path).first ? path.native() : wstring();
}

bool CBatManager::IsWorking() const
{
	CBlockLock lock(&this->managerLock);

	return this->workList.empty() == false;
}

bool CBatManager::IsWorkingWithoutNotification() const
{
	CBlockLock lock(&this->managerLock);

	return std::find_if(this->workList.begin(), this->workList.end(),
	                    [](const BAT_WORK_INFO& a) { return a.macroList.empty() || a.macroList[0].first != "NotifyID"; }) != this->workList.end();
}

void CBatManager::StartWork()
{
	CBlockLock lock(&this->managerLock);

	//���[�J�X���b�h���I�����悤�Ƃ��Ă���Ƃ��͂��̊�����҂�
	if( this->batWorkThread.joinable() && this->batWorkExitingFlag ){
		this->batWorkThread.join();
	}
	if( this->workList.empty() == false && this->idleMargin >= this->nextBatMargin ){
		if( this->batWorkThread.joinable() ){
			this->batWorkEvent.Set();
		}else{
			this->batWorkStopFlag = false;
			this->batWorkExitingFlag = false;
			this->batWorkThread = thread_(BatWorkThread, this);
		}
	}
}

void CBatManager::BatWorkThread(CBatManager* sys)
{
	DWORD notifyWorkWait = 0;
	BAT_WORK_INFO notifyWork;
	for(;;){
		while( notifyWorkWait && sys->batWorkStopFlag == false && sys->IsWorking() == false ){
			DWORD tick = GetTickCount();
			sys->batWorkEvent.WaitOne(notifyWorkWait);
			DWORD diff = GetTickCount() - tick;
			notifyWorkWait -= min(diff, notifyWorkWait);
			if( notifyWorkWait == 0 ){
				CBlockLock lock(&sys->managerLock);
				sys->workList.push_back(std::move(notifyWork));
			}
		}
		if( sys->batWorkStopFlag ){
			//���~
			break;
		}

		BAT_WORK_INFO work;
		bool workable = true;
		std::function<void(BAT_WORK_INFO&, vector<char>&)> customHandler_;
		{
			CBlockLock lock(&sys->managerLock);
			if( sys->workList.empty() ){
				//���̃t���O�𗧂Ă����Ƃ͓�x�ƃ��b�N���m�ۂ��Ă͂����Ȃ�
				sys->batWorkExitingFlag = true;
				sys->nextBatMargin = 0;
				break;
			}
			work = sys->workList[0];
#ifdef _WIN32
			if( !UtilPathEndsWith(work.batFilePath.c_str(), L".bat") &&
			    !UtilPathEndsWith(work.batFilePath.c_str(), L".ps1") ){
#else
			if( !UtilPathEndsWith(work.batFilePath.c_str(), L".sh") ){
#endif
				if( sys->customHandler && UtilPathEndsWith(work.batFilePath.c_str(), sys->customExt.c_str()) ){
					customHandler_ = sys->customHandler;
				}else{
					workable = false;
				}
			}
		}

		if( workable ){
			DWORD exBatMargin;
			DWORD exNotifyInterval;
			WORD exSW;
			bool exDirect;
			vector<char> buff;
			if( sys->CreateBatFile(work, exBatMargin, exNotifyInterval, exSW, exDirect, buff) ){
				{
					CBlockLock lock(&sys->managerLock);
					if( sys->idleMargin < exBatMargin ){
						//�A�C�h�����Ԃɗ]�T���Ȃ��̂Œ��~
						sys->batWorkExitingFlag = true;
						sys->nextBatMargin = exBatMargin;
						break;
					}
					//NotifyInterval�g��: macroList[0]��"NotifyID"�̂Ƃ��A���̒l��"0"�ɂ��Ďw�莞�Ԍ�ɍĔ��s����BidleMargin�ƕ��p�s��
					if( exNotifyInterval && notifyWorkWait == 0 &&
					    sys->workList[0].macroList.empty() == false && sys->workList[0].macroList[0].first == "NotifyID" ){
						notifyWorkWait = exNotifyInterval;
						notifyWork = sys->workList[0];
						notifyWork.macroList[0].second = L"0";
					}
				}
				if( buff.empty() == false ){
					if( customHandler_ ){
						customHandler_(work, buff);
					}
				}else{
#ifdef _WIN32
					bool executed = false;
					HANDLE hProcess = NULL;
					if( exDirect == false && sys->notifyManager.IsGUI() == false ){
						//�\���ł��Ȃ��̂�GUI�o�R�ŋN�����Ă݂�
						CSendCtrlCmd ctrlCmd;
						vector<DWORD> registGUI = sys->notifyManager.GetRegistGUI();
						for( size_t i = 0; i < registGUI.size(); i++ ){
							ctrlCmd.SetPipeSetting(CMD2_GUI_CTRL_PIPE, registGUI[i]);
							DWORD pid;
							if( ctrlCmd.SendGUIExecute(L'"' + work.batFilePath + L'"', &pid) == CMD_SUCCESS ){
								//�n���h���J���O�ɏI�����邩������Ȃ�
								executed = true;
								hProcess = OpenProcess(SYNCHRONIZE | PROCESS_SET_INFORMATION, FALSE, pid);
								if( hProcess ){
									SetPriorityClass(hProcess, BELOW_NORMAL_PRIORITY_CLASS);
								}
								break;
							}
						}
					}
					if( executed == false ){
						PROCESS_INFORMATION pi;
						STARTUPINFO si = {};
						si.cb = sizeof(si);
						si.dwFlags = STARTF_USESHOWWINDOW;
						si.wShowWindow = exSW;
						fs_path exePath;
						wstring strParam;
						if( UtilPathEndsWith(work.batFilePath.c_str(), L".ps1") ){
							//PowerShell
							strParam = L" -NoProfile -ExecutionPolicy RemoteSigned -File \"" + work.batFilePath + L"\"";
							WCHAR szSystemRoot[MAX_PATH];
							DWORD dwRet = GetEnvironmentVariable(L"SystemRoot", szSystemRoot, MAX_PATH);
							if( dwRet && dwRet < MAX_PATH ){
								exePath = szSystemRoot;
								exePath.append(L"System32\\WindowsPowerShell\\v1.0\\powershell.exe");
							}
						}else{
							//�R�}���h�v�����v�g
							strParam = L" /c \"\"" + work.batFilePath + L"\" \"";
							WCHAR szComSpec[MAX_PATH];
							DWORD dwRet = GetEnvironmentVariable(L"ComSpec", szComSpec, MAX_PATH);
							if( dwRet && dwRet < MAX_PATH ){
								exePath = szComSpec;
							}
						}
						vector<WCHAR> strBuff(strParam.c_str(), strParam.c_str() + strParam.size() + 1);
						//�����ŒZ���]�������C4701(pi����������)�ɂȂ�B�����炭�U�z��
						if( exePath.empty() == false ){
							if( CreateProcess(exePath.c_str(), strBuff.data(), NULL, NULL, FALSE,
							                  BELOW_NORMAL_PRIORITY_CLASS | (exDirect ? CREATE_UNICODE_ENVIRONMENT : 0),
							                  exDirect ? const_cast<LPWSTR>(CreateEnvironment(work).c_str()) : NULL,
							                  exDirect ? fs_path(work.batFilePath).parent_path().c_str() : NULL, &si, &pi) ){
								CloseHandle(pi.hThread);
								hProcess = pi.hProcess;
							}
						}
						if( hProcess == NULL ){
							_OutputDebugString(L"BAT�N���G���[�F%ls\r\n", work.batFilePath.c_str());
						}
					}
					if( hProcess ){
						//�I���Ď�
						HANDLE hEvents[2] = { sys->batWorkEvent.Handle(), hProcess };
						while( WaitForMultipleObjects(2, hEvents, FALSE, INFINITE) == WAIT_OBJECT_0 && sys->batWorkStopFlag == false );
						CloseHandle(hProcess);
					}
#else
					string execPath;
					WtoUTF8(work.batFilePath, execPath);
					string execDir;
					WtoUTF8(fs_path(work.batFilePath).parent_path().native(), execDir);
					pid_t pid = fork();
					if( pid == 0 ){
						if( chdir(execDir.c_str()) == 0 ){
							for( size_t i = 0; i < work.macroList.size(); i++ ){
								string strVal;
								WtoUTF8(work.macroList[i].second, strVal);
								setenv(work.macroList[i].first.c_str(), strVal.c_str(), 0);
							}
							execl(execPath.c_str(), execPath.c_str(), NULL);
						}
						exit(EXIT_FAILURE);
					}
					if( pid != -1 ){
						//�I���Ď�(�����悢��@�͂Ȃ����̂��c)
						while( sys->batWorkStopFlag == false && waitpid(pid, NULL, WNOHANG) == 0 ){
							sys->batWorkEvent.WaitOne(200);
						}
					}
#endif
				}
			}else{
				_OutputDebugString(L"BAT�t�@�C���쐬�G���[�F%ls\r\n", work.batFilePath.c_str());
			}
		}else{
			_OutputDebugString(L"BAT�g���q�G���[�F%ls\r\n", work.batFilePath.c_str());
		}

		CBlockLock lock(&sys->managerLock);
		sys->workList.erase(sys->workList.begin());
	}
}

bool CBatManager::CreateBatFile(BAT_WORK_INFO& info, DWORD& exBatMargin, DWORD& exNotifyInterval, WORD& exSW, bool& exDirect, vector<char>& buff) const
{
	//�o�b�`�̍쐬
	std::unique_ptr<FILE, decltype(&fclose)> fp(UtilOpenFile(info.batFilePath, UTIL_SECURE_READ), fclose);
	if( !fp ){
		return false;
	}

	//�g������: BatMargin
	exBatMargin = 0;
	//�g������: NotifyInterval
	exNotifyInterval = 0;
	//�g������: ���n���ɂ�钼�ڎ��s
	exDirect = true;
	//�J�X�^���n���h���p�t�@�C���̒��g
	buff.clear();
#ifdef _WIN32
	//�g������: �����ɂ��Ă̕ϐ���ISO�`���ɂ���
	bool exFormatTime = true;
	//�g������: �E�B���h�E�\�����
	exSW = SW_SHOWMINNOACTIVE;

	if( UtilPathEndsWith(info.batFilePath.c_str(), L".bat") ){
		exDirect = false;
		exFormatTime = false;
	}
#endif
	__int64 fileSize = 0;
	char olbuff[257];
	for( size_t n = fread(olbuff, 1, 256, fp.get()); ; n = fread(olbuff + 64, 1, 192, fp.get()) + 64 ){
		olbuff[n] = '\0';
		if( strstr(olbuff, "_EDCBX_BATMARGIN_=") ){
			//�ꎞ�I�ɒf�Ђ��i�[���邩������Ȃ����ŏI�I�ɐ�������΂悢
			exBatMargin = strtoul(strstr(olbuff, "_EDCBX_BATMARGIN_=") + 18, NULL, 10) * 60;
		}
		if( strstr(olbuff, "_EDCBX_NOTIFY_INTERVAL_=") ){
			exNotifyInterval = strtoul(strstr(olbuff, "_EDCBX_NOTIFY_INTERVAL_=") + 24, NULL, 10) * 1000;
		}
#ifdef _WIN32
		if( strstr(olbuff, "_EDCBX_HIDE_") ){
			exSW = SW_HIDE;
		}
		if( strstr(olbuff, "_EDCBX_NORMAL_") ){
			exSW = SW_SHOWNORMAL;
		}
		exDirect = exDirect || strstr(olbuff, "_EDCBX_DIRECT_");
		exFormatTime = exFormatTime || strstr(olbuff, "_EDCBX_FORMATTIME_");
#endif
		fileSize += (fileSize == 0 ? n : n - 64);
		if( n < 256 ){
			break;
		}
		std::copy(olbuff + 192, olbuff + 256, olbuff);
	}

	for( size_t i = 0; i < info.macroList.size(); ){
#ifdef _WIN32
		if( exFormatTime == false ){
			if( info.macroList[i].first.compare(0, 9, "StartTime") == 0 ||
			    info.macroList[i].first.compare(0, 14, "DurationSecond") == 0 ){
				info.macroList.erase(info.macroList.begin() + i);
				continue;
			}
			//�R�����g�A�E�g����������
			if( info.macroList[i].first.compare(0, 1, "#") == 0 ){
				info.macroList[i].first.erase(0, 1);
			}
		}
#endif
		//�R�����g�A�E�g����Ă�����̂�����
		if( info.macroList[i].first.compare(0, 1, "#") == 0 ){
			info.macroList.erase(info.macroList.begin() + i);
			continue;
		}
		for( size_t j = 0; j < info.macroList[i].second.size(); j++ ){
			//���䕶���ƃ_�u���N�H�[�g�͒u��������
			if( (L'\x1' <= info.macroList[i].second[j] && info.macroList[i].second[j] <= L'\x1f') || info.macroList[i].second[j] == L'\x7f' ){
				info.macroList[i].second[j] = L'��';
			}else if( info.macroList[i].second[j] == L'"' ){
				info.macroList[i].second[j] = L'�h';
			}
		}
		i++;
	}
#ifdef _WIN32
	if( exDirect && (UtilPathEndsWith(info.batFilePath.c_str(), L".bat") || UtilPathEndsWith(info.batFilePath.c_str(), L".ps1")) ){
#else
	if( UtilPathEndsWith(info.batFilePath.c_str(), L".sh") ){
#endif
		return true;
	}

	if( fileSize >= 64 * 1024 * 1024 ){
		return false;
	}
	buff.resize((size_t)fileSize + 1, '\0');
	rewind(fp.get());
	if( fread(buff.data(), 1, buff.size() - 1, fp.get()) != buff.size() - 1 ){
		return false;
	}
#ifdef _WIN32
	if( exDirect ){
		//�J�X�^���n���h��
		return true;
	}
	string strRead = buff.data();
	buff.clear();

	for( size_t pos = 0;; ){
		pos = strRead.find('$', pos);
		if( pos == string::npos ){
			break;
		}
		size_t next = strRead.find('$', pos + 1);
		if( next == string::npos ){
			break;
		}
		//"$�}�N��$"������Βu��
		auto itrMacro = std::find_if(info.macroList.begin(), info.macroList.end(),
			[=, &strRead](const pair<string, wstring>& a) { return strRead.compare(pos + 1, next - pos - 1, a.first) == 0; });
		if( itrMacro == info.macroList.end() ){
			pos++;
		}else{
			string strValA;
			WtoA(itrMacro->second, strValA);
			strRead.replace(pos, next - pos + 1, strValA);
			pos += strValA.size();
		}
	}

	//�ꎞ�t�@�C���ɃR�s�[
	info.batFilePath = GetCommonIniPath().replace_filename(this->managerName).concat(L".bat").native();
	fp.reset(UtilOpenFile(info.batFilePath, UTIL_SECURE_WRITE | UTIL_F_IONBF));
	if( !fp || fputs(strRead.c_str(), fp.get()) < 0 ){
		return false;
	}
#endif

	return true;
}

#ifdef _WIN32
wstring CBatManager::CreateEnvironment(const BAT_WORK_INFO& info)
{
	wstring strEnv;
	LPWCH env = GetEnvironmentStrings();
	if( env ){
		size_t n = 0;
		while( env[n] ){
			n += wcslen(env + n) + 1;
		}
		strEnv.assign(env, env + n);
		FreeEnvironmentStrings(env);
	}
	for( size_t i = 0; i < info.macroList.size(); i++ ){
		wstring strVar;
		UTF8toW(info.macroList[i].first, strVar);
		//��������ϐ����Ȃ���Βǉ�
		bool unique = true;
		for( size_t n = 0; unique && n < strEnv.size(); ){
			size_t m = strEnv.find_first_of(L'\0', n);
			if( m - n > strVar.size() && strEnv[n + strVar.size()] == L'=' ){
				strEnv[n + strVar.size()] = L'\0';
				unique = CompareNoCase(strVar, strEnv.c_str() + n) != 0;
				strEnv[n + strVar.size()] = L'=';
			}
			n = m + 1;
		}
		if( unique ){
			strEnv += strVar + L'=' + info.macroList[i].second + L'\0';
		}
	}
	return strEnv;
}
#endif
