using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace core
{
	public static class Helpers
	{
		public static SynchronizationContext mainCTX;
		public static CancellationTokenSource mainCTS = new CancellationTokenSource();

		public static string ProgramPath { get; private set; }
		public static string SettingsPath { get; private set; }

		static Helpers()
		{
			ProgramPath = System.AppDomain.CurrentDomain.BaseDirectory;
			//config = ConfigurationManager.OpenExeConfiguration(Path.Combine(ProgramPath, ".exe.config"));
			config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var fn = config.FilePath.ToLower();
			if (fn.Contains(".vshost."))
				config = ConfigurationManager.OpenExeConfiguration(fn.Replace(".vshost.", ".").Replace(".config", null));
			Encoding = Encoding.GetEncoding(ReadFromConfig(ENCODING, Default_Encoding));
			SettingsPath = ReadFromConfig("SettingsPath", System.IO.Path.Combine(ProgramPath, @"Settings"));
		}

		public static T SendNew<T>(this SynchronizationContext ctx, Func<T> NewMethod) where T : class
		{
			T res = null;
			ctx.Send(_ => res = NewMethod(), null);
			return res;
		}

		public static void Post(Action action)
		{
			if (mainCTX == null)
				action();
			else
				mainCTX.Post(_ => action(), null);
		}

		public static void Send(Action action)
		{
			if (mainCTX == null)
				action();
			else
				mainCTX.Send(_ => action(), null);
		}

		#region Mouse
		[DllImport("user32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool GetCursorPos(ref Win32Point pt);

		[StructLayout(LayoutKind.Sequential)]
		public struct Win32Point
		{
			public Int32 X;
			public Int32 Y;
		};
		public static Win32Point GetMousePosition()
		{
			Win32Point w32Mouse = new Win32Point();
			GetCursorPos(ref w32Mouse);
			return w32Mouse;
		}
		#endregion

		#region Console
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		internal static extern bool AllocConsole();
		[DllImport("kernel32.dll")]
		internal static extern bool FreeConsole();

		public static int ConsoleBufferHeight = 300;
		public static int ConsoleBufferWidth = 80;

		static bool CreateConsole()
		{
			var b = AllocConsole();
			TextWriter writer = new StreamWriter(Console.OpenStandardOutput()) { AutoFlush = true };
			Console.SetOut(writer);
			return b;
		}

		static bool _ConsoleEnabled = false;
		public static bool ConsoleEnabled
		{
			get { return _ConsoleEnabled; }
			set
			{
				lock (ConsoleLocker)
				{
					if (_ConsoleEnabled != value)
						_ConsoleEnabled = value ? AllocConsole() : !FreeConsole();
					if (_ConsoleEnabled)
					{
						Console.BufferHeight = ConsoleBufferHeight;
						Console.BufferWidth = ConsoleBufferWidth;
					}
				}
			}
		}
		static object ConsoleLocker = true;

		public static void ConsoleWrite(string Str, ConsoleColor clr = ConsoleColor.Gray)
		{
			lock (ConsoleLocker)
			{
				if (_ConsoleEnabled)
				{
					Console.ForegroundColor = clr;
					Console.WriteLine(Str);
				}
			}
		}
		#endregion

		#region Encoding
		public static string Default_Encoding = "windows-1251";
		public const string ENCODING = "ENCODING";
		public static Encoding Encoding;

		public static Encoding GetEncoding(string filename, Encoding def)
		{
			// Read the BOM
			var bom = new byte[4];
			using (var file = new FileStream(filename, FileMode.Open, FileAccess.Read))
			{
				file.Read(bom, 0, 4);
			}

			// Analyze the BOM
			if (bom[0] == 0x2b && bom[1] == 0x2f && bom[2] == 0x76)
				return Encoding.UTF7;
			if (bom[0] == 0xef && bom[1] == 0xbb && bom[2] == 0xbf)
				return Encoding.UTF8;
			if (bom[0] == 0xff && bom[1] == 0xfe)
				return Encoding.Unicode; //UTF-16LE
			if (bom[0] == 0xfe && bom[1] == 0xff)
				return Encoding.BigEndianUnicode; //UTF-16BE
			if (bom[0] == 0 && bom[1] == 0 && bom[2] == 0xfe && bom[3] == 0xff)
				return Encoding.UTF32;
			return def;
		}
		#endregion


		#region Reflection
		public static Type[] getModules(string InterfaceName, IEnumerable<Assembly> assemblies = null)
		{
			var asmbls = assemblies ?? AppDomain.CurrentDomain.GetAssemblies();
			List<Type> modules = new List<Type>();
			foreach (var assembly in asmbls)
			{
				var a_types = assembly.GetTypes();
				foreach (var type in a_types)
					if (type.GetInterface(InterfaceName) != null)
						modules.Add(type);
			}
			return modules.ToArray();
		}

		public static Type[] getModules(Type type, IEnumerable<Assembly> assemblies = null)
		{
			return getModules(type.FullName, assemblies);
		}

		public static Assembly LoadLibrary(string dllName)
		{
			try
			{
				return Assembly.LoadFrom(Path.GetFullPath(dllName));
			}
			catch (Exception e)
			{
				Helpers.ConsoleWrite(e.ToString(), ConsoleColor.DarkYellow);
				Helpers.ConsoleWrite("Error while loading " + dllName);
				Helpers.ConsoleWrite(e.Message);
				Helpers.ConsoleWrite(string.Empty);
			}
			return null;
		}

		public static Assembly[] LoadLibraries(string path, SearchOption Options = SearchOption.TopDirectoryOnly)
		{
			List<Assembly> assemblies = new List<Assembly>();
			if (Directory.Exists(path))
				foreach (var f in Directory.GetFiles(path, "*.dll", Options))
				{
					var assembly = LoadLibrary(f);
					if (assembly != null)
						assemblies.Add(assembly);
				}
			return assemblies.ToArray();
		}

		public static string AssemblyDirectory(Assembly asmbl)
		{
			UriBuilder uri = new UriBuilder(asmbl.CodeBase);
			string path = Uri.UnescapeDataString(uri.Path + uri.Fragment);
			return Path.GetDirectoryName(path);
		}
		#endregion

		#region Config
		static Configuration config;

		public static void WriteToConfig(string Key, string Value)
		{
			try
			{
				lock (config)
				{
					var k = config.AppSettings.Settings[Key];
					if (k == null)
						config.AppSettings.Settings.Add(Key, Value);
					else
						k.Value = Value;
					config.Save(ConfigurationSaveMode.Minimal);
				}
			}
			catch (Exception e)
			{
				ConsoleWrite(e.ToString(), ConsoleColor.DarkMagenta);
			}
		}

		public static void ConfigWrite(string key, object value)
		{
			WriteToConfig(key, value?.ToString());
		}

		public static string ReadFromConfig(string Key, string Default = null, bool CreateRecord = false)
		{
			lock (config)
			{
				var r = config.AppSettings.Settings[Key];
				if (r == null)
				{
					if (CreateRecord)
						WriteToConfig(Key, Default);
					return Default;
				}
				return r.Value;
			}
		}

		public static bool ConfigRead(string Key, bool Default, bool CreateRecord = false)
		{
			bool b;
			if (bool.TryParse(ReadFromConfig(Key, null, CreateRecord), out b))
				return b;
			return Default;
		}

		public static string ConfigRead(string Key, string Default, bool CreateRecord = false)
		{
			return ReadFromConfig(Key, Default, CreateRecord);
		}

		public static int ConfigRead(string Key, int Default, bool CreateRecord = false)
		{
			int val;
			if (int.TryParse(ReadFromConfig(Key, null, CreateRecord), out val))
				return val;
			return Default;
		}

		public static byte ConfigRead(string Key, byte Default, bool CreateRecord = false)
		{
			byte val;
			if (byte.TryParse(ReadFromConfig(Key, null, CreateRecord), out val))
				return val;
			return Default;
		}
		#endregion

		public static long TickCount
		{
			get { return System.Environment.TickCount & int.MaxValue; }
		}

		public static int TrueCnt(this BitArray bts)
		{
			var cnt = 0;
			for (int i = 0; i < bts.Count; i++)
				if (bts[i])
					cnt++;
			return cnt;
		}

		public static byte[] ToBytes(this BitArray bts)
		{
			byte[] bytes = new byte[bts.Length / 8 + (bts.Length % 8 == 0 ? 0 : 1)];
			bts.CopyTo(bytes, 0);
			return bytes;
		}

		public static int ToStream(this BitArray bts, Stream Stream)
		{
			var size = bts.Length / 8 + (bts.Length % 8 == 0 ? 0 : 1);
			byte[] bytes = new byte[size];
			bts.CopyTo(bytes, 0);
			Stream.Write(bytes, 0, size);
			return size;
		}

		public static BitArray ToBits(this Stream Stream)
		{
			byte[] bytes = new byte[Stream.Length];
			Stream.Read(bytes, 0, bytes.Length);
			return new BitArray(bytes);
		}

		public static int intParseOrDefault(string s, int Default)
		{
			int val;
			if (!int.TryParse(s, out val))
				return Default;
			return val;
		}

	}
}
