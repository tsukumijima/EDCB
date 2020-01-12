#pragma once

#include "EpgDataCap3Def.h"

class CEpgDataCap3Util
{
public:
	CEpgDataCap3Util(void);
	~CEpgDataCap3Util(void);

	//DLL�̏�����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// asyncMode		[IN]TRUE:�񓯊����[�h�AFALSE:�������[�h
	// loadDllFilePath	[IN]���[�h����DLL�p�X
	DWORD Initialize(
		BOOL asyncFlag,
		LPCWSTR loadDllFilePath = NULL
		);

	//DLL�̊J��
	//�߂�l�F
	// �G���[�R�[�h
	DWORD UnInitialize(
		);

	//��͑Ώۂ�TS�p�P�b�g�P��ǂݍ��܂���
	//�߂�l�F
	// �G���[�R�[�h
	// data		[IN]TS�p�P�b�g�P��
	// size		[IN]data�̃T�C�Y�i188�A192������ɂȂ�͂��j
	DWORD AddTSPacket(
		BYTE* data,
		DWORD size
		);

	//��̓f�[�^�̌��݂̃X�g���[���h�c���擾����
	//�߂�l�F
	// �G���[�R�[�h
	// originalNetworkID		[OUT]���݂�originalNetworkID
	// transportStreamID		[OUT]���݂�transportStreamID
	DWORD GetTSID(
		WORD* originalNetworkID,
		WORD* transportStreamID
		);

	//���X�g���[���̃T�[�r�X�ꗗ���擾����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// serviceListSize			[OUT]serviceList�̌�
	// serviceList				[OUT]�T�[�r�X���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
	DWORD GetServiceListActual(
		DWORD* serviceListSize,
		SERVICE_INFO** serviceList
		);

	//�~�ς��ꂽEPG���̂���T�[�r�X�ꗗ���擾����
	//SERVICE_EXT_INFO�̏��͂Ȃ�
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// serviceListSize			[OUT]serviceList�̌�
	// serviceList				[OUT]�T�[�r�X���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
	DWORD GetServiceListEpgDB(
		DWORD* serviceListSize,
		SERVICE_INFO** serviceList
		);

	//�w��T�[�r�X�̑SEPG�����擾����
	//�߂�l�F
	// �G���[�R�[�h
	// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
	// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
	// serviceID				[IN]�擾�Ώۂ�ServiceID
	// epgInfoListSize			[OUT]epgInfoList�̌�
	// epgInfoList				[OUT]EPG���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
	DWORD GetEpgInfoList(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		DWORD* epgInfoListSize,
		EPG_EVENT_INFO** epgInfoList
		);

	//�w��T�[�r�X�̑SEPG����񋓂���
	//�����F
	// enumEpgInfoListProc		[IN]EPG���̃��X�g���擾����R�[���o�b�N�֐�
	// param					[IN]�R�[���o�b�N����
	DWORD EnumEpgInfoList(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		BOOL (CALLBACK *enumEpgInfoListProc)(DWORD epgInfoListSize, EPG_EVENT_INFO* epgInfoList, LPVOID param),
		LPVOID param
		);

	//EPG�f�[�^�̒~�Ϗ�Ԃ����Z�b�g����
	void ClearSectionStatus();

	//EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
	//�߂�l�F
	// �X�e�[�^�X
	//�����F
	// l_eitFlag		[IN]L-EIT�̃X�e�[�^�X���擾
	EPG_SECTION_STATUS GetSectionStatus(
		BOOL l_eitFlag
		);

	//�w��T�[�r�X��EPG�f�[�^�̒~�Ϗ�Ԃ��擾����
	//�߂�l�F
	// �X�e�[�^�X,�擾�ł������ǂ���
	//�����F
	// originalNetworkID		[IN]�擾�Ώۂ�OriginalNetworkID
	// transportStreamID		[IN]�擾�Ώۂ�TransportStreamID
	// serviceID				[IN]�擾�Ώۂ�ServiceID
	// l_eitFlag				[IN]L-EIT�̃X�e�[�^�X���擾
	pair<EPG_SECTION_STATUS, BOOL> GetSectionStatusService(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		BOOL l_eitFlag
		);

	//�w��T�[�r�X�̌���or����EPG�����擾����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
	// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
	// serviceID				[IN]�擾�Ώۂ�ServiceID
	// nextFlag					[IN]TRUE�i���̔ԑg�j�AFALSE�i���݂̔ԑg�j
	// epgInfo					[OUT]EPG���iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
	DWORD GetEpgInfo(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		BOOL nextFlag,
		EPG_EVENT_INFO** epgInfo
		);

	//�w��C�x���g��EPG�����擾����
	//�߂�l�F
	// �G���[�R�[�h
	//�����F
	// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
	// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
	// serviceID				[IN]�擾�Ώۂ�ServiceID
	// EventID					[IN]�擾�Ώۂ�EventID
	// pfOnlyFlag				[IN]p/f����̂݌������邩�ǂ���
	// epgInfo					[OUT]EPG���iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
	DWORD SearchEpgInfo(
		WORD originalNetworkID,
		WORD transportStreamID,
		WORD serviceID,
		WORD eventID,
		BYTE pfOnlyFlag,
		EPG_EVENT_INFO** epgInfo
		);

	//PC���v�����Ƃ����X�g���[�����ԂƂ̍����擾����
	//�߂�l�F
	// ���̕b��
	int GetTimeDelay(
		);

private:
	typedef DWORD (WINAPI *InitializeEP3)(BOOL asyncFlag, DWORD* id);
	typedef DWORD (WINAPI *UnInitializeEP3)(DWORD id);
	typedef DWORD (WINAPI *AddTSPacketEP3)(DWORD id, BYTE* data, DWORD size);
	typedef DWORD (WINAPI *GetTSIDEP3)(DWORD id, WORD* originalNetworkID, WORD* transportStreamID);
	typedef DWORD (WINAPI *GetServiceListActualEP3)(DWORD id, DWORD* serviceListSize, SERVICE_INFO** serviceList);
	typedef DWORD (WINAPI *GetServiceListEpgDBEP3)(DWORD id, DWORD* serviceListSize, SERVICE_INFO** serviceList);
	typedef DWORD (WINAPI *GetEpgInfoListEP3)(DWORD id, WORD originalNetworkID, WORD transportStreamID, WORD serviceID, DWORD* epgInfoListSize, EPG_EVENT_INFO** epgInfoList);
	typedef DWORD (WINAPI *EnumEpgInfoListEP3)(DWORD id, WORD originalNetworkID, WORD transportStreamID, WORD serviceID,
	                                           BOOL (CALLBACK *enumEpgInfoListEP3Proc)(DWORD epgInfoListSize, EPG_EVENT_INFO* epgInfoList, LPVOID param), LPVOID param);
	typedef void (WINAPI *ClearSectionStatusEP3)(DWORD id);
	typedef EPG_SECTION_STATUS (WINAPI *GetSectionStatusEP3)(DWORD id, BOOL l_eitFlag);
	typedef EPG_SECTION_STATUS (WINAPI *GetSectionStatusServiceEP3)(DWORD id, WORD originalNetworkID, WORD transportStreamID, WORD serviceID, BOOL l_eitFlag);
	typedef DWORD (WINAPI *GetEpgInfoEP3)(DWORD id, WORD originalNetworkID, WORD transportStreamID, WORD serviceID, BOOL nextFlag, EPG_EVENT_INFO** epgInfo);
	typedef DWORD (WINAPI *SearchEpgInfoEP3)(DWORD id, WORD originalNetworkID, WORD transportStreamID, WORD serviceID, WORD eventID, BYTE pfOnlyFlag, EPG_EVENT_INFO** epgInfo);
	typedef int (WINAPI *GetTimeDelayEP3)(DWORD id);

	void* module;
	DWORD id;
	UnInitializeEP3			pfnUnInitializeEP3;
	AddTSPacketEP3			pfnAddTSPacketEP3;
	GetTSIDEP3				pfnGetTSIDEP3;
	GetEpgInfoListEP3		pfnGetEpgInfoListEP3;
	EnumEpgInfoListEP3		pfnEnumEpgInfoListEP3;
	ClearSectionStatusEP3	pfnClearSectionStatusEP3;
	GetSectionStatusEP3		pfnGetSectionStatusEP3;
	GetSectionStatusServiceEP3	pfnGetSectionStatusServiceEP3;
	GetServiceListActualEP3	pfnGetServiceListActualEP3;
	GetServiceListEpgDBEP3	pfnGetServiceListEpgDBEP3;
	GetEpgInfoEP3			pfnGetEpgInfoEP3;
	SearchEpgInfoEP3		pfnSearchEpgInfoEP3;
	GetTimeDelayEP3			pfnGetTimeDelayEP3;

	CEpgDataCap3Util(const CEpgDataCap3Util&);
	CEpgDataCap3Util& operator=(const CEpgDataCap3Util&);
};

