#include "stdafx.h"
#include "TSOut.h"

#include "../Common/TimeUtil.h"
#include "../Common/EpgTimerUtil.h"

CTSOut::CTSOut(void)
	: epgFile(NULL, fclose)
{
	this->chChangeState = CH_ST_INIT;
	this->chChangeTime = 0;
	this->lastONID = 0xFFFF;
	this->lastTSID = 0xFFFF;

	this->epgUtil.Initialize(FALSE);

	this->enableDecodeFlag = FALSE;
	this->emmEnableFlag = FALSE;

	this->nextCtrlID = 1;
	this->noLogScramble = FALSE;
	this->parseEpgPostProcess = FALSE;
}


CTSOut::~CTSOut(void)
{
	StopSaveEPG(FALSE);
}

void CTSOut::SetChChangeEvent(BOOL resetEpgUtil)
{
	CBlockLock lock(&this->objLock);

	this->chChangeState = CH_ST_WAIT_PAT;
	this->chChangeTime = GetTickCount();

	this->decodeUtil.UnLoadDll();

	if( resetEpgUtil == TRUE ){
		CBlockLock lock2(&this->epgUtilLock);
		//EpgDataCap3�͓������\�b�h�P�ʂŃA�g�~�b�N�B�������ȊO��objLock��epgUtilLock�̂ǂ��炩���l������΂悢
		this->epgUtil.UnInitialize();
		this->epgUtil.Initialize(FALSE);
	}
}

BOOL CTSOut::IsChUnknown(DWORD* elapsedTime)
{
	CBlockLock lock(&this->objLock);

	if( this->chChangeState != CH_ST_DONE ){
		if( elapsedTime != NULL ){
			*elapsedTime = this->chChangeState == CH_ST_INIT ? MAXDWORD : GetTickCount() - this->chChangeTime;
		}
		return TRUE;
	}
	return FALSE;
}

BOOL CTSOut::GetStreamID(WORD* ONID, WORD* TSID)
{
	CBlockLock lock(&this->objLock);

	if( this->chChangeState == CH_ST_DONE ){
		*ONID = this->lastONID;
		*TSID = this->lastTSID;
		return TRUE;
	}
	return FALSE;
}

void CTSOut::OnChChanged(WORD onid, WORD tsid)
{
	this->chChangeState = CH_ST_DONE;
	this->lastONID = onid;
	this->lastTSID = tsid;
	this->epgUtil.ClearSectionStatus();

	if( this->enableDecodeFlag != FALSE || this->emmEnableFlag != FALSE ){
		//�X�N�����u��������EMM�������ݒ肳��Ă���ꍇ�������s
		if( this->decodeUtil.SetNetwork(onid, tsid) == FALSE ){
			OutputDebugString(L"����Decode DLL load err [CTSOut::OnChChanged()]\r\n");
			//�Ď��s�͈Ӗ����Ȃ������Ȃ̂Ŕp�~
		}
		this->decodeUtil.SetEmm(this->emmEnableFlag);
	}
	ResetErrCount();

	this->serviceFilter.Clear(tsid);
}

void CTSOut::AddTSBuff(BYTE* data, DWORD dataSize)
{
	//data�͓����ς݂����̃T�C�Y��188�̐����{�ł��邱��

	CBlockLock lock(&this->objLock);
	if( dataSize == 0 || data == NULL ){
		return;
	}
	DWORD tick = GetTickCount();
	if( this->chChangeState == CH_ST_WAIT_PAT && tick - this->chChangeTime < 1000 ){
		//1�b�Ԃ̓`�����l���؂�ւ��O�̃p�P�b�g����\�����l�����Ė�������
		return;
	}
	this->decodeBuff.clear();

	BYTE* decodeData = NULL;
	DWORD decodeSize = 0;
	{
		for( DWORD i=0; i<dataSize; i+=188 ){
			CTSPacketUtil packet;
			if( packet.Set188TS(data + i, 188) == TRUE ){
				if( this->chChangeState != CH_ST_DONE ){
					//�`�����l���؂�ւ���
					if( packet.transport_scrambling_control != 0 ){
						//�X�N�����u���p�P�b�g�Ȃ̂ŉ�͂ł��Ȃ�
						continue;
					}
					this->epgUtil.AddTSPacket(data + i, 188);
					//GetTSID()���؂�ւ��O�̒l��Ԃ��Ȃ��悤��PAT��҂�
					if( this->chChangeState == CH_ST_WAIT_PAT ){
						if( packet.PID == 0 && packet.payload_unit_start_indicator ){
							this->chChangeState = CH_ST_WAIT_PAT2;
						}
					}else if( this->chChangeState == CH_ST_WAIT_PAT2 ){
						if( packet.PID == 0 && packet.payload_unit_start_indicator ){
							this->chChangeState = CH_ST_WAIT_ID;
						}
					}
					if( this->chChangeState == CH_ST_INIT || this->chChangeState == CH_ST_WAIT_ID ){
						WORD onid;
						WORD tsid;
						if( this->epgUtil.GetTSID(&onid, &tsid) == NO_ERR ){
							if( this->chChangeState == CH_ST_INIT ){
								_OutputDebugString(L"��Ch Init 0x%04X 0x%04X\r\n", onid, tsid);
								OnChChanged(onid, tsid);
							}else if( onid != this->lastONID || tsid != this->lastTSID ){
								_OutputDebugString(L"��Ch Change 0x%04X 0x%04X => 0x%04X 0x%04X\r\n", this->lastONID, this->lastTSID, onid, tsid);
								OnChChanged(onid, tsid);
							}else if( tick - this->chChangeTime > 7000 ){
								OutputDebugString(L"��Ch NoChange\r\n");
								OnChChanged(onid, tsid);
							}
						}
					}
				}else{
					this->serviceFilter.FilterPacket(this->decodeBuff, data + i, packet);
					if( this->serviceFilter.CatOrPmtUpdated() ){
						UpdateServiceUtil(FALSE);
					}
					if( packet.PID < BON_SELECTIVE_PID && this->parseEpgPostProcess == FALSE ){
						ParseEpgPacket(data + i, packet);
					}
				}
			}
		}
		if( this->chChangeState == CH_ST_DONE ){
			WORD onid;
			WORD tsid;
			if( this->epgUtil.GetTSID(&onid, &tsid) == NO_ERR ){
				if( onid != this->lastONID || tsid != this->lastTSID ){
					_OutputDebugString(L"��Ch Unexpected Change 0x%04X 0x%04X => 0x%04X 0x%04X\r\n", this->lastONID, this->lastTSID, onid, tsid);
					OnChChanged(onid, tsid);
				}
			}
		}
	}
	try{
		if( this->decodeBuff.empty() == false ){
			if( this->enableDecodeFlag ){
				//�f�R�[�h�K�v

				if( decodeUtil.Decode(&this->decodeBuff.front(), (DWORD)this->decodeBuff.size(), &decodeData, &decodeSize) == FALSE ){
					//�f�R�[�h���s
					decodeData = &this->decodeBuff.front();
					decodeSize = (DWORD)this->decodeBuff.size();
				}else{
					if( decodeData == NULL || decodeSize == 0 ){
						decodeData = NULL;
						decodeSize = 0;
					}
				}
			}else{
				//�f�R�[�h�̕K�v�Ȃ�
				decodeData = &this->decodeBuff.front();
				decodeSize = (DWORD)this->decodeBuff.size();
			}
		}
	}catch(...){
		_OutputDebugString(L"����CTSOut::AddTSBuff Exception2");
		//�f�R�[�h���s
		decodeData = &this->decodeBuff.front();
		decodeSize = (DWORD)this->decodeBuff.size();
	}
	
	//�f�R�[�h�ς݂̃f�[�^����͂�����
	if( this->parseEpgPostProcess ){
		for( DWORD i=0; i<decodeSize; i+=188 ){
			CTSPacketUtil packet;
			if( packet.Set188TS(decodeData + i, 188) && packet.PID < BON_SELECTIVE_PID ){
				ParseEpgPacket(decodeData + i, packet);
			}
		}
	}

	//�e�T�[�r�X�����Ƀf�[�^�n��
	{
		for( auto itrService = serviceUtilMap.begin(); itrService != serviceUtilMap.end(); itrService++ ){
			itrService->second->AddTSBuff(decodeData, decodeSize, [this](WORD onid, WORD tsid, WORD sid) -> int {
				CBlockLock lock2(&this->epgUtilLock);
				EPG_EVENT_INFO* epgInfo;
				return this->epgUtil.GetEpgInfo(onid, tsid, sid, FALSE, &epgInfo) == NO_ERR ? epgInfo->event_id : -1;
			});
		}
	}
}

void CTSOut::ParseEpgPacket(BYTE* data, const CTSPacketUtil& packet)
{
	if( this->epgFile ){
		if( packet.PID == 0 && packet.payload_unit_start_indicator ){
			if( this->epgFileState == EPG_FILE_ST_NONE ){
				this->epgFileState = EPG_FILE_ST_PAT;
			}else if( this->epgFileState == EPG_FILE_ST_PAT ){
				this->epgFileState = EPG_FILE_ST_TOT;
				//�ԑg��񂪕s�����Ȃ��悤���߂Ē~�Ϗ�Ԃ����Z�b�g
				this->epgUtil.ClearSectionStatus();
				//TOT��O�|���ŏ������ނ��߂̏ꏊ���m��
				BYTE nullData[188] = { 0x47, 0x1F, 0xFF, 0x10 };
				std::fill_n(nullData + 4, 184, 0xFF);
				this->epgFileTotPos = _ftelli64(this->epgFile.get());
				fwrite(nullData, 1, 188, this->epgFile.get());
			}
		}
		//�܂�PAT�A����(�����)TOT���������ށB���̏����͕K�{�ł͂Ȃ����ԑg�������m���������I�ɓǂݏo����
		if( packet.PID == 0x14 && this->epgFileState == EPG_FILE_ST_TOT ){
			this->epgFileState = EPG_FILE_ST_ALL;
			if( this->epgFileTotPos >= 0 ){
				_fseeki64(this->epgFile.get(), this->epgFileTotPos, SEEK_SET);
			}
			fwrite(data, 1, 188, this->epgFile.get());
			_fseeki64(this->epgFile.get(), 0, SEEK_END);
		}else if( (packet.PID == 0 && this->epgFileState >= EPG_FILE_ST_PAT) || this->epgFileState >= EPG_FILE_ST_TOT ){
			fwrite(data, 1, 188, this->epgFile.get());
		}
	}
	this->epgUtil.AddTSPacket(data, 188);
}

void CTSOut::UpdateServiceUtil(BOOL updateFilterSID)
{
	vector<WORD> filterSIDList;

	//�e�T�[�r�X��PMT��T��
	for( auto itrService = serviceUtilMap.begin(); itrService != serviceUtilMap.end(); itrService++ ){
		if( updateFilterSID ){
			filterSIDList.push_back(itrService->second->GetSID());
		}
		//EMM��PID
		for( auto itr = this->serviceFilter.CatUtil().GetPIDList().cbegin(); itr != this->serviceFilter.CatUtil().GetPIDList().end(); itr++ ){
			itrService->second->SetPIDName(*itr, L"EMM");
		}
		for( auto itrPmt = this->serviceFilter.PmtUtilMap().cbegin(); itrPmt != this->serviceFilter.PmtUtilMap().end(); itrPmt++ ){
			if( itrService->second->GetSID() == itrPmt->second.GetProgramNumber() ){
				//PMT����
				itrService->second->SetPmtPID(this->lastTSID, itrPmt->first);
				itrService->second->SetEmmPID(this->serviceFilter.CatUtil().GetPIDList());
			}

			itrService->second->SetPIDName(itrPmt->second.GetPcrPID(), L"PCR");
			wstring name;
			for( auto itrPID = itrPmt->second.GetPIDTypeList().cbegin(); itrPID != itrPmt->second.GetPIDTypeList().end(); itrPID++ ){
				switch( itrPID->second ){
				case 0x00:
					name = L"ECM";
					break;
				case 0x02:
					name = L"MPEG2 VIDEO";
					break;
				case 0x0F:
					name = L"MPEG2 AAC";
					break;
				case 0x1B:
					name = L"MPEG4 VIDEO";
					break;
				case 0x04:
					name = L"MPEG2 AUDIO";
					break;
				case 0x24:
					name = L"HEVC VIDEO";
					break;
				case 0x06:
					name = L"����";
					break;
				case 0x0D:
					name = L"�f�[�^�J���[�Z��";
					break;
				default:
					Format(name, L"stream_type 0x%0X", itrPID->second);
					break;
				}
				itrService->second->SetPIDName(itrPID->first, name);
			}
			Format(name, L"PMT(ServiceID 0x%04X)", itrPmt->second.GetProgramNumber());
			itrService->second->SetPIDName(itrPmt->first, name);
		}
	}
	if( updateFilterSID ){
		this->serviceFilter.SetServiceID(std::find(filterSIDList.begin(), filterSIDList.end(), 0xFFFF) != filterSIDList.end(), filterSIDList);
	}
}

//EPG�f�[�^�̕ۑ����J�n����
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
BOOL CTSOut::StartSaveEPG(
	const wstring& epgFilePath_
	)
{
	CBlockLock lock(&this->objLock);
	if( this->epgFile != NULL ){
		return FALSE;
	}
	this->epgFilePath = epgFilePath_;
	this->epgTempFilePath = epgFilePath_;
	this->epgTempFilePath += L".tmp";

	_OutputDebugString(L"��%ls\r\n", this->epgFilePath.c_str());
	_OutputDebugString(L"��%ls\r\n", this->epgTempFilePath.c_str());

	this->epgUtil.ClearSectionStatus();
	this->epgFileState = EPG_FILE_ST_NONE;

	UtilCreateDirectories(fs_path(this->epgTempFilePath).parent_path());
	this->epgFile.reset(UtilOpenFile(this->epgTempFilePath, UTIL_SECURE_WRITE));
	if( this->epgFile == NULL ){
		OutputDebugString(L"err\r\n");
		return FALSE;
	}

	return TRUE;
}

//EPG�f�[�^�̕ۑ����I������
void CTSOut::StopSaveEPG(
	BOOL copy
	)
{
	CBlockLock lock(&this->objLock);
	if( this->epgFile == NULL ){
		return;
	}

	this->epgFile.reset();

	if( copy == TRUE ){
		CopyFile(this->epgTempFilePath.c_str(), this->epgFilePath.c_str(), FALSE );
	}
	DeleteFile(this->epgTempFilePath.c_str());
}

//EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
//�߂�l�F
// �X�e�[�^�X
//�����F
// l_eitFlag		[IN]L-EIT�̃X�e�[�^�X���擾
EPG_SECTION_STATUS CTSOut::GetSectionStatus(
	BOOL l_eitFlag
	)
{
	CBlockLock lock(&this->epgUtilLock);

	return this->epgUtil.GetSectionStatus(l_eitFlag);
}

//�w��T�[�r�X��EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
pair<EPG_SECTION_STATUS, BOOL> CTSOut::GetSectionStatusService(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL l_eitFlag
	)
{
	CBlockLock lock(&this->epgUtilLock);

	return this->epgUtil.GetSectionStatusService(originalNetworkID, transportStreamID, serviceID, l_eitFlag);
}

//EMM�����̓���ݒ�
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// enable		[IN] TRUE�i��������j�AFALSE�i�������Ȃ��j
BOOL CTSOut::SetEmm(
	BOOL enable
	)
{
	CBlockLock lock(&this->objLock);

	try{
		if( this->chChangeState == CH_ST_DONE ){
			//�`���[�j���O�ς݂�
			if( enable != FALSE && this->enableDecodeFlag == FALSE && this->emmEnableFlag == FALSE ){
				//�ŏ��� EMM �������ݒ肳���ꍇ�� DLL ��ǂݍ���
				//�X�N�����u���������ݒ肳��Ă���ꍇ�͓ǂݍ��ݍς݂Ȃ̂ŏ��O
				if( this->decodeUtil.SetNetwork(this->lastONID, this->lastTSID) == FALSE ){
					OutputDebugString(L"����Decode DLL load err [CTSOut::SetEmm()]\r\n");
				}
			}
		}
	}catch(...){
		return FALSE;
	}

	this->emmEnableFlag = enable;
	return this->decodeUtil.SetEmm(enable);
}

//EMM�������s������
//�߂�l�F
// ������
DWORD CTSOut::GetEmmCount()
{
	CBlockLock lock(&this->objLock);

	return this->decodeUtil.GetEmmCount();
}

//DLL�̃��[�h��Ԃ��擾
//�߂�l�F
// TRUE�i���[�h�ɐ������Ă���j�AFALSE�i���[�h�Ɏ��s���Ă���j
//�����F
// loadErrDll		[OUT]���[�h�Ɏ��s����DLL�t�@�C����
BOOL CTSOut::GetLoadStatus(
	wstring& loadErrDll
	)
{
	CBlockLock lock(&this->objLock);

	return this->decodeUtil.GetLoadStatus(loadErrDll);
}

//���X�g���[���̃T�[�r�X�ꗗ���擾����
//�߂�l�F
// �G���[�R�[�h
//�����F
// funcGetList		[IN]�߂�l��NO_ERR�̂Ƃ��T�[�r�X���̌��Ƃ��̃��X�g�������Ƃ��ČĂяo�����֐�
DWORD CTSOut::GetServiceListActual(
	const std::function<void(DWORD, SERVICE_INFO*)>& funcGetList
	)
{
	CBlockLock lock(&this->epgUtilLock);

	DWORD serviceListSize;
	SERVICE_INFO* serviceList;
	DWORD ret = this->epgUtil.GetServiceListActual(&serviceListSize, &serviceList);
	if( ret == NO_ERR && funcGetList ){
		funcGetList(serviceListSize, serviceList);
	}
	return ret;
}

//���Ɏg�p���鐧��ID���擾����
//�߂�l�F
// id
DWORD CTSOut::GetNextID()
{
	DWORD nextID = 0xFFFFFFFF;

	auto itr = this->serviceUtilMap.find(this->nextCtrlID);
	if( itr == this->serviceUtilMap.end() ){
		//���݂��Ȃ�ID�Ȃ̂ł��̂܂܎g�p
		nextID = this->nextCtrlID;
		this->nextCtrlID++;
		if( this->nextCtrlID == 0 || this->nextCtrlID == 0xFFFFFFFF){
			this->nextCtrlID = 1;
		}
	}else{
		//��������H
		for( DWORD i=1; i<0xFFFFFFFF; i++ ){
			//�P���珇�Ԃɑ��݂��Ȃ�ID���m�F
			itr = this->serviceUtilMap.find(this->nextCtrlID);
			if( itr == this->serviceUtilMap.end() ){
				nextID = this->nextCtrlID;
				this->nextCtrlID++;
				if( this->nextCtrlID == 0 || this->nextCtrlID == 0xFFFFFFFF){
					this->nextCtrlID = 1;
				}
				break;
			}else{
				this->nextCtrlID++;
			}
			if( this->nextCtrlID == 0 || this->nextCtrlID == 0xFFFFFFFF){
				this->nextCtrlID = 1;
			}
		}
	}

	return nextID;
}

//TS�X�g���[������p�R���g���[�����쐬����
//�߂�l�F
// ���䎯��ID
//�����F
// sendUdpTcp	[IN]UDP/TCP���M�p�ɂ���
DWORD CTSOut::CreateServiceCtrl(
	BOOL sendUdpTcp
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = this->serviceUtilMap.insert(
		std::make_pair(GetNextID(), std::unique_ptr<COneServiceUtil>(new COneServiceUtil(sendUdpTcp)))).first;
	itr->second->SetBonDriver(bonFile);
	itr->second->SetNoLogScramble(noLogScramble);
	UpdateServiceUtil(TRUE);

	return itr->first;
}

//TS�X�g���[������p�R���g���[�����폜����
//�߂�l�F
// �G���[�R�[�h
//�����F
// id			[IN]���䎯��ID
BOOL CTSOut::DeleteServiceCtrl(
	DWORD id
	)
{
	CBlockLock lock(&this->objLock);

	if( serviceUtilMap.erase(id) == 0 ){
		return FALSE;
	}

	UpdateEnableDecodeFlag();
	UpdateServiceUtil(TRUE);

	return TRUE;
}

//����Ώۂ̃T�[�r�X��ݒ肷��
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s
//�����F
// id			[IN]���䎯��ID
// serviceID	[IN]�ΏۃT�[�r�XID
BOOL CTSOut::SetServiceID(
	DWORD id,
	WORD serviceID
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr == serviceUtilMap.end() ){
		return FALSE;
	}

	itr->second->SetSID(serviceID);
	UpdateServiceUtil(TRUE);

	return TRUE;
}

BOOL CTSOut::GetServiceID(
	DWORD id,
	WORD* serviceID
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr == serviceUtilMap.end() ){
		return FALSE;
	}
	if( serviceID != NULL ){
		*serviceID = itr->second->GetSID();
	}

	return TRUE;
}

//UDP�ő��M���s��
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// id			[IN]���䎯��ID
// sendList		[IN/OUT]���M�惊�X�g�BNULL�Œ�~�BPort�͎��ۂɑ��M�Ɏg�p����Port���Ԃ�B
BOOL CTSOut::SendUdp(
	DWORD id,
	vector<NW_SEND_INFO>* sendList
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr == serviceUtilMap.end() ){
		return FALSE;
	}

	itr->second->SendUdp(sendList);

	return TRUE;
}

//TCP�ő��M���s��
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// id			[IN]���䎯��ID
// sendList		[IN/OUT]���M�惊�X�g�BNULL�Œ�~�BPort�͎��ۂɑ��M�Ɏg�p����Port���Ԃ�B
BOOL CTSOut::SendTcp(
	DWORD id,
	vector<NW_SEND_INFO>* sendList
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr == serviceUtilMap.end() ){
		return FALSE;
	}

	itr->second->SendTcp(sendList);

	return TRUE;
}

//�w��T�[�r�X�̌���or����EPG�����擾����
//�߂�l�F
// �G���[�R�[�h
//�����F
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// nextFlag					[IN]TRUE�i���̔ԑg�j�AFALSE�i���݂̔ԑg�j
// epgInfo					[OUT]EPG���
DWORD CTSOut::GetEpgInfo(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL nextFlag,
	EPGDB_EVENT_INFO* epgInfo
	)
{
	CBlockLock lock(&this->epgUtilLock);

	EPG_EVENT_INFO* _epgInfo;
	DWORD err = this->epgUtil.GetEpgInfo(originalNetworkID, transportStreamID, serviceID, nextFlag, &_epgInfo);
	if( err == NO_ERR ){
		ConvertEpgInfo(originalNetworkID, transportStreamID, serviceID, _epgInfo, epgInfo);
	}

	return err;
}

//�w��C�x���g��EPG�����擾����
//�߂�l�F
// �G���[�R�[�h
//�����F
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// eventID					[IN]�擾�Ώۂ�EventID
// pfOnlyFlag				[IN]p/f����̂݌������邩�ǂ���
// epgInfo					[OUT]EPG���
DWORD CTSOut::SearchEpgInfo(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	WORD eventID,
	BYTE pfOnlyFlag,
	EPGDB_EVENT_INFO* epgInfo
	)
{
	CBlockLock lock(&this->epgUtilLock);

	EPG_EVENT_INFO* _epgInfo;
	DWORD err = this->epgUtil.SearchEpgInfo(originalNetworkID, transportStreamID, serviceID, eventID, pfOnlyFlag, &_epgInfo);
	if( err == NO_ERR ){
		ConvertEpgInfo(originalNetworkID, transportStreamID, serviceID, _epgInfo, epgInfo);
	}

	return err;
}

//PC���v�����Ƃ����X�g���[�����ԂƂ̍����擾����
//�߂�l�F
// ���̕b��
int CTSOut::GetTimeDelay(
	)
{
	CBlockLock lock(&this->epgUtilLock);

	return this->epgUtil.GetTimeDelay();
}

//�^�撆���ǂ���
//�߂�l�F
// TRUE�i�^�撆�j�AFALSE�i���Ă��Ȃ��j
BOOL CTSOut::IsRec()
{
	CBlockLock lock(&this->objLock);

	for( auto itr = this->serviceUtilMap.begin(); itr != this->serviceUtilMap.end(); itr++ ){
		if( itr->second->IsRec() == TRUE ){
			return TRUE;
		}
	}

	return FALSE;
}

BOOL CTSOut::StartSave(
	const SET_CTRL_REC_PARAM& recParam,
	const vector<wstring>& saveFolderSub,
	int maxBuffCount
)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(recParam.ctrlID);
	if( itr == serviceUtilMap.end() ){
		return FALSE;
	}

	return itr->second->StartSave(recParam, saveFolderSub, maxBuffCount);
}

BOOL CTSOut::EndSave(
	DWORD id,
	BOOL* subRecFlag
	)
{
	CBlockLock lock(&this->objLock);
	auto itr = serviceUtilMap.find(id);
	if( itr == serviceUtilMap.end() ){
		return FALSE;
	}

	return itr->second->EndSave(subRecFlag);
}

//�X�N�����u�����������̓���ݒ�
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// enable		[IN] TRUE�i��������j�AFALSE�i�������Ȃ��j
BOOL CTSOut::SetScramble(
	DWORD id,
	BOOL enable
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr == serviceUtilMap.end() ){
		return FALSE;
	}

	itr->second->SetScramble(enable);
	UpdateEnableDecodeFlag();
	return TRUE;
}

BOOL CTSOut::UpdateEnableDecodeFlag()
{
	BOOL sendUdpTcpOnly = TRUE;
	BOOL enable = FALSE;
	for( auto itr = this->serviceUtilMap.begin(); itr != this->serviceUtilMap.end(); itr++ ){
		if( itr->second->GetScramble() >= 0 ){
			if( itr->second->GetSendUdpTcp() ){
				//UDP/TCP���M�p�����̂Ƃ��͂��̐ݒ�l�ɏ]��
				if( sendUdpTcpOnly && itr->second->GetScramble() ){
					enable = TRUE;
				}
			}else{
				//�^��p�̂��̂�����Ƃ��͂��̐ݒ�l�ɏ]��
				sendUdpTcpOnly = FALSE;
				enable = FALSE;
				if( itr->second->GetScramble() ){
					enable = TRUE;
					break;
				}
			}
		}
	}

	try{
		if( this->chChangeState == CH_ST_DONE ){
			//�`���[�j���O�ς݂�
			if( enable != FALSE && this->enableDecodeFlag == FALSE && this->emmEnableFlag == FALSE ){
				//�ŏ��ɃX�N�����u���������ݒ肳���ꍇ�� DLL ���ēǂݍ��݂���
				//EMM �������ݒ肳��Ă���ꍇ�͓ǂݍ��ݍς݂Ȃ̂ŏ��O
				if( this->decodeUtil.SetNetwork(this->lastONID, this->lastTSID) == FALSE ){
					OutputDebugString(L"����Decode DLL load err [CTSOut::SetScramble()]\r\n");
				}
			}
		}
	}catch(...){
		return FALSE;
	}

	this->enableDecodeFlag = enable;
	return TRUE;
}

//�����ƃf�[�^�����܂߂邩�ǂ���
//�����F
// id					[IN]���䎯��ID
// enableCaption		[IN]������ TRUE�i�܂߂�j�AFALSE�i�܂߂Ȃ��j
// enableData			[IN]�f�[�^������ TRUE�i�܂߂�j�AFALSE�i�܂߂Ȃ��j
void CTSOut::SetServiceMode(
	DWORD id,
	BOOL enableCaption,
	BOOL enableData
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr != serviceUtilMap.end() ){
		itr->second->SetServiceMode(enableCaption, enableData);
	}
}

//�G���[�J�E���g���N���A����
//�����F
// id				[IN]���䎯��ID
void CTSOut::ClearErrCount(
	DWORD id
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr != serviceUtilMap.end() ){
		itr->second->ClearErrCount();
	}
}

//�h���b�v�ƃX�N�����u���̃J�E���g���擾����
//�����F
// id				[IN]���䎯��ID
// drop				[OUT]�h���b�v��
// scramble			[OUT]�X�N�����u����
void CTSOut::GetErrCount(
	DWORD id,
	ULONGLONG* drop,
	ULONGLONG* scramble
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr != serviceUtilMap.end() ){
		itr->second->GetErrCount(drop, scramble);
	}
}


//�^�撆�̃t�@�C���̏o�̓T�C�Y���擾����
//�����F
// id					[IN]���䎯��ID
// writeSize			[OUT]�o�̓T�C�Y
void CTSOut::GetRecWriteSize(
	DWORD id,
	__int64* writeSize
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr != serviceUtilMap.end() ){
		itr->second->GetRecWriteSize(writeSize);
	}
}

void CTSOut::ResetErrCount()
{
	for( auto itr = serviceUtilMap.begin(); itr != serviceUtilMap.end(); itr++ ){
		itr->second->ClearErrCount();
	}
}

wstring CTSOut::GetSaveFilePath(
	DWORD id
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr != serviceUtilMap.end() ){
		return itr->second->GetSaveFilePath();
	}
	return wstring();
}

void CTSOut::SaveErrCount(
	DWORD id,
	const wstring& filePath,
	BOOL asUtf8,
	int dropSaveThresh,
	int scrambleSaveThresh,
	ULONGLONG& drop,
	ULONGLONG& scramble
	)
{
	CBlockLock lock(&this->objLock);

	auto itr = serviceUtilMap.find(id);
	if( itr != serviceUtilMap.end() ){
		itr->second->SaveErrCount(filePath, asUtf8, dropSaveThresh, scrambleSaveThresh, drop, scramble);
	}
}

void CTSOut::SetSignalLevel(
	float signalLv
	)
{
	CBlockLock lock(&this->objLock);

	for( auto itr = serviceUtilMap.begin(); itr != serviceUtilMap.end(); itr++ ){
		itr->second->SetSignalLevel(signalLv);
	}
}


void CTSOut::SetBonDriver(
	const wstring& bonDriver
	)
{
	CBlockLock lock(&this->objLock);

	for( auto itr = serviceUtilMap.begin(); itr != serviceUtilMap.end(); itr++ ){
		itr->second->SetBonDriver(bonDriver);
	}
	bonFile = bonDriver;
}

void CTSOut::SetNoLogScramble(
	BOOL noLog
	)
{
	CBlockLock lock(&this->objLock);

	for( auto itr = serviceUtilMap.begin(); itr != serviceUtilMap.end(); itr++ ){
		itr->second->SetNoLogScramble(noLog);
	}
	noLogScramble = noLog;
}

void CTSOut::SetParseEpgPostProcess(
	BOOL parsePost
	)
{
	CBlockLock lock(&this->objLock);

	parseEpgPostProcess = parsePost;
}
