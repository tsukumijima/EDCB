#include "stdafx.h"
#include "WriteTSFile.h"

#include "../Common/PathUtil.h"
#include <objbase.h>

CWriteTSFile::CWriteTSFile(void)
{
	this->overWriteFlag = FALSE;
	this->createSize = 0;
	this->subRecFlag = FALSE;
	this->writeTotalSize = 0;
	this->maxBuffCount = -1;
}

CWriteTSFile::~CWriteTSFile(void)
{
	EndSave();
}

//�t�@�C���ۑ����J�n����
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// fileName				[IN]�ۑ��t�@�C����
// overWriteFlag		[IN]����t�@�C�������ݎ��ɏ㏑�����邩�ǂ����iTRUE�F����AFALSE�F���Ȃ��j
// createSize			[IN]�t�@�C���쐬���Ƀf�B�X�N�ɗ\�񂷂�e��
// saveFolder			[IN]�g�p����t�H���_�ꗗ
// saveFolderSub		[IN]HDD�̋󂫂��Ȃ��Ȃ����ꍇ�Ɉꎞ�I�Ɏg�p����t�H���_
BOOL CWriteTSFile::StartSave(
	const wstring& fileName,
	BOOL overWriteFlag_,
	ULONGLONG createSize_,
	const vector<REC_FILE_SET_INFO>& saveFolder,
	const vector<wstring>& saveFolderSub_,
	int maxBuffCount_
)
{
	if( saveFolder.size() == 0 ){
		OutputDebugString(L"CWriteTSFile::StartSave Err saveFolder 0\r\n");
		return FALSE;
	}
	
	if( this->outThread.joinable() == false ){
		this->fileList.clear();
		this->mainSaveFilePath = L"";
		this->overWriteFlag = overWriteFlag_;
		this->createSize = createSize_;
		this->maxBuffCount = maxBuffCount_;
		this->writeTotalSize = 0;
		this->subRecFlag = FALSE;
		this->saveFolderSub = saveFolderSub_;
		for( size_t i=0; i<saveFolder.size(); i++ ){
			this->fileList.push_back(std::unique_ptr<SAVE_INFO>(new SAVE_INFO));
			SAVE_INFO& item = *this->fileList.back();
			item.freeChk = FALSE;
			item.writePlugIn = saveFolder[i].writePlugIn;
			if( item.writePlugIn.size() == 0 ){
				item.writePlugIn = L"Write_Default.dll";
			}
			item.recFolder = saveFolder[i].recFolder;
			item.recFileName = saveFolder[i].recFileName;
			if( item.recFileName.size() == 0 ){
				item.recFileName = fileName;
			}
		}

		//��M�X���b�h�N��
		this->outStopState = 2;
		this->outStopEvent.Reset();
		this->outThread = thread_(OutThread, this);
		//�ۑ��J�n�܂ő҂�
		this->outStopEvent.WaitOne();
		//��~���(1)�łȂ���ΊJ�n���(0)�Ɉڂ�
		if( this->outStopState != 1 ){
			this->outStopState = 0;
			return TRUE;
		}
		this->outThread.join();
	}

	OutputDebugString(L"CWriteTSFile::StartSave Err 1\r\n");
	return FALSE;
}

BOOL CWriteTSFile::EndSave(BOOL* subRecFlag_)
{
	if( this->outThread.joinable() ){
		this->outStopState = 1;
		this->outStopEvent.Set();
		this->outThread.join();
		if( subRecFlag_ ){
			*subRecFlag_ = this->subRecFlag;
		}
		this->tsBuffList.clear();
		this->tsFreeList.clear();
		return TRUE;
	}
	return FALSE;
}

//�o�͗pTS�f�[�^�𑗂�
//�߂�l�F
// TRUE�i�����j�AFALSE�i���s�j
//�����F
// data		[IN]TS�f�[�^
// size		[IN]data�̃T�C�Y
BOOL CWriteTSFile::AddTSBuff(
	BYTE* data,
	DWORD size
	)
{
	if( data == NULL || size == 0 || this->outThread.joinable() == false ){
		return FALSE;
	}

	BOOL ret = TRUE;

	{
		CBlockLock lock(&this->outThreadLock);
		while( size != 0 ){
			if( this->tsFreeList.empty() ){
				//�o�b�t�@�𑝂₷
				if( this->maxBuffCount > 0 && this->tsBuffList.size() > (size_t)this->maxBuffCount ){
					_OutputDebugString(L"��writeBuffList MaxOver");
					for( auto itr = this->tsBuffList.begin(); itr != this->tsBuffList.end(); (itr++)->clear() );
					this->tsFreeList.splice(this->tsFreeList.end(), this->tsBuffList);
				}else{
					this->tsFreeList.push_back(vector<BYTE>());
					this->tsFreeList.back().reserve(48128);
				}
			}
			DWORD insertSize = min(48128 - (DWORD)this->tsFreeList.front().size(), size);
			this->tsFreeList.front().insert(this->tsFreeList.front().end(), data, data + insertSize);
			if( this->tsFreeList.front().size() == 48128 ){
				this->tsBuffList.splice(this->tsBuffList.end(), this->tsFreeList, this->tsFreeList.begin());
			}
			data += insertSize;
			size -= insertSize;
		}
	}
	return ret;
}

void CWriteTSFile::OutThread(CWriteTSFile* sys)
{
	//�v���O�C����COM�𗘗p���邩������Ȃ�����
	CoInitializeEx(NULL, COINIT_APARTMENTTHREADED | COINIT_DISABLE_OLE1DDE);

	BOOL emptyFlag = TRUE;
	for( size_t i=0; i<sys->fileList.size(); i++ ){
		if( sys->fileList[i]->writeUtil.Initialize(GetModulePath().replace_filename(L"Write").append(sys->fileList[i]->writePlugIn).c_str()) == FALSE ){
			OutputDebugString(L"CWriteTSFile::StartSave Err 3\r\n");
			sys->fileList[i].reset();
		}else{
			fs_path recFolder = sys->fileList[i]->recFolder;
			//�󂫗e�ʂ����炩���߃`�F�b�N
			__int64 freeBytes = UtilGetStorageFreeBytes(recFolder);
			bool isMainUnknownOrFree = (freeBytes < 0 || freeBytes > (__int64)sys->createSize + FREE_FOLDER_MIN_BYTES);
			if( isMainUnknownOrFree == false ){
				//�󂫂̂���T�u�t�H���_��T���Ă݂�
				vector<wstring>::iterator itrFree = std::find_if(sys->saveFolderSub.begin(), sys->saveFolderSub.end(),
					[&](const wstring& a) { return UtilComparePath(a.c_str(), recFolder.c_str()) &&
					                               UtilGetStorageFreeBytes(a) > (__int64)sys->createSize + FREE_FOLDER_MIN_BYTES; });
				if( itrFree != sys->saveFolderSub.end() ){
					sys->subRecFlag = TRUE;
					recFolder = *itrFree;
				}
			}
			//�J�n
			BOOL startRes = sys->fileList[i]->writeUtil.Start(fs_path(recFolder).append(sys->fileList[i]->recFileName).c_str(),
			                                                  sys->overWriteFlag, sys->createSize);
			if( startRes == FALSE ){
				OutputDebugString(L"CWriteTSFile::StartSave Err 2\r\n");
				//�G���[���T�u�t�H���_�Ń��g���C
				if( isMainUnknownOrFree ){
					vector<wstring>::iterator itrFree = std::find_if(sys->saveFolderSub.begin(), sys->saveFolderSub.end(),
						[&](const wstring& a) { return UtilComparePath(a.c_str(), recFolder.c_str()) &&
						                               UtilGetStorageFreeBytes(a) > (__int64)sys->createSize + FREE_FOLDER_MIN_BYTES; });
					if( itrFree != sys->saveFolderSub.end() ){
						sys->subRecFlag = TRUE;
						startRes = sys->fileList[i]->writeUtil.Start(fs_path(*itrFree).append(sys->fileList[i]->recFileName).c_str(),
						                                             sys->overWriteFlag, sys->createSize);
					}
				}
			}
			if( startRes == FALSE ){
				sys->fileList[i].reset();
			}else{
				if( i == 0 ){
					DWORD saveFilePathSize = 0;
					if( sys->fileList[i]->writeUtil.GetSavePath(NULL, &saveFilePathSize) && saveFilePathSize > 0 ){
						vector<WCHAR> saveFilePath(saveFilePathSize);
						if( sys->fileList[i]->writeUtil.GetSavePath(saveFilePath.data(), &saveFilePathSize) ){
							sys->mainSaveFilePath = saveFilePath.data();
						}
					}
				}
				sys->fileList[i]->freeChk = emptyFlag;
				emptyFlag = FALSE;
			}
		}
	}
	if( emptyFlag ){
		OutputDebugString(L"CWriteTSFile::StartSave Err fileList 0\r\n");
		CoUninitialize();
		sys->outStopState = 1;
		sys->outStopEvent.Set();
		return;
	}
	sys->outStopEvent.Set();
	//���ԏ��(2)�łȂ��Ȃ�܂ő҂�
	for( ; sys->outStopState == 2; Sleep(100) );
	std::list<vector<BYTE>> data;

	while( sys->outStopState == 0 ){
		//�o�b�t�@����f�[�^���o��
		{
			CBlockLock lock(&sys->outThreadLock);
			if( data.empty() == false ){
				//�ԋp
				data.front().clear();
				sys->tsFreeList.splice(sys->tsFreeList.end(), data);
			}
			if( sys->tsBuffList.empty() == false ){
				data.splice(data.end(), sys->tsBuffList, sys->tsBuffList.begin());
			}
		}

		if( data.empty() == false ){
			DWORD dataSize = (DWORD)data.front().size();
			for( size_t i=0; i<sys->fileList.size(); i++ ){
				{
					if( sys->fileList[i] ){
						DWORD write = 0;
						if( sys->fileList[i]->writeUtil.Write(data.front().data(), dataSize, &write) == FALSE ){
							//�󂫂��Ȃ��Ȃ���
							if( i == 0 ){
								CBlockLock lock(&sys->outThreadLock);
								if( sys->writeTotalSize >= 0 ){
									//�o�̓T�C�Y�̉��Z���~����
									sys->writeTotalSize = -(sys->writeTotalSize + 1);
								}
							}
							sys->fileList[i]->writeUtil.Stop();

							if( sys->fileList[i]->freeChk == TRUE ){
								//���̋󂫂�T��
								vector<wstring>::iterator itrFree = std::find_if(sys->saveFolderSub.begin(), sys->saveFolderSub.end(),
									[](const wstring& a) { return UtilGetStorageFreeBytes(a) > FREE_FOLDER_MIN_BYTES; });
								if( itrFree != sys->saveFolderSub.end() ){
									//�J�n
									if( sys->fileList[i]->writeUtil.Start(fs_path(*itrFree).append(sys->fileList[i]->recFileName).c_str(),
									                                      sys->overWriteFlag, 0) == FALSE ){
										//���s�����̂ŏI���
										sys->fileList[i].reset();
									}else{
										sys->subRecFlag = TRUE;

										if( dataSize > write ){
											sys->fileList[i]->writeUtil.Write(data.front().data()+write, dataSize-write, &write);
										}
									}
								}
							}else{
								//���s�����̂ŏI���
								sys->fileList[i].reset();
							}
						}else{
							//����ł͐��ۂɂ�����炸writeTotalSize��dataSize�����Z���Ă��邪
							//�o�̓T�C�Y�̗��p�P�[�X�I�ɂ�mainSaveFilePath�ƈ�v�����Ȃ��Ƃ��������Ǝv���̂ŁA���̂悤�ɕύX����
							if( i == 0 ){
								CBlockLock lock(&sys->outThreadLock);
								if( sys->writeTotalSize >= 0 ){
									sys->writeTotalSize += dataSize;
								}
							}
						}
					}
				}
			}
		}else{
			//TODO: �����ɂ̓��b�Z�[�W���f�B�X�p�b�`���ׂ�(�X���b�h���ŒP����COM�I�u�W�F�N�g�����������(����)���Ȃ�)
			sys->outStopEvent.WaitOne(100);
		}
	}

	//�c���Ă���o�b�t�@�������o��
	{
		CBlockLock lock(&sys->outThreadLock);
		if( sys->tsFreeList.empty() == false && sys->tsFreeList.front().empty() == false ){
			sys->tsBuffList.splice(sys->tsBuffList.end(), sys->tsFreeList, sys->tsFreeList.begin());
		}
		while( sys->tsBuffList.empty() == false ){
			for( size_t i=0; i<sys->fileList.size(); i++ ){
				if( sys->fileList[i] ){
					DWORD write = 0;
					sys->fileList[i]->writeUtil.Write(sys->tsBuffList.front().data(), (DWORD)sys->tsBuffList.front().size(), &write);
				}
			}
			sys->tsBuffList.pop_front();
		}
	}
	for( size_t i=0; i<sys->fileList.size(); i++ ){
		if( sys->fileList[i] ){
			sys->fileList[i]->writeUtil.Stop();
			sys->fileList[i].reset();
		}
	}

	CoUninitialize();
}

wstring CWriteTSFile::GetSaveFilePath()
{
	return this->mainSaveFilePath;
}

//�^�撆�̃t�@�C���̏o�̓T�C�Y���擾����
//�����F
// writeSize			[OUT]�ۑ��t�@�C����
void CWriteTSFile::GetRecWriteSize(
	__int64* writeSize
	)
{
	if( writeSize != NULL ){
		CBlockLock lock(&this->outThreadLock);
		*writeSize = this->writeTotalSize < 0 ? -(this->writeTotalSize + 1) : this->writeTotalSize;
	}
}
