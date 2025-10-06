#!/bin/bash

## Architecture
##  -a x86|x64
## Output directory
##  -o dir
## Archive
##  -r 7z|bz2
## Target
##  -t debug|release

# いずれかのコマンドがエラーになったら直ちに終了する
set -euo pipefail

arch=
target=release
out_dir=package
archive=7z

while getopts a:c:o:p:r:t: arg
do
    case $arg in
    a)
        arch=$OPTARG
        ;;
    o)
        out_dir=$OPTARG
        ;;
    r)
        archive=$OPTARG
        ;;
    t)
        target=$OPTARG
        ;;
    *)
        echo "Unknown parameter $arg" >&2
        exit 1
    esac
done

if [ "$arch" = "" ]
then
    if [ -d x64 ]
    then
        arch=x64
        arch2=x64
    else
        arch=x86
        arch2=
    fi
elif [ "$arch" = "x64" ]
then
    arch2=x64
else
    arch2=
fi

src_bin_dir=${arch}/${target}

dst_dir=${out_dir}/${arch}/${target}

rm -rf ${dst_dir}/*
mkdir -p ${dst_dir}
mkdir -p ${dst_dir}/BonDriver
mkdir -p ${dst_dir}/EdcbPlugIn
mkdir -p ${dst_dir}/RecName
mkdir -p ${dst_dir}/Setting
mkdir -p ${dst_dir}/Tools
mkdir -p ${dst_dir}/Write

cp -fp "${src_bin_dir}/EpgDataCap_Bon.exe" "${dst_dir}/EpgDataCap_Bon.exe"
if [ $? -ne 0 ]
then
    echo "Failed to copy EpgDataCap_Bon.exe" >&2
    exit 1
fi

cp -fp "${src_bin_dir}/EpgTimer.exe" "${dst_dir}/EpgTimer.exe"
cp -fp "${src_bin_dir}/EpgTimer.exe" "${dst_dir}/EpgTimerNW.exe"
cp -fp "${src_bin_dir}/EpgTimerAdminProxy.exe" "${dst_dir}/EpgTimerAdminProxy.exe"
cp -fp "${src_bin_dir}/EpgTimerPlugIn.tvtp" "${dst_dir}/EpgTimerPlugIn.tvtp"
cp -fp "${src_bin_dir}/EpgTimerSrv.exe" "${dst_dir}/EpgTimerSrv.exe"
cp -fp "${src_bin_dir}/EpgDataCap3.dll" "${dst_dir}/EpgDataCap3.dll"
cp -fp "${src_bin_dir}/EpgDataCap3_Unicode.dll" "${dst_dir}/EpgDataCap3_Unicode.dll"
cp -fp "${src_bin_dir}/IBonCast.dll" "${dst_dir}/IBonCast.dll"
cp -fp "${src_bin_dir}/SendTSTCP.dll" "${dst_dir}/SendTSTCP.dll"

cp -fp "ConvToSJIS.bat" "${dst_dir}/ConvToSJIS.bat"
cp -fp "ConvToUTF8.bat" "${dst_dir}/ConvToUTF8.bat"
cp -fp "LICENSE-Civetweb.md" "${dst_dir}/LICENSE-Civetweb.md"

doc_files=(History.txt HowToBuild.txt Readme.txt Readme_EpgDataCap_Bon.txt Readme_EpgTimer.txt Readme_Mod.txt)
for doc_file in ${doc_files[@]}
do
    cp -fp "Document/${doc_file}" "${dst_dir}/${doc_file}"
done

ini_files=(Bitrate.ini BonCtrl.ini ContentTypeText.txt EpgTimer.exe.rd.xaml EpgTimerSrv_Install.bat EpgTimerSrv_Remove.bat EpgTimerSrv_Setting.bat)
for ini_file in ${ini_files[@]}
do
    cp -fp "ini/${ini_file}" "${dst_dir}/${ini_file}"
done

cp -fp "${src_bin_dir}/EdcbPlugIn.tvtp" "${dst_dir}/EdcbPlugIn/EdcbPlugIn.tvtp"
cp -fp "EdcbPlugIn/EdcbPlugIn/ch2chset.vbs" "${dst_dir}/EdcbPlugIn/ch2chset.vbs"
cp -fp "EdcbPlugIn/EdcbPlugIn/EdcbPlugIn.ini" "${dst_dir}/EdcbPlugIn/EdcbPlugIn.ini"
cp -fp "EdcbPlugIn/EdcbPlugIn/EdcbPlugIn_Readme.txt" "${dst_dir}/EdcbPlugIn/EdcbPlugIn_Readme.txt"
cp -fp "${src_bin_dir}/Write/Write_OneService.dll" "${dst_dir}/EdcbPlugIn/Write_OneService.dll"
cp -fpr ini/HttpPublic "${dst_dir}"
cp -fpr ini/PostBatExamples "${dst_dir}"
cp -fp "${src_bin_dir}/RecName/RecName_Macro.dll" "${dst_dir}/RecName/RecName_Macro.dll"

tools_files=(tsidmove_helper.bat)
for tools_file in ${tools_files[@]}
do
    cp -fp "Tools/${tools_file}" "${dst_dir}/Tools/${tools_file}"
done

cp -fp "${src_bin_dir}/Tools/tsidmove.exe" "${dst_dir}/Tools/tsidmove.exe"
cp -fp "${src_bin_dir}/Tools/tspgtxt.exe" "${dst_dir}/Tools/tspgtxt.exe"
cp -fp "${src_bin_dir}/Tools/asyncbuf.exe" "${dst_dir}/Tools/asyncbuf.exe"
cp -fp "${src_bin_dir}/Tools/relayread.exe" "${dst_dir}/Tools/relayread.exe"
cp -fp "${src_bin_dir}/Write/Write_Default.dll" "${dst_dir}/Write/Write_Default.dll"

version=

git_hash=$(git rev-parse --short HEAD)

archive_name=${out_dir}/EDCB_${version}${git_hash}_${arch}

if [ "$archive" = 7z ]
then
    ## Archive with 7-Zip
    sevenz_exe='/c/Program Files/7-Zip/7z.exe'
    if [ ! -f "$sevenz_exe" ]
    then
        sevenz_exe='/c/Program Files (x86)/7-Zip/7z.exe'
        if [ ! -f "$sevenz_exe" ]
        then
            sevenz_exe='/mnt/c/Program Files/7-Zip/7z.exe'
            if [ ! -f "$sevenz_exe" ]
            then
                sevenz_exe='/mnt/c/Program Files (x86)/7-Zip/7z.exe'
                if [ ! -f "$sevenz_exe" ]
                then
                    echo "Unable to find 7z.exe" >&2
                    exit 1
                fi
            fi
        fi
    fi

    "$sevenz_exe" a "${archive_name}.7z" "./${dst_dir}/\*" -mx=9 -ms=on -myx=9
elif [ "$archive" = bz2 ]
then
    ## Archive with bzip2
    tar -jcf "${archive_name}.tar.bz2" -C "${dst_dir}" .
fi
