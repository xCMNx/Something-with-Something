using System;
using System.Collections;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace core
{
	public static class Helpers
	{
		public static SynchronizationContext mainCTX;
		public static CancellationTokenSource mainCTS = new CancellationTokenSource();

		public static string ProgramPath { get; private set; }
		public static string EncryptionKey { get; set; }

		static Helpers()
		{
			ProgramPath = System.AppDomain.CurrentDomain.BaseDirectory;
			//config = ConfigurationManager.OpenExeConfiguration(Path.Combine(ProgramPath, ".exe.config"));
			config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
			var fn = config.FilePath.ToLower();
			if (fn.Contains(".vshost."))
				config = ConfigurationManager.OpenExeConfiguration(fn.Replace(".vshost.", ".").Replace(".config", null));
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
		static SemaphoreSlim ConfigSema = new SemaphoreSlim(1);

		public static bool SetEncryptionKey(string encryptionKey)
		{
			ConfigSema.Wait();
			try
			{
				var r = config.AppSettings.Settings["state"];
				bool b;
				if (r != null && !bool.TryParse(Decrypt(r.Value, encryptionKey), out b))
					return false;
				EncryptionKey = string.IsNullOrWhiteSpace(encryptionKey) ? "defaultkey" : encryptionKey;
				InnerWriteToConfig("state", true.ToString());
				return true;
			}
			finally
			{
				ConfigSema.Release();
			}
		}

		public static void ChangeKey(string encryptionKey)
		{
			ConfigSema.Wait();
			try
			{
				foreach (KeyValueConfigurationElement s in config.AppSettings.Settings)
					s.Value = Encrypt(Decrypt(s.Value, EncryptionKey), encryptionKey);
				config.Save(ConfigurationSaveMode.Minimal);
			}
			finally
			{
				ConfigSema.Release();
			}
		}

		static void InnerWriteToConfig(string Key, string Value)
		{
			var value = Encrypt(Value, EncryptionKey);
			var k = config.AppSettings.Settings[Key];
			if (k == null)
				config.AppSettings.Settings.Add(Key, value);
			else
				k.Value = value;
			config.Save(ConfigurationSaveMode.Minimal);
		}

		public static void WriteToConfig(string Key, string Value)
		{
			try
			{
				ConfigSema.Wait();
				try
				{
					InnerWriteToConfig(Key, Value);
				}
				finally
				{
					ConfigSema.Release();
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
			ConfigSema.Wait();
			try
			{
				var r = config.AppSettings.Settings[Key];
				if (r == null)
				{
					if (CreateRecord)
						InnerWriteToConfig(Key, Default);
					return Default;
				}
				return Decrypt(r.Value, EncryptionKey);
			}
			finally
			{
				ConfigSema.Release();
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

		#region Encrypting

		public static string Encrypt(string cipherText, string sharedSecret)
		{
			if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(sharedSecret))
				return cipherText;
			return EncryptStringAES(cipherText, sharedSecret);
		}

		public static string Decrypt(string cipherText, string sharedSecret)
		{
			if (string.IsNullOrEmpty(cipherText) || string.IsNullOrEmpty(sharedSecret))
				return cipherText;
			try
			{
				return DecryptStringAES(cipherText, sharedSecret);
			}
			catch
			{
				return cipherText;
			}
		}

		private static byte[] _salt = Encoding.ASCII.GetBytes("o9965kbM7c5");

		/// <summary>
		/// Encrypt the given string using AES.  The string can be decrypted using 
		/// DecryptStringAES().  The sharedSecret parameters must match.
		/// </summary>
		/// <param name="plainText">The text to encrypt.</param>
		/// <param name="sharedSecret">A password used to generate a key for encryption.</param>
		public static string EncryptStringAES(string plainText, string sharedSecret)
		{
			if (string.IsNullOrEmpty(plainText))
				throw new ArgumentNullException("plainText");
			if (string.IsNullOrEmpty(sharedSecret))
				throw new ArgumentNullException("sharedSecret");

			string outStr = null;                       // Encrypted string to return
			RijndaelManaged aesAlg = null;              // RijndaelManaged object used to encrypt the data.

			try
			{
				// generate the key from the shared secret and the salt
				Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

				// Create a RijndaelManaged object
				aesAlg = new RijndaelManaged();
				aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);

				// Create a decryptor to perform the stream transform.
				ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

				// Create the streams used for encryption.
				using (MemoryStream msEncrypt = new MemoryStream())
				{
					// prepend the IV
					msEncrypt.Write(BitConverter.GetBytes(aesAlg.IV.Length), 0, sizeof(int));
					msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);
					using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
					{
						using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
						{
							//Write all data to the stream.
							swEncrypt.Write(plainText);
						}
					}
					outStr = Convert.ToBase64String(msEncrypt.ToArray());
				}
			}
			finally
			{
				// Clear the RijndaelManaged object.
				if (aesAlg != null)
					aesAlg.Clear();
			}

			// Return the encrypted bytes from the memory stream.
			return outStr;
		}

		/// <summary>
		/// Decrypt the given string.  Assumes the string was encrypted using 
		/// EncryptStringAES(), using an identical sharedSecret.
		/// </summary>
		/// <param name="cipherText">The text to decrypt.</param>
		/// <param name="sharedSecret">A password used to generate a key for decryption.</param>
		public static string DecryptStringAES(string cipherText, string sharedSecret)
		{
			if (string.IsNullOrEmpty(cipherText))
				throw new ArgumentNullException("cipherText");
			if (string.IsNullOrEmpty(sharedSecret))
				throw new ArgumentNullException("sharedSecret");

			// Declare the RijndaelManaged object
			// used to decrypt the data.
			RijndaelManaged aesAlg = null;

			// Declare the string used to hold
			// the decrypted text.
			string plaintext = null;

			try
			{
				// generate the key from the shared secret and the salt
				Rfc2898DeriveBytes key = new Rfc2898DeriveBytes(sharedSecret, _salt);

				// Create the streams used for decryption.                
				byte[] bytes = Convert.FromBase64String(cipherText);
				using (MemoryStream msDecrypt = new MemoryStream(bytes))
				{
					// Create a RijndaelManaged object
					// with the specified key and IV.
					aesAlg = new RijndaelManaged();
					aesAlg.Key = key.GetBytes(aesAlg.KeySize / 8);
					// Get the initialization vector from the encrypted stream
					aesAlg.IV = ReadByteArray(msDecrypt);
					// Create a decrytor to perform the stream transform.
					ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);
					using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
					{
						using (StreamReader srDecrypt = new StreamReader(csDecrypt))

							// Read the decrypted bytes from the decrypting stream
							// and place them in a string.
							plaintext = srDecrypt.ReadToEnd();
					}
				}
			}
			finally
			{
				// Clear the RijndaelManaged object.
				if (aesAlg != null)
					aesAlg.Clear();
			}

			return plaintext;
		}

		private static byte[] ReadByteArray(Stream s)
		{
			byte[] rawLength = new byte[sizeof(int)];
			if (s.Read(rawLength, 0, rawLength.Length) != rawLength.Length)
			{
				throw new SystemException("Stream did not contain properly formatted byte array");
			}

			byte[] buffer = new byte[BitConverter.ToInt32(rawLength, 0)];
			if (s.Read(buffer, 0, buffer.Length) != buffer.Length)
			{
				throw new SystemException("Did not read byte array properly");
			}

			return buffer;
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
