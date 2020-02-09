#include "stdafx.h"
#include "BonCtrl.h"

#include "../Common/CommonDef.h"
#include "../Common/TimeUtil.h"
#include "../Common/SendCtrlCmd.h"

CBonCtrl::CBonCtrl(void)
{
	this->statusSignalLv = 0.0f;
	this->viewSpace = -1;
	this->viewCh = -1;

	this->nwCtrlID = this->tsOut.CreateServiceCtrl(TRUE);
	this->nwCtrlEnableScramble = TRUE;
	this->nwCtrlNeedCaption = TRUE;
	this->nwCtrlNeedData = FALSE;
	this->nwCtrlAllService = FALSE;
	this->nwCtrlServiceID = 0xFFFF;

	this->chScanIndexOrStatus = ST_STOP;
	this->epgCapIndexOrStatus = ST_STOP;
	this->epgCapBackIndexOrStatus = ST_STOP;
	this->enableLiveEpgCap = FALSE;
	this->enableRecEpgCap = FALSE;

	this->epgCapBackBSBasic = TRUE;
	this->epgCapBackCS1Basic = TRUE;
	this->epgCapBackCS2Basic = TRUE;
	this->epgCapBackCS3Basic = FALSE;
	this->epgCapBackStartWaitSec = 30;
}


CBonCtrl::~CBonCtrl(void)
{
	CloseBonDriver();

	StopChScan();
}

void CBonCtrl::ReloadSetting(
	BOOL enableEmm,
	BOOL noLogScramble,
	BOOL parseEpgPostProcess,
	BOOL enableScramble,
	BOOL needCaption,
	BOOL needData,
	BOOL allService
	)
{
	this->tsOut.SetEmm(enableEmm);
	this->tsOut.SetNoLogScramble(noLogScramble);
	this->tsOut.SetParseEpgPostProcess(parseEpgPostProcess);
	this->nwCtrlEnableScramble = enableScramble;
	this->nwCtrlNeedCaption = needCaption;
	this->nwCtrlNeedData = needData;
	this->nwCtrlAllService = allService;
	this->tsOut.SetScramble(this->nwCtrlID, this->nwCtrlEnableScramble);
	this->tsOut.SetServiceMode(this->nwCtrlID, this->nwCtrlNeedCaption, this->nwCtrlNeedData);
	this->tsOut.SetServiceID(this->nwCtrlID, this->nwCtrlAllService ? 0xFFFF : this->nwCtrlServiceID);
}

void CBonCtrl::SetNWCtrlServiceID(
	WORD serviceID
	)
{
	if( this->nwCtrlServiceID != serviceID ){
		this->nwCtrlServiceID = serviceID;
		this->tsOut.SetServiceID(this->nwCtrlID, this->nwCtrlAllService ? 0xFFFF : this->nwCtrlServiceID);
	}
}

void CBonCtrl::Check()
{
	CheckChScan();
	CheckEpgCap();
	CheckEpgCapBack();
}

BOOL CBonCtrl::OpenBonDriver(
	LPCWSTR bonDriverFile,
	int openWait,
	DWORD tsBuffMaxCount
)
{
	CloseBonDriver();
	if( this->bonUtil.OpenBonDriver(GetModulePath().replace_filename(BON_DLL_FOLDER).c_str(), bonDriverFile,
	                                [=](BYTE* data, DWORD size, DWORD remain) { RecvCallback(data, size, remain, tsBuffMaxCount); },
	                                [=](float signalLv, int space, int ch) { StatusCallback(signalLv, space, ch); }, openWait) ){
		wstring bonFile = this->bonUtil.GetOpenBonDriverFileName();
		//��̓X���b�h�N��
		this->analyzeStopFlag = false;
		this->analyzeThread = thread_(AnalyzeThread, this);

		this->tsOut.SetBonDriver(bonFile);
		fs_path settingPath = GetSettingPath();
		wstring tunerName = this->bonUtil.GetTunerName();
		CheckFileName(tunerName);
		this->chUtil.LoadChSet(fs_path(settingPath).append(fs_path(bonFile).stem().concat(L"(" + tunerName + L").ChSet4.txt").native()).native(),
		                       fs_path(settingPath).append(L"ChSet5.txt").native());
		this->tsOut.ClearErrCount(this->nwCtrlID);
		return TRUE;
	}

	return FALSE;
}

//���[�h����BonDriver�̃t�@�C�������擾����i���[�h�������Ă��邩�̔���j
//���X���b�h�Z�[�t
//�߂�l�F
// TRUE�i�����j�FFALSE�iOpen�Ɏ��s���Ă���j
//�����F
// bonDriverFile		[OUT]BonDriver�̃t�@�C����(NULL��)
BOOL CBonCtrl::GetOpenBonDriver(
	wstring* bonDriverFile
	)
{
	BOOL ret = FALSE;

	wstring strBonDriverFile = this->bonUtil.GetOpenBonDriverFileName();
	if( strBonDriverFile.empty() == false ){
		ret = TRUE;
		if( bonDriverFile != NULL ){
			*bonDriverFile = strBonDriverFile;
		}
	}

	return ret;
}

BOOL CBonCtrl::SetCh(
	DWORD space,
	DWORD ch,
	WORD serviceID
)
{
	if( this->tsOut.IsRec() == TRUE ){
		return FALSE;
	}
	StopEpgCap();

	if( ProcessSetCh(space, ch, FALSE) ){
		this->nwCtrlServiceID = serviceID;
		this->tsOut.SetServiceID(this->nwCtrlID, this->nwCtrlAllService ? 0xFFFF : this->nwCtrlServiceID);
		return TRUE;
	}

	return FALSE;
}

BOOL CBonCtrl::ProcessSetCh(
	DWORD space,
	DWORD ch,
	BOOL chScan
	)
{
	DWORD spaceNow=0;
	DWORD chNow=0;

	BOOL ret = FALSE;
	if( this->bonUtil.GetOpenBonDriverFileName().empty() == false ){
		ret = TRUE;
		DWORD elapsed;
		if( this->bonUtil.GetNowCh(&spaceNow, &chNow) == false || space != spaceNow || ch != chNow || this->tsOut.IsChUnknown(&elapsed) && elapsed > 15000 ){
			StopBackgroundEpgCap();
			this->tsOut.SetChChangeEvent(chScan);
			_OutputDebugString(L"SetCh space %d, ch %d", space, ch);
			ret = this->bonUtil.SetCh(space, ch);
			StartBackgroundEpgCap();
		}
	}else{
		OutputDebugString(L"Err GetNowCh");
	}
	return ret;
}

//�`�����l���ύX�����ǂ���
//���X���b�h�Z�[�t
//�߂�l�F
// TRUE�i�ύX���j�AFALSE�i�����j
BOOL CBonCtrl::IsChChanging(BOOL* chChgErr)
{
	if( chChgErr != NULL ){
		*chChgErr = FALSE;
	}
	DWORD elapsed;
	if( this->tsOut.IsChUnknown(&elapsed) && elapsed != MAXDWORD ){
		if( elapsed > 15000 ){
			//�^�C���A�E�g����
			if( chChgErr != NULL ){
				*chChgErr = TRUE;
			}
			return FALSE;
		}
		return TRUE;
	}
	return FALSE;
}

//���݂̃X�g���[����ID���擾����
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// ONID		[OUT]originalNetworkID
// TSID		[OUT]transportStreamID
BOOL CBonCtrl::GetStreamID(
	WORD* ONID,
	WORD* TSID
	)
{
	return this->tsOut.GetStreamID(ONID, TSID);
}

//���[�h���Ă���BonDriver�̊J��
void CBonCtrl::CloseBonDriver()
{
	StopBackgroundEpgCap();

	StopEpgCap();

	if( this->analyzeThread.joinable() ){
		this->analyzeStopFlag = true;
		this->analyzeEvent.Set();
		this->analyzeThread.join();
	}

	this->bonUtil.CloseBonDriver();
	this->packetInit.ClearBuff();
	this->tsBuffList.clear();
	this->tsFreeList.clear();
	this->nwCtrlServiceID = 0xFFFF;
}

void CBonCtrl::RecvCallback(BYTE* data, DWORD size, DWORD remain, DWORD tsBuffMaxCount)
{
	BYTE* outData;
	DWORD outSize;
	if( data != NULL && size != 0 && this->packetInit.GetTSData(data, size, &outData, &outSize) ){
		CBlockLock lock(&this->buffLock);
		while( outSize != 0 ){
			if( this->tsFreeList.empty() ){
				//�o�b�t�@�𑝂₷
				if( this->tsBuffList.size() > tsBuffMaxCount ){
					for( auto itr = this->tsBuffList.begin(); itr != this->tsBuffList.end(); (itr++)->clear() );
					this->tsFreeList.splice(this->tsFreeList.end(), this->tsBuffList);
				}else{
					this->tsFreeList.push_back(vector<BYTE>());
					this->tsFreeList.back().reserve(48128);
				}
			}
			DWORD insertSize = min(48128 - (DWORD)this->tsFreeList.front().size(), outSize);
			this->tsFreeList.front().insert(this->tsFreeList.front().end(), outData, outData + insertSize);
			if( this->tsFreeList.front().size() == 48128 ){
				this->tsBuffList.splice(this->tsBuffList.end(), this->tsFreeList, this->tsFreeList.begin());
			}
			outData += insertSize;
			outSize -= insertSize;
		}
	}
	if( remain == 0 ){
		this->analyzeEvent.Set();
	}
}

void CBonCtrl::StatusCallback(float signalLv, int space, int ch)
{
	CBlockLock lock(&this->buffLock);
	this->statusSignalLv = signalLv;
	this->viewSpace = space;
	this->viewCh = ch;
}

void CBonCtrl::AnalyzeThread(CBonCtrl* sys)
{
	std::list<vector<BYTE>> data;

	while( sys->analyzeStopFlag == false ){
		//�o�b�t�@����f�[�^���o��
		float signalLv;
		{
			CBlockLock lock(&sys->buffLock);
			if( data.empty() == false ){
				//�ԋp
				data.front().clear();
				sys->tsFreeList.splice(sys->tsFreeList.end(), data);
			}
			if( sys->tsBuffList.empty() == false ){
				data.splice(data.end(), sys->tsBuffList, sys->tsBuffList.begin());
			}
			signalLv = sys->statusSignalLv;
		}
		sys->tsOut.SetSignalLevel(signalLv);
		if( data.empty() == false ){
			sys->tsOut.AddTSBuff(&data.front().front(), (DWORD)data.front().size());
		}else{
			sys->analyzeEvent.WaitOne(1000);
		}
	}
}

//�T�[�r�X�ꗗ���擾����
//�߂�l�F
// �G���[�R�[�h
//�����F
// serviceList				[OUT]�T�[�r�X���̃��X�g
DWORD CBonCtrl::GetServiceList(
	vector<CH_DATA4>* serviceList
	)
{
	return this->chUtil.GetEnumService(serviceList);
}

DWORD CBonCtrl::CreateServiceCtrl(
	BOOL duplicateNWCtrl
	)
{
	DWORD id = this->tsOut.CreateServiceCtrl(FALSE);
	if( duplicateNWCtrl ){
		this->tsOut.SetScramble(id, this->nwCtrlEnableScramble);
		this->tsOut.SetServiceMode(id, this->nwCtrlNeedCaption, this->nwCtrlNeedData);
		this->tsOut.SetServiceID(id, this->nwCtrlAllService ? 0xFFFF : this->nwCtrlServiceID);
	}
	return id;
}

//TS�X�g���[������p�R���g���[�����쐬����
//�߂�l�F
// �G���[�R�[�h
//�����F
// id			[IN]���䎯��ID
BOOL CBonCtrl::DeleteServiceCtrl(
	DWORD id
	)
{
	return this->tsOut.DeleteServiceCtrl(id);
}

//����Ώۂ̃T�[�r�X��ݒ肷��
BOOL CBonCtrl::SetServiceID(
	DWORD id,
	WORD serviceID
	)
{
	return this->tsOut.SetServiceID(id,serviceID);
}

BOOL CBonCtrl::GetServiceID(
	DWORD id,
	WORD* serviceID
	)
{
	return this->tsOut.GetServiceID(id,serviceID);
}

void CBonCtrl::SendUdp(
	vector<NW_SEND_INFO>* sendList
	)
{
	this->tsOut.SendUdp(this->nwCtrlID, sendList);
}

void CBonCtrl::SendTcp(
	vector<NW_SEND_INFO>* sendList
	)
{
	this->tsOut.SendTcp(this->nwCtrlID, sendList);
}

BOOL CBonCtrl::StartSave(
	const SET_CTRL_REC_PARAM& recParam,
	const vector<wstring>& saveFolderSub,
	int writeBuffMaxCount
)
{
	if( this->tsOut.StartSave(recParam, saveFolderSub, writeBuffMaxCount) ){
		StartBackgroundEpgCap();
		return TRUE;
	}
	return FALSE;
}

BOOL CBonCtrl::EndSave(
	DWORD id,
	BOOL* subRecFlag
	)
{
	return this->tsOut.EndSave(id, subRecFlag);
}

//�X�N�����u�����������̓���ݒ�
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// enable		[IN] TRUE�i��������j�AFALSE�i�������Ȃ��j
BOOL CBonCtrl::SetScramble(
	DWORD id,
	BOOL enable
	)
{
	return this->tsOut.SetScramble(id, enable);
}

//�����ƃf�[�^�����܂߂邩�ǂ���
//�����F
// id					[IN]���䎯��ID
// enableCaption		[IN]������ TRUE�i�܂߂�j�AFALSE�i�܂߂Ȃ��j
// enableData			[IN]�f�[�^������ TRUE�i�܂߂�j�AFALSE�i�܂߂Ȃ��j
void CBonCtrl::SetServiceMode(
	DWORD id,
	BOOL enableCaption,
	BOOL enableData
	)
{
	this->tsOut.SetServiceMode(id, enableCaption, enableData);
}

//�G���[�J�E���g���N���A����
void CBonCtrl::ClearErrCount(
	DWORD id
	)
{
	this->tsOut.ClearErrCount(id);
}

//�h���b�v�ƃX�N�����u���̃J�E���g���擾����
//�����F
// drop				[OUT]�h���b�v��
// scramble			[OUT]�X�N�����u����
void CBonCtrl::GetErrCount(
	DWORD id,
	ULONGLONG* drop,
	ULONGLONG* scramble
	)
{
	this->tsOut.GetErrCount(id, drop, scramble);
}

//�`�����l���X�L�������J�n����
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
BOOL CBonCtrl::StartChScan()
{
	if( this->tsOut.IsRec() == TRUE ){
		return FALSE;
	}
	if( this->chScanIndexOrStatus >= ST_WORKING ||
	    this->epgCapIndexOrStatus >= ST_WORKING ){
		return FALSE;
	}

	StopBackgroundEpgCap();

	if( this->bonUtil.GetOpenBonDriverFileName().empty() == false ){
		this->chScanChkList.clear();
		const vector<pair<wstring, vector<wstring>>>& spaceList = this->bonUtil.GetOriginalChList();
		for( size_t i = 0; i < spaceList.size(); i++ ){
			for( size_t j = 0; j < spaceList[i].second.size(); j++ ){
				if( spaceList[i].second[j].empty() == false ){
					CHK_CH_INFO item;
					item.space = (DWORD)i;
					item.spaceName = spaceList[i].first;
					item.ch = (DWORD)j;
					item.chName = spaceList[i].second[j];
					this->chScanChkList.push_back(item);
				}
			}
		}
		this->chScanIndexOrStatus = ST_WORKING;
		if( this->chScanChkList.empty() ){
			//�X�L����������̂��Ȃ��BST_COMPLETE�ɑJ��
			CheckChScan();
			return TRUE;
		}
		//�X�L�����J�n
		return TRUE;
	}

	return FALSE;
}

//�`�����l���X�L�������L�����Z������
void CBonCtrl::StopChScan()
{
	if( this->chScanIndexOrStatus >= ST_WORKING ){
		fs_path bonFile = this->bonUtil.GetOpenBonDriverFileName();
		wstring tunerName = this->bonUtil.GetTunerName();
		CheckFileName(tunerName);
		fs_path settingPath = GetSettingPath();
		wstring chSet4 = fs_path(settingPath).append(bonFile.stem().concat(L"(" + tunerName + L").ChSet4.txt").native()).native();
		wstring chSet5 = fs_path(settingPath).append(L"ChSet5.txt").native();
		this->chUtil.LoadChSet(chSet4, chSet5);
		this->chScanIndexOrStatus = ST_CANCEL;
	}
}

//�`�����l���X�L�����̏�Ԃ��擾����
//�߂�l�F
// �X�e�[�^�X
//�����F
// space		[OUT]�X�L�������̕���CH��space
// ch			[OUT]�X�L�������̕���CH��ch
// chName		[OUT]�X�L�������̕���CH�̖��O
// chkNum		[OUT]�`�F�b�N�ς݂̐�
// totalNum		[OUT]�`�F�b�N�Ώۂ̑���
CBonCtrl::JOB_STATUS CBonCtrl::GetChScanStatus(
	DWORD* space,
	DWORD* ch,
	wstring* chName,
	DWORD* chkNum,
	DWORD* totalNum
	)
{
	int indexOrStatus = this->chScanIndexOrStatus;
	if( indexOrStatus < ST_WORKING ){
		return (JOB_STATUS)indexOrStatus;
	}
	if( indexOrStatus < 0 ){
		indexOrStatus = 0;
	}
	if( space != NULL ){
		*space = this->chScanChkList[indexOrStatus].space;
	}
	if( ch != NULL ){
		*ch = this->chScanChkList[indexOrStatus].ch;
	}
	if( chName != NULL ){
		*chName = this->chScanChkList[indexOrStatus].chName;
	}
	if( chkNum != NULL ){
		*chkNum = indexOrStatus;
	}
	if( totalNum != NULL ){
		*totalNum = (DWORD)this->chScanChkList.size();
	}
	return ST_WORKING;
}

void CBonCtrl::CheckChScan()
{
	int chkCount = this->chScanIndexOrStatus;
	if( chkCount >= ST_WORKING ){
		if( chkCount == ST_WORKING ){
			fs_path iniPath = GetCommonIniPath().replace_filename(L"BonCtrl.ini");
			this->chScanChChgTimeOut = GetPrivateProfileInt(L"CHSCAN", L"ChChgTimeOut", 9, iniPath.c_str());
			this->chScanServiceChkTimeOut = GetPrivateProfileInt(L"CHSCAN", L"ServiceChkTimeOut", 8, iniPath.c_str());
			this->chUtil.Clear();
			this->chScanChkNext = TRUE;
		}

		if( this->chScanChkNext == FALSE ){
			DWORD elapsed;
			if( this->tsOut.IsChUnknown(&elapsed) ){
				if( elapsed > this->chScanChChgTimeOut * 1000 ){
					//�`�����l���؂�ւ���chChgTimeOut�b�ȏォ�����Ă�̂Ŗ��M���Ɣ��f
					OutputDebugString(L"��AutoScan Ch Change timeout\r\n");
					this->chScanChkNext = TRUE;
				}
			}else{
				if( GetTickCount() - this->chScanTick > (this->chScanChChgTimeOut + this->chScanServiceChkTimeOut) * 1000 ){
					//�`�����l���؂�ւ������������ǃT�[�r�X�ꗗ�Ƃ�Ȃ��̂Ŗ��M���Ɣ��f
					OutputDebugString(L"��AutoScan GetService timeout\r\n");
					this->chScanChkNext = TRUE;
				}else{
					//�T�[�r�X�ꗗ�̎擾���s��
					this->tsOut.GetServiceListActual([=](DWORD serviceListSize, SERVICE_INFO* serviceList) {
						if( serviceListSize > 0 ){
							//�ꗗ�̎擾���ł���
							for( int currSID = 0; currSID < 0x10000; ){
								//ServiceID���ɒǉ�
								int nextSID = 0x10000;
								for( DWORD i = 0; i < serviceListSize; i++ ){
									WORD serviceID = serviceList[i].service_id;
									if( serviceID == currSID && serviceList[i].extInfo && serviceList[i].extInfo->service_name ){
										if( wcslen(serviceList[i].extInfo->service_name) > 0 ){
											this->chUtil.AddServiceInfo(this->chScanChkList[chkCount].space,
											                            this->chScanChkList[chkCount].ch,
											                            this->chScanChkList[chkCount].chName, &(serviceList[i]));
										}
									}
									if( serviceID > currSID && serviceID < nextSID ){
										nextSID = serviceID;
									}
								}
								currSID = nextSID;
							}
							this->chScanChkNext = TRUE;
						}
					});
				}
			}
		}

		if( this->chScanChkNext ){
			//���̃`�����l����
			chkCount++;
			if( this->chScanChkList.size() <= (size_t)chkCount ){
				//�S���`�F�b�N�I������̂ŏI��
				fs_path bonFile = this->bonUtil.GetOpenBonDriverFileName();
				wstring tunerName = this->bonUtil.GetTunerName();
				CheckFileName(tunerName);
				fs_path settingPath = GetSettingPath();
				wstring chSet4 = fs_path(settingPath).append(bonFile.stem().concat(L"(" + tunerName + L").ChSet4.txt").native()).native();
				wstring chSet5 = fs_path(settingPath).append(L"ChSet5.txt").native();
				this->chUtil.SaveChSet(chSet4, chSet5);
				this->chScanIndexOrStatus = ST_COMPLETE;
				return;
			}
			this->chScanIndexOrStatus = chkCount;
			if( this->ProcessSetCh(this->chScanChkList[chkCount].space, this->chScanChkList[chkCount].ch, TRUE) ){
				this->chScanTick = GetTickCount();
				this->chScanChkNext = FALSE;
			}
		}
	}
}

//EPG�擾���J�n����
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// chList		[IN]EPG�擾����`�����l���ꗗ(NULL��)
BOOL CBonCtrl::StartEpgCap(
	const vector<SET_CH_INFO>* chList
	)
{
	if( this->tsOut.IsRec() == TRUE ){
		return FALSE;
	}
	if( this->chScanIndexOrStatus >= ST_WORKING ||
	    this->epgCapIndexOrStatus >= ST_WORKING ){
		return FALSE;
	}

	StopBackgroundEpgCap();

	if( this->bonUtil.GetOpenBonDriverFileName().empty() == false ){
		if( chList ){
			this->epgCapChList.clear();
			for( size_t i = 0; i < chList->size(); i++ ){
				//SID�w��̂ݑΉ�
				if( (*chList)[i].useSID ){
					this->epgCapChList.push_back((*chList)[i]);
				}
			}
		}else{
			this->epgCapChList = this->chUtil.GetEpgCapService();
		}
		if( this->epgCapChList.empty() ){
			//�擾������̂��Ȃ�
			this->epgCapIndexOrStatus = ST_COMPLETE;
			return TRUE;
		}
		//�擾�J�n
		this->epgCapIndexOrStatus = ST_WORKING;
		return TRUE;
	}

	return FALSE;
}

//EPG�擾���~����
void CBonCtrl::StopEpgCap(
	)
{
	if( this->epgCapIndexOrStatus >= ST_WORKING ){
		this->tsOut.StopSaveEPG(FALSE);
		this->epgCapIndexOrStatus = ST_CANCEL;
	}
}

//EPG�擾�̃X�e�[�^�X���擾����
//��info==NULL�̏ꍇ�Ɍ���X���b�h�Z�[�t
//�߂�l�F
// �X�e�[�^�X
//�����F
// info			[OUT]�擾���̃T�[�r�X
CBonCtrl::JOB_STATUS CBonCtrl::GetEpgCapStatus(
	SET_CH_INFO* info
	)
{
	int indexOrStatus = this->epgCapIndexOrStatus;
	if( indexOrStatus < ST_WORKING ){
		return (JOB_STATUS)indexOrStatus;
	}
	if( indexOrStatus < 0 ){
		indexOrStatus = 0;
	}
	if( info != NULL ){
		*info = this->epgCapChList[indexOrStatus];
	}
	return ST_WORKING;
}

void CBonCtrl::CheckEpgCap()
{
	int chkCount = this->epgCapIndexOrStatus;
	if( chkCount >= ST_WORKING ){
		if( chkCount == ST_WORKING ){
			fs_path iniPath = GetCommonIniPath().replace_filename(L"BonCtrl.ini");
			this->epgCapTimeOut = GetPrivateProfileInt(L"EPGCAP", L"EpgCapTimeOut", 10, iniPath.c_str());
			this->epgCapSaveTimeOut = GetPrivateProfileInt(L"EPGCAP", L"EpgCapSaveTimeOut", 0, iniPath.c_str()) != 0;
			//Common.ini�͈�ʂɊO���v���Z�X���ύX����\���̂���(�͂���)���̂Ȃ̂ŁA���p�̒��O�Ƀ`�F�b�N����
			fs_path commonIniPath = GetCommonIniPath();
			this->epgCapBSBasic = GetPrivateProfileInt(L"SET", L"BSBasicOnly", 1, commonIniPath.c_str()) != 0;
			this->epgCapCS1Basic = GetPrivateProfileInt(L"SET", L"CS1BasicOnly", 1, commonIniPath.c_str()) != 0;
			this->epgCapCS2Basic = GetPrivateProfileInt(L"SET", L"CS2BasicOnly", 1, commonIniPath.c_str()) != 0;
			this->epgCapCS3Basic = GetPrivateProfileInt(L"SET", L"CS3BasicOnly", 0, commonIniPath.c_str()) != 0;
			this->epgCapChkBS = FALSE;
			this->epgCapChkCS1 = FALSE;
			this->epgCapChkCS2 = FALSE;
			this->epgCapChkCS3 = FALSE;
			this->epgCapChkNext = TRUE;
		}

		if( this->epgCapChkNext == FALSE ){
			DWORD elapsed;
			if( this->tsOut.IsChUnknown(&elapsed) ){
				if( this->epgCapSetChState != 0 || elapsed > 15000 ){
					//�`�����l���؂�ւ����^�C���A�E�g�����̂Ŗ��M���Ɣ��f
					this->tsOut.StopSaveEPG(FALSE);
					this->epgCapChkNext = TRUE;
				}
			}else{
				DWORD tick = GetTickCount();
				if( this->epgCapSetChState == 0 ){
					//�؂�ւ�����
					this->epgCapSetChState = 1;
					this->epgCapTick = tick;
				}
				if( tick - this->epgCapTick > this->epgCapTimeOut * 60 * 1000 ){
					//timeOut���ȏォ�����Ă���Ȃ��~
					this->tsOut.StopSaveEPG(this->epgCapSaveTimeOut);
					this->epgCapChkNext = TRUE;
					_OutputDebugString(L"++%d����EPG�擾�������� or Ch�ύX�ŃG���[", this->epgCapTimeOut);
				}else if( tick - this->epgCapTick > 5000 ){
					SET_CH_INFO ch = this->epgCapChList[chkCount];
					BOOL basicOnly = ch.ONID == 4 && this->epgCapBSBasic ||
					                 ch.ONID == 6 && this->epgCapCS1Basic ||
					                 ch.ONID == 7 && this->epgCapCS2Basic ||
					                 ch.ONID == 10 && this->epgCapCS3Basic;
					//�؂�ւ���������5�b�ȏ�߂��Ă���̂Ŏ擾����
					if( this->epgCapSetChState == 1 ){
						//�擾�J�n
						wstring epgDataPath = L"";
						GetEpgDataFilePath(ch.ONID, basicOnly ? 0xFFFF : ch.TSID, epgDataPath);
						if( this->tsOut.StartSaveEPG(epgDataPath) ){
							this->epgCapSetChState = 2;
							this->epgCapLastChkTick = tick;
							if( ch.ONID == 4 ){
								this->epgCapChkBS = TRUE;
							}else if( ch.ONID == 6 ){
								this->epgCapChkCS1 = TRUE;
							}else if( ch.ONID == 7 ){
								this->epgCapChkCS2 = TRUE;
							}else if( ch.ONID == 10 ){
								this->epgCapChkCS3 = TRUE;
							}
						}else{
							this->epgCapChkNext = TRUE;
						}
					}else if( tick - this->epgCapLastChkTick > (DWORD)(this->epgCapSetChState == 2 ? 60 : 10) * 1000 ){
						//�Œ�60�b�͎擾���A�Ȍ�10�b���ƂɃ`�F�b�N
						this->epgCapSetChState = 3;
						this->epgCapLastChkTick = tick;
						vector<SET_CH_INFO> chkList = this->chUtil.GetEpgCapServiceAll(ch.ONID, basicOnly ? -1 : ch.TSID);
						//epgCapChList�̃T�[�r�X��EPG�擾�ΏۂłȂ������Ƃ��Ă��`�F�b�N���Ȃ���΂Ȃ�Ȃ�
						if( std::find_if(chkList.begin(), chkList.end(), [=](const SET_CH_INFO& a) {
						        return a.ONID == ch.ONID && a.TSID == ch.TSID && a.SID == ch.SID; }) == chkList.end() ){
							chkList.push_back(ch);
						}
						//�~�Ϗ�ԃ`�F�b�N
						for( vector<SET_CH_INFO>::iterator itr = chkList.begin(); itr != chkList.end(); itr++ ){
							BOOL leitFlag = this->chUtil.IsPartial(itr->ONID, itr->TSID, itr->SID);
							pair<EPG_SECTION_STATUS, BOOL> status = this->tsOut.GetSectionStatusService(itr->ONID, itr->TSID, itr->SID, leitFlag);
							if( status.second == FALSE ){
								status.first = this->tsOut.GetSectionStatus(leitFlag);
							}
							if( status.first != EpgNoData ){
								this->epgCapChkNext = TRUE;
								if( status.first != EpgHEITAll &&
								    status.first != EpgLEITAll &&
								    (status.first != EpgBasicAll || basicOnly == FALSE) ){
									this->epgCapChkNext = FALSE;
									break;
								}
							}
						}
						WORD onid;
						WORD tsid;
						if( this->tsOut.GetStreamID(&onid, &tsid) == FALSE || onid != ch.ONID || tsid != ch.TSID ){
							//�`�����l�����ω������̂Œ�~
							this->tsOut.StopSaveEPG(FALSE);
							this->epgCapChkNext = TRUE;
						}else if( this->epgCapChkNext ){
							this->tsOut.StopSaveEPG(TRUE);
						}
					}
				}
			}
		}

		if( this->epgCapChkNext ){
			//���̃`�����l����
			chkCount++;
			if( this->epgCapChList.size() <= (size_t)chkCount ){
				//�S���`�F�b�N�I������̂ŏI��
				this->epgCapIndexOrStatus = ST_COMPLETE;
				return;
			}
			if( this->epgCapChList[chkCount].ONID == 4 && this->epgCapBSBasic && this->epgCapChkBS ||
			    this->epgCapChList[chkCount].ONID == 6 && this->epgCapCS1Basic && this->epgCapChkCS1 ||
			    this->epgCapChList[chkCount].ONID == 7 && this->epgCapCS2Basic && this->epgCapChkCS2 ||
			    this->epgCapChList[chkCount].ONID == 10 && this->epgCapCS3Basic && this->epgCapChkCS3 ){
				chkCount++;
				while( (size_t)chkCount < this->epgCapChList.size() ){
					if( this->epgCapChList[chkCount].ONID != this->epgCapChList[chkCount - 1].ONID ){
						break;
					}
					chkCount++;
				}
				if( this->epgCapChList.size() <= (size_t)chkCount ){
					//�S���`�F�b�N�I������̂ŏI��
					this->epgCapIndexOrStatus = ST_COMPLETE;
					return;
				}
			}
			this->epgCapIndexOrStatus = chkCount;
			DWORD space;
			DWORD ch;
			if( this->chUtil.GetCh(this->epgCapChList[chkCount].ONID, this->epgCapChList[chkCount].TSID,
			                       this->epgCapChList[chkCount].SID, space, ch) &&
			    this->ProcessSetCh(space, ch, FALSE) ){
				this->epgCapSetChState = 0;
				this->epgCapChkNext = FALSE;
			}
		}
	}
}

void CBonCtrl::GetEpgDataFilePath(WORD ONID, WORD TSID, wstring& epgDataFilePath)
{
	Format(epgDataFilePath, L"%04X%04X_epg.dat", ONID, TSID);
	epgDataFilePath = GetSettingPath().append(EPG_SAVE_FOLDER).append(epgDataFilePath).native();
}

void CBonCtrl::SaveErrCount(
	DWORD id,
	const wstring& filePath,
	BOOL asUtf8,
	int dropSaveThresh,
	int scrambleSaveThresh,
	ULONGLONG& drop,
	ULONGLONG& scramble
	)
{
	this->tsOut.SaveErrCount(id, filePath, asUtf8, dropSaveThresh, scrambleSaveThresh, drop, scramble);
}

//�^�撆�̃t�@�C���̏o�̓T�C�Y���擾����
//�����F
// id					[IN]���䎯��ID
// writeSize			[OUT]�ۑ��t�@�C����
void CBonCtrl::GetRecWriteSize(
	DWORD id,
	__int64* writeSize
	)
{
	this->tsOut.GetRecWriteSize(id, writeSize);
}

//�o�b�N�O���E���h�ł�EPG�擾�ݒ�
//�����F
// enableLive	[IN]�������Ɏ擾����
// enableRec	[IN]�^�撆�Ɏ擾����
// enableRec	[IN]EPG�擾����`�����l���ꗗ
// *Basic		[IN]�P�`�����l�������{���̂ݎ擾���邩�ǂ���
// backStartWaitSec	[IN]Ch�؂�ւ��A�^��J�n��A�o�b�N�O���E���h�ł�EPG�擾���J�n����܂ł̕b��
void CBonCtrl::SetBackGroundEpgCap(
	BOOL enableLive,
	BOOL enableRec,
	BOOL BSBasic,
	BOOL CS1Basic,
	BOOL CS2Basic,
	BOOL CS3Basic,
	DWORD backStartWaitSec
	)
{
	this->enableLiveEpgCap = enableLive;
	this->enableRecEpgCap = enableRec;
	this->epgCapBackBSBasic = BSBasic;
	this->epgCapBackCS1Basic = CS1Basic;
	this->epgCapBackCS2Basic = CS2Basic;
	this->epgCapBackCS3Basic = CS3Basic;
	this->epgCapBackStartWaitSec = backStartWaitSec;

	if( this->epgCapBackIndexOrStatus >= ST_WORKING ){
		StartBackgroundEpgCap();
	}
}

void CBonCtrl::StartBackgroundEpgCap()
{
	StopBackgroundEpgCap();
	if( this->chScanIndexOrStatus < ST_WORKING &&
	    this->epgCapIndexOrStatus < ST_WORKING ){
		if( this->bonUtil.GetOpenBonDriverFileName().empty() == false ){
			this->epgCapTick = GetTickCount();
			this->epgCapBackIndexOrStatus = ST_WORKING;
		}
	}
}

void CBonCtrl::StopBackgroundEpgCap()
{
	if( this->epgCapBackIndexOrStatus >= 0 ){
		this->tsOut.StopSaveEPG(FALSE);
	}
	this->epgCapBackIndexOrStatus = ST_STOP;
}

void CBonCtrl::CheckEpgCapBack()
{
	DWORD tick = GetTickCount();
	if( this->epgCapBackIndexOrStatus == ST_WORKING ){
		//�擾�ҋ@��
		if( tick - this->epgCapTick > this->epgCapBackStartWaitSec * 1000 ){
			WORD onid;
			WORD tsid;
			if( (this->tsOut.IsRec() ? this->enableRecEpgCap : this->enableLiveEpgCap) && this->tsOut.GetStreamID(&onid, &tsid) ){
				BOOL basicOnly = onid == 4 && this->epgCapBackBSBasic ||
				                 onid == 6 && this->epgCapBackCS1Basic ||
				                 onid == 7 && this->epgCapBackCS2Basic ||
				                 onid == 10 && this->epgCapBackCS3Basic;
				this->epgCapChList = this->chUtil.GetEpgCapServiceAll(onid, basicOnly ? -1 : tsid);
				vector<SET_CH_INFO>::const_iterator itr = std::find_if(this->epgCapChList.begin(), this->epgCapChList.end(),
				                                                       [=](const SET_CH_INFO& a) { return a.TSID == tsid; });
				if( itr != this->epgCapChList.end() ){
					//�擾�Ώۃ`�����l���Ȃ̂Ŏ擾�J�n
					wstring epgDataPath;
					GetEpgDataFilePath(onid, basicOnly ? 0xFFFF : tsid, epgDataPath);
					if( this->tsOut.StartSaveEPG(epgDataPath) ){
						fs_path iniPath = GetCommonIniPath().replace_filename(L"BonCtrl.ini");
						this->epgCapTimeOut = GetPrivateProfileInt(L"EPGCAP", L"EpgCapTimeOut", 10, iniPath.c_str());
						this->epgCapSaveTimeOut = GetPrivateProfileInt(L"EPGCAP", L"EpgCapSaveTimeOut", 0, iniPath.c_str()) != 0;
						this->epgCapSetChState = 2;
						this->epgCapTick = tick;
						this->epgCapLastChkTick = tick;
						this->epgCapBackIndexOrStatus = (int)(itr - this->epgCapChList.begin());
					}
				}
			}
			if( this->epgCapBackIndexOrStatus == ST_WORKING ){
				this->epgCapChList.clear();
				this->epgCapBackIndexOrStatus = ST_STOP;
			}
		}
		return;
	}else if( this->epgCapBackIndexOrStatus < 0 ){
		return;
	}

	//�擾���B�Œ�60�b�͎擾���A�Ȍ�10�b���ƂɃ`�F�b�N
	if( tick - this->epgCapLastChkTick > (DWORD)(this->epgCapSetChState == 2 ? 60 : 10) * 1000 ){
		this->epgCapSetChState = 3;
		this->epgCapLastChkTick = tick;
		BOOL basicOnly = this->epgCapChList[0].ONID == 4 && this->epgCapBackBSBasic ||
		                 this->epgCapChList[0].ONID == 6 && this->epgCapBackCS1Basic ||
		                 this->epgCapChList[0].ONID == 7 && this->epgCapBackCS2Basic ||
		                 this->epgCapChList[0].ONID == 10 && this->epgCapBackCS3Basic;
		//�~�Ϗ�ԃ`�F�b�N
		BOOL chkNext = FALSE;
		for( vector<SET_CH_INFO>::const_iterator itr = this->epgCapChList.begin(); itr != this->epgCapChList.end(); itr++ ){
			BOOL leitFlag = this->chUtil.IsPartial(itr->ONID, itr->TSID, itr->SID);
			pair<EPG_SECTION_STATUS, BOOL> status = this->tsOut.GetSectionStatusService(itr->ONID, itr->TSID, itr->SID, leitFlag);
			if( status.second == FALSE ){
				status.first = this->tsOut.GetSectionStatus(leitFlag);
			}
			if( status.first != EpgNoData ){
				chkNext = TRUE;
				if( status.first != EpgHEITAll &&
				    status.first != EpgLEITAll &&
				    (status.first != EpgBasicAll || basicOnly == FALSE) ){
					chkNext = FALSE;
					break;
				}
			}
		}

		WORD onid;
		WORD tsid;
		if( this->tsOut.GetStreamID(&onid, &tsid) == FALSE ||
		    onid != this->epgCapChList[this->epgCapBackIndexOrStatus].ONID ||
		    tsid != this->epgCapChList[this->epgCapBackIndexOrStatus].TSID ){
			//�`�����l�����ω������̂Œ�~
			this->tsOut.StopSaveEPG(FALSE);
			this->epgCapBackIndexOrStatus = ST_STOP;
		}else if( chkNext ){
			this->tsOut.StopSaveEPG(TRUE);
			CSendCtrlCmd cmd;
			cmd.SetConnectTimeOut(1000);
			cmd.SendReloadEpg();
			this->epgCapBackIndexOrStatus = ST_STOP;
		}else{
			if( tick - this->epgCapTick > this->epgCapTimeOut * 60 * 1000 ){
				//timeOut���ȏォ�����Ă���Ȃ��~
				this->tsOut.StopSaveEPG(this->epgCapSaveTimeOut);
				CSendCtrlCmd cmd;
				cmd.SetConnectTimeOut(1000);
				cmd.SendReloadEpg();
				_OutputDebugString(L"++%d����EPG�擾�������� or Ch�ύX�ŃG���[", this->epgCapTimeOut);
				this->epgCapBackIndexOrStatus = ST_STOP;
			}
		}
	}
}

void CBonCtrl::GetViewStatusInfo(
	float* signalLv,
	int* space,
	int* ch,
	ULONGLONG* drop,
	ULONGLONG* scramble
	)
{
	this->tsOut.GetErrCount(this->nwCtrlID, drop, scramble);

	CBlockLock lock(&this->buffLock);
	*signalLv = this->statusSignalLv;
	*space = this->viewSpace;
	*ch = this->viewCh;
}
