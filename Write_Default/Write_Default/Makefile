TARGET ?= Write_Default
ifeq ($(OS),Windows_NT)
  CPPFLAGS := -I. -DNDEBUG -D_WINDOWS -DWRITE_DEFAULT_EXPORTS -D_UNICODE -DUNICODE $(CPPFLAGS)
else
  CPPFLAGS := -I. -DNDEBUG -fPIC -DWRITE_DEFAULT_EXPORTS -D_UNICODE -DUNICODE $(CPPFLAGS)
endif
CXXFLAGS := -std=c++17 -pedantic-errors -Os $(CXXFLAGS)
LDFLAGS2 = -Wl,-s,--dynamicbase,--nxcompat,--kill-at -static-libgcc -static-libstdc++ -shared $(LDFLAGS)
LDFLAGS2_LINUX = -Wl,-s -static-libgcc -static-libstdc++ -shared $(LDFLAGS)
WINDRES ?= windres
DEP_ALL_H := $(wildcard ../../BonCtrl/*.h ../../Common/*.h)
CUR_ALL_H := $(wildcard *.h)

build:
ifeq ($(OS),Windows_NT)
	$(MAKE) $(TARGET).dll
else
	$(MAKE) $(TARGET).so
endif

$(TARGET).dll: $(addsuffix .obj, \
  Common__PathUtil.cpp \
  Common__StringUtil.cpp \
  dllmain.cpp \
  SettingDlg.cpp \
  stdafx.cpp \
  WriteMain.cpp \
  Write_Default.cpp \
  Write_Default.rc \
  )
	$(CXX) $(LDFLAGS2) $^ $(LDLIBS) -o $@
$(TARGET).so: $(addsuffix .obj, \
  Common__PathUtil.cpp \
  Common__StringUtil.cpp \
  stdafx.cpp \
  WriteMain.cpp \
  Write_Default.cpp \
  )
	$(CXX) $(LDFLAGS2_LINUX) $^ $(LDLIBS) -o $@
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
	$(RM) stdafx.h.gch *.obj $(TARGET).dll $(TARGET).so $(TARGET).map
