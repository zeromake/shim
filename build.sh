#!/bin/sh

set -e

fwRoot=/c/Windows/Microsoft.NET/Framework/

rm -rf ./*.exe

version=`git describe --tags 2>/dev/null`

if [ "$1" == "ci" ]; then
    sed -i "s/private static string Version = \"develop\"/private static string Version = \"${version}\"/g" shim.cs
fi

if command -v mcs >/dev/null 2>&1; then
    echo "mono building"
    mcs -platform:X64 -out:shim-x86-64.exe -optimize shim.cs
    mcs -platform:X86 -out:shim-x86.exe -optimize shim.cs
elif [ -d ${fwRoot} ]; then
    fwdir=`find ${fwRoot} -maxdepth 1 -type d -name 'v*' | sort -r | head -1`
    echo "ms building by ${fwdir}"
    "${fwdir}/csc" //platform:x86 //optimize //nologo //out:shim-x86.exe shim.cs
    "${fwdir}/csc" //nowarn:1607 //platform:x64 //optimize //nologo //out:shim-x86-64.exe shim.cs
fi

if [ "$1" == "ci" ]; then
    sed -i "s/private static string Version = \"${version}\"/private static string Version = \"develop\"/g" shim.cs
fi

ls -lh ./*.exe
file ./*.exe
md5sum ./*.exe

echo "done"
