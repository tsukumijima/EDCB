#pragma once

class CPacketInit
{
public:
	CPacketInit(void);

	//���̓o�b�t�@��188�o�C�g�P�ʂ�TS�ɕϊ����A188�̔{���ɂȂ�悤�ɂ��낦��
	//�߂�l�F
	// TRUE�i�����j�AFALSE�i���s�j
	//�����F
	// inData			[IN]����TS�f�[�^
	// inSize			[IN]inData�̃T�C�Y�iBYTE�P�ʁj
	// outData			[OUT]188�o�C�g�ɐ��񂵂��o�b�t�@�i����Ăяo���܂ŕێ��j
	// outSize			[OUT]outData�̃T�C�Y�iBYTE�P�ʁj
	BOOL GetTSData(
		const BYTE* inData,
		DWORD inSize,
		BYTE** outData,
		DWORD* outSize
		);

	//�����o�b�t�@�̃N���A
	void ClearBuff();

protected:
	vector<BYTE> outBuff;
	vector<BYTE> nextStartBuff;

	DWORD packetSize;
};
