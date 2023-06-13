TARGET ?= EpgDataCap_Bon
CPPFLAGS := -I. -DNDEBUG -D_WINDOWS -DEPGDATACAP_BON_APPLICATION -D_UNICODE -DUNICODE $(CPPFLAGS)
#CXXFLAGS := -std=c++17 -pedantic-errors -Os $(CXXFLAGS)
CXXFLAGS := -std=c++17 -pedantic-errors -fsanitize=address -g -O0 $(CXXFLAGS)
LDFLAGS2 = -municode -mwindows -Wl,-s,--dynamicbase,--nxcompat -static-libgcc -static-libstdc++ $(LDFLAGS)
#LDFLAGS2_LINUX = -Wl,-s -static-libgcc -static-libstdc++ $(LDFLAGS)
LDFLAGS2_LINUX = -static-libgcc -static-libstdc++ -g $(LDFLAGS)
#LDLIBS_LINUX := -ldl $(LDLIBS)  # LDLIBS が上書きされる前に実行する
LDLIBS_LINUX := -ldl -lasan $(LDLIBS)  # LDLIBS が上書きされる前に実行する
LDLIBS := -lole32 -lcomctl32 -ldbghelp -lcomdlg32 $(LDLIBS)
WINDRES ?= windres
DEP_ALL_H := $(wildcard ../../BonCtrl/*.h ../../Common/*.h)
CUR_ALL_H := $(wildcard *.h)

build:
ifeq ($(OS),Windows_NT)
	$(MAKE) $(TARGET).exe
else
	$(MAKE) $(TARGET).elf
endif

$(TARGET).exe: $(addsuffix .obj, \
  BonCtrl__BonCtrl.cpp \
  BonCtrl__BonDriverUtil.cpp \
  BonCtrl__CATUtil.cpp \
  BonCtrl__ChSetUtil.cpp \
  BonCtrl__CreatePATPacket.cpp \
  BonCtrl__CreatePMTPacket.cpp \
  BonCtrl__DropCount.cpp \
  BonCtrl__OneServiceUtil.cpp \
  BonCtrl__PacketInit.cpp \
  BonCtrl__PMTUtil.cpp \
  BonCtrl__ScrambleDecoderUtil.cpp \
  BonCtrl__ServiceFilter.cpp \
  BonCtrl__TSOut.cpp \
  BonCtrl__WriteTSFile.cpp \
  Common__CtrlCmdUtil.cpp \
  Common__EpgDataCap3Util.cpp \
  Common__EpgTimerUtil.cpp \
  Common__IniUtil.cpp \
  Common__ParseTextInstances.cpp \
  Common__PathUtil.cpp \
  Common__PipeServer.cpp \
  Common__SendCtrlCmd.cpp \
  Common__SendTSTCPDllUtil.cpp \
  Common__StackTrace.cpp \
  Common__StringUtil.cpp \
  Common__TimeUtil.cpp \
  Common__TSBuffUtil.cpp \
  Common__TSPacketUtil.cpp \
  Common__WritePlugInUtil.cpp \
  EpgDataCap_Bon.cpp \
  EpgDataCap_BonDlg.cpp \
  SetDlgApp.cpp \
  SetDlgAppBtn.cpp \
  SetDlgBasic.cpp \
  SetDlgEpg.cpp \
  SetDlgNetwork.cpp \
  SetDlgService.cpp \
  SettingDlg.cpp \
  stdafx.cpp \
  TaskbarList.cpp \
  EpgDataCap_Bon.rc \
  )
	$(CXX) $(LDFLAGS2) $^ $(LDLIBS) -o $@

$(TARGET).elf: $(addsuffix .obj, \
  BonCtrl__BonCtrl.cpp \
  BonCtrl__BonDriverUtil.cpp \
  BonCtrl__CATUtil.cpp \
  BonCtrl__ChSetUtil.cpp \
  BonCtrl__CreatePATPacket.cpp \
  BonCtrl__CreatePMTPacket.cpp \
  BonCtrl__DropCount.cpp \
  BonCtrl__OneServiceUtil.cpp \
  BonCtrl__PacketInit.cpp \
  BonCtrl__PMTUtil.cpp \
  BonCtrl__ScrambleDecoderUtil.cpp \
  BonCtrl__ServiceFilter.cpp \
  BonCtrl__TSOut.cpp \
  BonCtrl__WriteTSFile.cpp \
  Common__CtrlCmdUtil.cpp \
  Common__EpgDataCap3Util.cpp \
  Common__EpgTimerUtil.cpp \
  Common__IniUtil.cpp \
  Common__LinuxMessageLoop.cpp \
  Common__ParseTextInstances.cpp \
  Common__PathUtil.cpp \
  Common__PipeServer.cpp \
  Common__SendCtrlCmd.cpp \
  Common__SendTSTCPDllUtil.cpp \
  Common__StackTrace.cpp \
  Common__StringUtil.cpp \
  Common__TimeUtil.cpp \
  Common__TSBuffUtil.cpp \
  Common__TSPacketUtil.cpp \
  Common__WritePlugInUtil.cpp \
  stdafx.cpp \
  EpgDataCap_Bon.cpp \
  EpgDataCap_BonDlg.cpp \
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
