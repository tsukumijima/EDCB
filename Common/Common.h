#pragma once

// ���ׂẴv���W�F�N�g�ɓK�p�����ǉ��w�b�_����ђ�`

// wprintf�֐��n���K�i�����ɂ���(VC14�ȍ~)�B���C�h������ɂ�%s�łȂ�%ls�Ȃǂ��g������
#define _CRT_STDIO_ISO_WIDE_SPECIFIERS
#define WIN32_LEAN_AND_MEAN
#define NOMINMAX
#include <windows.h>
#include <string>
#include <utility>
#include <map>
#include <vector>
#include <memory>
#include <algorithm>
#include <wchar.h>
#include <stdarg.h>
#include <limits.h>
#include <string.h>

using std::min;
using std::max;
using std::string;
using std::wstring;
using std::pair;
using std::map;
using std::multimap;
using std::vector;

#ifdef __clang__
#pragma clang diagnostic ignored "-Wlogical-op-parentheses"
#pragma clang diagnostic ignored "-Wunused-parameter"
#else
// 'identifier': unreferenced formal parameter
#pragma warning(disable : 4100)

#if defined(_MSC_VER) && _MSC_VER < 1900
// 'class': assignment operator was implicitly defined as deleted
#pragma warning(disable : 4512)
#endif
#endif

// �K�؂łȂ�NULL�̌��o�p
//#undef NULL
//#define NULL nullptr

#ifdef _MSC_VER
#include <sal.h>
#define PRINTF_FORMAT_SZ _In_z_ _Printf_format_string_
#else
#define PRINTF_FORMAT_SZ
#endif

#ifdef WRAP_OUTPUT_DEBUG_STRING
#undef OutputDebugString
#define OutputDebugString OutputDebugStringWrapper
// OutputDebugStringW�̃��b�p�[�֐�
// API�t�b�N�ɂ�鍂�x�Ȃ��̂łȂ��P�Ȃ�u���BOutputDebugStringA��DLL����̌Ăяo���̓��b�v����Ȃ�
void OutputDebugStringWrapper(LPCWSTR lpOutputString);
void SetSaveDebugLog(bool saveDebugLog);
#endif

inline void _OutputDebugString(PRINTF_FORMAT_SZ const WCHAR* format, ...)
{
	// TODO: ���̊֐����͗\�񖼈ᔽ�̏�ɕ���킵���̂ŕύX���ׂ�
	va_list params;
	va_start(params, format);
	// �������铙�G���[���͏���������̓W�J���ȗ�����
	WCHAR buff[1024];
#ifdef _WIN32
	const WCHAR* p = _vsnwprintf_s(buff, 1024, _TRUNCATE, format, params) < 0 ? format : buff;
#else
	const WCHAR* p = vswprintf(buff, 1024, format, params) < 0 ? format : buff;
#endif
	va_end(params);
	OutputDebugString(p);
}

// �K�؂łȂ�����������̌��o�p
//#define _OutputDebugString(...) (void)wprintf_s(__VA_ARGS__)
