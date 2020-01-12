#include "stdafx.h"
#include "WriteMain.h"
#include "../../Common/PathUtil.h"
#include "../../Common/TSPacketUtil.h"

extern HINSTANCE g_instance;

CWriteMain::CWriteMain()
	: file(NULL, fclose)
{
	{
		fs_path iniPath = GetModuleIniPath(g_instance);
		wstring name = GetPrivateProfileToString(L"SET", L"WritePlugin", L"", iniPath.c_str());
		if( name.empty() == false && name[0] != L';' ){
			//�o�̓v���O�C���𐔎�q��
			this->writePlugin.reset(new CWritePlugInUtil);
			if( this->writePlugin->Initialize(GetModulePath(g_instance).replace_filename(name).c_str()) == FALSE ){
				this->writePlugin.reset();
			}
		}
	}
}

CWriteMain::~CWriteMain()
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
	this->targetSID = 0;
	fs_path name = fs_path(this->savePath).filename();
	if( name.c_str()[0] == L'#' ){
		//�t�@�C������"#16�i��4��#"�Ŏn�܂�Ȃ炱����T�[�r�XID�Ɖ��߂��Ď�菜��
		WCHAR* endp;
		WORD sid = (WORD)wcstol(name.c_str() + 1, &endp, 16);
		if( endp - name.c_str() == 5 && *endp == L'#' ){
			this->targetSID = sid == 0xFFFF ? 0 : sid;
			this->savePath = fs_path(this->savePath).replace_filename(name.c_str() + 6).native();
		}
	}

	if( this->writePlugin ){
		if( this->writePlugin->Start(this->savePath.c_str(), overWriteFlag, createSize) == FALSE ){
			this->savePath = L"";
			return FALSE;
		}
		vector<WCHAR> path;
		DWORD pathSize = 0;
		if( this->writePlugin->GetSavePath(NULL, &pathSize) && pathSize > 0 ){
			path.resize(pathSize);
			if( this->writePlugin->GetSavePath(&path.front(), &pathSize) == FALSE ){
				path.clear();
			}
		}
		if( path.empty() ){
			this->writePlugin->Stop();
			this->savePath = L"";
			return FALSE;
		}
		this->savePath = &path.front();
	}else{
		_OutputDebugString(L"��CWriteMain::Start CreateFile:%ls\r\n", this->savePath.c_str());
		UtilCreateDirectories(fs_path(this->savePath).parent_path());
		this->file.reset(UtilOpenFile(this->savePath, (overWriteFlag ? UTIL_O_CREAT_WRONLY : UTIL_O_EXCL_CREAT_WRONLY) | UTIL_SH_READ | UTIL_F_IONBF));
		if( !this->file ){
			fs_path pathWoExt = this->savePath;
			fs_path ext = pathWoExt.extension();
			pathWoExt.replace_extension();
			for( int i = 1; ; i++ ){
				Format(this->savePath, L"%ls-(%d)%ls", pathWoExt.c_str(), i, ext.c_str());
				this->file.reset(UtilOpenFile(this->savePath, (overWriteFlag ? UTIL_O_CREAT_WRONLY : UTIL_O_EXCL_CREAT_WRONLY) | UTIL_SH_READ | UTIL_F_IONBF));
				if( this->file || i >= 999 ){
					_OutputDebugString(L"��CWriteMain::Start CreateFile:%ls\r\n", this->savePath.c_str());
					if( this->file ){
						break;
					}
					OutputDebugString(L"��CWriteMain::Start Err\r\n");
					this->savePath = L"";
					return FALSE;
				}
			}
		}
	}

	this->lastTSID = 0;
	this->packetInit.ClearBuff();
	this->serviceFilter.SetServiceID(this->targetSID == 0, vector<WORD>(1, this->targetSID));

	return TRUE;
}

BOOL CWriteMain::Stop(
	)
{
	this->file.reset();
	if( this->writePlugin ){
		this->writePlugin->Stop();
	}
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
	if( (this->writePlugin || this->file) && data != NULL && size > 0 && writeSize != NULL ){
		*writeSize = 0;
		if( this->targetSID == 0 ){
			//�S�T�[�r�X�Ȃ̂ŉ����M��Ȃ�
			if( this->writePlugin ){
				if( this->writePlugin->Write(data, size, writeSize) == FALSE ){
					return FALSE;
				}
			}else{
				*writeSize = (DWORD)fwrite(data, 1, size, this->file.get());
				if( *writeSize == 0 ){
					OutputDebugString(L"��CWriteMain::Write Err\r\n");
					this->file.reset();
					return FALSE;
				}
			}
			return TRUE;
		}else{
			//�w��T�[�r�X
			BYTE* outData;
			DWORD outSize;
			if( this->packetInit.GetTSData(data, size, &outData, &outSize) == FALSE ){
				outSize = 0;
			}
			this->outBuff.clear();
			for( DWORD i = 0; i < outSize; i += 188 ){
				CTSPacketUtil packet;
				if( packet.Set188TS(outData + i, 188) ){
					if( packet.PID == 0 && packet.payload_unit_start_indicator && 5 < packet.data_byteSize ){
						//TSID���擾
						WORD tsid = packet.data_byte[4] << 8 | packet.data_byte[5];
						if( this->lastTSID != tsid ){
							this->lastTSID = tsid;
							this->serviceFilter.Clear(tsid);
						}
					}
					if( this->lastTSID != 0 ){
						this->serviceFilter.FilterPacket(this->outBuff, outData + i, packet);
					}
				}
			}
			if( this->outBuff.empty() ){
				*writeSize = size;
				return TRUE;
			}
			DWORD write;
			if( this->writePlugin ){
				if( this->writePlugin->Write(&this->outBuff.front(), (DWORD)this->outBuff.size(), &write) == FALSE ){
					return FALSE;
				}
			}else{
				if( fwrite(&this->outBuff.front(), 1, this->outBuff.size(), this->file.get()) == 0 ){
					OutputDebugString(L"��CWriteMain::Write Err\r\n");
					this->file.reset();
					return FALSE;
				}
			}
			*writeSize = size;
			return TRUE;
		}
	}
	return FALSE;
}
