
// EpgDataCap_Bon.h : PROJECT_NAME アプリケーションのメイン ヘッダー ファイルです。
//

#pragma once

#include "resource.h"		// メイン シンボル
#include "../../Common/PathUtil.h"
#ifdef _WIN32
#include <windowsx.h>
#include <commctrl.h>
#endif


// CEpgDataCap_BonApp:
// このクラスの実装については、EpgDataCap_Bon.cpp を参照してください。
//

class CEpgDataCap_BonApp
{
public:
	CEpgDataCap_BonApp();

public:
#ifdef _WIN32
	BOOL InitInstance();
#else
	BOOL InitInstance(int argc, char* argv[]);
#endif
};
