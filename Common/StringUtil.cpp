#include "stdafx.h"
#include "StringUtil.h"

void Format(wstring& strBuff, PRINTF_FORMAT_SZ const WCHAR *format, ...)
{
	va_list params;

	va_start(params, format);

	try{
		vector<WCHAR> buff;
		WCHAR szSmall[256];
		for(;;){
			size_t s = buff.empty() ? 256 : buff.size();
			WCHAR* p = buff.empty() ? szSmall : buff.data();
			va_list copyParams;
#ifdef va_copy
			va_copy(copyParams, params);
#else
			copyParams = params;
#endif
#ifdef _WIN32
			int n = _vsnwprintf_s(p, s, _TRUNCATE, format, copyParams);
#else
			//�؂�̂ĈȊO�̃G���[�ł�-1���Ԃ�(�����ȃp�����[�^�[�n���h���͂Ȃ�)�̂Œ���
			int n = vswprintf(p, s, format, copyParams);
#endif
			va_end(copyParams);
			if( n >= 0 ){
				//�߂�ln���g���̂Ń��A�P�[�X("%c"��'\0'�Ȃ�)�ł͌���ƌ��ʂ��قȂ�
				strBuff.assign(p, n);
				break;
			}
			buff.resize(s * 2);
		}
	}catch(...){
		va_end(params);
		throw;
	}

    va_end(params);
}

void Replace(wstring& strBuff, const wstring& strOld, const wstring& strNew)
{
	wstring::size_type Pos = 0;
	wstring* strWork = &strBuff;
	wstring strForAlias;

	if( strWork == &strOld || strWork == &strNew ){
		strForAlias = strBuff;
		strWork = &strForAlias;
	}
	while ((Pos = strWork->find(strOld,Pos)) != wstring::npos)
	{
		strWork->replace(Pos,strOld.size(),strNew);
		Pos += strNew.size();
	}
	if( strWork == &strForAlias ){
		strBuff = std::move(strForAlias);
	}
}

size_t WtoA(const WCHAR* in, size_t inLen, vector<char>& out, UTIL_CONV_CODE code)
{
#ifdef _WIN32
	if( code != UTIL_CONV_UTF8 ){
		size_t n = WideCharToMultiByte((code == UTIL_CONV_DEFAULT ? 932 : CP_ACP), 0, in, (int)inLen, NULL, 0, NULL, NULL);
		if( out.size() < n + 1 ){
			out.resize(n + 1);
		}
		if( n ){
			n = WideCharToMultiByte((code == UTIL_CONV_DEFAULT ? 932 : CP_ACP), 0, in, (int)inLen, out.data(), (int)n, NULL, NULL);
		}
		out[n] = '\0';
		return strlen(out.data());
	}
#endif
	//WideCharToMultiByte(CP_UTF8)��4bytes�S����r�ς�
	//vector<char> c[2];
	//for( DWORD w = 1; w; w++ ){
	//    WtoA((WCHAR*)&w, 2, c[0], false);
	//    WtoA((WCHAR*)&w, 2, c[1], true);
	//    assert(strcmp(c[0].data(), c[1].data()) == 0);
	//}
#if WCHAR_MAX > 0xFFFF
	if( out.size() < inLen * 4 + 1 ){
		out.resize(inLen * 4 + 1);
#else
	if( out.size() < inLen * 3 + 1 ){
		out.resize(inLen * 3 + 1);
#endif
	}
	size_t n = 0;
	for( size_t i = 0; i < inLen && in[i]; ){
#if WCHAR_MAX > 0xFFFF
		int x = in[i++];
		if( x < 0 || 0x10000 <= x ){
			if( 0x10000 <= x && x < 0x110000 ){
#else
		int x = (WORD)in[i++];
		if( 0xD800 <= x && x < 0xE000 ){
			if( x < 0xDC00 && inLen > i && 0xDC00 <= (WORD)in[i] && (WORD)in[i] < 0xE000 ){
				x = 0x10000 + (x - 0xD800) * 0x400 + ((WORD)in[i++] - 0xDC00);
#endif
				out[n++] = (char)(0xF0 | x >> 18);
				out[n++] = (char)(0x80 | (x >> 12 & 0x3F));
				out[n++] = (char)(0x80 | (x >> 6 & 0x3F));
				out[n++] = (char)(0x80 | (x & 0x3F));
				continue;
			}
			x = 0xFFFD;
		}
		if( x < 0x80 ){
			out[n++] = (char)x;
		}else if( x < 0x800 ){
			out[n++] = (char)(0xC0 | x >> 6);
			out[n++] = (char)(0x80 | (x & 0x3F));
		}else{
			out[n++] = (char)(0xE0 | x >> 12);
			out[n++] = (char)(0x80 | (x >> 6 & 0x3F));
			out[n++] = (char)(0x80 | (x & 0x3F));
		}
	}
	out[n] = '\0';
	return n;
}

size_t AtoW(const char* in, size_t inLen, vector<WCHAR>& out, UTIL_CONV_CODE code)
{
#ifdef _WIN32
	if( code != UTIL_CONV_UTF8 ){
		size_t n = MultiByteToWideChar((code == UTIL_CONV_DEFAULT ? 932 : CP_ACP), 0, in, (int)inLen, NULL, 0);
		if( out.size() < n + 1 ){
			out.resize(n + 1);
		}
		if( n ){
			n = MultiByteToWideChar((code == UTIL_CONV_DEFAULT ? 932 : CP_ACP), 0, in, (int)inLen, out.data(), (int)n);
		}
		out[n] = L'\0';
		return wcslen(out.data());
	}
#endif
	//MultiByteToWideChar(CP_UTF8)��4bytes�S����r�ς�
	//vector<WCHAR> w[2];
	//for( DWORD c = 1; c; c++ ){
	//    AtoW((char*)&c, 4, w[0], false);
	//    AtoW((char*)&c, 4, w[1], true);
	//    //�A������0xFFFD�̐��͕s��
	//    for( int i = 0; i < 2; i++ ){
	//        for( int j = 0; w[i][j]; j++ ){
	//            if( w[i][j] == 0xFFFD && w[i][j + 1] == 0xFFFD ){
	//                w[i].erase(w[i].begin() + (j--));
	//            }
	//        }
	//    }
	//    assert(wcscmp(w[0].data(), w[1].data()) == 0);
	//}
	if( out.size() < inLen + 1 ){
		out.resize(inLen + 1);
	}
	size_t n = 0;
	for( size_t i = 0; i < inLen && in[i]; ){
		int x = (BYTE)in[i++];
		if( 0xC2 <= x && x < 0xE0 && inLen > i && 0x80 <= (BYTE)in[i] && (BYTE)in[i] < 0xC0 ){
			x = (x & 0x1F) << 6 | ((BYTE)in[i++] & 0x3F);
		}else if( 0xE0 <= x && x < 0xF0 && inLen - i > 1 &&
		          0x80 <= (BYTE)in[i] && (BYTE)in[i] < 0xC0 && ((x & 0x0F) || ((BYTE)in[i] & 0x20)) &&
		          0x80 <= (BYTE)in[i + 1] && (BYTE)in[i + 1] < 0xC0 ){
			x = (x & 0x0F) << 12 | ((BYTE)in[i] & 0x3F) << 6 | ((BYTE)in[i + 1] & 0x3F);
			i += 2;
			if( 0xD800 <= x && x < 0xE000 ){
				x = 0xFFFD;
			}
		}else if( 0xF0 <= x && x < 0xF8 && inLen - i > 2 &&
		          0x80 <= (BYTE)in[i] && (BYTE)in[i] < 0xC0 && ((x & 0x07) || ((BYTE)in[i] & 0x30)) &&
		          0x80 <= (BYTE)in[i + 1] && (BYTE)in[i + 1] < 0xC0 &&
		          0x80 <= (BYTE)in[i + 2] && (BYTE)in[i + 2] < 0xC0 ){
			x = (x & 0x07) << 18 | ((BYTE)in[i] & 0x3F) << 12 | ((BYTE)in[i + 1] & 0x3F) << 6 | ((BYTE)in[i + 2] & 0x3F);
			i += 3;
			if( x < 0x110000 ){
#if WCHAR_MAX > 0xFFFF
				out[n++] = (WCHAR)x;
#else
				out[n++] = (WCHAR)((x - 0x10000) / 0x400 + 0xD800);
				out[n++] = (WCHAR)((x - 0x10000) % 0x400 + 0xDC00);
#endif
				continue;
			}
			x = 0xFFFD;
		}else if( x >= 0x80 ){
			x = 0xFFFD;
		}
		out[n++] = (WCHAR)x;
	}
	out[n] = L'\0';
	return n;
}

void WtoA(const wstring& strIn, string& strOut, UTIL_CONV_CODE code)
{
	vector<char> buff;
	size_t len = WtoA(strIn.c_str(), strIn.size(), buff, code);
	strOut.assign(&buff.front(), &buff.front() + len);
}

void AtoW(const string& strIn, wstring& strOut, UTIL_CONV_CODE code)
{
	vector<WCHAR> buff;
	size_t len = AtoW(strIn.c_str(), strIn.size(), buff, code);
	strOut.assign(&buff.front(), &buff.front() + len);
}

bool Separate(const wstring& strIn, const WCHAR* sep, wstring& strLeft, wstring& strRight)
{
	wstring strL(strIn, 0, strIn.find(sep));
	if( strL.size() == strIn.size() ){
		strRight.clear();
		strLeft = std::move(strL);
		return false;
	}
	strRight = strIn.substr(strL.size() + wcslen(sep));
	strLeft = std::move(strL);
	return true;
}

int CompareNoCase(const char* s1, const char* s2)
{
	while( *s1 && ('a' <= *s1 && *s1 <= 'z' ? *s1 - 'a' + 'A' : *s1) ==
	              ('a' <= *s2 && *s2 <= 'z' ? *s2 - 'a' + 'A' : *s2) ){
		s1++;
		s2++;
	}
	return (unsigned char)*s1 - (unsigned char)*s2;
}

int CompareNoCase(const WCHAR* s1, const WCHAR* s2)
{
	while( *s1 && (L'a' <= *s1 && *s1 <= L'z' ? *s1 - L'a' + L'A' : *s1) ==
	              (L'a' <= *s2 && *s2 <= L'z' ? *s2 - L'a' + L'A' : *s2) ){
		s1++;
		s2++;
	}
	return *s1 - *s2;
}
