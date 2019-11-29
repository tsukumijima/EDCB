#include "stdafx.h"
#include "EpgDBUtil.h"

#include "../../Common/StringUtil.h"
#include "../../Common/TimeUtil.h"

//�������������f������text_char�t�B�[���h�ɒǉ����Ȃ��ꍇ�͂��̃}�N�����`����
//#define EPGDB_NO_ADD_CAPTION_TO_COMPONENT

//#define DEBUG_EIT
#ifdef DEBUG_EIT
static WCHAR g_szDebugEIT[128];
#endif

namespace Desc = AribDescriptor;

void CEpgDBUtil::Clear()
{
	this->serviceEventMap.clear();
}

void CEpgDBUtil::SetStreamChangeEvent()
{
	//������[p/f]�̃��Z�b�g�͂��Ȃ�
}

BOOL CEpgDBUtil::AddEIT(WORD PID, const Desc::CDescriptor& eit, __int64 streamTime)
{
	WORD original_network_id = (WORD)eit.GetNumber(Desc::original_network_id);
	WORD transport_stream_id = (WORD)eit.GetNumber(Desc::transport_stream_id);
	WORD service_id = (WORD)eit.GetNumber(Desc::service_id);
	ULONGLONG key = Create64Key(original_network_id, transport_stream_id, service_id);

	//�T�[�r�X��map���擾
	map<ULONGLONG, SERVICE_EVENT_INFO>::iterator itr;
	SERVICE_EVENT_INFO* serviceInfo = NULL;

	itr = serviceEventMap.find(key);
	if( itr == serviceEventMap.end() ){
		serviceInfo = &serviceEventMap.insert(std::make_pair(key, SERVICE_EVENT_INFO())).first->second;
	}else{
		serviceInfo = &itr->second;
	}

	BYTE table_id = (BYTE)eit.GetNumber(Desc::table_id);
	BYTE version_number = (BYTE)eit.GetNumber(Desc::version_number);
	BYTE section_number = (BYTE)eit.GetNumber(Desc::section_number);
	SI_TAG siTag;
	siTag.tableID = table_id;
	siTag.version = version_number;
	siTag.time = (DWORD)(streamTime / (10 * I64_1SEC));

	DWORD eventLoopSize = 0;
	Desc::CDescriptor::CLoopPointer lp;
	if( eit.EnterLoop(lp) ){
		eventLoopSize = eit.GetLoopSize(lp);
	}
	if( table_id <= 0x4F && section_number <= 1 ){
		//[p/f]
		if( siTag.time == 0 ){
			//�`�����l���ύX���̉������̂��߁A�^�C���X�^���v�s����[p/f]��������DB��̕s���łȂ�[p/f]���N���A����
			//EPG�t�@�C������͂���Ƃ��͌Â�[p/f]�ɂ��㏑������������̂ŁA���p���Ŏ��n��ɂ��邩�^�C���X�^���v���m�肳����H�v���K�v
			if( serviceInfo->nowEvent.empty() == false && serviceInfo->nowEvent.back().time != 0 ||
			    serviceInfo->nextEvent.empty() == false && serviceInfo->nextEvent.back().time != 0 ){
				serviceInfo->nowEvent.clear();
				serviceInfo->nextEvent.clear();
			}
		}
		if( eventLoopSize == 0 ){
			//��Z�N�V����
			if( section_number == 0 ){
				if( serviceInfo->nowEvent.empty() == false && siTag.time >= serviceInfo->nowEvent.back().time ){
					serviceInfo->nowEvent.clear();
				}
			}else{
				if( serviceInfo->nextEvent.empty() == false && siTag.time >= serviceInfo->nextEvent.back().time ){
					serviceInfo->nextEvent.clear();
				}
			}
		}
	}
	//�C�x���g���ƂɍX�V�K�v������
	for( DWORD i = 0; i < eventLoopSize; i++ ){
		eit.SetLoopIndex(lp, i);
		WORD event_id = (WORD)eit.GetNumber(Desc::event_id, lp);
		__int64 start_time = 0;
		DWORD mjd = eit.GetNumber(Desc::start_time_mjd, lp);
		DWORD bcd = eit.GetNumber(Desc::start_time_bcd, lp);
		if( mjd != 0xFFFF || bcd != 0xFFFFFF ){
			start_time = MJDtoI64Time(mjd, bcd);
		}
		DWORD duration = eit.GetNumber(Desc::d_duration, lp);
		if( duration != 0xFFFFFF ){
			duration = (duration >> 20 & 15) * 36000 + (duration >> 16 & 15) * 3600 + (duration >> 12 & 15) * 600 +
			           (duration >> 8 & 15) * 60 + (duration >> 4 & 15) * 10 + (duration & 15);
		}
		map<WORD, EVENT_INFO>::iterator itrEvent;
		EVENT_INFO* eventInfo = NULL;

		if( eit.GetNumber(Desc::running_status, lp) == 1 || eit.GetNumber(Desc::running_status, lp) == 3 ){
			//����s���܂��͒�~��
			_OutputDebugString(L"������s���܂��͒�~���C�x���g ONID:0x%04x TSID:0x%04x SID:0x%04x EventID:0x%04x\r\n",
				original_network_id,  transport_stream_id, service_id, event_id
				);
			continue;
		}

#ifdef DEBUG_EIT
		swprintf_s(g_szDebugEIT, L"%lc%04x.%02x%02x.%02d.%d\r\n", table_id <= 0x4F ? L'P' : L'S',
		           event_id, table_id, section_number, version_number, siTag.time % 1000000);
#endif
		//[actual]��[other]�͓����Ɉ���
		//[p/f]��[schedule]�͊e�X���S�ɓƗ����ăf�[�^�x�[�X���쐬����
		if( table_id <= 0x4F && section_number <= 1 ){
			//[p/f]
			if( section_number == 0 ){
				if( start_time == 0 ){
					OutputDebugString(L"Invalid EIT[p/f]\r\n");
				}else if( serviceInfo->nowEvent.empty() || siTag.time >= serviceInfo->nowEvent.back().time ){
					if( serviceInfo->nowEvent.empty() || serviceInfo->nowEvent.back().db.event_id != event_id ){
						//�C�x���g����ւ��
						serviceInfo->nowEvent.clear();
						if( serviceInfo->nextEvent.empty() == false && serviceInfo->nextEvent.back().db.event_id == event_id ){
							serviceInfo->nowEvent.swap(serviceInfo->nextEvent);
							serviceInfo->nowEvent.back().time = 0;
						}
					}
					if( serviceInfo->nowEvent.empty() ){
						serviceInfo->nowEvent.resize(1);
						eventInfo = serviceInfo->nowEvent.data();
						eventInfo->db.event_id = event_id;
						eventInfo->time = 0;
						eventInfo->tagBasic.version = 0xFF;
						eventInfo->tagBasic.time = 0;
						eventInfo->tagExt.version = 0xFF;
						eventInfo->tagExt.time = 0;
					}else{
						eventInfo = serviceInfo->nowEvent.data();
					}
				}
			}else{
				if( serviceInfo->nextEvent.empty() || siTag.time >= serviceInfo->nextEvent.back().time ){
					if( serviceInfo->nextEvent.empty() || serviceInfo->nextEvent.back().db.event_id != event_id ){
						serviceInfo->nextEvent.clear();
						if( serviceInfo->nowEvent.empty() == false && serviceInfo->nowEvent.back().db.event_id == event_id ){
							serviceInfo->nextEvent.swap(serviceInfo->nowEvent);
							serviceInfo->nextEvent.back().time = 0;
						}
					}
					if( serviceInfo->nextEvent.empty() ){
						serviceInfo->nextEvent.resize(1);
						eventInfo = serviceInfo->nextEvent.data();
						eventInfo->db.event_id = event_id;
						eventInfo->time = 0;
						eventInfo->tagBasic.version = 0xFF;
						eventInfo->tagBasic.time = 0;
						eventInfo->tagExt.version = 0xFF;
						eventInfo->tagExt.time = 0;
					}else{
						eventInfo = serviceInfo->nextEvent.data();
					}
				}
			}
		}else if( PID != 0x0012 || table_id > 0x4F ){
			//[schedule]��������(H-EIT�łȂ��Ƃ�)[p/f after]
			//TODO: �C�x���g���łɂ͑Ή����Ă��Ȃ�(�N���X�݌v�I�ɑΉ��͌�����)�BEDCB�I�ɂ͎��p��̃f�����b�g�͂��܂薳��
			if( start_time == 0 || duration == 0xFFFFFF ){
				OutputDebugString(L"Invalid EIT[schedule]\r\n");
			}else{
				itrEvent = serviceInfo->eventMap.find(event_id);
				if( itrEvent == serviceInfo->eventMap.end() ){
					eventInfo = &serviceInfo->eventMap[event_id];
					eventInfo->db.event_id = event_id;
					eventInfo->time = 0;
					eventInfo->tagBasic.version = 0xFF;
					eventInfo->tagBasic.time = 0;
					eventInfo->tagExt.version = 0xFF;
					eventInfo->tagExt.time = 0;
				}else{
					eventInfo = &itrEvent->second;
				}
			}
		}
		if( eventInfo ){
			//�J�n���ԓ��̓^�C���X�^���v�݂̂���ɍX�V
			if( siTag.time >= eventInfo->time ){
				eventInfo->db.StartTimeFlag = start_time != 0;
				SYSTEMTIME stZero = {};
				eventInfo->db.start_time = stZero;
				if( eventInfo->db.StartTimeFlag ){
					ConvertSystemTime(start_time, &eventInfo->db.start_time);
				}
				eventInfo->db.DurationFlag = duration != 0xFFFFFF;
				eventInfo->db.durationSec = duration != 0xFFFFFF ? duration : 0;
				eventInfo->db.freeCAFlag = eit.GetNumber(Desc::free_CA_mode, lp) != 0;
				eventInfo->time = siTag.time;
			}
			//�L�q�q�̓e�[�u���o�[�W�������������čX�V(�P�Ɍ����̂���)
			if( siTag.time >= eventInfo->tagExt.time ){
				if( version_number != eventInfo->tagExt.version ||
				    table_id != eventInfo->tagExt.tableID ||
				    siTag.time > eventInfo->tagExt.time + 180 ){
					if( AddExtEvent(&eventInfo->db, eit, lp) != FALSE ){
						eventInfo->tagExt = siTag;
					}
				}else{
					eventInfo->tagExt.time = siTag.time;
				}
			}
			//[schedule extended]�ȊO
			if( (table_id < 0x58 || 0x5F < table_id) && (table_id < 0x68 || 0x6F < table_id) ){
				if( table_id > 0x4F && eventInfo->tagBasic.version != 0xFF && eventInfo->tagBasic.tableID <= 0x4F ){
					//[schedule][p/f after]�Ƃ��^�p����T�[�r�X�������[p/f after]��D�悷��(���̂Ƃ���T�[�r�X�K�w���������Ă���̂ł��蓾�Ȃ��͂�)
					_OutputDebugString(L"Conflicts EIT[schedule][p/f after] SID:0x%04x EventID:0x%04x\r\n", service_id, eventInfo->db.event_id);
				}else if( siTag.time >= eventInfo->tagBasic.time ){
					if( version_number != eventInfo->tagBasic.version ||
					    table_id != eventInfo->tagBasic.tableID ||
					    siTag.time > eventInfo->tagBasic.time + 180 ){
						AddBasicInfo(&eventInfo->db, eit, lp, original_network_id, transport_stream_id);
					}
					eventInfo->tagBasic = siTag;
				}
			}
		}
	}

	//�Z�N�V�����X�e�[�^�X
	if( PID != 0x0012 ){
		//L-EIT
		if( table_id <= 0x4F ){
			if( serviceInfo->lastTableID != table_id ||
			    serviceInfo->sectionList[0].version != version_number + 1 ){
				serviceInfo->lastTableID = 0;
			}
			if( serviceInfo->lastTableID == 0 ){
				//���Z�b�g
				SECTION_FLAG_INFO infoZero = {};
				std::fill_n(serviceInfo->sectionList, 8, infoZero);
				for( int i = 1; i < 8; i++ ){
					//��0�e�[�u���ȊO�̃Z�N�V�����𖳎�
					std::fill_n(serviceInfo->sectionList[i].ignoreFlags, _countof(serviceInfo->sectionList[0].ignoreFlags), 0xFF);
				}
				serviceInfo->lastTableID = table_id;
			}
			//��0�Z�O�����g�ȊO�̃Z�N�V�����𖳎�
			std::fill_n(serviceInfo->sectionList[0].ignoreFlags + 1, _countof(serviceInfo->sectionList[0].ignoreFlags) - 1, 0xFF);
			//��0�Z�O�����g�̑����Ȃ��Z�N�V�����𖳎�
			for( int i = eit.GetNumber(Desc::segment_last_section_number) % 8 + 1; i < 8; i++ ){
				serviceInfo->sectionList[0].ignoreFlags[0] |= 1 << i;
			}
			serviceInfo->sectionList[0].version = version_number + 1;
			serviceInfo->sectionList[0].flags[0] |= 1 << (section_number % 8);
		}

	}else{
		//H-EIT
		if( table_id > 0x4F ){
			BYTE& lastTableID = table_id % 16 >= 8 ? serviceInfo->lastTableIDExt : serviceInfo->lastTableID;
			SECTION_FLAG_INFO* sectionList = table_id % 16 >= 8 ? serviceInfo->sectionExtList : serviceInfo->sectionList;
			if( lastTableID != eit.GetNumber(Desc::last_table_id) ){
				lastTableID = 0;
			}else if( sectionList[table_id % 8].version != 0 &&
			          sectionList[table_id % 8].version != version_number + 1 ){
				OutputDebugString(L"EIT[schedule] updated\r\n");
				lastTableID = 0;
			}
			if( lastTableID == 0 ){
				//���Z�b�g
				SECTION_FLAG_INFO infoZero = {};
				std::fill_n(sectionList, 8, infoZero);
				for( int i = eit.GetNumber(Desc::last_table_id) % 8 + 1; i < 8; i++ ){
					//�����Ȃ��e�[�u���̃Z�N�V�����𖳎�
					std::fill_n(sectionList[i].ignoreFlags, _countof(sectionList[0].ignoreFlags), 0xFF);
				}
				lastTableID = (BYTE)eit.GetNumber(Desc::last_table_id);
			}
			//�����Ȃ��Z�O�����g�̃Z�N�V�����𖳎�
			std::fill_n(sectionList[table_id % 8].ignoreFlags + (BYTE)eit.GetNumber(Desc::last_section_number) / 8 + 1,
			            _countof(sectionList[0].ignoreFlags) - (BYTE)eit.GetNumber(Desc::last_section_number) / 8 - 1, 0xFF);
			if( table_id % 8 == 0 && streamTime > 0 ){
				//�����ς݃Z�O�����g�̃Z�N�V�����𖳎�
				std::fill_n(sectionList[0].ignoreFlags, streamTime / (3 * 60 * 60 * I64_1SEC) % 8, 0xFF);
			}
			//���̃Z�O�����g�̑����Ȃ��Z�N�V�����𖳎�
			for( int i = eit.GetNumber(Desc::segment_last_section_number) % 8 + 1; i < 8; i++ ){
				sectionList[table_id % 8].ignoreFlags[section_number / 8] |= 1 << i;
			}
			sectionList[table_id % 8].version = version_number + 1;
			sectionList[table_id % 8].flags[section_number / 8] |= 1 << (section_number % 8);
		}
	}

	return TRUE;
}

int CEpgDBUtil::GetCaptionInfo(const Desc::CDescriptor& eit, Desc::CDescriptor::CLoopPointer lp)
{
	//�Z���N�^�̈��arib_caption_info
	DWORD infoSize;
	const BYTE* info = eit.GetBinary(Desc::selector_byte, &infoSize, lp);
	int flags = 0;
	for( DWORD i = 0; info && i * 4 + 4 < infoSize && i < info[0]; i++ ){
		//���{��(�ȊO)�̎���������
		flags |= memcmp(info + i * 4 + 2, "jpn", 3) == 0 ? 1 : 2;
	}
	return flags;
}

void CEpgDBUtil::AddBasicInfo(EPGDB_EVENT_INFO* eventInfo, const Desc::CDescriptor& eit, Desc::CDescriptor::CLoopPointer lpParent, WORD onid, WORD tsid)
{
	eventInfo->hasShortInfo = false;
	eventInfo->hasContentInfo = false;
	eventInfo->hasComponentInfo = false;
	eventInfo->eventGroupInfoGroupType = 0;
	eventInfo->eventRelayInfoGroupType = 0;
	Desc::CDescriptor::CLoopPointer lp = lpParent;
	if( eit.EnterLoop(lp) ){
#ifndef EPGDB_NO_ADD_CAPTION_TO_COMPONENT
		DWORD dataContentIndex = MAXDWORD;
#endif
		for( DWORD i = 0; eit.SetLoopIndex(lp, i); i++ ){
			switch( eit.GetNumber(Desc::descriptor_tag, lp) ){
			case Desc::short_event_descriptor:
				AddShortEvent(eventInfo, eit, lp);
				break;
			case Desc::content_descriptor:
				AddContent(eventInfo, eit, lp);
				break;
			case Desc::component_descriptor:
				AddComponent(eventInfo, eit, lp);
				break;
			case Desc::event_group_descriptor:
				if( eit.GetNumber(Desc::group_type, lp) == 1 ){
					AddEventGroup(eventInfo, eit, lp, onid, tsid);
				}else if( eit.GetNumber(Desc::group_type, lp) == 2 || eit.GetNumber(Desc::group_type, lp) == 4 ){
					AddEventRelay(eventInfo, eit, lp, onid, tsid);
				}
				break;
#ifndef EPGDB_NO_ADD_CAPTION_TO_COMPONENT
			case Desc::data_content_descriptor:
				if( eit.GetNumber(Desc::data_component_id, lp) == 0x0008 ){
					dataContentIndex = i;
				}
				break;
#endif
			}
		}
#ifndef EPGDB_NO_ADD_CAPTION_TO_COMPONENT
		if( eventInfo->hasComponentInfo && dataContentIndex != MAXDWORD ){
			eit.SetLoopIndex(lp, dataContentIndex);
			int flags = GetCaptionInfo(eit, lp);
			//�ԑg���Ŕ��f�ł��Ȃ��Ƃ�����
			if( (flags & 2) || ((flags & 1) && (eventInfo->hasShortInfo == false ||
			        eventInfo->shortInfo.event_name.find(CARIB8CharDecode::TELETEXT_MARK) == wstring::npos)) ){
				if( flags & 1 ){
					eventInfo->componentInfo.text_char += L"[��]";
				}
				if( flags & 2 ){
					eventInfo->componentInfo.text_char += L"[��]";
				}
			}else if( flags == 0 && eventInfo->hasShortInfo &&
			          eventInfo->shortInfo.event_name.find(CARIB8CharDecode::TELETEXT_MARK) != wstring::npos ){
				eventInfo->componentInfo.text_char += L"[����]";
			}
		}
#endif
	}
	if( AddAudioComponent(eventInfo, eit, lpParent) == FALSE ){
		eventInfo->hasAudioInfo = false;
	}
}

void CEpgDBUtil::AddShortEvent(EPGDB_EVENT_INFO* eventInfo, const Desc::CDescriptor& eit, Desc::CDescriptor::CLoopPointer lp)
{
	eventInfo->hasShortInfo = true;
	eventInfo->shortInfo.event_name.clear();
	eventInfo->shortInfo.text_char.clear();
	DWORD srcSize;
	const BYTE* src = eit.GetBinary(Desc::event_name_char, &srcSize, lp);
	if( src && srcSize > 0 ){
		this->arib.PSISI(src, srcSize, &eventInfo->shortInfo.event_name);
	}
	src = eit.GetBinary(Desc::text_char, &srcSize, lp);
	if( src && srcSize > 0 ){
		this->arib.PSISI(src, srcSize, &eventInfo->shortInfo.text_char);
	}
#ifdef DEBUG_EIT
	eventInfo->shortInfo.text_char = g_szDebugEIT + eventInfo->shortInfo.text_char;
#endif
}

BOOL CEpgDBUtil::AddExtEvent(EPGDB_EVENT_INFO* eventInfo, const Desc::CDescriptor& eit, Desc::CDescriptor::CLoopPointer lpParent)
{
	{
		BOOL foundFlag = FALSE;
		wstring extendText;
		vector<BYTE> itemBuff;
		BOOL itemDescFlag = FALSE;
		//text_length��0�ŉ^�p�����

		Desc::CDescriptor::CLoopPointer lp = lpParent;
		if( eit.EnterLoop(lp) ){
			for( DWORD i = 0; eit.SetLoopIndex(lp, i); i++ ){
				if( eit.GetNumber(Desc::descriptor_tag, lp) != Desc::extended_event_descriptor ){
					continue;
				}
				foundFlag = TRUE;
				Desc::CDescriptor::CLoopPointer lp2 = lp;
				if( eit.EnterLoop(lp2) ){
					for( DWORD j=0; eit.SetLoopIndex(lp2, j); j++ ){
						const BYTE* src;
						DWORD srcSize;
						src = eit.GetBinary(Desc::item_description_char, &srcSize, lp2);
						if( src && srcSize > 0 ){
							if( itemDescFlag == FALSE && itemBuff.size() > 0 ){
								wstring buff;
								this->arib.PSISI(itemBuff.data(), (DWORD)itemBuff.size(), &buff);
								buff += L"\r\n";
								extendText += buff;
								itemBuff.clear();
							}
							itemDescFlag = TRUE;
							itemBuff.insert(itemBuff.end(), src, src + srcSize);
						}
						src = eit.GetBinary(Desc::item_char, &srcSize, lp2);
						if( src && srcSize > 0 ){
							if( itemDescFlag && itemBuff.size() > 0 ){
								wstring buff;
								this->arib.PSISI(itemBuff.data(), (DWORD)itemBuff.size(), &buff);
								buff += L"\r\n";
								extendText += buff;
								itemBuff.clear();
							}
							itemDescFlag = FALSE;
							itemBuff.insert(itemBuff.end(), src, src + srcSize);
						}
					}
				}
			}
		}

		if( itemBuff.size() > 0 ){
			wstring buff;
			this->arib.PSISI(itemBuff.data(), (DWORD)itemBuff.size(), &buff);
			buff += L"\r\n";
			extendText += buff;
		}

		if( foundFlag == FALSE ){
			return FALSE;
		}
		eventInfo->hasExtInfo = true;
#ifdef DEBUG_EIT
		extendText = g_szDebugEIT + extendText;
#endif
		eventInfo->extInfo.text_char.swap(extendText);
	}

	return TRUE;
}

void CEpgDBUtil::AddContent(EPGDB_EVENT_INFO* eventInfo, const Desc::CDescriptor& eit, Desc::CDescriptor::CLoopPointer lp)
{
	eventInfo->hasContentInfo = true;
	eventInfo->contentInfo.nibbleList.clear();
	if( eit.EnterLoop(lp) ){
		eventInfo->contentInfo.nibbleList.resize(eit.GetLoopSize(lp));
		for( DWORD i = 0; eit.SetLoopIndex(lp, i); i++ ){
			EPG_CONTENT& nibble = eventInfo->contentInfo.nibbleList[i];
			DWORD n = eit.GetNumber(Desc::content_user_nibble, lp);
			nibble.content_nibble_level_1 = (n >> 12) & 0x0F;
			nibble.content_nibble_level_2 = (n >> 8) & 0x0F;
			nibble.user_nibble_1 = (n >> 4) & 0x0F;
			nibble.user_nibble_2 = n & 0x0F;
		}
	}
}

void CEpgDBUtil::AddComponent(EPGDB_EVENT_INFO* eventInfo, const Desc::CDescriptor& eit, Desc::CDescriptor::CLoopPointer lp)
{
	eventInfo->hasComponentInfo = true;
	eventInfo->componentInfo.stream_content = (BYTE)eit.GetNumber(Desc::stream_content, lp);
	eventInfo->componentInfo.component_type = (BYTE)eit.GetNumber(Desc::component_type, lp);
	eventInfo->componentInfo.component_tag = (BYTE)eit.GetNumber(Desc::component_tag, lp);
	eventInfo->componentInfo.text_char.clear();
	DWORD srcSize;
	const BYTE* src = eit.GetBinary(Desc::text_char, &srcSize, lp);
	if( src && srcSize > 0 ){
		this->arib.PSISI(src, srcSize, &eventInfo->componentInfo.text_char);
	}
}

BOOL CEpgDBUtil::AddAudioComponent(EPGDB_EVENT_INFO* eventInfo, const Desc::CDescriptor& eit, Desc::CDescriptor::CLoopPointer lpParent)
{
	{
		WORD listSize = 0;
		Desc::CDescriptor::CLoopPointer lp = lpParent;
		if( eit.EnterLoop(lp) ){
			for( DWORD i = 0; eit.SetLoopIndex(lp, i); i++ ){
				if( eit.GetNumber(Desc::descriptor_tag, lp) == Desc::audio_component_descriptor ){
					listSize++;
				}
			}
		}
		if( listSize == 0 ){
			return FALSE;
		}
		eventInfo->hasAudioInfo = true;
		eventInfo->audioInfo.componentList.clear();
		eventInfo->audioInfo.componentList.resize(listSize);

		for( WORD i=0, j=0; j<eventInfo->audioInfo.componentList.size(); i++ ){
			eit.SetLoopIndex(lp, i);
			if( eit.GetNumber(Desc::descriptor_tag, lp) == Desc::audio_component_descriptor ){
				EPGDB_AUDIO_COMPONENT_INFO_DATA& item = eventInfo->audioInfo.componentList[j++];

				item.stream_content = (BYTE)eit.GetNumber(Desc::stream_content, lp);
				item.component_type = (BYTE)eit.GetNumber(Desc::component_type, lp);
				item.component_tag = (BYTE)eit.GetNumber(Desc::component_tag, lp);

				item.stream_type = (BYTE)eit.GetNumber(Desc::stream_type, lp);
				item.simulcast_group_tag = (BYTE)eit.GetNumber(Desc::simulcast_group_tag, lp);
				item.ES_multi_lingual_flag = (BYTE)eit.GetNumber(Desc::ES_multi_lingual_flag, lp);
				item.main_component_flag = (BYTE)eit.GetNumber(Desc::main_component_flag, lp);
				item.quality_indicator = (BYTE)eit.GetNumber(Desc::quality_indicator, lp);
				item.sampling_rate = (BYTE)eit.GetNumber(Desc::sampling_rate, lp);

				item.text_char.clear();
				DWORD srcSize;
				const BYTE* src = eit.GetBinary(Desc::text_char, &srcSize, lp);
				if( src && srcSize > 0 ){
					this->arib.PSISI(src, srcSize, &item.text_char);
				}
			}
		}
	}

	return TRUE;
}

void CEpgDBUtil::AddEventGroup(EPGDB_EVENT_INFO* eventInfo, const Desc::CDescriptor& eit, Desc::CDescriptor::CLoopPointer lp, WORD onid, WORD tsid)
{
	eventInfo->eventGroupInfoGroupType = 1;
	eventInfo->eventGroupInfo.eventDataList.clear();
	if( eit.EnterLoop(lp) ){
		eventInfo->eventGroupInfo.eventDataList.resize(eit.GetLoopSize(lp));
		for( DWORD i = 0; eit.SetLoopIndex(lp, i); i++ ){
			EPG_EVENT_DATA& item = eventInfo->eventGroupInfo.eventDataList[i];
			item.original_network_id = onid;
			item.transport_stream_id = tsid;
			item.service_id = (WORD)eit.GetNumber(Desc::service_id, lp);
			item.event_id = (WORD)eit.GetNumber(Desc::event_id, lp);
		}
	}
}

void CEpgDBUtil::AddEventRelay(EPGDB_EVENT_INFO* eventInfo, const Desc::CDescriptor& eit, Desc::CDescriptor::CLoopPointer lp, WORD onid, WORD tsid)
{
	eventInfo->eventRelayInfoGroupType = (BYTE)eit.GetNumber(Desc::group_type, lp);
	eventInfo->eventRelayInfo.eventDataList.clear();
	if( eventInfo->eventRelayInfoGroupType == 2 ){
		if( eit.EnterLoop(lp) ){
			eventInfo->eventRelayInfo.eventDataList.resize(eit.GetLoopSize(lp));
			for( DWORD i = 0; eit.SetLoopIndex(lp, i); i++ ){
				EPG_EVENT_DATA& item = eventInfo->eventRelayInfo.eventDataList[i];
				item.original_network_id = onid;
				item.transport_stream_id = tsid;
				item.service_id = (WORD)eit.GetNumber(Desc::service_id, lp);
				item.event_id = (WORD)eit.GetNumber(Desc::event_id, lp);
			}
		}
	}else{
		if( eit.EnterLoop(lp, 1) ){
			//���l�b�g���[�N�ւ̃����[���͑�2���[�v�ɂ���̂ŁA����͋L�q�q��event_count�̒l�Ƃ͈قȂ�
			eventInfo->eventRelayInfo.eventDataList.resize(eit.GetLoopSize(lp));
			for( DWORD i = 0; eit.SetLoopIndex(lp, i); i++ ){
				EPG_EVENT_DATA& item = eventInfo->eventRelayInfo.eventDataList[i];
				item.original_network_id = (WORD)eit.GetNumber(Desc::original_network_id, lp);
				item.transport_stream_id = (WORD)eit.GetNumber(Desc::transport_stream_id, lp);
				item.service_id = (WORD)eit.GetNumber(Desc::service_id, lp);
				item.event_id = (WORD)eit.GetNumber(Desc::event_id, lp);
			}
		}
	}
}

void CEpgDBUtil::ClearSectionStatus()
{
	map<ULONGLONG, SERVICE_EVENT_INFO>::iterator itr;
	for( itr = this->serviceEventMap.begin(); itr != this->serviceEventMap.end(); itr++ ){
		itr->second.lastTableID = 0;
		itr->second.lastTableIDExt = 0;
	}
}

BOOL CEpgDBUtil::CheckSectionAll(const SECTION_FLAG_INFO (&sectionList)[8])
{
	for( size_t i = 0; i < 8; i++ ){
		for( size_t j = 0; j < sizeof(sectionList[0].flags); j++ ){
			if( (sectionList[i].flags[j] | sectionList[i].ignoreFlags[j]) != 0xFF ){
				return FALSE;
			}
		}
	}

	return TRUE;
}

EPG_SECTION_STATUS CEpgDBUtil::GetSectionStatusService(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL l_eitFlag
	)
{
	map<ULONGLONG, SERVICE_EVENT_INFO>::const_iterator itr =
		this->serviceEventMap.find(Create64Key(originalNetworkID, transportStreamID, serviceID));
	if( itr != this->serviceEventMap.end() ){
		if( l_eitFlag ){
			//L-EIT�̏�
			if( itr->second.lastTableID != 0 && itr->second.lastTableID <= 0x4F ){
				return CheckSectionAll(itr->second.sectionList) ? EpgLEITAll : EpgNeedData;
			}
		}else{
			//H-EIT�̏�
			if( itr->second.lastTableID > 0x4F ){
				//�g����񂪂Ȃ��ꍇ���u���܂����v�Ƃ݂Ȃ�
				BOOL extFlag = itr->second.lastTableIDExt == 0 || CheckSectionAll(itr->second.sectionExtList);
				return CheckSectionAll(itr->second.sectionList) ? (extFlag ? EpgHEITAll : EpgBasicAll) : (extFlag ? EpgExtendAll : EpgNeedData);
			}
		}
	}
	return EpgNoData;
}

EPG_SECTION_STATUS CEpgDBUtil::GetSectionStatus(BOOL l_eitFlag)
{
	EPG_SECTION_STATUS status = EpgNoData;

	map<ULONGLONG, SERVICE_EVENT_INFO>::iterator itr;
	for( itr = this->serviceEventMap.begin(); itr != this->serviceEventMap.end(); itr++ ){
		EPG_SECTION_STATUS s = GetSectionStatusService((WORD)(itr->first >> 32), (WORD)(itr->first >> 16), (WORD)itr->first, l_eitFlag);
		if( s != EpgNoData ){
			if( status == EpgNoData ){
				status = l_eitFlag ? EpgLEITAll : EpgHEITAll;
			}
			if( l_eitFlag ){
				if( s == EpgNeedData ){
					status = EpgNeedData;
					break;
				}
			}else{
				//�T�[�r�X���X�g����Ȃ�f���T�[�r�X�̂ݑΏ�
				map<ULONGLONG, BYTE>::iterator itrType;
				itrType = this->serviceList.find(itr->first);
				if( itrType != this->serviceList.end() ){
					if( itrType->second != 0x01 && itrType->second != 0xA5 ){
						continue;
					}
				}
				if( s == EpgNeedData || s == EpgExtendAll && status == EpgBasicAll || s == EpgBasicAll && status == EpgExtendAll ){
					status = EpgNeedData;
					break;
				}
				if( status == EpgHEITAll ){
					status = s;
				}
			}
		}
	}

	return status;
}

BOOL CEpgDBUtil::AddServiceListNIT(const Desc::CDescriptor& nit)
{
	wstring network_nameW = L"";

	Desc::CDescriptor::CLoopPointer lp;
	if( nit.EnterLoop(lp) ){
		for( DWORD i = 0; nit.SetLoopIndex(lp, i); i++ ){
			if( nit.GetNumber(Desc::descriptor_tag, lp) == Desc::network_name_descriptor ){
				DWORD srcSize;
				const BYTE* src = nit.GetBinary(Desc::d_char, &srcSize, lp);
				if( src && srcSize > 0 ){
					this->arib.PSISI(src, srcSize, &network_nameW);
				}
			}
		}
	}

	lp = Desc::CDescriptor::CLoopPointer();
	if( nit.EnterLoop(lp, 1) ){
		for( DWORD i = 0; nit.SetLoopIndex(lp, i); i++ ){
			//�T�[�r�X���X�V�p
			WORD onid = (WORD)nit.GetNumber(Desc::original_network_id, lp);
			WORD tsid = (WORD)nit.GetNumber(Desc::transport_stream_id, lp);
			map<DWORD, DB_TS_INFO>::iterator itrFind;
			itrFind = this->serviceInfoList.find((DWORD)onid << 16 | tsid);
			if( itrFind != this->serviceInfoList.end() ){
				itrFind->second.network_name = network_nameW;
			}

			Desc::CDescriptor::CLoopPointer lp2 = lp;
			if( nit.EnterLoop(lp2) ){
				for( DWORD j = 0; nit.SetLoopIndex(lp2, j); j++ ){
					if( nit.GetNumber(Desc::descriptor_tag, lp2) == Desc::service_list_descriptor ){
						Desc::CDescriptor::CLoopPointer lp3 = lp2;
						if( nit.EnterLoop(lp3) ){
							for( DWORD k=0; nit.SetLoopIndex(lp3, k); k++ ){
								ULONGLONG key = Create64Key(onid, tsid, (WORD)nit.GetNumber(Desc::service_id, lp3));
								map<ULONGLONG, BYTE>::iterator itrService;
								itrService = this->serviceList.find(key);
								if( itrService == this->serviceList.end() ){
									this->serviceList.insert(pair<ULONGLONG, BYTE>(key, (BYTE)nit.GetNumber(Desc::service_type, lp3)));
								}
							}
						}
					}
					if( nit.GetNumber(Desc::descriptor_tag, lp2) == Desc::ts_information_descriptor && itrFind != this->serviceInfoList.end()){
						//ts_name��remote_control_key_id
						DWORD srcSize;
						const BYTE* src = nit.GetBinary(Desc::ts_name_char, &srcSize, lp2);
						if( src && srcSize > 0 ){
							this->arib.PSISI(src, srcSize, &itrFind->second.ts_name);
						}
						itrFind->second.remote_control_key_id = (BYTE)nit.GetNumber(Desc::remote_control_key_id, lp2);
					}
					if( nit.GetNumber(Desc::descriptor_tag, lp2) == Desc::partial_reception_descriptor && itrFind != this->serviceInfoList.end()){
						//������M�t���O
						Desc::CDescriptor::CLoopPointer lp3 = lp2;
						if( nit.EnterLoop(lp3) ){
							for( DWORD k=0; nit.SetLoopIndex(lp3, k); k++ ){
								map<WORD, EPGDB_SERVICE_INFO>::iterator itrService;
								itrService = itrFind->second.serviceList.find((WORD)nit.GetNumber(Desc::service_id, lp3));
								if( itrService != itrFind->second.serviceList.end() ){
									itrService->second.partialReceptionFlag = 1;
								}
							}
						}
					}
				}
			}
		}
	}

	return TRUE;
}

BOOL CEpgDBUtil::AddServiceListSIT(WORD TSID, const Desc::CDescriptor& sit)
{
	WORD ONID = 0xFFFF;
	Desc::CDescriptor::CLoopPointer lp;
	if( sit.EnterLoop(lp) ){
		for( DWORD i = 0; sit.SetLoopIndex(lp, i); i++ ){
			if( sit.GetNumber(Desc::descriptor_tag, lp) == Desc::network_identification_descriptor ){
				ONID = (WORD)sit.GetNumber(Desc::network_id, lp);
			}
		}
	}
	if(ONID == 0xFFFF){
		return FALSE;
	}

	DWORD key = ((DWORD)ONID)<<16 | TSID;
	map<DWORD, DB_TS_INFO>::iterator itrTS;
	itrTS = this->serviceInfoList.find(key);
	if( itrTS == this->serviceInfoList.end() ){
		DB_TS_INFO info;
		info.remote_control_key_id = 0;

		lp = Desc::CDescriptor::CLoopPointer();
		if( sit.EnterLoop(lp, 1) ){
			for( DWORD i = 0; sit.SetLoopIndex(lp, i); i++ ){
				EPGDB_SERVICE_INFO item = {};
				item.ONID = ONID;
				item.TSID = TSID;
				item.SID = (WORD)sit.GetNumber(Desc::service_id, lp);

				Desc::CDescriptor::CLoopPointer lp2 = lp;
				if( sit.EnterLoop(lp2) ){
					for( DWORD j = 0; sit.SetLoopIndex(lp2, j); j++ ){
						if( sit.GetNumber(Desc::descriptor_tag, lp2) == Desc::service_descriptor ){
							wstring service_provider_name;
							wstring service_name;
							const BYTE* src;
							DWORD srcSize;
							src = sit.GetBinary(Desc::service_provider_name, &srcSize, lp2);
							if( src && srcSize > 0 ){
								this->arib.PSISI(src, srcSize, &service_provider_name);
							}
							src = sit.GetBinary(Desc::service_name, &srcSize, lp2);
							if( src && srcSize > 0 ){
								this->arib.PSISI(src, srcSize, &service_name);
							}
							item.service_provider_name.swap(service_provider_name);
							item.service_name.swap(service_name);

							item.service_type = (BYTE)sit.GetNumber(Desc::service_type, lp2);
						}
					}
				}
				info.serviceList.insert(std::make_pair(item.SID, item));
			}
		}
		this->serviceInfoList.insert(std::make_pair(key, info));
	}


	return TRUE;
}

BOOL CEpgDBUtil::AddSDT(const Desc::CDescriptor& sdt)
{
	DWORD key = sdt.GetNumber(Desc::original_network_id) << 16 | sdt.GetNumber(Desc::transport_stream_id);
	map<DWORD, DB_TS_INFO>::iterator itrTS;
	itrTS = this->serviceInfoList.find(key);
	if( itrTS == this->serviceInfoList.end() ){
		DB_TS_INFO info;
		info.remote_control_key_id = 0;
		itrTS = this->serviceInfoList.insert(std::make_pair(key, info)).first;
	}

	Desc::CDescriptor::CLoopPointer lp;
	if( sdt.EnterLoop(lp) ){
		for( DWORD i = 0; sdt.SetLoopIndex(lp, i); i++ ){
			map<WORD, EPGDB_SERVICE_INFO>::iterator itrS;
			itrS = itrTS->second.serviceList.find((WORD)sdt.GetNumber(Desc::service_id, lp));
			if( itrS == itrTS->second.serviceList.end()){
				EPGDB_SERVICE_INFO item = {};
				item.ONID = key >> 16;
				item.TSID = key & 0xFFFF;
				item.SID = (WORD)sdt.GetNumber(Desc::service_id, lp);

				Desc::CDescriptor::CLoopPointer lp2 = lp;
				if( sdt.EnterLoop(lp2) ){
					for( DWORD j = 0; sdt.SetLoopIndex(lp2, j); j++ ){
						if( sdt.GetNumber(Desc::descriptor_tag, lp2) != Desc::service_descriptor ){
							continue;
						}
						wstring service_provider_name;
						wstring service_name;
						const BYTE* src;
						DWORD srcSize;
						src = sdt.GetBinary(Desc::service_provider_name, &srcSize, lp2);
						if( src && srcSize > 0 ){
							this->arib.PSISI(src, srcSize, &service_provider_name);
						}
						src = sdt.GetBinary(Desc::service_name, &srcSize, lp2);
						if( src && srcSize > 0 ){
							this->arib.PSISI(src, srcSize, &service_name);
						}
						item.service_provider_name.swap(service_provider_name);
						item.service_name.swap(service_name);

						item.service_type = (BYTE)sdt.GetNumber(Desc::service_type, lp2);
					}
				}
				itrTS->second.serviceList.insert(std::make_pair(item.SID, item));
			}
		}
	}

	return TRUE;
}

//�w��T�[�r�X�̑SEPG�����擾����
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// epgInfoListSize			[OUT]epgInfoList�̌�
// epgInfoList				[OUT]EPG���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
BOOL CEpgDBUtil::GetEpgInfoList(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	DWORD* epgInfoListSize,
	EPG_EVENT_INFO** epgInfoList_
	)
{
	ULONGLONG key = Create64Key(originalNetworkID, transportStreamID, serviceID);

	map<ULONGLONG, SERVICE_EVENT_INFO>::iterator itr;
	itr = serviceEventMap.find(key);
	if( itr == serviceEventMap.end() ){
		return FALSE;
	}

	EPGDB_EVENT_INFO* evtPF[2] = {
		itr->second.nowEvent.empty() ? NULL : &itr->second.nowEvent.back().db,
		itr->second.nextEvent.empty() ? NULL : &itr->second.nextEvent.back().db
	};
	if( evtPF[0] == NULL || evtPF[1] && evtPF[0]->event_id > evtPF[1]->event_id ){
		std::swap(evtPF[0], evtPF[1]);
	}
	size_t listSize = itr->second.eventMap.size() +
		(evtPF[0] && itr->second.eventMap.count(evtPF[0]->event_id) == 0 ? 1 : 0) +
		(evtPF[1] && itr->second.eventMap.count(evtPF[1]->event_id) == 0 ? 1 : 0);
	if( listSize == 0 ){
		return FALSE;
	}
	*epgInfoListSize = (DWORD)listSize;
	this->epgInfoList.db.clear();

	map<WORD, EVENT_INFO>::iterator itrEvt = itr->second.eventMap.begin();
	while( evtPF[0] || itrEvt != itr->second.eventMap.end() ){
		if( itrEvt == itr->second.eventMap.end() || evtPF[0] && evtPF[0]->event_id < itrEvt->first ){
			//[p/f]���o��
			this->epgInfoList.db.push_back(*evtPF[0]);
			evtPF[0] = evtPF[1];
			evtPF[1] = NULL;
		}else{
			if( evtPF[0] && evtPF[0]->event_id == itrEvt->first ){
				//��������Ƃ���[p/f]��D��
				this->epgInfoList.db.push_back(*evtPF[0]);
				evtPF[0] = evtPF[1];
				evtPF[1] = NULL;
				if( this->epgInfoList.db.back().hasExtInfo == false && itrEvt->second.db.hasExtInfo ){
					this->epgInfoList.db.back().hasExtInfo = true;
					this->epgInfoList.db.back().extInfo = itrEvt->second.db.extInfo;
				}
			}else{
				//[schedule]���o��
				this->epgInfoList.db.push_back(itrEvt->second.db);
			}
			itrEvt++;
		}
	}

	this->epgInfoList.Update();
	*epgInfoList_ = this->epgInfoList.data.get();

	return TRUE;
}

//�w��T�[�r�X�̑SEPG����񋓂���
BOOL CEpgDBUtil::EnumEpgInfoList(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL (CALLBACK *enumEpgInfoListProc)(DWORD, EPG_EVENT_INFO*, LPVOID),
	LPVOID param
	)
{
	map<ULONGLONG, SERVICE_EVENT_INFO>::iterator itr =
		this->serviceEventMap.find(Create64Key(originalNetworkID, transportStreamID, serviceID));
	if( itr == this->serviceEventMap.end() ){
		return FALSE;
	}
	EPGDB_EVENT_INFO* evtPF[2] = {
		itr->second.nowEvent.empty() ? NULL : &itr->second.nowEvent.back().db,
		itr->second.nextEvent.empty() ? NULL : &itr->second.nextEvent.back().db
	};
	if( evtPF[0] == NULL || evtPF[1] && evtPF[0]->event_id > evtPF[1]->event_id ){
		std::swap(evtPF[0], evtPF[1]);
	}
	size_t listSize = itr->second.eventMap.size() +
		(evtPF[0] && itr->second.eventMap.count(evtPF[0]->event_id) == 0 ? 1 : 0) +
		(evtPF[1] && itr->second.eventMap.count(evtPF[1]->event_id) == 0 ? 1 : 0);
	if( listSize == 0 ){
		return FALSE;
	}
	if( enumEpgInfoListProc((DWORD)listSize, NULL, param) == FALSE ){
		return TRUE;
	}

	CEpgEventInfoAdapter adapter;
	map<WORD, EVENT_INFO>::iterator itrEvt = itr->second.eventMap.begin();
	while( evtPF[0] || itrEvt != itr->second.eventMap.end() ){
		//�}�X�^�[�𒼐ڎQ�Ƃ��č\�z����
		EPG_EVENT_INFO data;
		EPG_EXTENDED_EVENT_INFO extInfo;
		if( itrEvt == itr->second.eventMap.end() || evtPF[0] && evtPF[0]->event_id < itrEvt->first ){
			//[p/f]���o��
			data = adapter.Create(evtPF[0]);
			evtPF[0] = evtPF[1];
			evtPF[1] = NULL;
		}else{
			if( evtPF[0] && evtPF[0]->event_id == itrEvt->first ){
				//��������Ƃ���[p/f]��D��
				data = adapter.Create(evtPF[0]);
				evtPF[0] = evtPF[1];
				evtPF[1] = NULL;
				if( data.extInfo == NULL && itrEvt->second.db.hasExtInfo ){
					extInfo.text_charLength = (WORD)itrEvt->second.db.extInfo.text_char.size();
					extInfo.text_char = itrEvt->second.db.extInfo.text_char.c_str();
					data.extInfo = &extInfo;
				}
			}else{
				//[schedule]���o��
				data = adapter.Create(&itrEvt->second.db);
			}
			itrEvt++;
		}
		if( enumEpgInfoListProc(1, &data, param) == FALSE ){
			return TRUE;
		}
	}
	return TRUE;
}

//�~�ς��ꂽEPG���̂���T�[�r�X�ꗗ���擾����
//SERVICE_EXT_INFO�̏��͂Ȃ��ꍇ������
//�����F
// serviceListSize			[OUT]serviceList�̌�
// serviceList				[OUT]�T�[�r�X���̃��X�g�iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
void CEpgDBUtil::GetServiceListEpgDB(
	DWORD* serviceListSize,
	SERVICE_INFO** serviceList_
	)
{
	*serviceListSize = (DWORD)this->serviceEventMap.size();
	this->serviceDataList.reset(new SERVICE_INFO[*serviceListSize]);
	this->serviceDBList.reset(new EPGDB_SERVICE_INFO[*serviceListSize]);
	this->serviceAdapterList.reset(new CServiceInfoAdapter[*serviceListSize]);

	DWORD count = 0;
	map<ULONGLONG, SERVICE_EVENT_INFO>::iterator itr;
	for(itr = this->serviceEventMap.begin(); itr != this->serviceEventMap.end(); itr++ ){
		this->serviceDataList[count].original_network_id = (WORD)(itr->first>>32);
		this->serviceDataList[count].transport_stream_id = (WORD)((itr->first&0xFFFF0000)>>16);
		this->serviceDataList[count].service_id = (WORD)(itr->first&0xFFFF);
		this->serviceDataList[count].extInfo = NULL;

		DWORD infoKey = ((DWORD)this->serviceDataList[count].original_network_id) << 16 | this->serviceDataList[count].transport_stream_id;
		map<DWORD, DB_TS_INFO>::iterator itrInfo;
		itrInfo = this->serviceInfoList.find(infoKey);
		if( itrInfo != this->serviceInfoList.end() ){
			map<WORD, EPGDB_SERVICE_INFO>::iterator itrService;
			itrService = itrInfo->second.serviceList.find(this->serviceDataList[count].service_id);
			if( itrService != itrInfo->second.serviceList.end() ){
				this->serviceDBList[count] = itrService->second;
				this->serviceDBList[count].network_name = itrInfo->second.network_name;
				this->serviceDBList[count].ts_name = itrInfo->second.ts_name;
				this->serviceDBList[count].remote_control_key_id = itrInfo->second.remote_control_key_id;
				this->serviceDataList[count] = this->serviceAdapterList[count].Create(&this->serviceDBList[count]);
			}
		}

		count++;
	}

	*serviceList_ = this->serviceDataList.get();
}

//�w��T�[�r�X�̌���or����EPG�����擾����
//�����F
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// nextFlag					[IN]TRUE�i���̔ԑg�j�AFALSE�i���݂̔ԑg�j
// epgInfo					[OUT]EPG���iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
BOOL CEpgDBUtil::GetEpgInfo(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	BOOL nextFlag,
	EPG_EVENT_INFO** epgInfo_
	)
{
	this->epgInfo.db.clear();

	ULONGLONG key = Create64Key(originalNetworkID, transportStreamID, serviceID);

	map<ULONGLONG, SERVICE_EVENT_INFO>::iterator itr;
	itr = serviceEventMap.find(key);
	if( itr == serviceEventMap.end() ){
		return FALSE;
	}

	if( itr->second.nowEvent.empty() == false && nextFlag == FALSE ){
		this->epgInfo.db.push_back(itr->second.nowEvent.back().db);
	}else if( itr->second.nextEvent.empty() == false && nextFlag == TRUE ){
		this->epgInfo.db.push_back(itr->second.nextEvent.back().db);
	}
	if( this->epgInfo.db.empty() == false ){
		WORD eventID = this->epgInfo.db.back().event_id;
		if( this->epgInfo.db.back().hasExtInfo == false && itr->second.eventMap.count(eventID) && itr->second.eventMap[eventID].db.hasExtInfo ){
			this->epgInfo.db.back().hasExtInfo = true;
			this->epgInfo.db.back().extInfo = itr->second.eventMap[eventID].db.extInfo;
		}
		this->epgInfo.Update();
		*epgInfo_ = this->epgInfo.data.get();
		return TRUE;
	}

	return FALSE;
}

//�w��C�x���g��EPG�����擾����
//�����F
// originalNetworkID		[IN]�擾�Ώۂ�originalNetworkID
// transportStreamID		[IN]�擾�Ώۂ�transportStreamID
// serviceID				[IN]�擾�Ώۂ�ServiceID
// EventID					[IN]�擾�Ώۂ�EventID
// pfOnlyFlag				[IN]p/f����̂݌������邩�ǂ���
// epgInfo					[OUT]EPG���iDLL���Ŏ����I��delete����B���Ɏ擾���s���܂ŗL���j
BOOL CEpgDBUtil::SearchEpgInfo(
	WORD originalNetworkID,
	WORD transportStreamID,
	WORD serviceID,
	WORD eventID,
	BYTE pfOnlyFlag,
	EPG_EVENT_INFO** epgInfo_
	)
{
	this->searchEpgInfo.db.clear();

	ULONGLONG key = Create64Key(originalNetworkID, transportStreamID, serviceID);

	map<ULONGLONG, SERVICE_EVENT_INFO>::iterator itr;
	itr = serviceEventMap.find(key);
	if( itr == serviceEventMap.end() ){
		return FALSE;
	}

	if( itr->second.nowEvent.empty() == false && itr->second.nowEvent.back().db.event_id == eventID ){
		this->searchEpgInfo.db.push_back(itr->second.nowEvent.back().db);
	}else if( itr->second.nextEvent.empty() == false && itr->second.nextEvent.back().db.event_id == eventID ){
		this->searchEpgInfo.db.push_back(itr->second.nextEvent.back().db);
	}
	if( this->searchEpgInfo.db.empty() == false ){
		if( this->searchEpgInfo.db.back().hasExtInfo == false && itr->second.eventMap.count(eventID) && itr->second.eventMap[eventID].db.hasExtInfo ){
			this->searchEpgInfo.db.back().hasExtInfo = true;
			this->searchEpgInfo.db.back().extInfo = itr->second.eventMap[eventID].db.extInfo;
		}
		this->searchEpgInfo.Update();
		*epgInfo_ = this->searchEpgInfo.data.get();
		return TRUE;
	}
	if( pfOnlyFlag == 0 ){
		map<WORD, EVENT_INFO>::iterator itrEvent;
		itrEvent = itr->second.eventMap.find(eventID);
		if( itrEvent != itr->second.eventMap.end() ){
			this->searchEpgInfo.db.push_back(itrEvent->second.db);
			this->searchEpgInfo.Update();
			*epgInfo_ = this->searchEpgInfo.data.get();
			return TRUE;
		}
	}

	return FALSE;
}
