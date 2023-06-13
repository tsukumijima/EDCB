TARGET ?= EpgTimerSrv
CPPFLAGS := -I. -DNDEBUG -D_WINDOWS -DEPGTIMERSRV_APPLICATION -D_UNICODE -DUNICODE -DUSE_LUA -DUSE_IPV6 -DNO_CGI -DNO_POPEN -DOPENSSL_API_3_0 $(CPPFLAGS)
#CPPFLAGS += -DEPGDB_STD_WREGEX
#CFLAGS := -Os $(CFLAGS)
CFLAGS := -fsanitize=address -g -O0 $(CFLAGS)
#CXXFLAGS := -std=c++17 -pedantic-errors -Os $(CXXFLAGS)
CXXFLAGS := -std=c++17 -pedantic-errors -fsanitize=address -g -O0 $(CXXFLAGS)
LDFLAGS2 = -municode -mwindows -Wl,-s,--dynamicbase,--nxcompat -static-libgcc -static-libstdc++ $(LDFLAGS)
#LDFLAGS2_LINUX = -Wl,-s -static-libgcc -static-libstdc++ $(LDFLAGS)
LDFLAGS2_LINUX = -static-libgcc -static-libstdc++ -g $(LDFLAGS)
#LDLIBS_LINUX := -ldl -pthread -luuid $(LDLIBS)  # LDLIBS が上書きされる前に実行する
LDLIBS_LINUX := -ldl -pthread -luuid -lasan $(LDLIBS)  # LDLIBS が上書きされる前に実行する
LDLIBS := -lws2_32 -lole32 -lcomctl32 -lwinhttp -loleaut32 -lcrypt32 -liphlpapi -luuid -lnetapi32 -ldbghelp $(LDLIBS)
WINDRES ?= windres
DEP_ALL_H := $(wildcard ../../BonCtrl/*.h ../../Common/*.h)
CUR_ALL_H := $(wildcard *.h *.inl)

# Lazy loading fails with segfault if LUA_BUILD_AS_DLL defined.
ifeq ($(OS),Windows_NT)
  LDLIBS := -L. -llua52-mingw-lazy-$(or $(MSYSTEM_CARCH),$(findstring x86_64,$(MAKE_HOST)),i686) $(LDLIBS)
else
  LDLIBS_LINUX := -L. -llua5.2 $(LDLIBS_LINUX)
endif

ifndef MSYSTEM
  # Assume non-pthread threading model
  CPPFLAGS += -DMUST_IMPLEMENT_CLOCK_GETTIME
endif

build:
ifeq ($(OS),Windows_NT)
	$(MAKE) $(TARGET).exe
else
	$(MAKE) $(TARGET).elf
endif

$(TARGET).exe: $(addsuffix .obj, \
  BonCtrl__CreatePATPacket.cpp \
  BonCtrl__PacketInit.cpp \
  Common__CtrlCmdUtil.cpp \
  Common__EpgDataCap3Util.cpp \
  Common__EpgTimerUtil.cpp \
  Common__IniUtil.cpp \
  Common__ParseTextInstances.cpp \
  Common__PathUtil.cpp \
  Common__PipeServer.cpp \
  Common__ReNamePlugInUtil.cpp \
  Common__SendCtrlCmd.cpp \
  Common__SendTSTCPDllUtil.cpp \
  Common__StackTrace.cpp \
  Common__StringUtil.cpp \
  Common__TCPServer.cpp \
  Common__TimeShiftUtil.cpp \
  Common__TimeUtil.cpp \
  Common__TSPacketUtil.cpp \
  BatManager.cpp \
  civetweb.c \
  EpgDBManager.cpp \
  EpgTimerSrv.cpp \
  EpgTimerSrvMain.cpp \
  EpgTimerSrvSetting.cpp \
  EpgTimerTask.cpp \
  HttpServer.cpp \
  NotifyManager.cpp \
  RegExp.cpp \
  ReserveManager.cpp \
  stdafx.cpp \
  SyoboiCalUtil.cpp \
  TunerBankCtrl.cpp \
  UpnpSsdpServer.cpp \
  EpgTimerSrv.rc \
  )
	$(CXX) $(LDFLAGS2) $^ $(LDLIBS) -o $@
$(TARGET).elf: $(addsuffix .obj, \
  $(SOURCES_LUA) \
  BonCtrl__CreatePATPacket.cpp \
  BonCtrl__PacketInit.cpp \
  Common__CtrlCmdUtil.cpp \
  Common__EpgDataCap3Util.cpp \
  Common__EpgTimerUtil.cpp \
  Common__IniUtil.cpp \
  Common__LinuxMessageLoop.cpp \
  Common__ParseTextInstances.cpp \
  Common__PathUtil.cpp \
  Common__PipeServer.cpp \
  Common__ReNamePlugInUtil.cpp \
  Common__SendCtrlCmd.cpp \
  Common__SendTSTCPDllUtil.cpp \
  Common__StackTrace.cpp \
  Common__StringUtil.cpp \
  Common__TCPServer.cpp \
  Common__TimeShiftUtil.cpp \
  Common__TimeUtil.cpp \
  Common__TSPacketUtil.cpp \
  BatManager.cpp \
  civetweb.c \
  EpgDBManager.cpp \
  EpgTimerSrv.cpp \
  EpgTimerSrvMain.cpp \
  EpgTimerSrvSetting.cpp \
  HttpServer.cpp \
  NotifyManager.cpp \
  ReserveManager.cpp \
  stdafx.cpp \
  TunerBankCtrl.cpp \
  UpnpSsdpServer.cpp \
  )
	$(CXX) $(LDFLAGS2_LINUX) $^ $(LDLIBS_LINUX) -o $@
BonCtrl__%.cpp.obj: ../../BonCtrl/%.cpp stdafx.h.gch $(DEP_ALL_H)
	$(COMPILE.cpp) $(OUTPUT_OPTION) $<
Common__%.cpp.obj: ../../Common/%.cpp stdafx.h.gch $(DEP_ALL_H)
	$(COMPILE.cpp) $(OUTPUT_OPTION) $<
%.cpp.obj: %.cpp stdafx.h.gch $(DEP_ALL_H) $(CUR_ALL_H)
	$(COMPILE.cpp) $(OUTPUT_OPTION) $<
stdafx.h.gch: stdafx.h ../../Common/Common.h targetver.h
	$(COMPILE.cpp) $(OUTPUT_OPTION) $<
%.c.obj: %.c $(CUR_ALL_H)
	$(COMPILE.c) $(OUTPUT_OPTION) $<
%.rc.obj: %.rc.u.obj resource.h targetver.h
	$(WINDRES) $(CPPFLAGS) -c 65001 -J rc -O coff $(OUTPUT_OPTION) $<
%.rc.u.obj: %.rc
ifdef MSYSTEM
	iconv -f UTF-16LE -t UTF-8 $< >$@
else
	powershell -NoProfile -Command "&{cat $<|Out-File -Encoding UTF8 $@}"
endif
clean:
	$(RM) stdafx.h.gch *.obj $(TARGET).exe $(TARGET).map
