#!/bin/bash

# とりあえずビルドして動かすための暫定的なもの (手元環境の Ubuntu 20.04 向け、Ubuntu 22.04 では動かないかも)
# ビルドに利用している GCC は 10.3.0 で、これは Ubuntu 20.04 にデフォルトで入っているもの
# $ ./LinuxDebugBuild.sh rebuild と実行すると、ビルド成果物をすべて削除してから再度ビルドが実行される

# 一応動くがまともに動作していない箇所も多く、今のところ実用性はない…
# 2023/03/13 22:00 時点のコードでは EpgTimerSrv に起動ガチャがあり、起動ガチャに成功しなければ EpgTimerNW から接続できない…

# 実行前に、/path/to/EDCB/Debug/ 以下のフォルダに Windows の EDCB 環境から Setting/ 以下のフォルダ (EPG データも含まれる) と
# Common.ini / EpgDataCap_Bon.ini / EpgTimerSrv.ini がコピーされていることが前提
# また、各 ini ファイルの記述 (ファイルパスや BonDriver の数/構成) は適宜 Linux 環境向けに修正しておくこと

# BonDriver としては https://github.com/nns779/BonDriver_LinuxPTX を使えるようにしたいところだが、まだ動かせるところまで行っていない…
# PX-W3U4 の場合、BonDriver_LinuxPTX をビルドした上で、BonDriver/ フォルダ以下に BonDriver_PX4-T.so/ini と BonDriver_PX4-S.so/ini として
# リネームした上で配置すると、EDCB 側で一応認識される（単に EpgTimerSrv.ini の設定値を見ているだけかもしれないけど…）

# EpgDataCap_Bon (CLI 版) を実行するには、Debug/ フォルダに移動した上で以下の通り実行する
# $ LD_PRELOAD='/usr/lib/x86_64-linux-gnu/libasan.so.6.0.0' ./EpgDataCap_Bon.elf

# EpgTimerSrv (CLI 版) を実行するには、Debug/ フォルダに移動した上で以下の通り実行する
# $ LD_PRELOAD='/usr/lib/x86_64-linux-gnu/libasan.so.6.0.0' ./EpgTimerSrv.elf

# このほか、VSCode からデバッグすることもできる（はず）

# --------------------------------------------------------------------------------------------

# Debug/ 以下のすべてのファイルの所有者を現在のユーザーに変更する
sudo mkdir -p ../../Debug/
sudo chown -R $USER:$USER ../../Debug/

# サブフォルダを作成
mkdir -p ../../Debug/BonDriver/
mkdir -p ../../Debug/RecName/
mkdir -p ../../Debug/Write/

# Debug/ 以下の .ini ファイルと .txt ファイルのパーミッションを 644 に変更する
chmod 644 ../../Debug/*.ini
chmod 644 ../../Debug/*.txt
chmod 644 ../../Debug/Setting/*.ini
chmod 644 ../../Debug/Setting/*.txt

# 別途各自で Windows 環境からコピーしてきた ini ファイルの文字エンコーディング周りの修正

# BonDriver/BonDriver_*.so に実行権限を付与する
chmod +x ../../Debug/BonDriver/BonDriver_*.so

# Common.ini がもし UTF-8 以外だったら、Shift-JIS から UTF-8 に変換する
if [ `file -b --mime-encoding ../../Debug/Common.ini` != "utf-8" ]; then
    iconv -f CP932 -t UTF-8 ../../Debug/Common.ini > ../../Debug/Common.ini.tmp
    mv ../../Debug/Common.ini.tmp ../../Debug/Common.ini
    rm -f ../../Debug/Common.ini.tmp
fi

# Common.ini 内の .exe という記述を .elf に変換する
sed -i -e 's/\.exe/\.elf/g' ../../Debug/Common.ini

# EpgDataCap_Bon.ini 内の LastBon=BonDriver_*.dll という記述を LastBon=BonDriver_*.so に変換する
sed -i 's/LastBon=BonDriver_\([A-Za-z0-9_-]*\).dll/LastBon=BonDriver_\1.so/g' ../../Debug/EpgDataCap_Bon.ini

# EpgTimerSrv.ini がもし UTF-8 以外だったら、UTF-16LE から UTF-8 に変換する
if [ `file -b --mime-encoding ../../Debug/EpgTimerSrv.ini` != "utf-8" ]; then
    iconv -f UTF-16LE -t UTF-8 ../../Debug/EpgTimerSrv.ini > ../../Debug/EpgTimerSrv.ini.tmp
    mv ../../Debug/EpgTimerSrv.ini.tmp ../../Debug/EpgTimerSrv.ini
    rm -f ../../Debug/EpgTimerSrv.ini.tmp
fi

# EpgTimerSrv.ini 内の .dll という記述を .so に変換する
sed -i -e 's/\.dll/\.so/g' ../../Debug/EpgTimerSrv.ini

# Common.ini・EpgDataCap_Bon.ini・EpgTimerSrv.ini の BOM を削除する
sed -i -e '1s/^\xEF\xBB\xBF//' ../../Debug/Common.ini
sed -i -e '1s/^\xEF\xBB\xBF//' ../../Debug/EpgDataCap_Bon.ini
sed -i -e '1s/^\xEF\xBB\xBF//' ../../Debug/EpgTimerSrv.ini

# Setting/*.ChSet4.txt がもし UTF-8 以外だったら、UTF-8 with BOM に変換する
for file in ../../Debug/Setting/*.ChSet4.txt; do
    if [ `file -b --mime-encoding "$file"` != "utf-8" ]; then
        iconv -f CP932 -t UTF-8 "$file" | (echo -ne "\xEF\xBB\xBF"; cat) > "$file.tmp"
        mv "$file.tmp" "$file"
        rm -f "$file.tmp"
    fi
done

# Setting/ChSet5.txt がもし UTF-8 以外だったら、UTF-8 with BOM に変換する
if [ `file -b --mime-encoding ../../Debug/Setting/ChSet5.txt` != "utf-8" ]; then
    iconv -f CP932 -t UTF-8 ../../Debug/Setting/ChSet5.txt | (echo -ne "\xEF\xBB\xBF"; cat) > ../../Debug/Setting/ChSet5.txt.tmp
    mv ../../Debug/Setting/ChSet5.txt.tmp ../../Debug/Setting/ChSet5.txt
    rm -f ../../Debug/Setting/ChSet5.txt.tmp
fi

# --------------------------------------------------------------------------------------------

# Debug/ 以下に Bitrate.ini・BonCtrl.ini・ContentTypeText.txt をコピーする
# なお、Bitrate.ini と BonCtrl.ini は Shift-JIS で保存されているので、iconv で UTF-8 に変換する
iconv -f CP932 -t UTF-8 ../../ini/Bitrate.ini > ../../Debug/Bitrate.ini
iconv -f CP932 -t UTF-8 ../../ini/BonCtrl.ini > ../../Debug/BonCtrl.ini
cp -a ../../ini/ContentTypeText.txt ../../Debug/ContentTypeText.txt

# BonCtrl.ini 内の .dll という記述を .so に変換する
sed -i -e 's/\.dll/\.so/g' ../../Debug/BonCtrl.ini

# /usr/lib/x86_64-linux-gnu/liblua5.2.so (インストールされているはず) を Debug/ 以下にシンボリックリンクを貼る
ln -sf /usr/lib/x86_64-linux-gnu/liblua5.2.so ../../Debug/lua52.so

# Debug/ 以下に HttpPublic フォルダをコピーする
cp -ar ../../ini/HttpPublic/ ../../Debug/

# --------------------------------------------------------------------------------------------

# ビルドに必要なパッケージをインストール (Ubuntu 20.04 向け)
# この他にも build-essential などが必要だが、これらは既にインストールされているものとする
# libasan6 は AddressSanitizer 用のライブラリで、デバッグ用に入れている（不要になったら削除する予定）
sudo apt install -y libasan6 liblua5.2-dev

# 引数に rebuild が指定されている場合は、make clean を実行する
if [ "$1" = "rebuild" ]; then
    make clean
fi

# 今のところ、SendTSTCP.so は移植できていない
# エラー終了を無視する
make -j4 || true

# Debug/ 以下にビルド成果物をコピーする
# 実行ファイルに拡張子がついていないと分かりづらいので、.elf の拡張子をつけている
cp -a EpgDataCap3.so ../../Debug/EpgDataCap3.so
cp -a EpgDataCap3_Unicode.so ../../Debug/EpgDataCap3_Unicode.so
cp -a EpgDataCap_Bon.elf ../../Debug/EpgDataCap_Bon.elf
cp -a EpgTimerSrv.elf ../../Debug/EpgTimerSrv.elf
cp -a RecName_Macro.so ../../Debug/RecName/RecName_Macro.so
cp -a Write_Default.so ../../Debug/Write/Write_Default.so

# 最後に、念押しで Debug/ 以下のすべてのファイルの所有者を現在のユーザーに変更する
sudo chown -R $USER:$USER ../../Debug/
