using System;
using mymiroir;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace mymiroircli
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			string filter = "*";
			bool addhash = false;
			bool addtimestamp = false;
			bool compress = false;

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

				//if(string.IsNullOrEmpty(copyto))
				//	copyto = Directory.GetCurrentDirectory();

				Console.WriteLine ("watch: " + watch);
				Console.WriteLine ("copyto: " + mirror);

				mymiroir.mymiroir mm = new mymiroir.mymiroir(){
					WatchPath = watch,
					MirrorPath = mirror,
					Filter = filter,
					AddHash = addhash,
					AddTimestamp = addtimestamp,
					Compress = compress,
					Exec = exec
				};
				mm.NewFile += HandleNewFile;
				mm.NewFileCopyStart += HandleNewFileCopyStart;
				mm.NewFileCopyFinish += HandleNewFileCopyFinish;
				mm.NewFileCompressStart += HandleNewFileCompressStart;
				mm.NewFileCompressFinish += HandleNewFileCompressFinish;
				mm.FileChange += HandleFileChange;
				mm.start();

				exitEvent.WaitOne();
			}
			else
			{
				Console.WriteLine("usage: mymiroir-cli [<args>]\n");
				Console.WriteLine("Arguments are:");
				Console.WriteLine("\twatch\t\tPath to watch for changes, if not specified current will be used");
				Console.WriteLine("\tfilter\t\tFilter file types, default is *");
				Console.WriteLine("\tmirror\t\tMirror path, copy changed file to path");
				Console.WriteLine("\thash\t\tAdd hash to filename on copy");
				Console.WriteLine("\ttimestamp\tAdd time now on filename on copy");
				Console.WriteLine("\tcompress\tCompress mirror file");
				Console.WriteLine("\texec\t\tExecute on change, parameters: $0 is filename, $1 is fullpath, $2 is hash, $3 timestamp");
				Console.WriteLine("\thelp\t\tShow this information");
				Console.WriteLine("\nExample: mymiroir-cli watch /my/file/path");
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
