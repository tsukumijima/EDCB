// EpgDataCap3.cpp : DLL �A�v���P�[�V�����p�ɃG�N�X�|�[�g�����֐����`���܂��B
//

#include "stdafx.h"

#include "EpgDataCap3Main.h"
#include "../../Common/ErrDef.h"
#include "../../Common/InstanceManager.h"

#define DLL_EXPORT extern "C" __declspec(dllexport)

CInstanceManager<CEpgDataCap3Main> g_instMng;

//DLL�̏�����
//�߂�l�F
// �G���[�R�[�h
//�����F
// asyncFlag		[IN]�\��i�K��FALSE��n�����Ɓj
// id				[OUT]����ID
DLL_EXPORT
DWORD WINAPI InitializeEP(
	BOOL asyncFlag,
	DWORD* id
	)
{
	if (id == NULL) {
		return ERR_INVALID_ARG;
	}

	DWORD err = ERR_FALSE;
	*id = g_instMng.INVALID_ID;

	try {
		std::shared_ptr<CEpgDataCap3Main> ptr = std::make_shared<CEpgDataCap3Main>();
		*id = g_instMng.push(ptr);
		err = NO_ERR;
	} catch (std::bad_alloc &) {
		err = ERR_FALSE;
	}

	_OutputDebugString(L"EgpDataCap3 [InitializeEP : id=%d]\n", *id);

	return err;
}

//DLL�̊J��
//�߂�l�F
// �G���[�R�[�h
//�����F
// id		[IN]����ID InitializeEP�̖߂�l
DLL_EXPORT
DWORD WINAPI UnInitializeEP(
	DWORD id
	)
{
	_OutputDebugString(L"EgpDataCap3 [UnInitializeEP : id=%d]\n", id);

	DWORD err = ERR_NOT_INIT;
	{
		std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.pop(id);
		if (ptr != NULL) {
			err = NO_ERR;
		}
	}

	return err;
}

//��͑Ώۂ�TS�p�P�b�g�P��ǂݍ��܂���
//�߂�l�F
// �G���[�R�[�h
//�����F
// id		[IN]����ID InitializeEP�̖߂�l
// data		[IN]TS�p�P�b�g�P��
// size		[IN]data�̃T�C�Y�i188�łȂ���΂Ȃ�Ȃ��j
DLL_EXPORT
DWORD WINAPI AddTSPacketEP(
	DWORD id,
	BYTE* data,
	DWORD size
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return ERR_NOT_INIT;
	}
	if (data == NULL || size != 188) {
		return ERR_INVALID_ARG;
	}

	ptr->AddTSPacket(data);
	return NO_ERR;
}

//��̓f�[�^�̌��݂̃X�g���[���h�c���擾����
//�߂�l�F
// �G���[�R�[�h
//�����F
// id						[IN]����ID
// originalNetworkID		[OUT]���݂�originalNetworkID
// transportStreamID		[OUT]���݂�transportStreamID
DLL_EXPORT
DWORD WINAPI GetTSIDEP(
	DWORD id,
	WORD* originalNetworkID,
	WORD* transportStreamID
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return ERR_NOT_INIT;
	}
	if (originalNetworkID == NULL || transportStreamID == NULL) {
		return ERR_INVALID_ARG;
	}

	return ptr->GetTSID(originalNetworkID, transportStreamID);
}

//���X�g���[���̃T�[�r�X�ꗗ���擾����
//�߂�l�F
// �G���[�R�[�h
//�����F
// id						[IN]����ID
// serviceListSize			[OUT]serviceList�̌�
// serviceList				[OUT]�T�[�r�X���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
DLL_EXPORT
DWORD WINAPI GetServiceListActualEP(
	DWORD id,
	DWORD* serviceListSize,
	SERVICE_INFO** serviceList
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return ERR_NOT_INIT;
	}
	if (serviceListSize == NULL || serviceList == NULL) {
		return ERR_INVALID_ARG;
	}

	return ptr->GetServiceListActual(serviceListSize, serviceList);
}

//�~�ς��ꂽEPG���̂���T�[�r�X�ꗗ���擾����
//SERVICE_EXT_INFO�̏��͂Ȃ��ꍇ������
//�߂�l�F
// �G���[�R�[�h
//�����F
// id						[IN]����ID
// serviceListSize			[OUT]serviceList�̌�
// serviceList				[OUT]�T�[�r�X���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
DLL_EXPORT
DWORD WINAPI GetServiceListEpgDBEP(
	DWORD id,
	DWORD* serviceListSize,
	SERVICE_INFO** serviceList
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return ERR_NOT_INIT;
	}
	if (serviceListSize == NULL || serviceList == NULL) {
		return ERR_INVALID_ARG;
	}

	ptr->GetServiceListEpgDB(serviceListSize, serviceList);
	return NO_ERR;
}

//�w��T�[�r�X�̑SEPG�����擾����
//�߂�l�F
// �G���[�R�[�h
//�����F
// id						[IN]����ID
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// epgInfoListSize			[OUT]epgInfoList�̌�
// epgInfoList				[OUT]EPG���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
DLL_EXPORT
DWORD WINAPI GetEpgInfoListEP(
	DWORD id,
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	DWORD* epgInfoListSize,
	EPG_EVENT_INFO** epgInfoList
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return ERR_NOT_INIT;
	}
	if (epgInfoListSize == NULL || epgInfoList == NULL) {
		return ERR_INVALID_ARG;
	}

	if (ptr->GetEpgInfoList(originalNetworkID, transportStreamID, serviceID, epgInfoListSize, epgInfoList) == FALSE) {
		return ERR_NOT_FIND;
	}
	return NO_ERR;
}

//�w��T�[�r�X�̑SEPG����񋓂���
//�d�l��GetEpgInfoListEP()���p���A�߂�l��NO_ERR�̂Ƃ��R�[���o�b�N����������
//����R�[���o�b�N��epgInfoListSize�ɑSEPG���̌��AepgInfoList��NULL������
//���񂩂��epgInfoListSize�ɗ񋓂��Ƃ�EPG���̌�������
//FALSE��Ԃ��Ɨ񋓂𒆎~�ł���
//�����F
// enumEpgInfoListEPProc	[IN]EPG���̃��X�g���擾����R�[���o�b�N�֐�
// param					[IN]�R�[���o�b�N����
DLL_EXPORT
DWORD WINAPI EnumEpgInfoListEP(
	DWORD id,
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL (CALLBACK *enumEpgInfoListEPProc)(DWORD epgInfoListSize, EPG_EVENT_INFO* epgInfoList, LPVOID param),
	LPVOID param
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return ERR_NOT_INIT;
	}
	if (enumEpgInfoListEPProc == NULL) {
		return ERR_INVALID_ARG;
	}

	if (ptr->EnumEpgInfoList(originalNetworkID, transportStreamID, serviceID, enumEpgInfoListEPProc, param) == FALSE) {
		return ERR_NOT_FIND;
	}
	return NO_ERR;
}

//�w��T�[�r�X�̌���or����EPG�����擾����
//�߂�l�F
// �G���[�R�[�h
//�����F
// id						[IN]����ID
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// nextFlag					[IN]TRUE�i���̔ԑg�j�AFALSE�i���݂̔ԑg�j
// epgInfo					[OUT]EPG���iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
DLL_EXPORT
DWORD WINAPI GetEpgInfoEP(
	DWORD id,
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL nextFlag,
	EPG_EVENT_INFO** epgInfo
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return ERR_NOT_INIT;
	}
	if (epgInfo == NULL) {
		return ERR_INVALID_ARG;
	}

	if (ptr->GetEpgInfo(originalNetworkID, transportStreamID, serviceID, nextFlag, epgInfo) == FALSE) {
		return ERR_NOT_FIND;
	}
	return NO_ERR;
}

//�w��C�x���g��EPG�����擾����
//�߂�l�F
// �G���[�R�[�h
//�����F
// id						[IN]����ID
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// eventID					[IN]�擾�Ώۂ�EventID
// pfOnlyFlag				[IN]p/f����̂݌������邩�ǂ���
// epgInfo					[OUT]EPG���iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
DLL_EXPORT
DWORD WINAPI SearchEpgInfoEP(
	DWORD id,
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	WORD eventID,
	BYTE pfOnlyFlag,
	EPG_EVENT_INFO** epgInfo
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return ERR_NOT_INIT;
	}
	if (epgInfo == NULL) {
		return ERR_INVALID_ARG;
	}

	if (ptr->SearchEpgInfo(originalNetworkID, transportStreamID, serviceID, eventID, pfOnlyFlag, epgInfo) == FALSE) {
		return ERR_NOT_FIND;
	}
	return NO_ERR;
}

//EPG�f�[�^�̒~�Ϗ�Ԃ����Z�b�g����
//�����F
// id						[IN]����ID
DLL_EXPORT
void WINAPI ClearSectionStatusEP(
	DWORD id
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return;
	}

	ptr->ClearSectionStatus();
}

//EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
//�߂�l�F
// �X�e�[�^�X
//�����F
// id						[IN]����ID
// l_eitFlag				[IN]L-EIT�̃X�e�[�^�X���擾
DLL_EXPORT
EPG_SECTION_STATUS WINAPI GetSectionStatusEP(
	DWORD id,
	BOOL l_eitFlag
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return EpgNoData;
	}

	return ptr->GetSectionStatus(l_eitFlag);
}

//�w��T�[�r�X��EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
//�߂�l�F
// �X�e�[�^�X
//�����F
// id						[IN]����ID
// originalNetworkID		[IN]�擾�Ώۂ�OriginalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�TransportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// l_eitFlag				[IN]L-EIT�̃X�e�[�^�X���擾
DLL_EXPORT
EPG_SECTION_STATUS WINAPI GetSectionStatusServiceEP(
	DWORD id,
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL l_eitFlag
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return EpgNoData;
	}

	return ptr->GetSectionStatusService(originalNetworkID, transportStreamID, serviceID, l_eitFlag);
}

//PC���v�����Ƃ����X�g���[�����ԂƂ̍����擾����
//�߂�l�F
// ���̕b��
//�����F
// id						[IN]����ID
DLL_EXPORT
int WINAPI GetTimeDelayEP(
	DWORD id
	)
{
	std::shared_ptr<CEpgDataCap3Main> ptr = g_instMng.find(id);
	if (ptr == NULL) {
		return 0;
	}

	return ptr->GetTimeDelay();
}