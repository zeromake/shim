mcs -platform:X64 -out:shim-x86-64.exe -optimize .\shim.cs && mcs -platform:X86 -out:shim-x86.exe -optimize .\shim.cs