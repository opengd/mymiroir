/*
* mymiroir 
* Copyright (C) 2015 Erik Johansson
*
* This program is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace mymiroir
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			string filter = "*";
			bool hash = false;
			bool timestamp = false;
			bool compress = false;
			bool recursive = false;
			bool header = false;
			bool ignore = false;
			bool remove = false;
			bool verbose = false;

			string watch = null; 
			string mirror = null;
			string mirrorfile = null;

			string exec = null;

			var help = false;

			try {
				for (var i = 0; i < args.Length; i++) {
					switch (args [i]) {
					case ("help"):
						help = true;
						break;
					case ("watch"):
						watch = args [++i];
						break;
					case ("mirror"):
						mirror = args [++i];
						break;
					case ("mirrorfile"):
						mirrorfile = args [++i];
						break;
					case ("filter"):
						filter = args [++i];
						break;
					case ("hash"):
						hash = true;
						break;
					case ("timestamp"):
						timestamp = true;
						break;
					case ("exec"):
						exec = args [++i];
						break;
					case ("compress"):
						compress = true;
						break;
					case ("recursive"):
						recursive = true;
						break;
					case ("header"):
						header = true;
						break;
					case ("ignore"):
						ignore = true;
						break;
					case ("remove"):
						remove = true;
						break;
					case ("verbose"):
						verbose = true;
						break;
					default:
						break;
					}
				}
			} catch (Exception e) {
				Debug.WriteLine (e.Message);
				help = true;
			}

			if (!help) {

				var exitEvent = new ManualResetEvent(false);

				Console.CancelKeyPress += (sender, eventArgs) => {
	                              eventArgs.Cancel = true;
	                              exitEvent.Set();
	                          };

				if(string.IsNullOrEmpty(watch))
					watch = Directory.GetCurrentDirectory();

				var mm = new mymiroir(){
					WatchPath = watch,
					MirrorPath = mirror,
					Filter = filter,
					Hash = hash,
					Timestamp = timestamp,
					Compress = compress,
					Exec = exec,
					Recursive = recursive,
					Ignore = ignore,
					Remove = remove,
					MirrorFile = mirrorfile,
					Verbose = verbose
				};

				mm.FileChange += HandleFileChange;

				bool suc = mm.start(header);

				if(suc) 
					exitEvent.WaitOne();
				else {
					Console.WriteLine("Could not find specified path to watch. Use argument ignore to don't do this check.");
					help = true;
				}
			}
			
			if(help) {
				Console.WriteLine("mymiroir Assembly Version: " + System.Reflection.Assembly.GetEntryAssembly().GetName().Version);
				Console.WriteLine("usage: mymiroir.exe [<args>]\n");

				Console.WriteLine("mymiroir is a filesytem watcher that will take action if anything changes on the file path under watch.\n");

				Console.WriteLine("Arguments are:");
				Console.WriteLine("   compress\tCompress mirror file");
				Console.WriteLine("   exec\t\tExecute on change, parameters: %0 filename, %1 hash, %2 timestamp, %3 fullpath");
				Console.WriteLine("   filter\tFilter file types, default is *");
				Console.WriteLine("   hash\t\tOutput MD5 hash for file");
				Console.WriteLine("   header\tShow config header on start");
				Console.WriteLine("   help\t\tShow this information");
				Console.WriteLine("   ignore\tIgnore check if the specified watch path exist or not");
				Console.WriteLine("   mirror\tMirror path, copy changed file to path");
				Console.WriteLine("   mirrorfile\tFilename for new mirror file, default is \"%0.%1.%2\", parameters: %0 filename, %1 hash, %2 timestamp");
				Console.WriteLine("   recursive\tRecursive, watch all subfolders");
				Console.WriteLine("   remove\tRemove changed file");
				Console.WriteLine("   timestamp\tOutput current timestamp");
				Console.WriteLine("   verbose\tVerbose output");
				Console.WriteLine("   watch\tPath to watch for changes, if not specified current will be used");

				Console.WriteLine("\nExample: mymiroir watch /my/file/path");
				Console.WriteLine("         mymiroir watch /my/file/path exec \"/run/my/script.sh %0\"");
				Console.WriteLine("\nRun as service: mono-service mymiroir.exe");
			}
		}

		static void HandleFileChange (object sender, FileChangeEventArgs e)
		{
			Console.WriteLine(e.file.FullPath);

			if(e.hash != null)
				Console.WriteLine(e.hash);

			if(e.timestamp != null)
				Console.WriteLine(e.timestamp);

			//FileInfo fi1 = new FileInfo(e.FullPath);
			//Console.WriteLine ("FSW: NewFile: FileSize: " + fi1.Length);
		}
	}
}
