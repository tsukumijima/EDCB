#include "stdafx.h"
#include "EpgDataCap3Main.h"

#include "../../Common/TimeUtil.h"

CEpgDataCap3Main::CEpgDataCap3Main(void)
{
	decodeUtilClass.SetEpgDB(&(this->epgDBUtilClass));
}

CEpgDataCap3Main::~CEpgDataCap3Main(void)
{
	decodeUtilClass.SetEpgDB(NULL);
}

//��͑Ώۂ�TS�p�P�b�g�P��ǂݍ��܂���
// data		[IN]TS�p�P�b�g�P��
void CEpgDataCap3Main::AddTSPacket(
	BYTE* data
	)
{
	CBlockLock lock(&this->utilLock);

	this->decodeUtilClass.AddTSData(data);
}

//��̓f�[�^�̌��݂̃X�g���[���h�c���擾����
//�����F
// originalNetworkID		[OUT]���݂�originalNetworkID
// transportStreamID		[OUT]���݂�transportStreamID
BOOL CEpgDataCap3Main::GetTSID(
	WORD* originalNetworkID,
	WORD* transportStreamID
	)
{
	CBlockLock lock(&this->utilLock);
	return this->decodeUtilClass.GetTSID(originalNetworkID, transportStreamID);
}

//���X�g���[���̃T�[�r�X�ꗗ���擾����
//�����F
// serviceListSize			[OUT]serviceList�̌�
// serviceList				[OUT]�T�[�r�X���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
BOOL CEpgDataCap3Main::GetServiceListActual(
	DWORD* serviceListSize,
	SERVICE_INFO** serviceList
	)
{
	CBlockLock lock(&this->utilLock);
	return this->decodeUtilClass.GetServiceListActual(serviceListSize, serviceList);
}

//�~�ς��ꂽEPG���̂���T�[�r�X�ꗗ���擾����
//SERVICE_EXT_INFO�̏��͂Ȃ��ꍇ������
//�����F
// serviceListSize			[OUT]serviceList�̌�
// serviceList				[OUT]�T�[�r�X���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
void CEpgDataCap3Main::GetServiceListEpgDB(
	DWORD* serviceListSize,
	SERVICE_INFO** serviceList
	)
{
	CBlockLock lock(&this->utilLock);
	this->epgDBUtilClass.GetServiceListEpgDB(serviceListSize, serviceList);
}

//�w��T�[�r�X�̑SEPG�����擾����
//�����F
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// epgInfoListSize			[OUT]epgInfoList�̌�
// epgInfoList				[OUT]EPG���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
BOOL CEpgDataCap3Main::GetEpgInfoList(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	DWORD* epgInfoListSize,
	EPG_EVENT_INFO** epgInfoList
	)
{
	CBlockLock lock(&this->utilLock);
	return this->epgDBUtilClass.GetEpgInfoList(originalNetworkID, transportStreamID, serviceID, epgInfoListSize, epgInfoList);
}

//�w��T�[�r�X�̑SEPG����񋓂���
BOOL CEpgDataCap3Main::EnumEpgInfoList(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL (CALLBACK *enumEpgInfoListProc)(DWORD, EPG_EVENT_INFO*, LPVOID),
	LPVOID param
	)
{
	CBlockLock lock(&this->utilLock);
	return this->epgDBUtilClass.EnumEpgInfoList(originalNetworkID, transportStreamID, serviceID, enumEpgInfoListProc, param);
}

//�w��T�[�r�X�̌���or����EPG�����擾����
//�����F
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// nextFlag					[IN]TRUE�i���̔ԑg�j�AFALSE�i���݂̔ԑg�j
// epgInfo					[OUT]EPG���iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
BOOL CEpgDataCap3Main::GetEpgInfo(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL nextFlag,
	EPG_EVENT_INFO** epgInfo
	)
{
	CBlockLock lock(&this->utilLock);
	if( this->epgDBUtilClass.GetEpgInfo(originalNetworkID, transportStreamID, serviceID, nextFlag, epgInfo) == FALSE ){
		return FALSE;
	}

	//TODO: ���������I�ʂ����C�u�������ōs���͔̂����Ɏv�����A�݊��̂��ߎc���Ă���
	__int64 nowTime;
	if( this->decodeUtilClass.GetNowTime(&nowTime) == FALSE ){
		nowTime = GetNowI64Time();
	}
	if( nextFlag == FALSE && (*epgInfo)->StartTimeFlag != FALSE && (*epgInfo)->DurationFlag != FALSE ){
		if( nowTime < ConvertI64Time((*epgInfo)->start_time) || ConvertI64Time((*epgInfo)->start_time) + (*epgInfo)->durationSec * I64_1SEC < nowTime ){
			//���ԓ��ɂȂ��̂Ŏ��s
			epgInfo = NULL;
			return FALSE;
		}
	}else if( nextFlag == TRUE && (*epgInfo)->StartTimeFlag != FALSE ){
		if( nowTime > ConvertI64Time((*epgInfo)->start_time) ){
			//�J�n���Ԃ��߂��Ă���̂Ŏ��s
			epgInfo = NULL;
			return FALSE;
		}
	}

	return TRUE;
}

//�w��C�x���g��EPG�����擾����
//�����F
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// EventID					[IN]�擾�Ώۂ�EventID
// pfOnlyFlag				[IN]p/f����̂݌������邩�ǂ���
// epgInfo					[OUT]EPG���iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
BOOL CEpgDataCap3Main::SearchEpgInfo(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	WORD eventID,
	BYTE pfOnlyFlag,
	EPG_EVENT_INFO** epgInfo
	)
{
	CBlockLock lock(&this->utilLock);

	return this->epgDBUtilClass.SearchEpgInfo(originalNetworkID, transportStreamID, serviceID, eventID, pfOnlyFlag, epgInfo);
}

//EPG�f�[�^�̒~�Ϗ�Ԃ����Z�b�g����
void CEpgDataCap3Main::ClearSectionStatus()
{
	CBlockLock lock(&this->utilLock);
	this->epgDBUtilClass.ClearSectionStatus();
	return ;
}

//EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
//�߂�l�F
// �X�e�[�^�X
//�����F
// l_eitFlag		[IN]L-EIT�̃X�e�[�^�X���擾
EPG_SECTION_STATUS CEpgDataCap3Main::GetSectionStatus(BOOL l_eitFlag)
{
	CBlockLock lock(&this->utilLock);
	EPG_SECTION_STATUS status = this->epgDBUtilClass.GetSectionStatus(l_eitFlag);
	return status;
}

//�w��T�[�r�X��EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
EPG_SECTION_STATUS CEpgDataCap3Main::GetSectionStatusService(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL l_eitFlag
	)
{
	CBlockLock lock(&this->utilLock);
	return this->epgDBUtilClass.GetSectionStatusService(originalNetworkID, transportStreamID, serviceID, l_eitFlag);
}

//PC���v�����Ƃ����X�g���[�����ԂƂ̍����擾����
//�߂�l�F
// ���̕b��
int CEpgDataCap3Main::GetTimeDelay(
	)
{
	CBlockLock lock(&this->utilLock);
	__int64 time;
	DWORD tick;
	if( this->decodeUtilClass.GetNowTime(&time, &tick) == FALSE ){
		return 0;
	}
	__int64 delay = time + (GetTickCount() - tick) * (I64_1SEC / 1000) - GetNowI64Time();
	return (int)(delay / I64_1SEC);
}
