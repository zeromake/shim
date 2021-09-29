#!/bin/sh

set -e

fwRoot=/c/Windows/Microsoft.NET/Framework/

rm -rf ./*.exe

if command -v mcs >/dev/null 2>&1; then
    echo "mono building"
    mcs -platform:X64 -out:shim-x86-64.exe -optimize shim.cs
    mcs -platform:X86 -out:shim-x86.exe -optimize shim.cs
elif [ -d ${fwRoot} ]; then
    echo "ms building"
    fwdir=`find ${fwRoot} -maxdepth 1 -type d -name 'v*' | sort -r | head -1`
    "${fwdir}/csc" //nologo //out:shim.exe shim.cs
fi

file ./*.exe
md5sum ./*.exe

echo "done"
