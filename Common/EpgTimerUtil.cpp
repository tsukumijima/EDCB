﻿#include "stdafx.h"
#include "EpgTimerUtil.h"

#include "PathUtil.h"
#include "StringUtil.h"
#include "TimeUtil.h"

DWORD CalcCrc32(int n, const BYTE* c)
{
	//CRCPOLY = 0x04C11DB7
	static const DWORD crctable[256] = {
		0x00000000, 0x04C11DB7, 0x09823B6E, 0x0D4326D9, 0x130476DC, 0x17C56B6B, 0x1A864DB2, 0x1E475005,
		0x2608EDB8, 0x22C9F00F, 0x2F8AD6D6, 0x2B4BCB61, 0x350C9B64, 0x31CD86D3, 0x3C8EA00A, 0x384FBDBD,
		0x4C11DB70, 0x48D0C6C7, 0x4593E01E, 0x4152FDA9, 0x5F15ADAC, 0x5BD4B01B, 0x569796C2, 0x52568B75,
		0x6A1936C8, 0x6ED82B7F, 0x639B0DA6, 0x675A1011, 0x791D4014, 0x7DDC5DA3, 0x709F7B7A, 0x745E66CD,
		0x9823B6E0, 0x9CE2AB57, 0x91A18D8E, 0x95609039, 0x8B27C03C, 0x8FE6DD8B, 0x82A5FB52, 0x8664E6E5,
		0xBE2B5B58, 0xBAEA46EF, 0xB7A96036, 0xB3687D81, 0xAD2F2D84, 0xA9EE3033, 0xA4AD16EA, 0xA06C0B5D,
		0xD4326D90, 0xD0F37027, 0xDDB056FE, 0xD9714B49, 0xC7361B4C, 0xC3F706FB, 0xCEB42022, 0xCA753D95,
		0xF23A8028, 0xF6FB9D9F, 0xFBB8BB46, 0xFF79A6F1, 0xE13EF6F4, 0xE5FFEB43, 0xE8BCCD9A, 0xEC7DD02D,
		0x34867077, 0x30476DC0, 0x3D044B19, 0x39C556AE, 0x278206AB, 0x23431B1C, 0x2E003DC5, 0x2AC12072,
		0x128E9DCF, 0x164F8078, 0x1B0CA6A1, 0x1FCDBB16, 0x018AEB13, 0x054BF6A4, 0x0808D07D, 0x0CC9CDCA,
		0x7897AB07, 0x7C56B6B0, 0x71159069, 0x75D48DDE, 0x6B93DDDB, 0x6F52C06C, 0x6211E6B5, 0x66D0FB02,
		0x5E9F46BF, 0x5A5E5B08, 0x571D7DD1, 0x53DC6066, 0x4D9B3063, 0x495A2DD4, 0x44190B0D, 0x40D816BA,
		0xACA5C697, 0xA864DB20, 0xA527FDF9, 0xA1E6E04E, 0xBFA1B04B, 0xBB60ADFC, 0xB6238B25, 0xB2E29692,
		0x8AAD2B2F, 0x8E6C3698, 0x832F1041, 0x87EE0DF6, 0x99A95DF3, 0x9D684044, 0x902B669D, 0x94EA7B2A,
		0xE0B41DE7, 0xE4750050, 0xE9362689, 0xEDF73B3E, 0xF3B06B3B, 0xF771768C, 0xFA325055, 0xFEF34DE2,
		0xC6BCF05F, 0xC27DEDE8, 0xCF3ECB31, 0xCBFFD686, 0xD5B88683, 0xD1799B34, 0xDC3ABDED, 0xD8FBA05A,
		0x690CE0EE, 0x6DCDFD59, 0x608EDB80, 0x644FC637, 0x7A089632, 0x7EC98B85, 0x738AAD5C, 0x774BB0EB,
		0x4F040D56, 0x4BC510E1, 0x46863638, 0x42472B8F, 0x5C007B8A, 0x58C1663D, 0x558240E4, 0x51435D53,
		0x251D3B9E, 0x21DC2629, 0x2C9F00F0, 0x285E1D47, 0x36194D42, 0x32D850F5, 0x3F9B762C, 0x3B5A6B9B,
		0x0315D626, 0x07D4CB91, 0x0A97ED48, 0x0E56F0FF, 0x1011A0FA, 0x14D0BD4D, 0x19939B94, 0x1D528623,
		0xF12F560E, 0xF5EE4BB9, 0xF8AD6D60, 0xFC6C70D7, 0xE22B20D2, 0xE6EA3D65, 0xEBA91BBC, 0xEF68060B,
		0xD727BBB6, 0xD3E6A601, 0xDEA580D8, 0xDA649D6F, 0xC423CD6A, 0xC0E2D0DD, 0xCDA1F604, 0xC960EBB3,
		0xBD3E8D7E, 0xB9FF90C9, 0xB4BCB610, 0xB07DABA7, 0xAE3AFBA2, 0xAAFBE615, 0xA7B8C0CC, 0xA379DD7B,
		0x9B3660C6, 0x9FF77D71, 0x92B45BA8, 0x9675461F, 0x8832161A, 0x8CF30BAD, 0x81B02D74, 0x857130C3,
		0x5D8A9099, 0x594B8D2E, 0x5408ABF7, 0x50C9B640, 0x4E8EE645, 0x4A4FFBF2, 0x470CDD2B, 0x43CDC09C,
		0x7B827D21, 0x7F436096, 0x7200464F, 0x76C15BF8, 0x68860BFD, 0x6C47164A, 0x61043093, 0x65C52D24,
		0x119B4BE9, 0x155A565E, 0x18197087, 0x1CD86D30, 0x029F3D35, 0x065E2082, 0x0B1D065B, 0x0FDC1BEC,
		0x3793A651, 0x3352BBE6, 0x3E119D3F, 0x3AD08088, 0x2497D08D, 0x2056CD3A, 0x2D15EBE3, 0x29D4F654,
		0xC5A92679, 0xC1683BCE, 0xCC2B1D17, 0xC8EA00A0, 0xD6AD50A5, 0xD26C4D12, 0xDF2F6BCB, 0xDBEE767C,
		0xE3A1CBC1, 0xE760D676, 0xEA23F0AF, 0xEEE2ED18, 0xF0A5BD1D, 0xF464A0AA, 0xF9278673, 0xFDE69BC4,
		0x89B8FD09, 0x8D79E0BE, 0x803AC667, 0x84FBDBD0, 0x9ABC8BD5, 0x9E7D9662, 0x933EB0BB, 0x97FFAD0C,
		0xAFB010B1, 0xAB710D06, 0xA6322BDF, 0xA2F33668, 0xBCB4666D, 0xB8757BDA, 0xB5365D03, 0xB1F740B4
	};
	DWORD r;

	r = 0xFFFFFFFF;
	while (--n >= 0)
		r = (r << 8) ^ crctable[(BYTE)(r >> 24) ^ *c++];
	return r;
}

__int64 MJDtoI64Time(DWORD mjd, DWORD bcdTime)
{
	DWORD h = (bcdTime >> 20 & 3) * 10 + (bcdTime >> 16 & 15);
	DWORD m = (bcdTime >> 12 & 7) * 10 + (bcdTime >> 8 & 15);
	DWORD s = (bcdTime >> 4 & 7) * 10 + (bcdTime & 15);
	//"1858-11-17"
	return 81377568000000000 + (((mjd * 24 + h) * 60 + m) * 60LL + s) * 10000000;
}

DWORD GetBitrateFromIni(WORD onid, WORD tsid, WORD sid)
{
	fs_path iniPath = GetCommonIniPath().replace_filename(L"Bitrate.ini");

	for( int i = 0; i < 4; i++ ){
		WCHAR key[32];
		swprintf_s(key, L"%04X%04X%04X", i > 2 ? 0xFFFF : onid, i > 1 ? 0xFFFF : tsid, i > 0 ? 0xFFFF : sid);
		int bitrate = GetPrivateProfileInt(L"BITRATE", key, 0, iniPath.c_str());
		if( bitrate > 0 ){
			return bitrate;
		}
	}
	return 19 * 1024;
}

//EPG情報をTextに変換
wstring ConvertEpgInfoText(const EPGDB_EVENT_INFO* info, const wstring* serviceName, const wstring* extraText)
{
	wstring text;
	if( info == NULL ){
		return text;
	}

	text = L"未定";
	if( info->StartTimeFlag ){
		SYSTEMTIME st = info->start_time;
		Format(text, L"%04d/%02d/%02d(%ls) %02d:%02d",
		       st.wYear, st.wMonth, st.wDay, GetDayOfWeekName(st.wDayOfWeek), st.wHour, st.wMinute);
		wstring time = L" ～ 未定";
		if( info->DurationFlag ){
			ConvertSystemTime(ConvertI64Time(st) + info->durationSec * I64_1SEC, &st);
			Format(time, L"～%02d:%02d", st.wHour, st.wMinute);
		}
		text += time;
	}
	text += L"\r\n";
	if( serviceName != NULL ){
		text += *serviceName;
		text += L"\r\n";
	}

	if( info->hasShortInfo ){
		text += info->shortInfo.event_name;
		text += L"\r\n\r\n";
		text += info->shortInfo.text_char;
		text += L"\r\n\r\n";
	}

	if( info->hasExtInfo ){
		text += L"詳細情報\r\n";
		text += info->extInfo.text_char;
		text += L"\r\n\r\n";
	}

	if( extraText != NULL ){
		text += *extraText;
	}
	wstring buff = L"";
	Format(buff, L"OriginalNetworkID:%d(0x%04X)\r\nTransportStreamID:%d(0x%04X)\r\nServiceID:%d(0x%04X)\r\nEventID:%d(0x%04X)\r\n",
		info->original_network_id, info->original_network_id,
		info->transport_stream_id, info->transport_stream_id,
		info->service_id, info->service_id,
		info->event_id, info->event_id);
	text += buff;
	return text;
}

namespace
{
struct KIND_INFO {
	WORD first;
	LPCWSTR str;
};

LPCWSTR SearchKindInfoArray(WORD key, const KIND_INFO* arr, size_t len)
{
	const KIND_INFO* ret = lower_bound_first(arr, arr + len, key);
	return ret != arr + len && ret->first == key ? ret->str : L"";
}
}

LPCWSTR GetGenreName(BYTE nibble1, BYTE nibble2)
{
	static const KIND_INFO contentKindSortedArray[] = {
	{ 0x0000, L"定時・総合" },
	{ 0x0001, L"天気" },
	{ 0x0002, L"特集・ドキュメント" },
	{ 0x0003, L"政治・国会" },
	{ 0x0004, L"経済・市況" },
	{ 0x0005, L"海外・国際" },
	{ 0x0006, L"解説" },
	{ 0x0007, L"討論・会談" },
	{ 0x0008, L"報道特番" },
	{ 0x0009, L"ローカル・地域" },
	{ 0x000A, L"交通" },
	{ 0x000F, L"その他" },
	{ 0x00FF, L"ニュース／報道" },

	{ 0x0100, L"スポーツニュース" },
	{ 0x0101, L"野球" },
	{ 0x0102, L"サッカー" },
	{ 0x0103, L"ゴルフ" },
	{ 0x0104, L"その他の球技" },
	{ 0x0105, L"相撲・格闘技" },
	{ 0x0106, L"オリンピック・国際大会" },
	{ 0x0107, L"マラソン・陸上・水泳" },
	{ 0x0108, L"モータースポーツ" },
	{ 0x0109, L"マリン・ウィンタースポーツ" },
	{ 0x010A, L"競馬・公営競技" },
	{ 0x010F, L"その他" },
	{ 0x01FF, L"スポーツ" },

	{ 0x0200, L"芸能・ワイドショー" },
	{ 0x0201, L"ファッション" },
	{ 0x0202, L"暮らし・住まい" },
	{ 0x0203, L"健康・医療" },
	{ 0x0204, L"ショッピング・通販" },
	{ 0x0205, L"グルメ・料理" },
	{ 0x0206, L"イベント" },
	{ 0x0207, L"番組紹介・お知らせ" },
	{ 0x020F, L"その他" },
	{ 0x02FF, L"情報／ワイドショー" },

	{ 0x0300, L"国内ドラマ" },
	{ 0x0301, L"海外ドラマ" },
	{ 0x0302, L"時代劇" },
	{ 0x030F, L"その他" },
	{ 0x03FF, L"ドラマ" },

	{ 0x0400, L"国内ロック・ポップス" },
	{ 0x0401, L"海外ロック・ポップス" },
	{ 0x0402, L"クラシック・オペラ" },
	{ 0x0403, L"ジャズ・フュージョン" },
	{ 0x0404, L"歌謡曲・演歌" },
	{ 0x0405, L"ライブ・コンサート" },
	{ 0x0406, L"ランキング・リクエスト" },
	{ 0x0407, L"カラオケ・のど自慢" },
	{ 0x0408, L"民謡・邦楽" },
	{ 0x0409, L"童謡・キッズ" },
	{ 0x040A, L"民族音楽・ワールドミュージック" },
	{ 0x040F, L"その他" },
	{ 0x04FF, L"音楽" },

	{ 0x0500, L"クイズ" },
	{ 0x0501, L"ゲーム" },
	{ 0x0502, L"トークバラエティ" },
	{ 0x0503, L"お笑い・コメディ" },
	{ 0x0504, L"音楽バラエティ" },
	{ 0x0505, L"旅バラエティ" },
	{ 0x0506, L"料理バラエティ" },
	{ 0x050F, L"その他" },
	{ 0x05FF, L"バラエティ" },

	{ 0x0600, L"洋画" },
	{ 0x0601, L"邦画" },
	{ 0x0602, L"アニメ" },
	{ 0x060F, L"その他" },
	{ 0x06FF, L"映画" },

	{ 0x0700, L"国内アニメ" },
	{ 0x0701, L"海外アニメ" },
	{ 0x0702, L"特撮" },
	{ 0x070F, L"その他" },
	{ 0x07FF, L"アニメ／特撮" },

	{ 0x0800, L"社会・時事" },
	{ 0x0801, L"歴史・紀行" },
	{ 0x0802, L"自然・動物・環境" },
	{ 0x0803, L"宇宙・科学・医学" },
	{ 0x0804, L"カルチャー・伝統文化" },
	{ 0x0805, L"文学・文芸" },
	{ 0x0806, L"スポーツ" },
	{ 0x0807, L"ドキュメンタリー全般" },
	{ 0x0808, L"インタビュー・討論" },
	{ 0x080F, L"その他" },
	{ 0x08FF, L"ドキュメンタリー／教養" },

	{ 0x0900, L"現代劇・新劇" },
	{ 0x0901, L"ミュージカル" },
	{ 0x0902, L"ダンス・バレエ" },
	{ 0x0903, L"落語・演芸" },
	{ 0x0904, L"歌舞伎・古典" },
	{ 0x090F, L"その他" },
	{ 0x09FF, L"劇場／公演" },

	{ 0x0A00, L"旅・釣り・アウトドア" },
	{ 0x0A01, L"園芸・ペット・手芸" },
	{ 0x0A02, L"音楽・美術・工芸" },
	{ 0x0A03, L"囲碁・将棋" },
	{ 0x0A04, L"麻雀・パチンコ" },
	{ 0x0A05, L"車・オートバイ" },
	{ 0x0A06, L"コンピュータ・ＴＶゲーム" },
	{ 0x0A07, L"会話・語学" },
	{ 0x0A08, L"幼児・小学生" },
	{ 0x0A09, L"中学生・高校生" },
	{ 0x0A0A, L"大学生・受験" },
	{ 0x0A0B, L"生涯教育・資格" },
	{ 0x0A0C, L"教育問題" },
	{ 0x0A0F, L"その他" },
	{ 0x0AFF, L"趣味／教育" },

	{ 0x0B00, L"高齢者" },
	{ 0x0B01, L"障害者" },
	{ 0x0B02, L"社会福祉" },
	{ 0x0B03, L"ボランティア" },
	{ 0x0B04, L"手話" },
	{ 0x0B05, L"文字（字幕）" },
	{ 0x0B06, L"音声解説" },
	{ 0x0B0F, L"その他" },
	{ 0x0BFF, L"福祉" },

	{ 0x0FFF, L"その他" },

	{ 0x6000, L"中止の可能性あり" },
	{ 0x6001, L"延長の可能性あり" },
	{ 0x6002, L"中断の可能性あり" },
	{ 0x6003, L"別話数放送の可能性あり" },
	{ 0x6004, L"編成未定枠" },
	{ 0x6005, L"繰り上げの可能性あり" },
	{ 0x60FF, L"編成情報" },

	{ 0x6100, L"中断ニュースあり" },
	{ 0x6101, L"臨時サービスあり" },
	{ 0x61FF, L"特性情報" },

	{ 0x6200, L"3D映像あり" },
	{ 0x62FF, L"3D映像" },

	{ 0x7000, L"テニス" },
	{ 0x7001, L"バスケットボール" },
	{ 0x7002, L"ラグビー" },
	{ 0x7003, L"アメリカンフットボール" },
	{ 0x7004, L"ボクシング" },
	{ 0x7005, L"プロレス" },
	{ 0x700F, L"その他" },
	{ 0x70FF, L"スポーツ(CS)" },

	{ 0x7100, L"アクション" },
	{ 0x7101, L"SF／ファンタジー" },
	{ 0x7102, L"コメディー" },
	{ 0x7103, L"サスペンス／ミステリー" },
	{ 0x7104, L"恋愛／ロマンス" },
	{ 0x7105, L"ホラー／スリラー" },
	{ 0x7106, L"ウエスタン" },
	{ 0x7107, L"ドラマ／社会派ドラマ" },
	{ 0x7108, L"アニメーション" },
	{ 0x7109, L"ドキュメンタリー" },
	{ 0x710A, L"アドベンチャー／冒険" },
	{ 0x710B, L"ミュージカル／音楽映画" },
	{ 0x710C, L"ホームドラマ" },
	{ 0x710F, L"その他" },
	{ 0x71FF, L"洋画(CS)" },

	{ 0x7200, L"アクション" },
	{ 0x7201, L"SF／ファンタジー" },
	{ 0x7202, L"お笑い／コメディー" },
	{ 0x7203, L"サスペンス／ミステリー" },
	{ 0x7204, L"恋愛／ロマンス" },
	{ 0x7205, L"ホラー／スリラー" },
	{ 0x7206, L"青春／学園／アイドル" },
	{ 0x7207, L"任侠／時代劇" },
	{ 0x7208, L"アニメーション" },
	{ 0x7209, L"ドキュメンタリー" },
	{ 0x720A, L"アドベンチャー／冒険" },
	{ 0x720B, L"ミュージカル／音楽映画" },
	{ 0x720C, L"ホームドラマ" },
	{ 0x720F, L"その他" },
	{ 0x72FF, L"邦画(CS)" },

	{ 0xFFFF, L"なし" },
	};
	return SearchKindInfoArray(nibble1 << 8 | nibble2, contentKindSortedArray, array_size(contentKindSortedArray));
}

LPCWSTR GetComponentTypeName(BYTE content, BYTE type)
{
	static const KIND_INFO componentKindSortedArray[] = {
	{ 0x0101, L"480i(525i)、アスペクト比4:3" },
	{ 0x0102, L"480i(525i)、アスペクト比16:9 パンベクトルあり" },
	{ 0x0103, L"480i(525i)、アスペクト比16:9 パンベクトルなし" },
	{ 0x0104, L"480i(525i)、アスペクト比 > 16:9" },
	{ 0x0191, L"2160p、アスペクト比4:3" },
	{ 0x0192, L"2160p、アスペクト比16:9 パンベクトルあり" },
	{ 0x0193, L"2160p、アスペクト比16:9 パンベクトルなし" },
	{ 0x0194, L"2160p、アスペクト比 > 16:9" },
	{ 0x01A1, L"480p(525p)、アスペクト比4:3" },
	{ 0x01A2, L"480p(525p)、アスペクト比16:9 パンベクトルあり" },
	{ 0x01A3, L"480p(525p)、アスペクト比16:9 パンベクトルなし" },
	{ 0x01A4, L"480p(525p)、アスペクト比 > 16:9" },
	{ 0x01B1, L"1080i(1125i)、アスペクト比4:3" },
	{ 0x01B2, L"1080i(1125i)、アスペクト比16:9 パンベクトルあり" },
	{ 0x01B3, L"1080i(1125i)、アスペクト比16:9 パンベクトルなし" },
	{ 0x01B4, L"1080i(1125i)、アスペクト比 > 16:9" },
	{ 0x01C1, L"720p(750p)、アスペクト比4:3" },
	{ 0x01C2, L"720p(750p)、アスペクト比16:9 パンベクトルあり" },
	{ 0x01C3, L"720p(750p)、アスペクト比16:9 パンベクトルなし" },
	{ 0x01C4, L"720p(750p)、アスペクト比 > 16:9" },
	{ 0x01D1, L"240p アスペクト比4:3" },
	{ 0x01D2, L"240p アスペクト比16:9 パンベクトルあり" },
	{ 0x01D3, L"240p アスペクト比16:9 パンベクトルなし" },
	{ 0x01D4, L"240p アスペクト比 > 16:9" },
	{ 0x01E1, L"1080p(1125p)、アスペクト比4:3" },
	{ 0x01E2, L"1080p(1125p)、アスペクト比16:9 パンベクトルあり" },
	{ 0x01E3, L"1080p(1125p)、アスペクト比16:9 パンベクトルなし" },
	{ 0x01E4, L"1080p(1125p)、アスペクト比 > 16:9" },
	{ 0x0201, L"1/0モード（シングルモノ）" },
	{ 0x0202, L"1/0＋1/0モード（デュアルモノ）" },
	{ 0x0203, L"2/0モード（ステレオ）" },
	{ 0x0204, L"2/1モード" },
	{ 0x0205, L"3/0モード" },
	{ 0x0206, L"2/2モード" },
	{ 0x0207, L"3/1モード" },
	{ 0x0208, L"3/2モード" },
	{ 0x0209, L"3/2＋LFEモード（3/2.1モード）" },
	{ 0x020A, L"3/3.1モード" },
	{ 0x020B, L"2/0/0-2/0/2-0.1モード" },
	{ 0x020C, L"5/2.1モード" },
	{ 0x020D, L"3/2/2.1モード" },
	{ 0x020E, L"2/0/0-3/0/2-0.1モード" },
	{ 0x020F, L"0/2/0-3/0/2-0.1モード" },
	{ 0x0210, L"2/0/0-3/2/3-0.2モード" },
	{ 0x0211, L"3/3/3-5/2/3-3/0/0.2モード" },
	{ 0x0240, L"視覚障害者用音声解説" },
	{ 0x0241, L"聴覚障害者用音声" },
	{ 0x0501, L"H.264|MPEG-4 AVC、480i(525i)、アスペクト比4:3" },
	{ 0x0502, L"H.264|MPEG-4 AVC、480i(525i)、アスペクト比16:9 パンベクトルあり" },
	{ 0x0503, L"H.264|MPEG-4 AVC、480i(525i)、アスペクト比16:9 パンベクトルなし" },
	{ 0x0504, L"H.264|MPEG-4 AVC、480i(525i)、アスペクト比 > 16:9" },
	{ 0x0591, L"H.264|MPEG-4 AVC、2160p、アスペクト比4:3" },
	{ 0x0592, L"H.264|MPEG-4 AVC、2160p、アスペクト比16:9 パンベクトルあり" },
	{ 0x0593, L"H.264|MPEG-4 AVC、2160p、アスペクト比16:9 パンベクトルなし" },
	{ 0x0594, L"H.264|MPEG-4 AVC、2160p、アスペクト比 > 16:9" },
	{ 0x05A1, L"H.264|MPEG-4 AVC、480p(525p)、アスペクト比4:3" },
	{ 0x05A2, L"H.264|MPEG-4 AVC、480p(525p)、アスペクト比16:9 パンベクトルあり" },
	{ 0x05A3, L"H.264|MPEG-4 AVC、480p(525p)、アスペクト比16:9 パンベクトルなし" },
	{ 0x05A4, L"H.264|MPEG-4 AVC、480p(525p)、アスペクト比 > 16:9" },
	{ 0x05B1, L"H.264|MPEG-4 AVC、1080i(1125i)、アスペクト比4:3" },
	{ 0x05B2, L"H.264|MPEG-4 AVC、1080i(1125i)、アスペクト比16:9 パンベクトルあり" },
	{ 0x05B3, L"H.264|MPEG-4 AVC、1080i(1125i)、アスペクト比16:9 パンベクトルなし" },
	{ 0x05B4, L"H.264|MPEG-4 AVC、1080i(1125i)、アスペクト比 > 16:9" },
	{ 0x05C1, L"H.264|MPEG-4 AVC、720p(750p)、アスペクト比4:3" },
	{ 0x05C2, L"H.264|MPEG-4 AVC、720p(750p)、アスペクト比16:9 パンベクトルあり" },
	{ 0x05C3, L"H.264|MPEG-4 AVC、720p(750p)、アスペクト比16:9 パンベクトルなし" },
	{ 0x05C4, L"H.264|MPEG-4 AVC、720p(750p)、アスペクト比 > 16:9" },
	{ 0x05D1, L"H.264|MPEG-4 AVC、240p アスペクト比4:3" },
	{ 0x05D2, L"H.264|MPEG-4 AVC、240p アスペクト比16:9 パンベクトルあり" },
	{ 0x05D3, L"H.264|MPEG-4 AVC、240p アスペクト比16:9 パンベクトルなし" },
	{ 0x05D4, L"H.264|MPEG-4 AVC、240p アスペクト比 > 16:9" },
	{ 0x05E1, L"H.264|MPEG-4 AVC、1080p(1125p)、アスペクト比4:3" },
	{ 0x05E2, L"H.264|MPEG-4 AVC、1080p(1125p)、アスペクト比16:9 パンベクトルあり" },
	{ 0x05E3, L"H.264|MPEG-4 AVC、1080p(1125p)、アスペクト比16:9 パンベクトルなし" },
	{ 0x05E4, L"H.264|MPEG-4 AVC、1080p(1125p)、アスペクト比 > 16:9" },
	};
	return SearchKindInfoArray(content << 8 | type, componentKindSortedArray, array_size(componentKindSortedArray));
}

//EPG情報をTextに変換
wstring ConvertEpgInfoText2(const EPGDB_EVENT_INFO* info, const wstring& serviceName)
{
	wstring text;
	if( info == NULL ){
		return text;
	}

	if( info->hasContentInfo ){
		text+=L"ジャンル : \r\n";
		AppendEpgContentInfoText(text, *info);
		text+=L"\r\n";
	}

	if( info->hasComponentInfo ){
		text+=L"映像 : ";
		AppendEpgComponentInfoText(text, *info);
		text+=L"\r\n";
	}

	if( info->hasAudioInfo ){
		text+=L"音声 : ";
		AppendEpgAudioComponentInfoText(text, *info);
	}

	text+=L"\r\n";
	if (!(0x7880 <= info->original_network_id && info->original_network_id <= 0x7FE8)){
		if (info->freeCAFlag == 0)
        {
            text += L"無料放送\r\n";
        }
        else
        {
            text += L"有料放送\r\n";
        }
        text += L"\r\n";
    }

	return ConvertEpgInfoText(info, &serviceName, &text);
}

void ConvertEpgInfo(WORD onid, WORD tsid, WORD sid, const EPG_EVENT_INFO* src, EPGDB_EVENT_INFO* dest)
{
	dest->original_network_id = onid;
	dest->transport_stream_id = tsid;
	dest->service_id = sid;
	dest->event_id = src->event_id;
	dest->StartTimeFlag = src->StartTimeFlag;
	dest->start_time = src->start_time;
	dest->DurationFlag = src->DurationFlag;
	dest->durationSec = src->durationSec;
	dest->freeCAFlag = src->freeCAFlag;

	dest->hasShortInfo = src->shortInfo != NULL;
	if( dest->hasShortInfo ){
		dest->shortInfo.event_name = src->shortInfo->event_name;
		dest->shortInfo.text_char = src->shortInfo->text_char;
	}
	dest->hasExtInfo = src->extInfo != NULL;
	if( dest->hasExtInfo ){
		dest->extInfo.text_char = src->extInfo->text_char;
	}
	dest->hasContentInfo = src->contentInfo != NULL;
	if( dest->hasContentInfo ){
		dest->contentInfo.nibbleList.clear();
		if( src->contentInfo->listSize > 0 ){
			dest->contentInfo.nibbleList.assign(
				src->contentInfo->nibbleList, src->contentInfo->nibbleList + src->contentInfo->listSize);
		}
	}
	dest->hasComponentInfo = src->componentInfo != NULL;
	if( dest->hasComponentInfo ){
		dest->componentInfo.stream_content = src->componentInfo->stream_content;
		dest->componentInfo.component_type = src->componentInfo->component_type;
		dest->componentInfo.component_tag = src->componentInfo->component_tag;
		dest->componentInfo.text_char = src->componentInfo->text_char;
	}
	dest->hasAudioInfo = src->audioInfo != NULL;
	if( dest->hasAudioInfo ){
		dest->audioInfo.componentList.resize(src->audioInfo->listSize);
		for( WORD i=0; i<src->audioInfo->listSize; i++ ){
			EPGDB_AUDIO_COMPONENT_INFO_DATA& item = dest->audioInfo.componentList[i];
			item.stream_content = src->audioInfo->audioList[i].stream_content;
			item.component_type = src->audioInfo->audioList[i].component_type;
			item.component_tag = src->audioInfo->audioList[i].component_tag;
			item.stream_type = src->audioInfo->audioList[i].stream_type;
			item.simulcast_group_tag = src->audioInfo->audioList[i].simulcast_group_tag;
			item.ES_multi_lingual_flag = src->audioInfo->audioList[i].ES_multi_lingual_flag;
			item.main_component_flag = src->audioInfo->audioList[i].main_component_flag;
			item.quality_indicator = src->audioInfo->audioList[i].quality_indicator;
			item.sampling_rate = src->audioInfo->audioList[i].sampling_rate;
			item.text_char = src->audioInfo->audioList[i].text_char;
		}
	}
	dest->eventGroupInfoGroupType = 0;
	if( src->eventGroupInfo != NULL ){
		dest->eventGroupInfoGroupType = src->eventGroupInfo->group_type;
		dest->eventGroupInfo.eventDataList.clear();
		if( src->eventGroupInfo->event_count > 0 ){
			dest->eventGroupInfo.eventDataList.assign(
				src->eventGroupInfo->eventDataList, src->eventGroupInfo->eventDataList + src->eventGroupInfo->event_count);
		}
	}
	dest->eventRelayInfoGroupType = 0;
	if( src->eventRelayInfo != NULL ){
		dest->eventRelayInfoGroupType = src->eventRelayInfo->group_type;
		dest->eventRelayInfo.eventDataList.clear();
		if( src->eventRelayInfo->event_count > 0 ){
			dest->eventRelayInfo.eventDataList.assign(
				src->eventRelayInfo->eventDataList, src->eventRelayInfo->eventDataList + src->eventRelayInfo->event_count);
		}
	}
}

void AppendEpgContentInfoText(wstring& text, const EPGDB_EVENT_INFO& info)
{
	if( info.hasContentInfo ){
		for( size_t i = 0; i < info.contentInfo.nibbleList.size(); i++ ){
			BYTE nibble1 = info.contentInfo.nibbleList[i].content_nibble_level_1;
			BYTE nibble2 = info.contentInfo.nibbleList[i].content_nibble_level_2;
			if( nibble1 == 0x0E && nibble2 <= 0x01 ){
				//番組付属情報またはCS拡張用情報
				nibble1 = info.contentInfo.nibbleList[i].user_nibble_1 | (0x60 + nibble2 * 16);
				nibble2 = info.contentInfo.nibbleList[i].user_nibble_2;
			}
			WCHAR buff[32];
			LPCWSTR ret = GetGenreName(nibble1, 0xFF);
			if( ret[0] ){
				text += ret;
				ret = GetGenreName(nibble1, nibble2);
				if( ret[0] ){
					text += L" - ";
					text += ret;
				}else if( nibble1 != 0x0F ){
					swprintf_s(buff, L" - (0x%02X)", nibble2);
					text += buff;
				}
			}else{
				swprintf_s(buff, L"(0x%02X) - (0x%02X)", nibble1, nibble2);
				text += buff;
			}
			text += L"\r\n";
		}
	}
}

void AppendEpgComponentInfoText(wstring& text, const EPGDB_EVENT_INFO& info)
{
	if( info.hasComponentInfo ){
		LPCWSTR ret = GetComponentTypeName(info.componentInfo.stream_content, info.componentInfo.component_type);
		if( ret[0] ){
			text += ret;
			if( info.componentInfo.text_char.empty() == false ){
				text += L"\r\n";
				text += info.componentInfo.text_char;
			}
		}
	}
}

void AppendEpgAudioComponentInfoText(wstring& text, const EPGDB_EVENT_INFO& info)
{
	if( info.hasAudioInfo ){
		for( size_t i = 0; i < info.audioInfo.componentList.size(); i++ ){
			LPCWSTR ret = GetComponentTypeName(info.audioInfo.componentList[i].stream_content, info.audioInfo.componentList[i].component_type);
			if( ret[0] ){
				text += ret;
				if( info.audioInfo.componentList[i].text_char.empty() == false ){
					text += L"\r\n";
					text += info.audioInfo.componentList[i].text_char;
				}
			}
			text += L"\r\n";
			text += L"サンプリングレート : ";
			switch( info.audioInfo.componentList[i].sampling_rate ){
				case 0x01:
					text += L"16kHz";
					break;
				case 0x02:
					text += L"22.05kHz";
					break;
				case 0x03:
					text += L"24kHz";
					break;
				case 0x05:
					text += L"32kHz";
					break;
				case 0x06:
					text += L"44.1kHz";
					break;
				case 0x07:
					text += L"48kHz";
					break;
			}
			text += L"\r\n";
		}
	}
}

EPG_EVENT_INFO CEpgEventInfoAdapter::Create(EPGDB_EVENT_INFO* ref)
{
	EPGDB_EVENT_INFO& r = *ref;
	EPG_EVENT_INFO dest;
	dest.event_id = r.event_id;
	dest.StartTimeFlag = r.StartTimeFlag;
	dest.start_time = r.start_time;
	dest.DurationFlag = r.DurationFlag;
	dest.durationSec = r.durationSec;
	dest.freeCAFlag = r.freeCAFlag;
	dest.shortInfo = NULL;
	if( r.hasShortInfo ){
		shortInfo.event_nameLength = (WORD)r.shortInfo.event_name.size();
		shortInfo.event_name = r.shortInfo.event_name.c_str();
		shortInfo.text_charLength = (WORD)r.shortInfo.text_char.size();
		shortInfo.text_char = r.shortInfo.text_char.c_str();
		dest.shortInfo = &shortInfo;
	}
	dest.extInfo = NULL;
	if( r.hasExtInfo ){
		extInfo.text_charLength = (WORD)r.extInfo.text_char.size();
		extInfo.text_char = r.extInfo.text_char.c_str();
		dest.extInfo = &extInfo;
	}
	dest.contentInfo = NULL;
	if( r.hasContentInfo ){
		contentInfo.listSize = (WORD)r.contentInfo.nibbleList.size();
		contentInfo.nibbleList = r.contentInfo.nibbleList.data();
		dest.contentInfo = &contentInfo;
	}
	dest.componentInfo = NULL;
	if( r.hasComponentInfo ){
		componentInfo.stream_content = r.componentInfo.stream_content;
		componentInfo.component_type = r.componentInfo.component_type;
		componentInfo.component_tag = r.componentInfo.component_tag;
		componentInfo.text_charLength = (WORD)r.componentInfo.text_char.size();
		componentInfo.text_char = r.componentInfo.text_char.c_str();
		dest.componentInfo = &componentInfo;
	}
	dest.audioInfo = NULL;
	if( r.hasAudioInfo ){
		audioInfo.listSize = (WORD)r.audioInfo.componentList.size();
		audioList.resize(audioInfo.listSize);
		for( WORD i = 0; i < audioInfo.listSize; i++ ){
			audioList[i].stream_content = r.audioInfo.componentList[i].stream_content;
			audioList[i].component_type = r.audioInfo.componentList[i].component_type;
			audioList[i].component_tag = r.audioInfo.componentList[i].component_tag;
			audioList[i].stream_type = r.audioInfo.componentList[i].stream_type;
			audioList[i].simulcast_group_tag = r.audioInfo.componentList[i].simulcast_group_tag;
			audioList[i].ES_multi_lingual_flag = r.audioInfo.componentList[i].ES_multi_lingual_flag;
			audioList[i].main_component_flag = r.audioInfo.componentList[i].main_component_flag;
			audioList[i].quality_indicator = r.audioInfo.componentList[i].quality_indicator;
			audioList[i].sampling_rate = r.audioInfo.componentList[i].sampling_rate;
			audioList[i].text_charLength = (WORD)r.audioInfo.componentList[i].text_char.size();
			audioList[i].text_char = r.audioInfo.componentList[i].text_char.c_str();
		}
		audioInfo.audioList = audioList.data();
		dest.audioInfo = &audioInfo;
	}
	dest.eventGroupInfo = NULL;
	if( r.eventGroupInfoGroupType ){
		eventGroupInfo.group_type = r.eventGroupInfoGroupType;
		eventGroupInfo.event_count = (BYTE)r.eventGroupInfo.eventDataList.size();
		eventGroupInfo.eventDataList = r.eventGroupInfo.eventDataList.data();
		dest.eventGroupInfo = &eventGroupInfo;
	}
	dest.eventRelayInfo = NULL;
	if( r.eventRelayInfoGroupType ){
		eventRelayInfo.group_type = r.eventRelayInfoGroupType;
		eventRelayInfo.event_count = (BYTE)r.eventRelayInfo.eventDataList.size();
		eventRelayInfo.eventDataList = r.eventRelayInfo.eventDataList.data();
		dest.eventRelayInfo = &eventRelayInfo;
	}
	return dest;
}

SERVICE_INFO CServiceInfoAdapter::Create(const EPGDB_SERVICE_INFO* ref)
{
	const EPGDB_SERVICE_INFO& r = *ref;
	SERVICE_INFO dest;
	dest.original_network_id = r.ONID;
	dest.transport_stream_id = r.TSID;
	dest.service_id = r.SID;
	extInfo.service_type = r.service_type;
	extInfo.partialReceptionFlag = r.partialReceptionFlag;
	extInfo.service_provider_name = r.service_provider_name.c_str();
	extInfo.service_name = r.service_name.c_str();
	extInfo.network_name = r.network_name.c_str();
	//互換のためts_nameは未取得や空文字列のときNULLとすべき
	//(service_nameはNULLにしてはいけない。BonCtrl/ChSetUtil.cppを参照)
	extInfo.ts_name = r.ts_name.empty() ? NULL : r.ts_name.c_str();
	extInfo.remote_control_key_id = r.remote_control_key_id;
	dest.extInfo = &extInfo;
	return dest;
}
