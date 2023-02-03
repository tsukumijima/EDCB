TARGET ?= SendTSTCP
ifeq ($(OS),Windows_NT)
  CPPFLAGS := -I. -DNDEBUG -D_WINDOWS -DSENDTSTCP_EXPORTS -D_UNICODE -DUNICODE $(CPPFLAGS)
else
  CPPFLAGS := -I. -DNDEBUG -fPIC -DSENDTSTCP_EXPORTS -D_UNICODE -DUNICODE $(CPPFLAGS)
endif
CXXFLAGS := -std=c++17 -pedantic-errors -Os $(CXXFLAGS)
LDFLAGS2 = -Wl,-s,--dynamicbase,--nxcompat,--kill-at -static-libgcc -static-libstdc++ -shared $(LDFLAGS)
LDFLAGS2_LINUX = -Wl,-s -static-libgcc -static-libstdc++ -shared $(LDFLAGS)
LDLIBS := -lws2_32 $(LDLIBS)
WINDRES ?= windres
DEP_ALL_H := $(wildcard ../../BonCtrl/*.h ../../Common/*.h)
CUR_ALL_H := $(wildcard *.h)

$(TARGET).dll: $(addsuffix .obj, \
  Common__StringUtil.cpp \
  dllmain.cpp \
  SendTSTCP.cpp \
  SendTSTCPMain.cpp \
  stdafx.cpp \
  SendTSTCP.rc \
  )
$(TARGET).so: $(addsuffix .obj, \
  Common__StringUtil.cpp \
  SendTSTCP.cpp \
  SendTSTCPMain.cpp \
  stdafx.cpp \
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
