﻿#include "stdafx.h"
#include "CreatePATPacket.h"

CCreatePATPacket::CCreatePATPacket(void)
{
	this->version = 0;
	this->counter = 0;
}

//作成PATのパラメータを設定
//引数：
// TSID				[IN]TransportStreamID
// PIDList			[IN]PMTのPIDとServiceIDのリスト
void CCreatePATPacket::SetParam(
	WORD TSID_,
	const vector<pair<WORD, WORD>>& PIDList_
)
{
	//変更なければ変える必要なし
	if( this->packet.empty() == false && this->TSID == TSID_ && this->PIDList == PIDList_ ){
		return;
	}
	this->TSID = TSID_;
	this->PIDList = PIDList_;

	this->version++;
	if( this->version > 31 ){
		this->version = 0;
	}

	CreatePAT();
}

//作成PATのバッファポインタを取得
//戻り値：作成PATのバッファポインタ
BOOL CCreatePATPacket::GetPacket(
	BYTE** pbBuff,				//[OUT] 作成したPATパケットへのポインタ（次回呼び出し時まで有効）
	DWORD* pdwSize,				//[OUT] pbBuffのサイズ
	BOOL incrementFlag			//[IN] TSパケットのCounterをインクリメントするかどうか（TRUE:する、FALSE：しない）
)
{
	if( incrementFlag == TRUE ){
		IncrementCounter();
	}
	if( this->packet.empty() == false ){
		*pbBuff = &this->packet[0];
		*pdwSize = (DWORD)this->packet.size();
	}else{
		return FALSE;
	}
	return TRUE;
}

//作成PATのバッファをクリア
void CCreatePATPacket::Clear()
{
	this->packet.clear();
	this->PSI.clear();
}

void CCreatePATPacket::CreatePAT()
{
	//まずPSI作成
	//pointer_field + last_section_numberまで+PID+CRCのサイズ
	this->PSI.resize(1 + 8 + this->PIDList.size() * 4 + 4);

	this->PSI[0] = 0;
	this->PSI[1] = 0;
	this->PSI[2] = (this->PSI.size()-4)>>8&0x0F;
	this->PSI[2] |= 0xB0; 
	this->PSI[3] = (this->PSI.size()-4)&0xFF;
	this->PSI[4] = (BYTE)((this->TSID&0xFF00)>>8);
	this->PSI[5] = (BYTE)(this->TSID&0x00FF);
	this->PSI[6] = this->version<<1;
	this->PSI[6] |= 0xC1;
	this->PSI[7] = 0;
	this->PSI[8] = 0;

	DWORD dwCreateSize = 0;
	vector<pair<WORD, WORD>>::iterator itr;
	for( itr = this->PIDList.begin(); itr != this->PIDList.end(); itr++ ){
		this->PSI[9+dwCreateSize] = (BYTE)((itr->second&0xFF00)>>8);
		this->PSI[9+dwCreateSize+1] = (BYTE)(itr->second&0x00FF);
		this->PSI[9+dwCreateSize+2] = (BYTE)((itr->first&0xFF00)>>8);
		this->PSI[9+dwCreateSize+3] = (BYTE)(itr->first&0x00FF);
		dwCreateSize+=4;
	}

	DWORD ulCrc = CalcCrc32(8+dwCreateSize,&this->PSI[1]);
	this->PSI[this->PSI.size()-4] = (BYTE)((ulCrc&0xFF000000)>>24);
	this->PSI[this->PSI.size()-3] = (BYTE)((ulCrc&0x00FF0000)>>16);
	this->PSI[this->PSI.size()-2] = (BYTE)((ulCrc&0x0000FF00)>>8);
	this->PSI[this->PSI.size()-1] = (BYTE)(ulCrc&0x000000FF);

	CreatePacket();
}

void CCreatePATPacket::CreatePacket()
{
	this->packet.clear();

	//TSパケットを作成
	for( size_t i = 0 ; i<this->PSI.size(); i+=184 ){
		this->packet.push_back(0x47);
		this->packet.push_back(i==0 ? 0x60 : 0x00);
		this->packet.push_back(0x00);
		this->packet.push_back(0x10);
		this->packet.insert(this->packet.end(), this->PSI.begin() + i, this->PSI.begin() + min(i + 184, this->PSI.size()));
		this->packet.resize(((this->packet.size() - 1) / 188 + 1) * 188, 0xFF);
	}
}

void CCreatePATPacket::IncrementCounter()
{
	for( size_t i = 0 ; i+3<this->packet.size(); i+=188 ){
		this->packet[i+3] = (BYTE)(this->counter | 0x10);
		this->counter++;
		if( this->counter >= 16 ){
			this->counter = 0;
		}
	}
}
