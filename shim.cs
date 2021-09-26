using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.InteropServices; 

namespace shim {
	
	class Program {
		[DllImport("kernel32.dll", SetLastError=true)]
		static extern bool CreateProcess(string lpApplicationName,
			string lpCommandLine, IntPtr lpProcessAttributes, 
			IntPtr lpThreadAttributes, bool bInheritHandles, 
			uint dwCreationFlags, IntPtr lpEnvironment, string lpCurrentDirectory,
			[In] ref STARTUPINFO lpStartupInfo, 
			out PROCESS_INFORMATION lpProcessInformation);
		// private PROCESS_INFORMATION pi;
		
		enum CtrlTypes
        {
            CTRL_C_EVENT = 0,
            CTRL_BREAK_EVENT,
            CTRL_CLOSE_EVENT,
            CTRL_LOGOFF_EVENT = 5,
            CTRL_SHUTDOWN_EVENT
        }

		private delegate bool HandlerRoutine(CtrlTypes CtrlType);

		[DllImport("kernel32.dll", SetLastError=true)]
		private static extern bool SetConsoleCtrlHandler(HandlerRoutine Handler, bool Add);

		// [DllImport("kernel32.dll", SetLastError=true)]
		// private static extern bool GenerateConsoleCtrlEvent(CtrlTypes dwCtrlType, uint dwProcessGroupId);

		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		struct STARTUPINFO {
			public Int32 cb;
			public string lpReserved;
			public string lpDesktop;
			public string lpTitle;
			public Int32 dwX;
			public Int32 dwY;
			public Int32 dwXSize;
			public Int32 dwYSize;
			public Int32 dwXCountChars;
			public Int32 dwYCountChars;
			public Int32 dwFillAttribute;
			public Int32 dwFlags;
			public Int16 wShowWindow;
			public Int16 cbReserved2;
			public IntPtr lpReserved2;
			public IntPtr hStdInput;
			public IntPtr hStdOutput;
			public IntPtr hStdError;
		}

		[StructLayout(LayoutKind.Sequential)]
		internal struct PROCESS_INFORMATION {
			public IntPtr hProcess;
			public IntPtr hThread;
			public int dwProcessId;
			public int dwThreadId;
		}

		[DllImport("kernel32.dll", SetLastError=true)]
  		static extern UInt32 WaitForSingleObject(IntPtr hHandle, UInt32 dwMilliseconds);
  		const UInt32 INFINITE = 0xFFFFFFFF;

  		[DllImport("kernel32.dll", SetLastError=true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		static extern bool GetExitCodeProcess(IntPtr hProcess, out uint lpExitCode);

		static int Main(string[] args) {
			var exe = Assembly.GetExecutingAssembly().Location;
			var dir = Path.GetDirectoryName(exe);
			var name = Path.GetFileNameWithoutExtension(exe);

			var configPath = Path.Combine(dir, name + ".shim");
			if(!File.Exists(configPath)) {
				Console.Error.WriteLine("Couldn't find " + Path.GetFileName(configPath) + " in " + dir);
				return 1;
			}

			var config = Config(configPath);
			var path = Get(config, "path");
			var add_args = Get(config, "args");
			var curr_dir = Get(config, "dir");
            var environment = Get(config, "environment");
			var signal = Get(config, "signal");

			var hasSignal = !(string.IsNullOrEmpty(signal) || signal != "true");

            string[] envs = environment.Split(new Char[] {';'});

			if(string.IsNullOrEmpty(curr_dir)) {
				curr_dir = null;
			}

			var si = new STARTUPINFO();
			var pi = new PROCESS_INFORMATION();

			// create command line
			var cmd_args = add_args ?? "";
			var pass_args = Serialize(args);
			if(!string.IsNullOrEmpty(pass_args)) {
				if(!string.IsNullOrEmpty(cmd_args)) cmd_args += " ";
				cmd_args += pass_args;
			}
			if(!string.IsNullOrEmpty(cmd_args)) cmd_args = " " + cmd_args;
			var cmd = "\"" + path + "\"" + cmd_args;
            foreach (var env in envs)
            {
                var i = env.IndexOf('=');
                var k = env.Substring(0, i).Trim();
                var v = env.Substring(i+1).Trim();
                System.Environment.SetEnvironmentVariable(k, v);
            }

			// var p = new Program();
			// HandlerRoutine newDelegate = new HandlerRoutine(p.Handler);
			if (hasSignal) {
            	SetConsoleCtrlHandler(null, true);
			}

			if(!CreateProcess(null, cmd, IntPtr.Zero, IntPtr.Zero,
				bInheritHandles: true,
				dwCreationFlags: 0,
				lpEnvironment: IntPtr.Zero, // inherit parent
				lpCurrentDirectory: curr_dir, // inherit parent
				lpStartupInfo: ref si,
				lpProcessInformation: out pi)) {

				return Marshal.GetLastWin32Error();
			}
			// p.pi = pi;
			WaitForSingleObject(pi.hProcess, INFINITE);

			uint exit_code = 0;
			GetExitCodeProcess(pi.hProcess, out exit_code);

			// Close process and thread handles. 
    		CloseHandle(pi.hProcess);
    		CloseHandle(pi.hThread);

			return (int)exit_code;
		}

		// private bool Handler(CtrlTypes CtrlType) {
		// 	GenerateConsoleCtrlEvent(CtrlType, (uint)this.pi.dwProcessId);
		// 	return true;
		// }

		static string Serialize(string[] args) {
			return string.Join(" ", args.Select(a => a.Contains(' ') ? '"' + a + '"' : a));
		}

		static string Get(Dictionary<string, string> dic, string key) {
			string value = null;
			dic.TryGetValue(key, out value);
			return value;
		}

		static Dictionary<string, string> Config(string path) {
			var config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			foreach(var line in File.ReadAllLines(path)) {
				var m = Regex.Match(line, @"([^=]+)=(.*)");
				if(m.Success) {
					config[m.Groups[1].Value.Trim()] = m.Groups[2].Value.Trim();
				}
			}
			return config;
		}
	}
}