#pragma once

// MFC�Ŏg�����p
/*#ifdef _DEBUG
#undef new
#endif
#ifdef _DEBUG
#define new DEBUG_NEW
#endif
*/

#include "ColorDef.h"

//���������W��
//G�Z�b�g
#define MF_JIS_KANJI1 0x39 //JIS�݊�����1��
#define MF_JIS_KANJI2 0x3A //JIS�݊�����2��
#define MF_KIGOU 0x3B //�ǉ��L��
#define MF_ASCII 0x4A //�p��
#define MF_HIRA  0x30 //������
#define MF_KANA  0x31 //�Љ���
#define MF_KANJI 0x42 //����
#define MF_MOSAIC_A 0x32 //���U�C�NA
#define MF_MOSAIC_B 0x33 //���U�C�NB
#define MF_MOSAIC_C 0x34 //���U�C�NC
#define MF_MOSAIC_D 0x35 //���U�C�ND
#define MF_PROP_ASCII 0x36 //�v���|�[�V���i���p��
#define MF_PROP_HIRA  0x37 //�v���|�[�V���i��������
#define MF_PROP_KANA  0x38 //�v���|�[�V���i���Љ���
#define MF_JISX_KANA 0x49 //JIX X0201�Љ���
//DRCS
#define MF_DRCS_0 0x40 //DRCS-0
#define MF_DRCS_1 0x41 //DRCS-1
#define MF_DRCS_2 0x42 //DRCS-2
#define MF_DRCS_3 0x43 //DRCS-3
#define MF_DRCS_4 0x44 //DRCS-4
#define MF_DRCS_5 0x45 //DRCS-5
#define MF_DRCS_6 0x46 //DRCS-6
#define MF_DRCS_7 0x47 //DRCS-7
#define MF_DRCS_8 0x48 //DRCS-8
#define MF_DRCS_9 0x49 //DRCS-9
#define MF_DRCS_10 0x4A //DRCS-10
#define MF_DRCS_11 0x4B //DRCS-11
#define MF_DRCS_12 0x4C //DRCS-12
#define MF_DRCS_13 0x4D //DRCS-13
#define MF_DRCS_14 0x4E //DRCS-14
#define MF_DRCS_15 0x4F //DRCS-15
#define MF_MACRO 0x70 //�}�N��

//�����W���̕���
#define MF_MODE_G 1 //G�Z�b�g
#define MF_MODE_DRCS 2 //DRCS
#define MF_MODE_OTHER 3 //���̑�

#ifdef ARIB8CHAR_DECODE_H_IMPLEMENT_TABLE

static const WCHAR AsciiTable[]={
	L'�I', L'�h', L'��', L'��', L'��', L'��', L'�f',
	L'�i', L'�j', L'��', L'�{', L'�C', L'\xFF0D', L'�D', L'�^',
	L'�O', L'�P', L'�Q', L'�R', L'�S', L'�T', L'�U', L'�V',
	L'�W', L'�X', L'�F', L'�G', L'��', L'��', L'��', L'�H',
	L'��', L'�`', L'�a', L'�b', L'�c', L'�d', L'�e', L'�f',
	L'�g', L'�h', L'�i', L'�j', L'�k', L'�l', L'�m', L'�n',
	L'�o', L'�p', L'�q', L'�r', L'�s', L'�t', L'�u', L'�v',
	L'�w', L'�x', L'�y', L'�m', L'��', L'�n', L'�O', L'�Q',
	L'�e', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'�o', L'�b', L'�p', L'�P'
};
static const WCHAR HiraTable[]={
	L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'�@', L'�@', L'�@', L'�T',
	L'�U', L'�[', L'�B', L'�u', L'�v', L'�A', L'�E'
};
static const WCHAR KanaTable[]={
	L'�@', L'�A', L'�B', L'�C', L'�D', L'�E', L'�F',
	L'�G', L'�H', L'�I', L'�J', L'�K', L'�L', L'�M', L'�N',
	L'�O', L'�P', L'�Q', L'�R', L'�S', L'�T', L'�U', L'�V',
	L'�W', L'�X', L'�Y', L'�Z', L'�[', L'�\', L'�]', L'�^',
	L'�_', L'�`', L'�a', L'�b', L'�c', L'�d', L'�e', L'�f',
	L'�g', L'�h', L'�i', L'�j', L'�k', L'�l', L'�m', L'�n',
	L'�o', L'�p', L'�q', L'�r', L'�s', L'�t', L'�u', L'�v',
	L'�w', L'�x', L'�y', L'�z', L'�{', L'�|', L'�}', L'�~',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'��', L'�R',
	L'�S', L'�[', L'�B', L'�u', L'�v', L'�A', L'�E'
};
static const WCHAR JisXKanaTable[]={
	L'�B', L'�u', L'�v', L'�A', L'�E', L'��', L'�@',
	L'�B', L'�D', L'�F', L'�H', L'��', L'��', L'��', L'�b',
	L'�[', L'�A', L'�C', L'�E', L'�G', L'�I', L'�J', L'�L',
	L'�N', L'�P', L'�R', L'�T', L'�V', L'�X', L'�Z', L'�\',
	L'�^', L'�`', L'�c', L'�e', L'�g', L'�i', L'�j', L'�k',
	L'�l', L'�m', L'�n', L'�q', L'�t', L'�w', L'�z', L'�}',
	L'�~', L'��', L'��', L'��', L'��', L'��', L'��', L'��',
	L'��', L'��', L'��', L'��', L'��', L'��', L'�J', L'�K',
	L'�E', L'�E', L'�E', L'�E', L'�E', L'�E', L'�E', L'�E',
	L'�E', L'�E', L'�E', L'�E', L'�E', L'�E', L'�E', L'�E',
	L'�E', L'�E', L'�E', L'�E', L'�E', L'�E', L'�E', L'�E',
	L'�E', L'�E', L'�E', L'�E', L'�E', L'�E', L'�E'
};

struct GAIJI_TABLE{
	const WCHAR* strCharUnicode;
	const WCHAR* strChar;
};

static const GAIJI_TABLE GaijiTable[]={
	{L"\x2491", L"10."},
	{L"\x2492", L"11."},
	{L"\x2493", L"12."},
	{L"\xD83C\xDD4A", L"[HV]"}, //90��48�_
	{L"\xD83C\xDD4C", L"[SD]"},
	{L"\xD83C\xDD3F", L"[�o]"},
	{L"\xD83C\xDD46", L"[�v]"},
	{L"\xD83C\xDD4B", L"[MV]"},
	{L"\xD83C\xDE10", L"[��]"},
	{L"\xD83C\xDE11", L"[��]"},
	{L"\xD83C\xDE12", L"[�o]"},
	{L"\xD83C\xDE13", L"[�f]"},
	{L"\xD83C\xDD42", L"[�r]"},
	{L"\xD83C\xDE14", L"[��]"},
	{L"\xD83C\xDE15", L"[��]"},
	{L"\xD83C\xDE16", L"[��]"},
	{L"\xD83C\xDD4D", L"[SS]"},
	{L"\xD83C\xDD31", L"[�a]"},
	{L"\xD83C\xDD3D", L"[�m]"},//
	{L"\x2B1B", L"��"},//90��64�_
	{L"\x2B24", L"��"},
	{L"\xD83C\xDE17", L"[�V]"},
	{L"\xD83C\xDE18", L"[��]"},
	{L"\xD83C\xDE19", L"[�f]"},
	{L"\xD83C\xDE1A", L"[��]"},
	{L"\xD83C\xDE1B", L"[��]"},
	{L"\x26BF", L"[�E]"},
	{L"\xD83C\xDE1C", L"[�O]"},
	{L"\xD83C\xDE1D", L"[��]"},
	{L"\xD83C\xDE1E", L"[��]"},
	{L"\xD83C\xDE1F", L"[�V]"},
	{L"\xD83C\xDE20", L"[��]"},
	{L"\xD83C\xDE21", L"[�I]"},
	{L"\xD83C\xDE22", L"[��]"},
	{L"\xD83C\xDE23", L"[��]"},
	{L"\xD83C\xDE24", L"[��]"},//90��80�_
	{L"\xD83C\xDE25", L"[��]"},
	{L"\xD83C\xDD4E", L"[PPV]"},
	{L"\x3299", L"(��)"},
	{L"\xD83C\xDE00", L"�ق�"},
	//91��͔�΂�
	{L"\x27A1", L"��"},//92��1�_
	{L"\x2B05", L"��"},
	{L"\x2B06", L"��"},
	{L"\x2B07", L"��"},
	{L"\x2B2F", L"��"},
	{L"\x2B2E", L"��"},
	{L"�N", L"�N"},
	{L"��", L"��"},
	{L"��", L"��"},
	{L"�~", L"�~"},
	{L"\x33A1", L"m^2"},
	{L"\x33A5", L"m^3"},
	{L"\x339D", L"cm"},
	{L"\x33A0", L"cm^2"},
	{L"\x33A4", L"cm^3"},
	{L"\xD83C\xDD00", L"�O."},//92��16�_
	{L"\x2488", L"�P."},
	{L"\x2489", L"�Q."},
	{L"\x248A", L"�R."},
	{L"\x248B", L"�S."},
	{L"\x248C", L"�T."},
	{L"\x248D", L"�U."},
	{L"\x248E", L"�V."},
	{L"\x248F", L"�W."},
	{L"\x2490", L"�X."},
	{L"��", L"��"},
	{L"��", L"��"},
	{L"��", L"��"},
	{L"��", L"��"},
	{L"�O", L"�O"},
	{L"�V", L"�V"},
	{L"\xD83C\xDD01", L"�O,"},//92��32�_
	{L"\xD83C\xDD02", L"�P,"},
	{L"\xD83C\xDD03", L"�Q,"},
	{L"\xD83C\xDD04", L"�R,"},
	{L"\xD83C\xDD05", L"�S,"},
	{L"\xD83C\xDD06", L"�T,"},
	{L"\xD83C\xDD07", L"�U,"},
	{L"\xD83C\xDD08", L"�V,"},
	{L"\xD83C\xDD09", L"�W,"},
	{L"\xD83C\xDD0A", L"�X,"},
	{L"\x3233", L"[��]"},
	{L"\x3236", L"[��]"},
	{L"\x3232", L"[�L]"},
	{L"\x3231", L"[��]"},
	{L"\x3239", L"[��]"},
	{L"\x3244", L"(��)"},
	{L"\x25B6", L"��"},//92��48�_
	{L"\x25C0", L"��"},
	{L"\x3016", L"�y"},
	{L"\x3017", L"�z"},
	{L"\x27D0", L"��"},
	{L"\x00B2", L"^2"},
	{L"\x00B3", L"^3"},
	{L"\xD83C\xDD2D", L"(CD)"},
	{L"(vn)", L"(vn)"},
	{L"(ob)", L"(ob)"},
	{L"(cb)", L"(cb)"},
	{L"(ce", L"(ce"},
	{L"mb)", L"mb)"},
	{L"(hp)", L"(hp)"},
	{L"(br)", L"(br)"},
	{L"(��)", L"(��)"},
	{L"(��)", L"(��)"},//92��64�_
	{L"(ms)", L"(ms)"},
	{L"(��)", L"(��)"},
	{L"(bs)", L"(bs)"},
	{L"(��)", L"(��)"},
	{L"(tb)", L"(tb)"},
	{L"(tp)", L"(tp)"},
	{L"(ds)", L"(ds)"},
	{L"(ag)", L"(ag)"},
	{L"(eg)", L"(eg)"},
	{L"(vo)", L"(vo)"},
	{L"(fl)", L"(fl)"},
	{L"(ke", L"(ke"},
	{L"y)", L"y)"},
	{L"(sa", L"(sa"},
	{L"x)", L"x)"},
	{L"(sy", L"(sy"},//92��80�_
	{L"n)", L"n)"},
	{L"(or", L"(or"},
	{L"g)", L"g)"},
	{L"(pe", L"(pe"},
	{L"r)", L"r)"},
	{L"\xD83C\xDD2C", L"(�q)"},
	{L"\xD83C\xDD2B", L"(�b)"},
	{L"\x3247", L"(�)"},
	{L"\xD83C\xDD90", L"�c�i"},
	{L"\xD83C\xDE26", L"[��]"},
	{L"\x213B", L"Fax"},
	{L"\x322A", L"(��)"},//93��1�_
	{L"\x322B", L"(��)"},
	{L"\x322C", L"(��)"},
	{L"\x322D", L"(��)"},
	{L"\x322E", L"(��)"},
	{L"\x322F", L"(�y)"},
	{L"\x3230", L"(��)"},
	{L"\x3237", L"(�j)"},
	{L"��", L"��"},
	{L"��", L"��"},
	{L"��", L"��"},
	{L"�~", L"�~"},
	{L"\x2116", L"No."},
	{L"\x2121", L"Tel"},
	{L"\x3036", L"(��)"},
	{L"\x26BE", L"()()"},//93��16�_
	{L"\xD83C\xDE40", L"[�{]"},
	{L"\xD83C\xDE41", L"[�O]"},
	{L"\xD83C\xDE42", L"[��]"},
	{L"\xD83C\xDE43", L"[��]"},
	{L"\xD83C\xDE44", L"[�_]"},
	{L"\xD83C\xDE45", L"[��]"},
	{L"\xD83C\xDE46", L"[��]"},
	{L"\xD83C\xDE47", L"[��]"},
	{L"\xD83C\xDE48", L"[�s]"},
	{L"\xD83C\xDD2A", L"[�r]"},
	{L"\xD83C\xDE27", L"[��]"},
	{L"\xD83C\xDE28", L"[��]"},
	{L"\xD83C\xDE29", L"[��]"},
	{L"\xD83C\xDE14", L"[��]"},
	{L"\xD83C\xDE2A", L"[�O]"},
	{L"\xD83C\xDE2B", L"[�V]"},//93��32�_
	{L"\xD83C\xDE2C", L"[��]"},
	{L"\xD83C\xDE2D", L"[��]"},
	{L"\xD83C\xDE2E", L"[�E]"},
	{L"\xD83C\xDE2F", L"[�w]"},
	{L"\xD83C\xDE30", L"[��]"},
	{L"\xD83C\xDE31", L"[��]"},
	{L"\x2113", L"l"},
	{L"\x338F", L"kg"},
	{L"\x3390", L"Hz"},
	{L"\x33CA", L"ha"},
	{L"\x339E", L"km"},
	{L"\x33A2", L"km^2"},
	{L"\x3371", L"hPa"},
	{L"�E", L"�E"},
	{L"�E", L"�E"},
	{L"\x00BD", L"1/2"},//93��48�_
	{L"\x2189", L"0/3"},
	{L"\x2153", L"1/3"},
	{L"\x2154", L"2/3"},
	{L"\x00BC", L"1/4"},
	{L"\x00BE", L"3/4"},
	{L"\x2155", L"1/5"},
	{L"\x2156", L"2/5"},
	{L"\x2157", L"3/5"},
	{L"\x2158", L"4/5"},
	{L"\x2159", L"1/6"},
	{L"\x215A", L"5/6"},
	{L"\x2150", L"1/7"},
	{L"\x215B", L"1/8"},
	{L"\x2151", L"1/9"},
	{L"\x2152", L"1/10"},
	{L"\x203C", L"!!"},//93��78�_
	{L"\x2049", L"!?"},
	{L"�T", L"�T"},//94��1�_
	{L"�U", L"�U"},
	{L"�V", L"�V"},
	{L"�W", L"�W"},
	{L"�X", L"�X"},
	{L"�Y", L"�Y"},
	{L"�Z", L"�Z"},
	{L"�[", L"�["},
	{L"�\", L"�\"},
	{L"�]", L"�]"},
	{L"\x216A", L"XI"},
	{L"\x216B", L"XII"},
	{L"�P", L"�P"},
	{L"�Q", L"�Q"},
	{L"�R", L"�R"},
	{L"�S", L"�S"},//94��16�_
	{L"\x2474", L"(�P)"},
	{L"\x2475", L"(�Q)"},
	{L"\x2476", L"(�R)"},
	{L"\x2477", L"(�S)"},
	{L"\x2478", L"(�T)"},
	{L"\x2479", L"(�U)"},
	{L"\x247A", L"(�V)"},
	{L"\x247B", L"(�W)"},
	{L"\x247C", L"(�X)"},
	{L"\x247D", L"(10)"},
	{L"\x247E", L"(11)"},
	{L"\x247F", L"(12)"},
	{L"\x3251", L"(21)"},
	{L"\x3252", L"(22)"},
	{L"\x3253", L"(23)"},
	{L"\x3254", L"(24)"},//94��32�_
	{L"\xD83C\xDD10", L"(�`)"},
	{L"\xD83C\xDD11", L"(�a)"},
	{L"\xD83C\xDD12", L"(�b)"},
	{L"\xD83C\xDD13", L"(�c)"},
	{L"\xD83C\xDD14", L"(�d)"},
	{L"\xD83C\xDD15", L"(�e)"},
	{L"\xD83C\xDD16", L"(�f)"},
	{L"\xD83C\xDD17", L"(�g)"},
	{L"\xD83C\xDD18", L"(�h)"},
	{L"\xD83C\xDD19", L"(�i)"},
	{L"\xD83C\xDD1A", L"(�j)"},
	{L"\xD83C\xDD1B", L"(�k)"},
	{L"\xD83C\xDD1C", L"(�l)"},
	{L"\xD83C\xDD1D", L"(�m)"},
	{L"\xD83C\xDD1E", L"(�n)"},
	{L"\xD83C\xDD1F", L"(�o)"},//94��48�_
	{L"\xD83C\xDD20", L"(�p)"},
	{L"\xD83C\xDD21", L"(�q)"},
	{L"\xD83C\xDD22", L"(�r)"},
	{L"\xD83C\xDD23", L"(�s)"},
	{L"\xD83C\xDD24", L"(�t)"},
	{L"\xD83C\xDD25", L"(�u)"},
	{L"\xD83C\xDD26", L"(�v)"},
	{L"\xD83C\xDD27", L"(�w)"},
	{L"\xD83C\xDD28", L"(�x)"},
	{L"\xD83C\xDD29", L"(�y)"},
	{L"\x3255", L"(25)"},
	{L"\x3256", L"(26)"},
	{L"\x3257", L"(27)"},
	{L"\x3258", L"(28)"},
	{L"\x3259", L"(29)"},
	{L"\x325A", L"(30)"},//94��64�_
	{L"�@", L"�@"},
	{L"�A", L"�A"},
	{L"�B", L"�B"},
	{L"�C", L"�C"},
	{L"�D", L"�D"},
	{L"�E", L"�E"},
	{L"�F", L"�F"},
	{L"�G", L"�G"},
	{L"�H", L"�H"},
	{L"�I", L"�I"},
	{L"�J", L"�J"},
	{L"�K", L"�K"},
	{L"�L", L"�L"},
	{L"�M", L"�M"},
	{L"�N", L"�N"},
	{L"�O", L"�O"},//94��80�_
	{L"\x2776", L"(�P)"},
	{L"\x2777", L"(�Q)"},
	{L"\x2778", L"(�R)"},
	{L"\x2779", L"(�S)"},
	{L"\x277A", L"(�T)"},
	{L"\x277B", L"(�U)"},
	{L"\x277C", L"(�V)"},
	{L"\x277D", L"(�W)"},
	{L"\x277E", L"(�X)"},
	{L"\x277F", L"(10)"},
	{L"\x24EB", L"(11)"},
	{L"\x24EC", L"(12)"},
	{L"\x325B", L"(31)"}
};

static const GAIJI_TABLE GaijiTbl2[]={
	{L"\x3402", L"��"},
	{L"\xD840\xDD58", L"��"},
	{L"\x4EFD", L"��"},
	{L"\x4EFF", L"�f"},
	{L"�q", L"�q"},
	{L"�a", L"�a"},
	{L"\x509C", L"�s"},
	{L"\x511E", L"��"},
	{L"\x51BC", L"��"},
	{L"\x351F", L"��"}, //10
	{L"��", L"��"},
	{L"\x5361", L"��"},
	{L"\x536C", L"��"},
	{L"��", L"��"},
	{L"\xD842\xDFB7", L"�g"},
	{L"\x544D", L"��"},
	{L"\x5496", L"��"},
	{L"��", L"��"},
	{L"��", L"��"},
	{L"\x550E", L"��"}, //20
	{L"\x554A", L"��"},
	{L"\x5672", L"��"},
	{L"\x56E4", L"��"},
	{L"\x5733", L"��"},
	{L"\x5734", L"��"},
	{L"��", L"��"},
	{L"\x5880", L"��"},
	{L"\x59E4", L"��"},
	{L"\x5A23", L"��"},
	{L"\x5A55", L"��"}, //30
	{L"��", L"��"},
	{L"��", L"��"},
	{L"\x37E2", L"��"},
	{L"\x5EAC", L"��"},
	{L"��", L"��"},
	{L"�g", L"�g"},
	{L"��", L"��"},
	{L"\x6017", L"��"},
	{L"\xFA6B", L"�b"},
	{L"��", L"��"}, //40
	{L"��", L"��"},
	{L"\x66C8", L"��"},
	{L"��", L"��"},
	{L"��", L"��"},
	{L"�f", L"�f"},
	{L"��", L"��"},
	{L"\x9FC4", L"��"},
	{L"\x6911", L"��"},
	{L"\x693B", L"��"},
	{L"\x6A45", L"��"}, //50
	{L"\x6A91", L"��"},
	{L"��", L"��"},
	{L"\xD84C\xDFCC", L"��"},
	{L"\xD84C\xDFFE", L"��"},
	{L"\xD84D\xDDC4", L"��"},
	{L"\x6BF1", L"��"},
	{L"\x6CE0", L"��"},
	{L"\x6D2E", L"��"},
	{L"\xFA45", L"�C"},
	{L"\x6DBF", L"��"}, //60
	{L"\x6DCA", L"��"},
	{L"�C", L"�C"},
	{L"\xFA46", L"��"},
	{L"\x6F5E", L"��"},
	{L"\x6FF9", L"��"},
	{L"\x7064", L"��"},
	{L"\xFA6C", L"��"},
	{L"\xD850\xDEEE", L"��"},
	{L"�W", L"�W"},
	{L"�Y", L"�Y"}, //70
	{L"\x7200", L"��"},
	{L"\x739F", L"��"},
	{L"\x73A8", L"��"},
	{L"�a", L"�a"},
	{L"�b", L"�b"},
	{L"\x741B", L"��"},
	{L"\x7421", L"��"},
	{L"\xFA4A", L"��"},
	{L"�g", L"�g"},
	{L"�h", L"�h"}, //80
	{L"\x742C", L"��"},
	{L"\x7439", L"��"},
	{L"\x744B", L"��"},
	{L"\x3EDA", L"��"},
	{L"\x7575", L"�`"},
	{L"\x7581", L"��"},
	{L"\x7772", L"��"},
	{L"\x4093", L"��"},
	{L"\x78C8", L"��"},
	{L"\x78E0", L"��"}, //90
	{L"�_", L"�_"},
	{L"�X", L"�X"},
	{L"\x9FC6", L"��"},
	{L"\x4103", L"��"},
	{L"\x9FC5", L"��"},
	{L"\x79DA", L"��"},
	{L"\x7A1E", L"��"},
	{L"\x7B7F", L"��"},
	{L"\x7C31", L"��"},
	{L"\x4264", L"��"}, //100
	{L"\x7D8B", L"��"},
	{L"��", L"��"},
	{L"\x8118", L"��"},
	{L"\x813A", L"��"},
	{L"\xFA6D", L"��"},
	{L"\x82AE", L"��"},
	{L"��", L"��"},
	{L"�`", L"�`"},
	{L"�H", L"�H"},
	{L"��", L"��"}, //110
	{L"\x85CE", L"��"},
	{L"�I", L"�I"},
	{L"\x87EC", L"��"},
	{L"\x880B", L"��"},
	{L"��", L"��"},
	{L"�p", L"�p"},
	{L"��", L"��"},
	{L"\x8DCE", L"��"},
	{L"��", L"��"},
	{L"\x8FF6", L"��"}, //120
	{L"\x90DD", L"��"},
	{L"��", L"��"},
	{L"�A", L"�A"},
	{L"\x91B2", L"��"},
	{L"\x9233", L"��"},
	{L"�_", L"�_"},
	{L"��", L"��"},
	{L"�^", L"�^"},
	{L"��", L"��"},
	{L"\x96DE", L"��"}, //130
	{L"�L", L"�L"},
	{L"\x9940", L"��"},
	{L"��", L"��"},
	{L"�I", L"�I"},
	{L"\x9DD7", L"��"},
	{L"\x9EB4", L"��"},
	{L"\x9EB5", L"��"}
};

//�f�t�H���g�}�N����(NUL�͌��ʂ��Ȃ��ƋK�肳��Ă���)
static const BYTE DefaultMacro[][20]={
	{ 0x1B,0x24,0x39,0x1B,0x29,0x4A,0x1B,0x2A,0x30,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x24,0x39,0x1B,0x29,0x31,0x1B,0x2A,0x30,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x24,0x39,0x1B,0x29,0x20,0x41,0x1B,0x2A,0x30,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x32,0x1B,0x29,0x34,0x1B,0x2A,0x35,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x32,0x1B,0x29,0x33,0x1B,0x2A,0x35,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x32,0x1B,0x29,0x20,0x41,0x1B,0x2A,0x35,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x20,0x41,0x1B,0x29,0x20,0x42,0x1B,0x2A,0x20,0x43,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x20,0x44,0x1B,0x29,0x20,0x45,0x1B,0x2A,0x20,0x46,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x20,0x47,0x1B,0x29,0x20,0x48,0x1B,0x2A,0x20,0x49,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x20,0x4A,0x1B,0x29,0x20,0x4B,0x1B,0x2A,0x20,0x4C,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x20,0x4D,0x1B,0x29,0x20,0x4E,0x1B,0x2A,0x20,0x4F,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x24,0x39,0x1B,0x29,0x20,0x42,0x1B,0x2A,0x30,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x24,0x39,0x1B,0x29,0x20,0x43,0x1B,0x2A,0x30,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x24,0x39,0x1B,0x29,0x20,0x44,0x1B,0x2A,0x30,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x31,0x1B,0x29,0x30,0x1B,0x2A,0x4A,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D },
	{ 0x1B,0x28,0x4A,0x1B,0x29,0x32,0x1B,0x2A,0x20,0x41,0x1B,0x2B,0x20,0x70,0x0F,0x1B,0x7D }
};

#endif //ARIB8CHAR_DECODE_H_IMPLEMENT_TABLE

//�����T�C�Y
typedef enum{
	STR_SMALL = 0, //SSZ
	STR_MEDIUM, //MSZ
	STR_NORMAL, //NSZ
	STR_MICRO, //SZX 0x60
	STR_HIGH_W, //SZX 0x41
	STR_WIDTH_W, //SZX 0x44
	STR_W, //SZX 0x45
	STR_SPECIAL_1, //SZX 0x6B
	STR_SPECIAL_2, //SZX 0x64
} STRING_SIZE;

struct CAPTION_CHAR_DATA{
	wstring strDecode;
	STRING_SIZE emCharSizeMode;

	CLUT_DAT stCharColor;
	CLUT_DAT stBackColor;
	CLUT_DAT stRasterColor;

	BOOL bUnderLine;
	BOOL bShadow;
	BOOL bBold;
	BOOL bItalic;
	BYTE bFlushMode;

	WORD wCharW;
	WORD wCharH;
	WORD wCharHInterval;
	WORD wCharVInterval;
};

struct CAPTION_DATA{
	BOOL bClear;
	WORD wSWFMode;
	WORD wClientX;
	WORD wClientY;
	WORD wClientW;
	WORD wClientH;
	WORD wPosX;
	WORD wPosY;
	vector<CAPTION_CHAR_DATA> CharList;
	DWORD dwWaitTime;
};

class CARIB8CharDecode
{
public:
	static const WCHAR* TELETEXT_MARK;

	//PSI/SI��z�肵��wstring�ւ̕ϊ�
	BOOL PSISI( const BYTE* pbSrc, DWORD dwSrcSize, wstring* strDec );
	//������z�肵��wstring�ւ̕ϊ�
	BOOL Caption( const BYTE* pbSrc, DWORD dwSrcSize, vector<CAPTION_DATA>* pCaptionList );

protected:
	struct MF_MODE{
		int iMF; //���������W��
		int iMode; //�����W���̕���
		int iByte; //�ǂݍ��݃o�C�g��
	};

	BOOL m_bPSI;

	MF_MODE m_G0;
	MF_MODE m_G1;
	MF_MODE m_G2;
	MF_MODE m_G3;
	MF_MODE* m_GL;
	MF_MODE* m_GR;

	//�f�R�[�h����������
	wstring m_strDecode;
	//�����T�C�Y
	STRING_SIZE m_emStrSize;

	//CLUT�̃C���f�b�N�X
	BYTE m_bCharColorIndex;
	BYTE m_bBackColorIndex;
	BYTE m_bRasterColorIndex;
	BYTE m_bDefPalette;

	BOOL m_bUnderLine;
	BOOL m_bShadow;
	BOOL m_bBold;
	BOOL m_bItalic;
	BYTE m_bFlushMode;

	//�\������
	WORD m_wSWFMode;
	WORD m_wClientX;
	WORD m_wClientY;
	WORD m_wClientW;
	WORD m_wClientH;
	WORD m_wPosX;
	WORD m_wPosY;
	WORD m_wCharW;
	WORD m_wCharH;
	WORD m_wCharHInterval;
	WORD m_wCharVInterval;
	WORD m_wMaxChar;

	DWORD m_dwWaitTime;

	vector<CAPTION_DATA>* m_pCaptionList;
#ifndef _WIN32
	static const WCHAR m_jisTable[84 * 94 + 1];
#endif
protected:
	void InitPSISI(void);
	void InitCaption(void);
	BOOL Analyze( const BYTE* pbSrc, DWORD dwSrcSize, DWORD* pdwReadSize );

	BOOL IsSmallCharMode(void);
	BOOL IsChgPos(void);
	void CreateCaptionData(CAPTION_DATA* pItem);
	void CreateCaptionCharData(CAPTION_CHAR_DATA* pItem);
	void CheckModify(void);

	//���䕄��
	BOOL C0( const BYTE* pbSrc, DWORD dwSrcSize, DWORD* pdwReadSize );
	BOOL C1( const BYTE* pbSrc, DWORD dwSrcSize, DWORD* pdwReadSize );
	BOOL GL_GR( const BYTE* pbSrc, DWORD dwSrcSize, DWORD* pdwReadSize, const MF_MODE* mode );
	//�G�X�P�[�v�V�[�P���X
	BOOL ESC( const BYTE* pbSrc, DWORD dwSrcSize, DWORD* pdwReadSize );
	//�Q�o�C�g�����ϊ�
	BOOL ToSJIS( const BYTE bFirst, const BYTE bSecond );
	BOOL ToCustomFont( const BYTE bFirst, const BYTE bSecond );

	BOOL CSI( const BYTE* pbSrc, DWORD dwSrcSize, DWORD* pdwReadSize );

};
