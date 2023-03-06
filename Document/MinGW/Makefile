ifeq ($(OS),Windows_NT)
	TARGETS = EpgDataCap_Bon \
              EpgDataCap3 \
              EpgTimerAdminProxy \
              EpgTimerPlugIn \
              RecName_Macro \
              SendTSTCP \
              Write_Default \
              Write_OneService \
              EdcbPlugIn \
              EpgTimerSrv
else
	TARGETS = EpgDataCap_Bon \
              EpgDataCap3 \
              RecName_Macro \
              SendTSTCP \
              Write_Default \
              EpgTimerSrv
endif

ifeq ($(OS),Windows_NT)
	MUNICODE := -municode
else
	MUNICODE :=
endif

all:
ifeq ($(OS),Windows_NT)
	asyncbuf.exe relayread.exe tsidmove.exe cp_dep $(addsuffix .all, $(TARGETS))
else
	$(MAKE) $(addsuffix .all, $(TARGETS))
endif

clean:
ifeq ($(OS),Windows_NT)
	$(addsuffix .clean, $(TARGETS)) rm_dep rm_tools
else
	$(MAKE) $(addsuffix .clean, $(TARGETS))
endif

%.all:
	$(MAKE) -C ../../$*/$* TARGET=../../Document/MinGW/$*
%.clean:
	$(MAKE) -C ../../$*/$* TARGET=$(if $(MSYSTEM),../../Document/MinGW/$*,..\..\Document\MinGW\$*) clean
cp_dep:
ifdef MINGW_PREFIX
	cp -n $(MINGW_PREFIX)/bin/libwinpthread-1.dll .
endif
rm_dep:
	$(RM) libwinpthread-1.dll
asyncbuf.exe: ../../ini/Tools/asyncbuf.c
	$(CC) $(MUNICODE) -Wl,-s -static -D_UNICODE -Os -o $@ $<
relayread.exe: ../../ini/Tools/relayread.c
	$(CC) $(MUNICODE) -Wl,-s -static -D_UNICODE -Os -o $@ $<
tsidmove.exe: ../../Common/ParseTextInstances.cpp \
              ../../Common/PathUtil.cpp \
              ../../Common/StringUtil.cpp \
              ../../Common/TimeUtil.cpp \
              ../../ini/Tools/tsidmove/tsidmove.cpp
	$(CXX) $(MUNICODE) -Wl,-s -static-libgcc -static-libstdc++ -I../../ini/Tools/tsidmove -D_UNICODE -std=c++17 -pedantic-errors -Os -o $@ $+
rm_tools:
	$(RM) asyncbuf.exe relayread.exe tsidmove.exe
