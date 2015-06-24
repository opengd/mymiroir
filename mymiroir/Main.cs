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
			bool addhash = false;
			bool addtimestamp = false;
			bool compress = false;
			bool recursive = false;
			bool header = false;
			bool ignore = false;

			string watch = null; 
			string mirror = null;

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
					case ("filter"):
						filter = args [++i];
						break;
					case ("hash"):
						addhash = true;
						break;
					case ("timestamp"):
						addtimestamp = true;
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
					AddHash = addhash,
					AddTimestamp = addtimestamp,
					Compress = compress,
					Exec = exec,
					Recursive = recursive,
					Ignore = ignore
				};

				mm.NewFile += HandleNewFile;
				mm.NewFileCopyStart += HandleNewFileCopyStart;
				mm.NewFileCopyFinish += HandleNewFileCopyFinish;
				mm.NewFileCompressStart += HandleNewFileCompressStart;
				mm.NewFileCompressFinish += HandleNewFileCompressFinish;
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
				Console.WriteLine("   exec\t\tExecute on change, parameters: $0 is filename, $1 is fullpath, $2 is hash, $3 timestamp");
				Console.WriteLine("   filter\tFilter file types, default is *");
				Console.WriteLine("   hash\t\tAdd hash to filename on copy");
				Console.WriteLine("   header\tShow config header on start");
				Console.WriteLine("   help\t\tShow this information");
				Console.WriteLine("   ignore\tIgnore check if the specified watch path exist or not");
				Console.WriteLine("   mirror\tMirror path, copy changed file to path");
				Console.WriteLine("   recursive\tRecursive, watch all subfolders");
				Console.WriteLine("   timestamp\tAdd time now on filename on copy");
				Console.WriteLine("   watch\tPath to watch for changes, if not specified current will be used");

				Console.WriteLine("\nExample: mymiroir watch /my/file/path");
				Console.WriteLine("         mymiroir watch /my/file/path exec \"/run/my/script.sh $0\"");
				Console.WriteLine("\nRun as service: mono-service mymiroir.exe");
			}
		}

		static void HandleFileChange (object sender, FileChangeEventArgs e)
		{
			Console.WriteLine(e.file);
		}

		static void HandleNewFileCompressFinish (object sender, CompressFileEventArgs e)
		{
			Console.WriteLine ("FSW: NewFile: Compress: Finish: Source: " + e.SourceFile);
			Console.WriteLine ("FSW: NewFile: Compress: Finish: Destination: " + e.DestinationFile);			
		}

		static void HandleNewFileCompressStart (object sender, CompressFileEventArgs e)
		{
			Console.WriteLine ("FSW: NewFile: Compress: Start: Source: " + e.SourceFile);
			Console.WriteLine ("FSW: NewFile: Compress: Start: Destination: " + e.DestinationFile);
		}

		static void HandleNewFileCopyFinish (object sender, CopyFileEventArgs e)
		{
			Console.WriteLine ("FSW: NewFile: MirrorCopy: Finish: Source: " + e.SourceFile);
			Console.WriteLine ("FSW: NewFile: MirrorCopy: Finish: Destination: " + e.DestinationFile);
		}

		static void HandleNewFileCopyStart (object sender, CopyFileEventArgs e)
		{
			Console.WriteLine ("FSW: NewFile: MirrorCopy: Start: Source: " + e.SourceFile);
			Console.WriteLine ("FSW: NewFile: MirrorCopy: Start: Destination: " + e.DestinationFile);
		}

		static void HandleNewFile (object sender, FileSystemEventArgs e)
		{
			Console.WriteLine ("FSW: NewFile: " + e.FullPath);
			FileInfo fi1 = new FileInfo(e.FullPath);
			Console.WriteLine ("FSW: NewFile: FileSize: " + fi1.Length);
		}
	}
}
