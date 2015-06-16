using System;
using mymiroir;
using System.IO;

namespace mymiroircli
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			if(args.Length < 2) 
			{
				//mymiroir.mymiroir mm = new mymiroir.mymiroir();
				Console.WriteLine ("FSW: NONE");
			}
			else 
			{
				mymiroir.mymiroir mm = new mymiroir.mymiroir(args[0], args[1]);
				mm.NewFile += HandleNewFile;
				mm.NewFileCopyStart += HandleNewFileCopyStart;
				mm.NewFileCopyFinish += HandleNewFileCopyFinish;
				mm.NewFileCompressStart += HandleNewFileCompressStart;
				mm.NewFileCompressFinish += HandleNewFileCompressFinish;
				Console.WriteLine ("FSW: " + args[0]);
			}
			
			
			
			Console.ReadLine();
			//while(true){}
			
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
