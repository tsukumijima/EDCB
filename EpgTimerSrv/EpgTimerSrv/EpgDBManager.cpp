#include "stdafx.h"
#include "EpgDBManager.h"

#include "../../Common/CommonDef.h"
#include "../../Common/TimeUtil.h"
#include "../../Common/StringUtil.h"
#include "../../Common/PathUtil.h"
#include "../../Common/EpgTimerUtil.h"
#include "../../Common/EpgDataCap3Util.h"
#include "../../Common/CtrlCmdUtil.h"
#include <list>

CEpgDBManager::CEpgDBManager()
{
	this->epgMapRefLock = std::make_pair(0, &this->epgMapLock);
	this->loadStop = false;
	this->loadForeground = false;
	this->initialLoadDone = false;
	this->archivePeriodSec = 0;
}

CEpgDBManager::~CEpgDBManager()
{
	CancelLoadData();
}

void CEpgDBManager::SetArchivePeriod(int periodSec)
{
	CBlockLock lock(&this->epgMapLock);
	this->archivePeriodSec = periodSec;
}

void CEpgDBManager::ReloadEpgData(bool foreground)
{
	CancelLoadData();

	//�t�H�A�O���E���h�ǂݍ��݂𒆒f�����ꍇ�͈����p��
	if( this->loadForeground == false ){
		this->loadForeground = foreground;
	}
	this->loadThread = thread_(LoadThread, this);
}

namespace
{

//�����A�[�J�C�u�p�t�@�C�����J��
FILE* OpenOldArchive(LPCWSTR dir, __int64 t, int flags)
{
	SYSTEMTIME st;
	ConvertSystemTime(t, &st);
	WCHAR name[32];
	swprintf_s(name, L"%04d%02d%02d.dat", st.wYear, st.wMonth, st.wDay);
	return UtilOpenFile(fs_path(dir).append(name), flags);
}

//���݂��钷���A�[�J�C�u�̓��t���������Ń��X�g����
vector<__int64> ListOldArchive(LPCWSTR dir)
{
	vector<__int64> timeList;
	EnumFindFile(fs_path(dir).append(L"????????.dat"), [&](UTIL_FIND_DATA& findData) -> bool {
		if( findData.isDir == false && findData.fileName.size() == 12 ){
			//���t(�K�����j��)�����
			LPWSTR endp;
			DWORD ymd = wcstoul(findData.fileName.c_str(), &endp, 10);
			if( endp && endp - findData.fileName.c_str() == 8 && endp[0] == L'.' ){
				SYSTEMTIME st = {};
				st.wYear = ymd / 10000 % 10000;
				st.wMonth = ymd / 100 % 100;
				st.wDay = ymd % 100;
				__int64 t = ConvertI64Time(st);
				if( t != 0 && ConvertSystemTime(t, &st) && st.wDayOfWeek == 0 ){
					timeList.push_back(t);
				}
			}
		}
		return true;
	});
	std::sort(timeList.begin(), timeList.end());
	return timeList;
}

//�����A�[�J�C�u�p�t�@�C���̃C���f�b�N�X�̈��ǂ�
void ReadOldArchiveIndex(FILE* fp, vector<BYTE>& buff, vector<__int64>& index, DWORD* headerSize)
{
	rewind(fp);
	buff.clear();
	index.clear();
	while( buff.size() < 4096 * 1024 ){
		buff.resize(buff.size() + 1024);
		size_t n = fread(buff.data() + buff.size() - 1024, 1, 1024, fp);
		buff.resize(buff.size() - 1024 + n);
		if( n == 0 || ReadVALUE(&index, buff.data(), (DWORD)buff.size(), headerSize) ){
			break;
		}
		index.clear();
	}
}

//�����A�[�J�C�u�p�t�@�C���̓���ʒu��EPG�f�[�^��ǂ�
void ReadOldArchiveEventInfo(FILE* fp, const vector<__int64>& index, size_t indexPos, DWORD headerSize, vector<BYTE>& buff, EPGDB_SERVICE_EVENT_INFO& info)
{
	buff.clear();
	info.eventList.clear();
	DWORD buffSize = (DWORD)index[indexPos];
	__int64 pos = headerSize;
	for( size_t i = 0; i + 3 < indexPos; i += 4 ){
		pos += (DWORD)index[i];
	}
	if( buffSize > 0 && _fseeki64(fp, 0, SEEK_END) == 0 && _ftelli64(fp) >= pos + buffSize && _fseeki64(fp, pos, SEEK_SET) == 0 ){
		buff.resize(buffSize);
		if( fread(buff.data(), 1, buffSize, fp) == buffSize ){
			WORD ver;
			DWORD readSize;
			if( ReadVALUE(&ver, buff.data(), buffSize, &readSize) == FALSE ||
			    ReadVALUE2(ver, &info, buff.data() + readSize, buffSize - readSize, NULL) == FALSE ){
				info.eventList.clear();
			}
		}
	}
}

}

void CEpgDBManager::LoadThread(CEpgDBManager* sys)
{
	OutputDebugString(L"Start Load EpgData\r\n");
	DWORD time = GetTickCount();

	if( sys->loadForeground == false ){
		//�o�b�N�O���E���h�Ɉڍs
		SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_BELOW_NORMAL);
	}
	CEpgDataCap3Util epgUtil;
	if( epgUtil.Initialize(FALSE) != NO_ERR ){
		OutputDebugString(L"��EpgDataCap3�̏������Ɏ��s���܂����B\r\n");
		sys->loadForeground = false;
		sys->initialLoadDone = true;
		sys->loadStop = true;
		return;
	}

	__int64 utcNow = GetNowI64Time() - I64_UTIL_TIMEZONE;

	//EPG�t�@�C���̌���
	vector<wstring> epgFileList;
	const fs_path settingPath = GetSettingPath();
	const fs_path epgDataPath = fs_path(settingPath).append(EPG_SAVE_FOLDER);

	//�w��t�H���_�̃t�@�C���ꗗ�擾
	EnumFindFile(fs_path(epgDataPath).append(L"*_epg.dat"), [&](UTIL_FIND_DATA& findData) -> bool {
		if( findData.isDir == false && findData.lastWriteTime != 0 ){
			//���������t�@�C�����ꗗ�ɒǉ�
			//���O���B������TSID==0xFFFF�̏ꍇ�͓����`�����l���̘A���ɂ��X�g���[�����N���A����Ȃ��\��������̂Ō��ɂ܂Ƃ߂�
			WCHAR prefix = findData.lastWriteTime + 7 * 24 * 60 * 60 * I64_1SEC < utcNow ? L'0' :
			               UtilPathEndsWith(findData.fileName.c_str(), L"ffff_epg.dat") ? L'2' :  L'1';
			wstring item = prefix + fs_path(epgDataPath).append(findData.fileName).native();
			epgFileList.insert(std::lower_bound(epgFileList.begin(), epgFileList.end(), item), item);
		}
		return true;
	});

	DWORD loadElapsed = 0;
	DWORD loadTick = GetTickCount();

	//EPG�t�@�C���̉��
	for( vector<wstring>::iterator itr = epgFileList.begin(); itr != epgFileList.end(); itr++ ){
		if( sys->loadStop ){
			//�L�����Z�����ꂽ
			return;
		}
		fs_path path = itr->c_str() + 1;
		if( (*itr)[0] == L'0' ){
			//1�T�Ԉȏ�O�̃t�@�C���Ȃ̂ō폜
			DeleteFile(path.c_str());
			_OutputDebugString(L"��delete %ls\r\n", path.c_str());
		}else{
			BYTE readBuff[188*256];
			bool swapped = false;
			std::unique_ptr<FILE, decltype(&fclose)> file(NULL, fclose);
			//�ꎞ�t�@�C���̏�Ԃ𒲂ׂ�B�擾����CreateFile(tmp)��CloseHandle(tmp)��CopyFile(tmp,master)��DeleteFile(tmp)�̗����������x����
			bool mightExist = false;
			if( UtilFileExists(fs_path(path).concat(L".tmp"), &mightExist).first || mightExist ){
				//�ꎞ�t�@�C�������遨���������㏑������邩������Ȃ��̂ŋ��L�ŊJ���đޔ�������
				_OutputDebugString(L"��lockless read %ls\r\n", path.c_str());
				for( int retry = 0; retry < 25; retry++ ){
					std::unique_ptr<FILE, decltype(&fclose)> masterFile(UtilOpenFile(path, UTIL_SHARED_READ), fclose);
					if( !masterFile ){
						Sleep(200);
						continue;
					}
					for( retry = 0; retry < 3; retry++ ){
						file.reset(UtilOpenFile(fs_path(path).concat(L".swp"), UTIL_O_CREAT_RDWR));
						if( file ){
							swapped = true;
							rewind(masterFile.get());
							for( size_t n; (n = fread(readBuff, 1, sizeof(readBuff), masterFile.get())) != 0; ){
								fwrite(readBuff, 1, n, file.get());
							}
							for( int i = 0; i < 25; i++ ){
								//�ꎞ�t�@�C����ǂݍ��݋��L�ŊJ���遨�㏑������������Ȃ��̂ŏ����҂�
								if( !std::unique_ptr<FILE, decltype(&fclose)>(UtilOpenFile(
								        fs_path(path).concat(L".tmp"), UTIL_O_RDONLY | UTIL_SH_READ | UTIL_SH_DELETE), fclose) ){
									break;
								}
								Sleep(200);
							}
							//�ޔ𒆂ɏ㏑������Ă��Ȃ����Ƃ��m�F����
							bool matched = false;
							rewind(masterFile.get());
							rewind(file.get());
							for(;;){
								size_t n = fread(readBuff + sizeof(readBuff) / 2, 1, sizeof(readBuff) / 2, file.get());
								if( n == 0 ){
									matched = fread(readBuff, 1, sizeof(readBuff) / 2, masterFile.get()) == 0;
									break;
								}else if( fread(readBuff, 1, n, masterFile.get()) != n || memcmp(readBuff, readBuff + sizeof(readBuff) / 2, n) ){
									break;
								}
							}
							if( matched ){
								rewind(file.get());
								break;
							}
							file.reset();
						}
						Sleep(200);
					}
					break;
				}
			}else{
				//�r���ŊJ��
				file.reset(UtilOpenFile(path, UTIL_SECURE_READ));
			}
			if( !file ){
				_OutputDebugString(L"Error %ls\r\n", path.c_str());
			}else{
				//PAT�𑗂�(�X�g���[�����m���Ƀ��Z�b�g���邽��)
				DWORD seekPos = 0;
				for( DWORD i = 0; fread(readBuff, 1, 188, file.get()) == 188; i += 188 ){
					//PID
					if( ((readBuff[1] & 0x1F) << 8 | readBuff[2]) == 0 ){
						//payload_unit_start_indicator
						if( (readBuff[1] & 0x40) != 0 ){
							if( seekPos != 0 ){
								break;
							}
						}else if( seekPos == 0 ){
							continue;
						}
						seekPos = i + 188;
						epgUtil.AddTSPacket(readBuff, 188);
					}
				}
				_fseeki64(file.get(), seekPos, SEEK_SET);
				//TOT��擪�Ɏ����Ă��đ���(�X�g���[���̎������m�肳���邽��)
				bool ignoreTOT = false;
				while( fread(readBuff, 1, 188, file.get()) == 188 ){
					if( ((readBuff[1] & 0x1F) << 8 | readBuff[2]) == 0x14 ){
						ignoreTOT = true;
						epgUtil.AddTSPacket(readBuff, 188);
						break;
					}
				}
				_fseeki64(file.get(), seekPos, SEEK_SET);
				for( size_t n; (n = fread(readBuff, 1, sizeof(readBuff), file.get())) != 0; ){
					for( size_t i = 0; i + 188 <= n; i += 188 ){
						if( ignoreTOT && ((readBuff[i+1] & 0x1F) << 8 | readBuff[i+2]) == 0x14 ){
							ignoreTOT = false;
						}else{
							epgUtil.AddTSPacket(readBuff+i, 188);
						}
					}
					if( sys->loadForeground == false ){
						//�������x����������2/3�ɂȂ�悤�ɋx�ށBI/O���׌y�����_��
						DWORD tick = GetTickCount();
						loadElapsed += tick - loadTick;
						loadTick = tick;
						if( loadElapsed > 20 ){
							Sleep(min<DWORD>(loadElapsed / 2, 100));
							loadElapsed = 0;
							loadTick = GetTickCount();
						}
					}
				}
				file.reset();
			}
			if( swapped ){
				DeleteFile(fs_path(path).concat(L".swp").c_str());
			}
		}
	}

	//EPG�f�[�^���擾
	DWORD serviceListSize = 0;
	SERVICE_INFO* serviceList = NULL;
	if( epgUtil.GetServiceListEpgDB(&serviceListSize, &serviceList) == FALSE ){
		sys->loadForeground = false;
		sys->initialLoadDone = true;
		sys->loadStop = true;
		return;
	}
	map<LONGLONG, EPGDB_SERVICE_EVENT_INFO> nextMap;
	for( const SERVICE_INFO* info = serviceList; info != serviceList + serviceListSize; info++ ){
		LONGLONG key = Create64Key(info->original_network_id, info->transport_stream_id, info->service_id);
		EPGDB_SERVICE_EVENT_INFO itemZero = {};
		EPGDB_SERVICE_EVENT_INFO& item = nextMap.insert(std::make_pair(key, itemZero)).first->second;
		item.serviceInfo.ONID = info->original_network_id;
		item.serviceInfo.TSID = info->transport_stream_id;
		item.serviceInfo.SID = info->service_id;
		if( info->extInfo != NULL ){
			item.serviceInfo.service_type = info->extInfo->service_type;
			item.serviceInfo.partialReceptionFlag = info->extInfo->partialReceptionFlag;
			if( info->extInfo->service_provider_name != NULL ){
				item.serviceInfo.service_provider_name = info->extInfo->service_provider_name;
			}
			if( info->extInfo->service_name != NULL ){
				item.serviceInfo.service_name = info->extInfo->service_name;
			}
			if( info->extInfo->network_name != NULL ){
				item.serviceInfo.network_name = info->extInfo->network_name;
			}
			if( info->extInfo->ts_name != NULL ){
				item.serviceInfo.ts_name = info->extInfo->ts_name;
			}
			item.serviceInfo.remote_control_key_id = info->extInfo->remote_control_key_id;
		}
		epgUtil.EnumEpgInfoList(item.serviceInfo.ONID, item.serviceInfo.TSID, item.serviceInfo.SID, EnumEpgInfoListProc, &item);
	}
	epgUtil.UnInitialize();

	__int64 arcMax = GetNowI64Time() / I64_1SEC * I64_1SEC;
	__int64 arcMin = LLONG_MAX;
	__int64 oldMax = LLONG_MIN;
	__int64 oldMin = LLONG_MIN;
	{
		CBlockLock lock(&sys->epgMapLock);
		if( sys->archivePeriodSec > 0 ){
			//�A�[�J�C�u����
			arcMin = arcMax - sys->archivePeriodSec * I64_1SEC;
			if( sys->archivePeriodSec > 14 * 24 * 3600 ){
				//�����A�[�J�C�u����
				SYSTEMTIME st;
				ConvertSystemTime(arcMax - 14 * 24 * 3600 * I64_1SEC, &st);
				//�Ώۂ�2�T�ȏ�O�̓��j0������1�T��
				oldMin = arcMax - ((((14 + st.wDayOfWeek) * 24 + st.wHour) * 60 + st.wMinute) * 60 + st.wSecond) * I64_1SEC;
				oldMax = oldMin + 7 * 24 * 3600 * I64_1SEC;
			}
		}
	}
	arcMax += 3600 * I64_1SEC;

	//����̓A�[�J�C�u�t�@�C������ǂݍ���
	map<LONGLONG, EPGDB_SERVICE_EVENT_INFO> arcFromFile;
	if( arcMin < LLONG_MAX && sys->epgArchive.empty() ){
		vector<BYTE> buff;
		std::unique_ptr<FILE, decltype(&fclose)> fp(UtilOpenFile(fs_path(settingPath).append(EPG_ARCHIVE_DATA_NAME), UTIL_SECURE_READ), fclose);
		if( fp && _fseeki64(fp.get(), 0, SEEK_END) == 0 ){
			__int64 fileSize = _ftelli64(fp.get());
			if( 0 < fileSize && fileSize < INT_MAX ){
				buff.resize((size_t)fileSize);
				rewind(fp.get());
				if( fread(buff.data(), 1, buff.size(), fp.get()) != buff.size() ){
					buff.clear();
				}
			}
		}
		if( buff.empty() == false ){
			WORD ver;
			DWORD readSize;
			vector<EPGDB_SERVICE_EVENT_INFO> list;
			if( ReadVALUE(&ver, buff.data(), (DWORD)buff.size(), &readSize) &&
			    ReadVALUE2(ver, &list, buff.data() + readSize, (DWORD)buff.size() - readSize, NULL) ){
				for( size_t i = 0; i < list.size(); i++ ){
					LONGLONG key = Create64Key(list[i].serviceInfo.ONID, list[i].serviceInfo.TSID, list[i].serviceInfo.SID);
					arcFromFile[key] = std::move(list[i]);
				}
			}
		}
	}

	//�����A�[�J�C�u�̃C���f�b�N�X�̈���L���b�V��
	vector<vector<__int64>> oldCache;
	if( oldMin > LLONG_MIN ){
		fs_path epgArcPath = fs_path(settingPath).append(EPG_ARCHIVE_FOLDER);
		//�L���b�V���̐擪�͓��t���
		oldCache.push_back(ListOldArchive(epgArcPath.c_str()));
		oldCache.resize(1 + oldCache.front().size());
		if( sys->epgOldIndexCache.empty() == false ){
			for( size_t i = 0; i < sys->epgOldIndexCache.front().size(); i++ ){
				size_t j = std::lower_bound(oldCache.front().begin(), oldCache.front().end(), sys->epgOldIndexCache.front()[i]) - oldCache.front().begin();
				if( j != oldCache.front().size() && oldCache.front()[j] == sys->epgOldIndexCache.front()[i] ){
					//�L���b�V���ς݂̂��̂��p��
					oldCache[1 + j] = sys->epgOldIndexCache[1 + i];
				}
			}
		}
		vector<BYTE> buff;
		for( size_t i = 0; i < oldCache.front().size(); i++ ){
			if( oldCache[1 + i].empty() ){
				//�L���b�V������
				std::unique_ptr<FILE, decltype(&fclose)> fp(OpenOldArchive(epgArcPath.c_str(), oldCache.front()[i], UTIL_SECURE_READ), fclose);
				if( fp ){
					ReadOldArchiveIndex(fp.get(), buff, oldCache[1 + i], NULL);
				}
			}
		}
	}

	if( sys->loadForeground == false ){
		//�t�H�A�O���E���h�ɕ��A
		SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_NORMAL);
	}
	for(;;){
		//�f�[�^�x�[�X��r������
		{
			CBlockLock lock(&sys->epgMapLock);
			if( sys->epgMapRefLock.first == 0 ){
				if( arcFromFile.empty() == false ){
					sys->epgArchive.swap(arcFromFile);
				}
				//�C�x���g���A�[�J�C�u�Ɉړ�����
				for( auto itr = sys->epgMap.begin(); arcMin < LLONG_MAX && itr != sys->epgMap.end(); itr++ ){
					auto itrArc = sys->epgArchive.find(itr->first);
					if( itrArc != sys->epgArchive.end() ){
						itrArc->second.serviceInfo = std::move(itr->second.serviceInfo);
					}
					for( size_t i = 0; i < itr->second.eventList.size(); i++ ){
						if( itr->second.eventList[i].StartTimeFlag &&
						    itr->second.eventList[i].DurationFlag &&
						    ConvertI64Time(itr->second.eventList[i].start_time) < arcMax &&
						    ConvertI64Time(itr->second.eventList[i].start_time) > arcMin ){
							if( itrArc == sys->epgArchive.end() ){
								//�T�[�r�X��ǉ�
								itrArc = sys->epgArchive.insert(std::make_pair(itr->first, EPGDB_SERVICE_EVENT_INFO())).first;
								itrArc->second.serviceInfo = std::move(itr->second.serviceInfo);
							}
							itrArc->second.eventList.push_back(std::move(itr->second.eventList[i]));
						}
					}
				}

				//EPG�f�[�^���X�V����
				sys->epgMap.swap(nextMap);

				//�A�[�J�C�u����s�v�ȃC�x���g������
				for( auto itr = sys->epgMap.cbegin(); arcMin < LLONG_MAX && itr != sys->epgMap.end(); itr++ ){
					auto itrArc = sys->epgArchive.find(itr->first);
					if( itrArc != sys->epgArchive.end() ){
						//��f�[�^�x�[�X�̍ŌÂ��V�������͕̂s�v
						__int64 minStart = LLONG_MAX;
						for( size_t i = 0; i < itr->second.eventList.size(); i++ ){
							if( itr->second.eventList[i].StartTimeFlag && ConvertI64Time(itr->second.eventList[i].start_time) < minStart ){
								minStart = ConvertI64Time(itr->second.eventList[i].start_time);
							}
						}
						itrArc->second.eventList.erase(std::remove_if(itrArc->second.eventList.begin(), itrArc->second.eventList.end(), [=](const EPGDB_EVENT_INFO& a) {
							return ConvertI64Time(a.start_time) + a.durationSec * I64_1SEC > minStart;
						}), itrArc->second.eventList.end());
					}
				}
				//�A�[�J�C�u����Â��C�x���g������
				vector<EPGDB_SERVICE_EVENT_INFO> epgOld;
				vector<__int64> oldIndex;
				for( auto itr = sys->epgArchive.begin(); itr != sys->epgArchive.end(); ){
					auto itrOld = epgOld.end();
					size_t j = 0;
					for( size_t i = 0; i < itr->second.eventList.size(); i++ ){
						__int64 startTime = ConvertI64Time(itr->second.eventList[i].start_time);
						if( startTime < oldMax && startTime >= oldMin ){
							//������Ɉړ�����
							if( itrOld == epgOld.end() ){
								//�T�[�r�X��ǉ�
								epgOld.push_back(EPGDB_SERVICE_EVENT_INFO());
								itrOld = epgOld.end() - 1;
								itrOld->serviceInfo = itr->second.serviceInfo;
								oldIndex.push_back(0);
								oldIndex.push_back(itr->first);
								oldIndex.push_back(LLONG_MAX);
								oldIndex.push_back(0);
							}
							//�J�n���Ԃ̍ŏ��l�ƍő�l
							*(oldIndex.end() - 2) = min(*(oldIndex.end() - 2), startTime - oldMin);
							*(oldIndex.end() - 1) = max(*(oldIndex.end() - 1), startTime - oldMin);
							itrOld->eventList.push_back(std::move(itr->second.eventList[i]));
						}else if( startTime > max(arcMin, oldMin) && startTime < arcMax ){
							//�c��
							if( i != j ){
								itr->second.eventList[j] = std::move(itr->second.eventList[i]);
							}
							j++;
						}
					}
					if( j == 0 ){
						//��̃T�[�r�X������
						sys->epgArchive.erase(itr++);
					}else{
						itr->second.eventList.erase(itr->second.eventList.begin() + j, itr->second.eventList.end());
						itr++;
					}
				}

				//�����A�[�J�C�u�p�t�@�C���ɏ�������
				if( epgOld.empty() == false ){
					fs_path epgArcPath = fs_path(settingPath).append(EPG_ARCHIVE_FOLDER);
					if( UtilFileExists(epgArcPath).first == false ){
						UtilCreateDirectory(epgArcPath);
					}
					std::unique_ptr<FILE, decltype(&fclose)> fp(OpenOldArchive(epgArcPath.c_str(), oldMin, UTIL_O_EXCL_CREAT_WRONLY), fclose);
					if( fp ){
						DWORD buffSize;
						vector<std::unique_ptr<BYTE[]>> buffList;
						buffList.reserve(epgOld.size());
						while( epgOld.empty() == false ){
							//�T�[�r�X�P�ʂŏ������݁A�V�[�N�ł���悤�ɃC���f�b�N�X�����
							buffList.push_back(NewWriteVALUE2WithVersion(5, epgOld.back(), buffSize));
							epgOld.pop_back();
							oldIndex[epgOld.size() * 4] = buffSize;
						}
						std::unique_ptr<BYTE[]> buff = NewWriteVALUE(oldIndex, buffSize);
						fwrite(buff.get(), 1, buffSize, fp.get());
						while( buffList.empty() == false ){
							fwrite(buffList.back().get(), 1, (size_t)*(oldIndex.end() - buffList.size() * 4), fp.get());
							buffList.pop_back();
						}
						if( oldCache.empty() == false ){
							//�L���b�V���ɒǉ�
							size_t i = std::lower_bound(oldCache.front().begin(), oldCache.front().end(), oldMin) - oldCache.front().begin();
							if( i == oldCache.front().size() || oldCache.front()[i] != oldMin ){
								oldCache.front().insert(oldCache.front().begin() + i, oldMin);
								oldCache.insert(oldCache.begin() + 1 + i, vector<__int64>());
							}
							oldCache[1 + i].swap(oldIndex);
						}
					}
				}
				sys->epgOldIndexCache.swap(oldCache);
				break;
			}
		}
		Sleep(1);
	}
	if( sys->loadForeground == false ){
		//�o�b�N�O���E���h�Ɉڍs
		SetThreadPriority(GetCurrentThread(), THREAD_PRIORITY_BELOW_NORMAL);
	}
	nextMap.clear();

	//�A�[�J�C�u�t�@�C���ɏ�������
	if( arcMin < LLONG_MAX ){
		vector<const EPGDB_SERVICE_EVENT_INFO*> valp;
		valp.reserve(sys->epgArchive.size());
		for( auto itr = sys->epgArchive.cbegin(); itr != sys->epgArchive.end(); valp.push_back(&(itr++)->second) );
		DWORD buffSize;
		std::unique_ptr<BYTE[]> buff = NewWriteVALUE2WithVersion(5, valp, buffSize);
		std::unique_ptr<FILE, decltype(&fclose)> fp(UtilOpenFile(fs_path(settingPath).append(EPG_ARCHIVE_DATA_NAME), UTIL_SECURE_WRITE), fclose);
		if( fp ){
			fwrite(buff.get(), 1, buffSize, fp.get());
		}
	}

	_OutputDebugString(L"End Load EpgData %dmsec\r\n", GetTickCount()-time);

	sys->loadForeground = false;
	sys->initialLoadDone = true;
	sys->loadStop = true;
}

BOOL CALLBACK CEpgDBManager::EnumEpgInfoListProc(DWORD epgInfoListSize, EPG_EVENT_INFO* epgInfoList, LPVOID param)
{
	EPGDB_SERVICE_EVENT_INFO* item = (EPGDB_SERVICE_EVENT_INFO*)param;

	try{
		if( epgInfoList == NULL ){
			item->eventList.reserve(epgInfoListSize);
		}else{
			for( DWORD i=0; i<epgInfoListSize; i++ ){
				item->eventList.resize(item->eventList.size() + 1);
				ConvertEpgInfo(item->serviceInfo.ONID, item->serviceInfo.TSID, item->serviceInfo.SID, &epgInfoList[i], &item->eventList.back());
				if( item->eventList.back().hasShortInfo ){
					//�����H��APR(���s)���܂ނ���
					Replace(item->eventList.back().shortInfo.event_name, L"\r\n", L"");
				}
				//������͊��\�[�g�����d�l�ł͂Ȃ��̂ő}���\�[�g���Ă���
				for( size_t j = item->eventList.size() - 1; j > 0 && item->eventList[j].event_id < item->eventList[j-1].event_id; j-- ){
					std::swap(item->eventList[j], item->eventList[j-1]);
				}
			}
		}
	}catch( std::bad_alloc& ){
		return FALSE;
	}
	return TRUE;
}

bool CEpgDBManager::IsLoadingData()
{
	return this->loadThread.joinable() && this->loadStop == false;
}

void CEpgDBManager::CancelLoadData()
{
	if( this->loadThread.joinable() ){
		this->loadStop = true;
		this->loadThread.join();
		this->loadStop = false;
	}
}

void CEpgDBManager::SearchEpg(const EPGDB_SEARCH_KEY_INFO* keys, size_t keysSize, __int64 enumStart, __int64 enumEnd, wstring* findKey,
                              const std::function<void(const EPGDB_EVENT_INFO*, wstring*)>& enumProc) const
{
#if !defined(EPGDB_STD_WREGEX) && defined(_WIN32)
	CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
	try
#endif
	{
		std::unique_ptr<SEARCH_CONTEXT[]> ctxs(new SEARCH_CONTEXT[keysSize]);
		size_t ctxsSize = 0;
		vector<__int64> enumServiceKey;
		for( size_t i = 0; i < keysSize; i++ ){
			if( InitializeSearchContext(ctxs[ctxsSize], enumServiceKey, keys + i) ){
				ctxsSize++;
			}
		}
		if( ctxsSize == 0 || EnumEventInfo(enumServiceKey.data(), enumServiceKey.size(), enumStart, enumEnd,
		                                   [=, &enumProc, &ctxs](const EPGDB_EVENT_INFO* info, const EPGDB_SERVICE_INFO*) {
			if( info ){
				if( IsMatchEvent(ctxs.get(), ctxsSize, info, findKey) ){
					enumProc(info, findKey);
				}
			}else{
				//�񋓊���
				enumProc(NULL, findKey);
			}
		}) == false ){
			//�񋓂Ȃ��Ŋ���
			enumProc(NULL, findKey);
		}
	}
#if !defined(EPGDB_STD_WREGEX) && defined(_WIN32)
	catch(...){
		CoUninitialize();
		throw;
	}
	CoUninitialize();
#endif
}

void CEpgDBManager::SearchArchiveEpg(const EPGDB_SEARCH_KEY_INFO* keys, size_t keysSize, __int64 enumStart, __int64 enumEnd, bool deletableBeforeEnumDone,
                                     wstring* findKey, const std::function<void(const EPGDB_EVENT_INFO*, wstring*)>& enumProc) const
{
#if !defined(EPGDB_STD_WREGEX) && defined(_WIN32)
	CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);
	try
#endif
	{
		std::unique_ptr<SEARCH_CONTEXT[]> ctxs(new SEARCH_CONTEXT[keysSize]);
		size_t ctxsSize = 0;
		vector<__int64> enumServiceKey;
		for( size_t i = 0; i < keysSize; i++ ){
			if( InitializeSearchContext(ctxs[ctxsSize], enumServiceKey, keys + i) ){
				ctxsSize++;
			}
		}
		if( ctxsSize == 0 ){
			//�񋓂Ȃ��Ŋ���
			enumProc(NULL, findKey);
		}else{
			EnumArchiveEventInfo(enumServiceKey.data(), enumServiceKey.size(), enumStart, enumEnd, deletableBeforeEnumDone,
			                     [=, &enumProc, &ctxs](const EPGDB_EVENT_INFO* info, const EPGDB_SERVICE_INFO*) {
				if( info ){
					if( IsMatchEvent(ctxs.get(), ctxsSize, info, findKey) ){
						enumProc(info, findKey);
					}
				}else{
					//�񋓊���
					enumProc(NULL, findKey);
				}
			});
		}
	}
#if !defined(EPGDB_STD_WREGEX) && defined(_WIN32)
	catch(...){
		CoUninitialize();
		throw;
	}
	CoUninitialize();
#endif
}

bool CEpgDBManager::InitializeSearchContext(SEARCH_CONTEXT& ctx, vector<__int64>& enumServiceKey, const EPGDB_SEARCH_KEY_INFO* key)
{
	if( key->andKey.compare(0, 7, L"^!{999}") == 0 ){
		//�����������L�[���[�h���w�肳��Ă���̂Ō������Ȃ�
		return false;
	}
	wstring andKey = key->andKey;
	ctx.caseFlag = false;
	if( andKey.compare(0, 7, L"C!{999}") == 0 ){
		//�召��������ʂ���L�[���[�h���w�肳��Ă���
		andKey.erase(0, 7);
		ctx.caseFlag = true;
	}
	ctx.chkDurationMinSec = 0;
	ctx.chkDurationMaxSec = MAXDWORD;
	if( andKey.compare(0, 4, L"D!{1") == 0 ){
		LPWSTR endp;
		DWORD dur = wcstoul(andKey.c_str() + 3, &endp, 10);
		if( endp - andKey.c_str() == 12 && endp[0] == L'}' ){
			//�ԑg�����i�荞�ރL�[���[�h���w�肳��Ă���
			andKey.erase(0, 13);
			ctx.chkDurationMinSec = dur / 10000 % 10000 * 60;
			ctx.chkDurationMaxSec = dur % 10000 == 0 ? MAXDWORD : dur % 10000 * 60;
		}
	}

	//�L�[���[�h����
	ctx.andKeyList.clear();
	ctx.notKeyList.clear();

	if( key->regExpFlag ){
		//���K�\���̒P�ƃL�[���[�h
		if( andKey.empty() == false ){
			ctx.andKeyList.push_back(vector<pair<wstring, RegExpPtr>>());
			AddKeyword(ctx.andKeyList.back(), andKey, ctx.caseFlag, true, key->titleOnlyFlag != FALSE);
		}
		if( key->notKey.empty() == false ){
			AddKeyword(ctx.notKeyList, key->notKey, ctx.caseFlag, true, key->titleOnlyFlag != FALSE);
		}
	}else{
		//���K�\���ł͂Ȃ��̂ŃL�[���[�h�̕���
		Replace(andKey, L"�@", L" ");
		while( andKey.empty() == false ){
			wstring buff;
			Separate(andKey, L" ", buff, andKey);
			if( buff == L"|" ){
				//OR����
				ctx.andKeyList.push_back(vector<pair<wstring, RegExpPtr>>());
			}else if( buff.empty() == false ){
				if( ctx.andKeyList.empty() ){
					ctx.andKeyList.push_back(vector<pair<wstring, RegExpPtr>>());
				}
				AddKeyword(ctx.andKeyList.back(), std::move(buff), ctx.caseFlag, false, key->titleOnlyFlag != FALSE);
			}
		}
		wstring notKey = key->notKey;
		Replace(notKey, L"�@", L" ");
		while( notKey.empty() == false ){
			wstring buff;
			Separate(notKey, L" ", buff, notKey);
			if( buff.empty() == false ){
				AddKeyword(ctx.notKeyList, std::move(buff), ctx.caseFlag, false, key->titleOnlyFlag != FALSE);
			}
		}
	}

	ctx.key = key;
	for( auto itr = key->serviceList.begin(); itr != key->serviceList.end(); itr++ ){
		bool found = false;
		for( size_t i = 0; i + 1 < enumServiceKey.size(); i += 2 ){
			if( enumServiceKey[i + 1] == *itr ){
				found = true;
				break;
			}
		}
		if( found == false ){
			enumServiceKey.push_back(0);
			enumServiceKey.push_back(*itr);
		}
	}
	return true;
}

bool CEpgDBManager::IsMatchEvent(SEARCH_CONTEXT* ctxs, size_t ctxsSize, const EPGDB_EVENT_INFO* itrEvent, wstring* findKey)
{
	for( size_t i = 0; i < ctxsSize; i++ ){
		SEARCH_CONTEXT& ctx = ctxs[i];
		const EPGDB_SEARCH_KEY_INFO& key = *ctx.key;
		//�����L�[����������ꍇ�̓T�[�r�X���m�F
		if( ctxsSize < 2 || std::find(key.serviceList.begin(), key.serviceList.end(),
		        Create64Key(itrEvent->original_network_id, itrEvent->transport_stream_id, itrEvent->service_id)) != key.serviceList.end() ){
			{
				if( key.freeCAFlag == 1 ){
					//���������̂�
					if( itrEvent->freeCAFlag != 0 ){
						//�L������
						continue;
					}
				}else if( key.freeCAFlag == 2 ){
					//�L�������̂�
					if( itrEvent->freeCAFlag == 0 ){
						//��������
						continue;
					}
				}
				//�W�������m�F
				if( key.contentList.size() > 0 ){
					//�W�������w�肠��̂ŃW�������ōi�荞��
					if( itrEvent->hasContentInfo == false ){
						if( itrEvent->hasShortInfo == false ){
							//2�߂̃T�[�r�X�H�ΏۊO�Ƃ���
							continue;
						}
						//�W���������Ȃ�
						bool findNo = false;
						for( size_t j = 0; j < key.contentList.size(); j++ ){
							if( key.contentList[j].content_nibble_level_1 == 0xFF &&
							    key.contentList[j].content_nibble_level_2 == 0xFF ){
								//�W�������Ȃ��̎w�肠��
								findNo = true;
								break;
							}
						}
						if( key.notContetFlag == 0 ){
							if( findNo == false ){
								continue;
							}
						}else{
							//NOT��������
							if( findNo ){
								continue;
							}
						}
					}else{
						bool equal = IsEqualContent(key.contentList, itrEvent->contentInfo.nibbleList);
						if( key.notContetFlag == 0 ){
							if( equal == false ){
								//�W�������Ⴄ�̂őΏۊO
								continue;
							}
						}else{
							//NOT��������
							if( equal ){
								continue;
							}
						}
					}
				}

				//�f���m�F
				if( key.videoList.size() > 0 ){
					if( itrEvent->hasComponentInfo == false ){
						continue;
					}
					WORD type = itrEvent->componentInfo.stream_content << 8 || itrEvent->componentInfo.component_type;
					if( std::find(key.videoList.begin(), key.videoList.end(), type) == key.videoList.end() ){
						continue;
					}
				}

				//�����m�F
				if( key.audioList.size() > 0 ){
					if( itrEvent->hasAudioInfo == false ){
						continue;
					}
					bool findContent = false;
					for( size_t j=0; j<itrEvent->audioInfo.componentList.size(); j++ ){
						WORD type = itrEvent->audioInfo.componentList[j].stream_content << 8 | itrEvent->audioInfo.componentList[j].component_type;
						if( std::find(key.audioList.begin(), key.audioList.end(), type) != key.audioList.end() ){
							findContent = true;
							break;
						}
					}
					if( findContent == false ){
						continue;
					}
				}

				//���Ԋm�F
				if( key.dateList.size() > 0 ){
					if( itrEvent->StartTimeFlag == FALSE ){
						//�J�n���ԕs���Ȃ̂őΏۊO
						continue;
					}
					bool inTime = IsInDateTime(key.dateList, itrEvent->start_time);
					if( key.notDateFlag == 0 ){
						if( inTime == false ){
							//���Ԕ͈͊O�Ȃ̂őΏۊO
							continue;
						}
					}else{
						//NOT��������
						if( inTime ){
							continue;
						}
					}
				}

				//�ԑg���ōi�荞��
				if( itrEvent->DurationFlag == FALSE ){
					//�s���Ȃ̂ōi�荞�݂���Ă���ΑΏۊO
					if( 0 < ctx.chkDurationMinSec || ctx.chkDurationMaxSec < MAXDWORD ){
						continue;
					}
				}else{
					if( itrEvent->durationSec < ctx.chkDurationMinSec || ctx.chkDurationMaxSec < itrEvent->durationSec ){
						continue;
					}
				}

				if( findKey ){
					findKey->clear();
				}

				//�L�[���[�h�m�F
				if( itrEvent->hasShortInfo == false ){
					if( ctx.andKeyList.empty() == false ){
						//���e�ɂ�����炸�ΏۊO
						continue;
					}
				}
				if( FindKeyword(ctx.notKeyList, *itrEvent, ctx.targetWord, ctx.distForFind, ctx.caseFlag, false, false) ){
					//not�L�[���[�h���������̂őΏۊO
					continue;
				}
				if( ctx.andKeyList.empty() == false ){
					bool found = false;
					for( size_t j = 0; j < ctx.andKeyList.size(); j++ ){
						if( FindKeyword(ctx.andKeyList[j], *itrEvent, ctx.targetWord, ctx.distForFind, ctx.caseFlag, key.aimaiFlag != 0, true, findKey) ){
							found = true;
							break;
						}
					}
					if( found == false ){
						//and�L�[���[�h������Ȃ������̂őΏۊO
						continue;
					}
				}
				return true;
			}
		}
	}
	return false;
}

bool CEpgDBManager::IsEqualContent(const vector<EPGDB_CONTENT_DATA>& searchKey, const vector<EPGDB_CONTENT_DATA>& eventData)
{
	for( size_t i=0; i<searchKey.size(); i++ ){
		EPGDB_CONTENT_DATA c = searchKey[i];
		if( 0x60 <= c.content_nibble_level_1 && c.content_nibble_level_1 <= 0x7F ){
			//�ԑg�t�����܂���CS�g���p���ɕϊ�����
			c.user_nibble_1 = c.content_nibble_level_1 & 0x0F;
			c.user_nibble_2 = c.content_nibble_level_2;
			c.content_nibble_level_2 = (c.content_nibble_level_1 - 0x60) >> 4;
			c.content_nibble_level_1 = 0x0E;
		}
		for( size_t j=0; j<eventData.size(); j++ ){
			if( c.content_nibble_level_1 == eventData[j].content_nibble_level_1 ){
				if( c.content_nibble_level_2 == 0xFF ){
					//�����ނ��ׂ�
					return true;
				}
				if( c.content_nibble_level_2 == eventData[j].content_nibble_level_2 ){
					if( c.content_nibble_level_1 != 0x0E ){
						//�g���łȂ�
						return true;
					}
					if( c.user_nibble_1 == eventData[j].user_nibble_1 ){
						if( c.user_nibble_2 == 0xFF ){
							//�g�������ނ��ׂ�
							return true;
						}
						if( c.user_nibble_2 == eventData[j].user_nibble_2 ){
							return true;
						}
					}
				}
			}
		}
	}
	return false;
}

bool CEpgDBManager::IsInDateTime(const vector<EPGDB_SEARCH_DATE_INFO>& dateList, const SYSTEMTIME& time)
{
	int weekMin = (time.wDayOfWeek * 24 + time.wHour) * 60 + time.wMinute;
	for( size_t i=0; i<dateList.size(); i++ ){
		int start = (dateList[i].startDayOfWeek * 24 + dateList[i].startHour) * 60 + dateList[i].startMin;
		int end = (dateList[i].endDayOfWeek * 24 + dateList[i].endHour) * 60 + dateList[i].endMin;
		if( start >= end ){
			if( start <= weekMin || weekMin <= end ){
				return true;
			}
		}else{
			if( start <= weekMin && weekMin <= end ){
				return true;
			}
		}
	}

	return false;
}

bool CEpgDBManager::FindKeyword(const vector<pair<wstring, RegExpPtr>>& keyList, const EPGDB_EVENT_INFO& info, wstring& word,
                                vector<int>& dist, bool caseFlag, bool aimai, bool andFlag, wstring* findKey)
{
	for( size_t i = 0; i < keyList.size(); i++ ){
		const wstring& key = keyList[i].first;
		if( i == 0 || key.compare(0, 7, keyList[i - 1].first) ){
			//�����Ώۂ��ς�����̂ō쐬
			word.clear();
			if( key.compare(0, 7, L":title:") == 0 ){
				if( info.hasShortInfo ){
					word += info.shortInfo.event_name;
				}
			}else if( key.compare(0, 7, L":event:") == 0 ){
				if( info.hasShortInfo ){
					word += info.shortInfo.event_name;
					word += L"\r\n";
					word += info.shortInfo.text_char;
					if( info.hasExtInfo ){
						word += L"\r\n";
						word += info.extInfo.text_char;
					}
				}
			}else if( key.compare(0, 7, L":genre:") == 0 ){
				AppendEpgContentInfoText(word, info);
			}else if( key.compare(0, 7, L":video:") == 0 ){
				AppendEpgComponentInfoText(word, info);
			}else if( key.compare(0, 7, L":audio:") == 0 ){
				AppendEpgAudioComponentInfoText(word, info);
			}else{
				throw std::runtime_error("");
			}
			ConvertSearchText(word);
		}

		if( keyList[i].second ){
			//���K�\��
#if !defined(EPGDB_STD_WREGEX) && defined(_WIN32)
			OleCharPtr target(SysAllocString(word.c_str()), SysFreeString);
			if( target ){
				IDispatch* pMatches;
				if( SUCCEEDED(keyList[i].second->Execute(target.get(), &pMatches)) ){
					std::unique_ptr<IMatchCollection, decltype(&ComRelease)> matches((IMatchCollection*)pMatches, ComRelease);
					long count;
					if( SUCCEEDED(matches->get_Count(&count)) && count > 0 ){
						if( andFlag == false ){
							//���������̂ŏI��
							return true;
						}
						if( findKey && i + 1 == keyList.size() ){
							//�ŏI�L�[�̃}�b�`���L�^
							IDispatch* pMatch;
							if( SUCCEEDED(matches->get_Item(0, &pMatch)) ){
								std::unique_ptr<IMatch2, decltype(&ComRelease)> match((IMatch2*)pMatch, ComRelease);
								BSTR value_;
								if( SUCCEEDED(match->get_Value(&value_)) ){
									OleCharPtr value(value_, SysFreeString);
									*findKey = SysStringLen(value.get()) ? value.get() : L"";
								}
							}
						}
					}else if( andFlag ){
						//������Ȃ������̂ŏI��
						return false;
					}
				}else if( andFlag ){
					return false;
				}
#else
			std::wsmatch m;
			if( std::regex_search(word, m, *keyList[i].second) ){
				if( andFlag == false ){
					//���������̂ŏI��
					return true;
				}
				if( findKey && i + 1 == keyList.size() ){
					//�ŏI�L�[�̃}�b�`���L�^
					*findKey = m[0];
				}
#endif
			}else if( andFlag ){
				return false;
			}
		}else{
			//�ʏ�
			if( key.size() > 7 &&
			    (aimai ? FindLikeKeyword(key, 7, word, dist, caseFlag) :
			     caseFlag ? std::search(word.begin(), word.end(), key.begin() + 7, key.end()) != word.end() :
			                std::search(word.begin(), word.end(), key.begin() + 7, key.end(),
			                            [](wchar_t l, wchar_t r) { return (L'a' <= l && l <= L'z' ? l - L'a' + L'A' : l) ==
			                                                              (L'a' <= r && r <= L'z' ? r - L'a' + L'A' : r); }) != word.end()) ){
				if( andFlag == false ){
					//���������̂ŏI��
					return true;
				}
			}else if( andFlag ){
				//������Ȃ������̂ŏI��
				return false;
			}
		}
	}

	if( andFlag && findKey ){
		//���������L�[���L�^
		size_t n = findKey->size();
		for( size_t i = 0; i < keyList.size(); i++ ){
			if( keyList[i].second == NULL ){
				if( n == 0 && findKey->empty() == false ){
					*findKey += L' ';
				}
				findKey->insert(findKey->size() - n, keyList[i].first, 7, wstring::npos);
				if( n != 0 ){
					findKey->insert(findKey->end() - n, L' ');
				}
			}
		}
	}
	return andFlag;
}

bool CEpgDBManager::FindLikeKeyword(const wstring& key, size_t keyPos, const wstring& word, vector<int>& dist, bool caseFlag)
{
	//�ҏW�������������l�ȉ��ɂȂ镶���񂪊܂܂�邩���ׂ�
	size_t l = 0;
	size_t curr = key.size() - keyPos + 1;
	dist.assign(curr * 2, 0);
	for( size_t i = 1; i < curr; i++ ){
		dist[i] = dist[i - 1] + 1;
	}
	for( size_t i = 0; i < word.size(); i++ ){
		wchar_t x = word[i];
		for( size_t j = 0; j < key.size() - keyPos; j++ ){
			wchar_t y = key[j + keyPos];
			if( caseFlag && x == y ||
			    caseFlag == false && (L'a' <= x && x <= L'z' ? x - L'a' + L'A' : x) == (L'a' <= y && y <= L'z' ? y - L'a' + L'A' : y) ){
				dist[curr + j + 1] = dist[l + j];
			}else{
				dist[curr + j + 1] = 1 + (dist[l + j] < dist[l + j + 1] ? min(dist[l + j], dist[curr + j]) : min(dist[l + j + 1], dist[curr + j]));
			}
		}
		//75%���������l�Ƃ���
		if( dist[curr + key.size() - keyPos] * 4 <= (int)(key.size() - keyPos) ){
			return true;
		}
		std::swap(l, curr);
	}
	return false;
}

void CEpgDBManager::AddKeyword(vector<pair<wstring, RegExpPtr>>& keyList, wstring key, bool caseFlag, bool regExp, bool titleOnly)
{
	keyList.push_back(std::make_pair(wstring(), RegExpPtr(
#if !defined(EPGDB_STD_WREGEX) && defined(_WIN32)
		NULL, ComRelease
#endif
		)));
	if( regExp ){
		key = (titleOnly ? L"::title:" : L"::event:") + key;
	}
	size_t regPrefix = key.compare(0, 2, L"::") ? 0 : 1;
	if( key.compare(regPrefix, 7, L":title:") &&
	    key.compare(regPrefix, 7, L":event:") &&
	    key.compare(regPrefix, 7, L":genre:") &&
	    key.compare(regPrefix, 7, L":video:") &&
	    key.compare(regPrefix, 7, L":audio:") ){
		//�����Ώۂ��s���Ȃ̂Ŏw�肷��
		key = (titleOnly ? L":title:" : L":event:") + key;
	}else if( regPrefix != 0 ){
		key.erase(0, 1);
		//���������ł͑Ώۂ�S�p�󔒂̂܂ܔ�r���Ă������ߐ��K�\�����S�p�̃P�[�X�������B���ʂɒu��������
		Replace(key, L"�@", L" ");
		//RegExp�I�u�W�F�N�g���\�z���Ă���
#if !defined(EPGDB_STD_WREGEX) && defined(_WIN32)
		void* pv;
		if( SUCCEEDED(CoCreateInstance(CLSID_RegExp, NULL, CLSCTX_INPROC_SERVER, IID_IRegExp, &pv)) ){
			keyList.back().second.reset((IRegExp*)pv);
			OleCharPtr pattern(SysAllocString(key.c_str() + 7), SysFreeString);
			if( pattern &&
			    SUCCEEDED(keyList.back().second->put_IgnoreCase(caseFlag ? VARIANT_FALSE : VARIANT_TRUE)) &&
			    SUCCEEDED(keyList.back().second->put_Pattern(pattern.get())) ){
				keyList.back().first.swap(key);
				return;
			}
			keyList.back().second.reset();
		}
#else
		try{
			keyList.back().second.reset(new std::wregex(key.c_str() + 7,
				caseFlag ? std::regex_constants::ECMAScript : std::regex_constants::ECMAScript | std::regex_constants::icase));
			keyList.back().first.swap(key);
			return;
		}catch( std::regex_error& ){
		}
#endif
		//��(��ɕs��v)�ɂ���
		key.erase(7);
	}
	ConvertSearchText(key);
	keyList.back().first.swap(key);
}

bool CEpgDBManager::GetServiceList(vector<EPGDB_SERVICE_INFO>* list) const
{
	CRefLock lock(&this->epgMapRefLock);

	if( this->epgMap.empty() ){
		return false;
	}
	list->reserve(list->size() + this->epgMap.size());
	for( auto itr = this->epgMap.cbegin(); itr != this->epgMap.end(); itr++ ){
		list->push_back(itr->second.serviceInfo);
	}
	return true;
}

pair<__int64, __int64> CEpgDBManager::GetEventMinMaxTimeProc(__int64 keyMask, __int64 key, bool archive) const
{
	const map<LONGLONG, EPGDB_SERVICE_EVENT_INFO>& target = archive ? this->epgArchive : this->epgMap;
	pair<__int64, __int64> ret(LLONG_MAX, LLONG_MIN);
	auto itr = target.begin();
	auto itrEnd = target.end();
	if( keyMask == 0 ){
		itrEnd = itr = target.find(key);
		if( itr != target.end() ){
			itrEnd++;
		}
	}
	for( ; itr != itrEnd; itr++ ){
		if( (itr->first | keyMask) == key ){
			for( auto jtr = itr->second.eventList.begin(); jtr != itr->second.eventList.end(); jtr++ ){
				if( jtr->StartTimeFlag ){
					__int64 startTime = ConvertI64Time(jtr->start_time);
					ret.first = min(ret.first, startTime);
					ret.second = max(ret.second, startTime);
				}
			}
		}
	}
	return ret;
}

pair<__int64, __int64> CEpgDBManager::GetArchiveEventMinMaxTime(__int64 keyMask, __int64 key) const
{
	CRefLock lock(&this->epgMapRefLock);

	pair<__int64, __int64> ret = GetEventMinMaxTimeProc(keyMask, key, true);
	if( this->epgOldIndexCache.empty() == false ){
		const vector<__int64>& timeList = this->epgOldIndexCache.front();
		//�����A�[�J�C�u�̍ŏ��J�n���Ԃ𒲂ׂ�
		bool found = false;
		for( size_t i = 0; found == false && i < timeList.size(); i++ ){
			const vector<__int64>& index = this->epgOldIndexCache[1 + i];
			for( size_t j = 0; j + 3 < index.size(); j += 4 ){
				if( (index[j + 1] | keyMask) == key ){
					ret.first = min(ret.first, timeList[i] + index[j + 2]);
					found = true;
				}
			}
		}
		//�����A�[�J�C�u�̍ő�J�n���Ԃ𒲂ׂ�
		found = false;
		for( size_t i = timeList.size(); found == false && i > 0; i-- ){
			const vector<__int64>& index = this->epgOldIndexCache[i];
			for( size_t j = 0; j + 3 < index.size(); j += 4 ){
				if( (index[j + 1] | keyMask) == key ){
					ret.second = max(ret.second, timeList[i - 1] + index[j + 3]);
					found = true;
				}
			}
		}
	}
	return ret;
}

bool CEpgDBManager::EnumEventInfoProc(__int64* keys, size_t keysSize, __int64 enumStart, __int64 enumEnd,
                                      const std::function<void(const EPGDB_EVENT_INFO*, const EPGDB_SERVICE_INFO*)>& enumProc, bool archive) const
{
	const map<LONGLONG, EPGDB_SERVICE_EVENT_INFO>& target = archive ? this->epgArchive : this->epgMap;
	auto itr = target.begin();
	auto itrEnd = target.end();
	if( keysSize == 2 && keys[0] == 0 ){
		itrEnd = itr = target.find(keys[1]);
		if( itr == target.end() || (archive == false && itr->second.eventList.empty()) ){
			return false;
		}
		itrEnd++;
	}
	for( ; itr != itrEnd; itr++ ){
		for( size_t i = 0; i + 1 < keysSize; i += 2 ){
			if( (itr->first | keys[i]) == keys[i + 1] ){
				for( auto jtr = itr->second.eventList.begin(); jtr != itr->second.eventList.end(); jtr++ ){
					//��A�[�J�C�u�ł͎��Ԗ���܂ޗ񋓂Ǝ��Ԗ���̂ݗ񋓂̓��ʈ���������
					if( archive || ((enumStart != 0 || enumEnd != LLONG_MAX) && (enumStart != LLONG_MAX || jtr->StartTimeFlag)) ){
						if( jtr->StartTimeFlag == 0 ){
							continue;
						}
						__int64 startTime = ConvertI64Time(jtr->start_time);
						if( startTime < enumStart || enumEnd <= startTime ){
							continue;
						}
					}
					enumProc(&*jtr, &itr->second.serviceInfo);
				}
				break;
			}
		}
	}
	//�񋓊���
	enumProc(NULL, NULL);
	return true;
}

void CEpgDBManager::EnumArchiveEventInfo(__int64* keys, size_t keysSize, __int64 enumStart, __int64 enumEnd, bool deletableBeforeEnumDone,
                                         const std::function<void(const EPGDB_EVENT_INFO*, const EPGDB_SERVICE_INFO*)>& enumProc) const
{
	CRefLock lock(&this->epgMapRefLock);

	std::list<EPGDB_SERVICE_EVENT_INFO> infoPool;
	if( enumStart < enumEnd && this->epgOldIndexCache.size() > 1 ){
		//�����A�[�J�C�u���ǂށBdeletableBeforeEnumDone���͗񋓒��ł����Ă��ȑO�ɗ񋓂��ꂽ�f�[�^�̐����͕ۏ؂��Ȃ�
		fs_path epgArcPath;
		const vector<__int64>& timeList = this->epgOldIndexCache.front();
		//�Ώۊ��Ԃ����ǂ߂�OK
		auto itr = std::upper_bound(timeList.begin(), timeList.end(), enumStart);
		if( itr != timeList.begin() && enumStart < *(itr - 1) + 7 * 24 * 3600 * I64_1SEC ){
			itr--;
		}
		auto itrEnd = std::lower_bound(itr, timeList.end(), enumEnd);
		vector<BYTE> buff;
		vector<__int64> index;
		EPGDB_SERVICE_EVENT_INFO info;
		for( ; itr != itrEnd; itr++ ){
			if( epgArcPath.empty() ){
				epgArcPath = GetSettingPath().append(EPG_ARCHIVE_FOLDER);
			}
			std::unique_ptr<FILE, decltype(&fclose)> fp(OpenOldArchive(epgArcPath.c_str(), *itr, UTIL_SECURE_READ), fclose);
			if( fp ){
				DWORD headerSize;
				ReadOldArchiveIndex(fp.get(), buff, index, &headerSize);
				for( size_t i = 0; i + 3 < index.size(); i += 4 ){
					for( size_t j = 0; j + 1 < keysSize; j += 2 ){
						if( (index[i + 1] | keys[j]) == keys[j + 1] ){
							//�ΏۃT�[�r�X�����ǂ߂�OK
							EPGDB_SERVICE_EVENT_INFO* pi = &info;
							if( deletableBeforeEnumDone == false ){
								infoPool.push_back(EPGDB_SERVICE_EVENT_INFO());
								pi = &infoPool.back();
							}
							ReadOldArchiveEventInfo(fp.get(), index, i, headerSize, buff, *pi);
							for( auto jtr = pi->eventList.cbegin(); jtr != pi->eventList.end(); jtr++ ){
								if( jtr->StartTimeFlag ){
									__int64 startTime = ConvertI64Time(jtr->start_time);
									if( enumStart <= startTime && startTime < enumEnd ){
										enumProc(&*jtr, &pi->serviceInfo);
									}
								}
							}
							break;
						}
					}
				}
			}
		}
	}
	if( EnumEventInfoProc(keys, keysSize, enumStart, enumEnd, enumProc, true) == false ){
		//�񋓊���
		enumProc(NULL, NULL);
	}
}

bool CEpgDBManager::SearchEpg(
	WORD ONID,
	WORD TSID,
	WORD SID,
	WORD EventID,
	EPGDB_EVENT_INFO* result
	) const
{
	CRefLock lock(&this->epgMapRefLock);

	LONGLONG key = Create64Key(ONID, TSID, SID);
	auto itr = this->epgMap.find(key);
	if( itr != this->epgMap.end() ){
		EPGDB_EVENT_INFO infoKey;
		infoKey.event_id = EventID;
		auto itrInfo = std::lower_bound(itr->second.eventList.begin(), itr->second.eventList.end(), infoKey,
		                                [](const EPGDB_EVENT_INFO& a, const EPGDB_EVENT_INFO& b) { return a.event_id < b.event_id; });
		if( itrInfo != itr->second.eventList.end() && itrInfo->event_id == EventID ){
			*result = *itrInfo;
			return true;
		}
	}
	return false;
}

bool CEpgDBManager::SearchEpg(
	WORD ONID,
	WORD TSID,
	WORD SID,
	LONGLONG startTime,
	DWORD durationSec,
	EPGDB_EVENT_INFO* result
	) const
{
	CRefLock lock(&this->epgMapRefLock);

	LONGLONG key = Create64Key(ONID, TSID, SID);
	auto itr = this->epgMap.find(key);
	if( itr != this->epgMap.end() ){
		for( auto itrInfo = itr->second.eventList.cbegin(); itrInfo != itr->second.eventList.end(); itrInfo++ ){
			if( itrInfo->StartTimeFlag != 0 && itrInfo->DurationFlag != 0 ){
				if( startTime == ConvertI64Time(itrInfo->start_time) &&
					durationSec == itrInfo->durationSec
					){
						*result = *itrInfo;
						return true;
				}
			}
		}
	}
	return false;
}

bool CEpgDBManager::SearchServiceName(
	WORD ONID,
	WORD TSID,
	WORD SID,
	wstring& serviceName
	) const
{
	CRefLock lock(&this->epgMapRefLock);

	LONGLONG key = Create64Key(ONID, TSID, SID);
	auto itr = this->epgMap.find(key);
	if( itr != this->epgMap.end() ){
		serviceName = itr->second.serviceInfo.service_name;
		return true;
	}
	return false;
}

//�����Ώۂ⌟���p�^�[������S���p�̋�ʂ���菜��(��ConvertText.txt�ɑ���)
//ConvertText.txt�ƈقȂ蔼�p���_�J�i��(�Ӑ}�ʂ�)�u������_�A�m�n�C�D�S�p�󔒂�u������_�A�\(U+2015)����(U+0396)��u�����Ȃ��_�ɒ���
void CEpgDBManager::ConvertSearchText(wstring& str)
{
	//�S�p�p������т��̃e�[�u���ɂ��镶�����u������
	//�ŏ��̕���(UTF-16)���L�[�Ƃ��ă\�[�g�ς݁B����L�[���̏����̓}�b�`�̗D�揇
	static const WCHAR convertFrom[][2] = {
		L"�f", L"�h", L"�@",
		L"�I", L"��", L"��", L"��", L"��", L"�i", L"�j", L"��", L"�{", L"�C", L"\xFF0D", L"�D", L"�^",
		L"�F", L"�G", L"��", L"��", L"��", L"�H", L"��", L"�m", L"�n", L"�O", L"�Q", L"�M", L"�o", L"�b", L"�p", L"\xFF5E",
		L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�",
		{L'�', L'�'}, L"�", {L'�', L'�'}, L"�", {L'�', L'�'}, L"�", {L'�', L'�'}, L"�", {L'�', L'�'}, L"�",
		{L'�', L'�'}, L"�", {L'�', L'�'}, L"�", {L'�', L'�'}, L"�", {L'�', L'�'}, L"�", {L'�', L'�'}, L"�",
		{L'�', L'�'}, L"�", {L'�', L'�'}, L"�", {L'�', L'�'}, L"�", {L'�', L'�'}, L"�", {L'�', L'�'}, L"�",
		L"�", L"�", L"�", L"�", L"�",
		{L'�', L'�'}, {L'�', L'�'}, L"�", {L'�', L'�'}, {L'�', L'�'}, L"�", {L'�', L'�'}, {L'�', L'�'}, L"�",
		{L'�', L'�'}, {L'�', L'�'}, L"�", {L'�', L'�'}, {L'�', L'�'}, L"�",
		L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�", L"�",
		L"��",
	};
	static const WCHAR convertTo[] = {
		L'\'', L'"', L' ',
		L'!', L'#', L'$', L'%', L'&', L'(', L')', L'*', L'+', L',', L'-', L'.', L'/',
		L':', L';', L'<', L'=', L'>', L'?', L'@', L'[', L']', L'^', L'_', L'`', L'{', L'|', L'}', L'~',
		L'�B', L'�u', L'�v', L'�A', L'�E', L'��', L'�@', L'�B', L'�D', L'�F', L'�H', L'��', L'��', L'��', L'�b', L'�[', L'�A', L'�C', L'�E', L'�G', L'�I',
		L'�K', L'�J', L'�M', L'�L', L'�O', L'�N', L'�Q', L'�P', L'�S', L'�R',
		L'�U', L'�T', L'�W', L'�V', L'�Y', L'�X', L'�[', L'�Z', L'�]', L'�\',
		L'�_', L'�^', L'�a', L'�`', L'�d', L'�c', L'�f', L'�e', L'�h', L'�g',
		L'�i', L'�j', L'�k', L'�l', L'�m',
		L'�o', L'�p', L'�n', L'�r', L'�s', L'�q', L'�u', L'�v', L'�t',
		L'�x', L'�y', L'�w', L'�{', L'�|', L'�z',
		L'�}', L'�~', L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'�J', L'�K',
		L'\\',
	};

	for( wstring::iterator itr = str.begin(), itrEnd = str.end(); itr != itrEnd; itr++ ){
		//����: ����͕����ʒu�̘A�����𗘗p���ăe�[�u���Q�Ƃ����炷���߂̏����B��L�̃e�[�u����M��ꍇ�͂������m�F���邱��
		WCHAR c = *itr;
		if( (L'�I' <= c && c <= L'��') || c == L'�@' || c == L'�f' || c == L'�h' ){
			if( L'�O' <= c && c <= L'�X' ){
				*itr = c - L'�O' + L'0';
			}else if( L'�`' <= c && c <= L'�y' ){
				*itr = c - L'�`' + L'A';
			}else if( L'��' <= c && c <= L'��' ){
				*itr = c - L'��' + L'a';
			}else{
				const WCHAR (*f)[2] = std::lower_bound(convertFrom, convertFrom + _countof(convertFrom), &*itr,
				                                       [](LPCWSTR a, LPCWSTR b) { return (unsigned short)a[0] < (unsigned short)b[0]; });
				for( ; f != convertFrom + _countof(convertFrom) && (*f)[0] == c; f++ ){
					if( (*f)[1] == L'\0' ){
						*itr = convertTo[f - convertFrom];
						break;
					}else if( itr + 1 != itrEnd && *(itr + 1) == (*f)[1] ){
						size_t i = itrEnd - itr - 1;
						str.replace(itr, itr + 2, 1, convertTo[f - convertFrom]);
						//�C�e���[�^���ėL����
						itrEnd = str.end();
						itr = itrEnd - i;
						break;
					}
				}
			}
		}
	}
}

