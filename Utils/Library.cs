//
// Library.cs
//
// Author:
//       Jae Stutzman <jaebird@gmail.com>
//
// Copyright (c) 2016 Jae Stutzman
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.IO;
using System.Xml;
using System.Reflection;
using System.Runtime.InteropServices;

namespace HamLibSharp.Utils
{
	internal class Library
	{
		internal static string Copyright { get; private set; }

		internal static string License { get; private set; }

		internal static Version Version { get; private set; }

		private Library ()
		{
		}

		static Library ()
		{
			var assembly = Assembly.GetExecutingAssembly ();
			var copyrightAttributes = assembly.GetCustomAttributes (typeof(AssemblyCopyrightAttribute), false);

			//var attribute = null;
			if (copyrightAttributes.Length > 0) {
				var attribute = copyrightAttributes [0] as AssemblyCopyrightAttribute;
				Copyright = attribute.Copyright;
			}

			var metadataAttributes = assembly.GetCustomAttributes (typeof(AssemblyMetadataAttribute), false);

			//var attribute = null;
			if (metadataAttributes.Length > 0) {
				var attribute = metadataAttributes [0] as AssemblyMetadataAttribute;
				if (attribute.Key == "License") {
					License = attribute.Value;
				}
			}

			Version = assembly.GetName ().Version;
		}

		private static string GetDllConfig (bool x64)
		{
			string dllDirectory = string.Empty;

			string codeBase = Assembly.GetExecutingAssembly ().CodeBase;
			UriBuilder uri = new UriBuilder (codeBase);
			string path = Uri.UnescapeDataString (uri.Path);
			XmlTextReader reader = new XmlTextReader (path + ".config");
			try {
				while (reader.Read ()) {
					// Do some work here on the data.
					//Console.WriteLine(reader.Name);

					if (reader.Name == "dllwinpath") {
						while (reader.MoveToNextAttribute ()) { // Read the attributes.
							//Console.Write(" " + reader.Name + "='" + reader.Value + "'");
							switch (reader.Name) {
							case "x64":
								if (x64) {
									dllDirectory = reader.Value;
								}
								break;
							case "x86":
								if (!x64) {
									dllDirectory = reader.Value;
								}
								break;
							}
						}
						//Console.WriteLine();
					}
				}
			} catch (Exception) {}

			return dllDirectory;
		}

		internal static bool LoadLibrary (string dllName)
		{
			if (System.Environment.OSVersion.Platform != PlatformID.MacOSX && System.Environment.OSVersion.Platform != PlatformID.Unix) {

				var dllPath = dllName;
				string dllDir = string.Empty;

				if (System.Environment.Is64BitProcess) {
					// "x64"
					dllDir = GetDllConfig (true);
					dllPath = System.IO.Path.Combine (dllDir, dllName);
				} else {
					// "x86"
					dllDir = GetDllConfig (false);
					dllPath = System.IO.Path.Combine (dllDir, dllName);
				}

				if (!File.Exists (dllPath) || string.IsNullOrWhiteSpace(dllDir)) {
					var thisAssemblyLocation = Assembly.GetExecutingAssembly().CodeBase.Replace("file:///", "");
					var dir = Path.GetDirectoryName(thisAssemblyLocation); // may be shadow copied
					var file = Path.Combine(dir, "libhamlib-2.dll");
					var inCurrentWorkingDir = Path.Combine(Environment.CurrentDirectory, "libhamlib-2.dll");
					if (File.Exists(file))
					{
						dllDir = dir;
					}
					else if (File.Exists(inCurrentWorkingDir))
					{
						dllDir = Environment.CurrentDirectory;
					}
					else
					{
						throw new FileNotFoundException($"Unable to Load Dll. File not found: {file}", file);
					}
				}

				var path = Environment.GetEnvironmentVariable("Path");
				var fullDllDir = Path.GetFullPath(dllDir);
				Environment.SetEnvironmentVariable("Path", fullDllDir + ";" + path);
				path = Environment.GetEnvironmentVariable("Path");
				//Console.WriteLine (path);

				//Console.WriteLine (dllPath);
				return true; //LoadLibraryInterop (dllPath) != IntPtr.Zero ? true : false;
			}
			else if (Environment.OSVersion.Platform == PlatformID.Unix)
			{
				// on Linux, if libhamlib.so.2 or libhamlib-2.dll.so exists in the same directory as this assembly
				// was started from (not the shadow-copy of it in /var/tmp/.net), and it doesn't exist next to 
				// the copy of the assembly being run (may be the shadow copy of it), copy it into that directory.
				// overwrite it if necessary (this permits hamlib version changes, whether to the shadow copy
				// happens or not)

				var thisAssemblyLocation = Assembly.GetExecutingAssembly().CodeBase.Replace("file://", "");
				var destLib = Path.Combine(thisAssemblyLocation, "libhamlib-2.dll.so");
				var sourceDir = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
				var unrenamedSourceFile = Path.Combine(sourceDir, "libhamlib.so.2");
				var renamedSourceFile = Path.Combine(sourceDir, "libhamlib-2.dll.so");

				if (File.Exists(renamedSourceFile))
				{
					File.Copy(renamedSourceFile, destLib, true);
				}
				else if (File.Exists(unrenamedSourceFile))
				{
					File.Copy(unrenamedSourceFile, destLib, true);
				}
			}

			return true;
		}

		// This is only available on Windows...but only required for Windows to work around 32-bit vs 64-bit DllImport issues
		[DllImport ("kernel32.dll", EntryPoint = "LoadLibrary")]
		private static extern IntPtr LoadLibraryInterop (string dllToLoad);
	}
}

