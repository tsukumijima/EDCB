// tsidmove: EDCB�̗\��AEPG�����\��A�v���O���������\��Ɋ܂܂��TransportStreamID�̏���ύX���� (2018-04-20)
// ���\��t�@�C�����ɂ��̃t�H�[�N�Ɣ�݊��̍��ڒǉ������ꂽ�t�H�[�N�ł͎g���܂킵�s�\
#include "stdafx.h"
#include "../../../Common/CommonDef.h"
#include "../../../Common/ParseTextInstances.h"
#include "../../../Common/PathUtil.h"
#include <locale.h>

static const CH_DATA5 *CheckTSID(WORD onid, WORD tsid, WORD sid, const CParseChText5 &chText5)
{
	if (chText5.GetMap().count(static_cast<LONGLONG>(onid) << 32 | static_cast<DWORD>(tsid) << 16 | sid) == 0) {
		// ������Ȃ�����
		auto itr = std::find_if(chText5.GetMap().begin(), chText5.GetMap().end(),
		                        [=](const pair<LONGLONG, CH_DATA5> &a) { return a.second.originalNetworkID == onid && a.second.serviceID == sid; });
		if (itr != chText5.GetMap().end()) {
			// TSID�𖳎�����Ό�������
			return &itr->second;
		}
	}
	return nullptr;
}

int wmain(int argc, wchar_t **argv)
{
	setlocale(LC_ALL, "");

	// --dray-run���͏������݂���؂��Ȃ�
	const bool dryrun = (argc >= 2 && wcscmp(argv[1], L"--dry-run") == 0);
	if (argc != 2 || (!dryrun && wcscmp(argv[1], L"--run") != 0)) {
		fputws(L"Usage: tsidmove --dry-run|--run\n", stdout);
		return 2;
	}

	// ���̃c�[����EDCB�t�H���_�����̒����̃t�H���_�ɒu����Ă���͂�
	fs_path iniPath = GetCommonIniPath();
	if (UtilFileExists(iniPath).first == false) {
		iniPath = iniPath.parent_path().replace_filename(L"Common.ini");
		if (UtilFileExists(iniPath).first == false) {
			fputws(L"Error: Common.ini��������܂���B\n", stdout);
			return 1;
		}
	}

	// �u�ݒ�֌W�ۑ��t�H���_�v
	fs_path settingPath = GetPrivateProfileToString(L"SET", L"DataSavePath", L"", iniPath.c_str());
	if (settingPath.empty()) {
		settingPath = fs_path(iniPath).replace_filename(L"Setting");
	}
	wprintf_s(L"\"%ls\"�t�H���_���`�F�b�N���Ă��܂�...\n", settingPath.c_str());

	fs_path chSet5Path = fs_path(settingPath).append(L"ChSet5.txt");
	if (UtilFileExists(chSet5Path).first == false) {
		fputws(L"Error: ChSet5.txt��������܂���B\n", stdout);
		return 1;
	}
	CParseChText5 chText5;
	if (!chText5.ParseText(chSet5Path.c_str())) {
		fputws(L"Error: ChSet5.txt���J���܂���B\n", stdout);
		return 1;
	}

	for (auto itr = chText5.GetMap().cbegin(); itr != chText5.GetMap().end(); itr++) {
		for (auto jtr = itr; ++jtr != chText5.GetMap().end(); ) {
			if (itr->second.originalNetworkID == jtr->second.originalNetworkID &&
			    itr->second.serviceID == jtr->second.serviceID) {
				fputws(L"Warning: ChSet5.txt��TSID�ȊO��ID���������T�[�r�X������܂��B�Â���񂪎c���Ă��܂��񂩁H\n", stdout);
				itr = chText5.GetMap().end();
				itr--;
				break;
			}
		}
	}

	HANDLE hMutex = CreateMutex(nullptr, FALSE, EPG_TIMER_BON_SRV_MUTEX);
	if (hMutex) {
		if (GetLastError() == ERROR_ALREADY_EXISTS) {
			CloseHandle(hMutex);
			hMutex = nullptr;
		}
	}
	if (!hMutex) {
		fputws(L"Error: EpgTimerSrv.exe���I�������Ă��������B\n", stdout);
		return 1;
	}
	CloseHandle(hMutex);

	fputws(RESERVE_TEXT_NAME L"(�\��t�@�C��)", stdout);
	{
		CParseReserveText text;
		if (!text.ParseText(fs_path(settingPath).append(RESERVE_TEXT_NAME).c_str())) {
			fputws(L"���X�L�b�v���܂��B\n", stdout);
		} else {
			int n = 0;
			for (size_t i = 0; i < text.GetMap().size(); i++) {
				auto itr = text.GetMap().cbegin();
				std::advance(itr, i);
				RESERVE_DATA r = itr->second;
				const CH_DATA5 *ch = CheckTSID(r.originalNetworkID, r.transportStreamID, r.serviceID, chText5);
				if (ch) {
					wprintf_s(L"\n  ID=%d, TSID=%d(0x%04X) -> %d(0x%04X) (%ls)",
					          r.reserveID, r.transportStreamID, r.transportStreamID,
					          ch->transportStreamID, ch->transportStreamID, ch->serviceName.c_str());
					r.transportStreamID = ch->transportStreamID;
					text.ChgReserve(r);
					n++;
				}
			}
			if (n == 0) {
				fputws(L"�ɕύX�͂���܂���B\n", stdout);
			} else {
				wprintf_s(L"\n�ȏ��%d���ڕύX���܂�...\n", n);
				if (!dryrun) {
					if (text.SaveText()) {
						fputws(L"�����B\n", stdout);
					} else {
						fputws(L"Error: ���s�B\n", stdout);
					}
				}
			}
		}
	}

	fputws(EPG_AUTO_ADD_TEXT_NAME L"(EPG�����\��t�@�C��)", stdout);
	{
		CParseEpgAutoAddText text;
		if (!text.ParseText(fs_path(settingPath).append(EPG_AUTO_ADD_TEXT_NAME).c_str())) {
			fputws(L"���X�L�b�v���܂��B\n", stdout);
		} else {
			int n = 0;
			for (size_t i = 0; i < text.GetMap().size(); i++) {
				auto itr = text.GetMap().cbegin();
				std::advance(itr, i);
				EPG_AUTO_ADD_DATA a = itr->second;
				bool modified = false;
				for (auto jtr = a.searchInfo.serviceList.begin(); jtr != a.searchInfo.serviceList.end(); jtr++) {
					const CH_DATA5 *ch = CheckTSID(static_cast<WORD>(*jtr >> 32), static_cast<WORD>(*jtr >> 16), static_cast<WORD>(*jtr), chText5);
					if (ch) {
						wprintf_s(L"\n  ID=%d, TSID=%d(0x%04X) -> %d(0x%04X) (%ls)",
						          a.dataID, static_cast<WORD>(*jtr >> 16), static_cast<WORD>(*jtr >> 16),
						          ch->transportStreamID, ch->transportStreamID, ch->serviceName.c_str());
						*jtr = (*jtr & 0xFFFF0000FFFFLL) | (static_cast<DWORD>(ch->transportStreamID) << 16);
						modified = true;
					}
				}
				if (modified) {
					text.ChgData(a);
					n++;
				}
			}
			if (n == 0) {
				fputws(L"�ɕύX�͂���܂���B\n", stdout);
			} else {
				wprintf_s(L"\n�ȏ��%d���ڕύX���܂�...\n", n);
				if (!dryrun) {
					if (text.SaveText()) {
						fputws(L"�����B\n", stdout);
					} else {
						fputws(L"Error: ���s�B\n", stdout);
					}
				}
			}
		}
	}

	fputws(MANUAL_AUTO_ADD_TEXT_NAME L"(�v���O���������\��t�@�C��)", stdout);
	{
		CParseManualAutoAddText text;
		if (!text.ParseText(fs_path(settingPath).append(MANUAL_AUTO_ADD_TEXT_NAME).c_str())) {
			fputws(L"���X�L�b�v���܂��B\n", stdout);
		} else {
			int n = 0;
			for (size_t i = 0; i < text.GetMap().size(); i++) {
				auto itr = text.GetMap().cbegin();
				std::advance(itr, i);
				MANUAL_AUTO_ADD_DATA m = itr->second;
				const CH_DATA5 *ch = CheckTSID(m.originalNetworkID, m.transportStreamID, m.serviceID, chText5);
				if (ch) {
					wprintf_s(L"\n  ID=%d, TSID=%d(0x%04X) -> %d(0x%04X) (%ls)",
					          m.dataID, m.transportStreamID, m.transportStreamID,
					          ch->transportStreamID, ch->transportStreamID, ch->serviceName.c_str());
					m.transportStreamID = ch->transportStreamID;
					text.ChgData(m);
					n++;
				}
			}
			if (n == 0) {
				fputws(L"�ɕύX�͂���܂���B\n", stdout);
			} else {
				wprintf_s(L"\n�ȏ��%d���ڕύX���܂�...\n", n);
				if (!dryrun) {
					if (text.SaveText()) {
						fputws(L"�����B\n", stdout);
					} else {
						fputws(L"Error: ���s�B\n", stdout);
					}
				}
			}
		}
	}

	fputws(L"�I���B\n", stdout);
	return 0;
}
