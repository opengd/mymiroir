using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.IO.Compression;
using System.Security.Cryptography;

namespace mymiroir
{
	public class CopyFileEventArgs
	{
		public string SourceFile;
		public string DestinationFile;
		
		public CopyFileEventArgs(string SourceFile = "", string DestinationFile = "")
		{
			this.SourceFile = SourceFile;
			this.DestinationFile = DestinationFile;
		}	
	}
	
	public class CompressFileEventArgs : CopyFileEventArgs {
		
		public CompressFileEventArgs(string SourceFile = "", string DestinationFile = "")
		{
			this.SourceFile = SourceFile;
			this.DestinationFile = DestinationFile;
		}
	}
	
	public class mymiroir
	{
		
		public delegate void NewFileEventHandler(object sender, FileSystemEventArgs e);
		public delegate void CopyFileEventHandler(object sender, CopyFileEventArgs e);
		public delegate void CompressFileEventHandler(object sender, CompressFileEventArgs e);
		
		public event NewFileEventHandler NewFile;
		public event CopyFileEventHandler NewFileCopyStart;
		public event CopyFileEventHandler NewFileCopyFinish;
		public event CompressFileEventHandler NewFileCompressStart;
		public event CompressFileEventHandler NewFileCompressFinish;
		
		ArrayList filePool;
		
		private FileSystemWatcher fsw;
		
		private Uri uriMirrorPath;
		private Uri uriWatchPath;
		
		private int maxThreads;
		
		private bool isLinux;
		
		string MirrorPath
		{
			get {
				return uriMirrorPath.LocalPath;
			}
			
			set
			{
				string s = value;
				s = s.TrimEnd(new char[]{'/', '\\'});
				uriMirrorPath = new Uri(s);
			}
		}
		
		string WatchPath
		{
			get {
				return uriWatchPath.LocalPath;
			}
			
			set
			{
				string s = value;
				s = s.TrimEnd(new char[]{'/', '\\'});
				uriWatchPath = new Uri(s);
				//init ();
			}
		}
		
		bool IsLinux
		{
			get
			{
				return isLinux;
			}
			set
			{
				isLinux = value;
			}
		}
		
		public mymiroir ()
		{			
			init ();
		}
		
		public mymiroir(string watch_path)
		{
			init ();
		}
		
		public mymiroir(string watch_path, string mirror_path)
		{
			WatchPath = watch_path ;
			MirrorPath = mirror_path;
			
			init ();
		}
		
		public void init ()
		{
			filePool = new ArrayList();
			
			if(Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX) 
			{
				IsLinux = true;
				Console.WriteLine ("FSW: Platform: " + "Linux");
			}
			else 
			{
				IsLinux = false;
				Console.WriteLine ("FSW: Platform: " + "Windows");
			}
			
				
			fsw = new FileSystemWatcher();

			if(Directory.Exists(WatchPath)) 
			{
				fsw.Path = WatchPath;
			}
			
			Console.WriteLine ("FSW: WatchPath: " + WatchPath);
			Console.WriteLine ("FSW: MirrorPath: " + MirrorPath);
			
			fsw.Filter = "*.*";
			if(IsLinux) fsw.NotifyFilter = NotifyFilters.LastWrite;
			else
			{
				fsw.NotifyFilter = NotifyFilters.LastWrite;
			}
			
			fsw.Error += new ErrorEventHandler(fsw_OnError);
			
			if(IsLinux) fsw.Changed += new FileSystemEventHandler(fsw_OnChanged);
			else { 
				//fsw.Created += new FileSystemEventHandler(fsw_OnCreated);
				fsw.Changed += new FileSystemEventHandler(fsw_OnChanged);
			}
			
			fsw.EnableRaisingEvents = true;
		}
		
		private void fsw_OnError(object sender, ErrorEventArgs e)
		{
			Console.WriteLine ("FSW: Error: " + e.GetException());
		}
		
		private void fsw_OnCreated(object sender, FileSystemEventArgs e)
		{
			Thread t = new Thread(OnNewFile);
			t.Start(e);
		}
		
		private void fsw_OnChanged(object sender, FileSystemEventArgs e)
		{
			Thread t = new Thread(OnNewFile);
			t.Start(e);

		}
				
		private void OnNewFile(object arguments)
		{
			FileSystemEventArgs e = (FileSystemEventArgs)arguments;
			
			string newMirrorFile;
			Console.WriteLine (e.ChangeType.ToString());
			
			if(!checkFilePool(e.FullPath))
			{
				filePool.Add(e.FullPath);	

				var time = DateTime.Now;

				var pathEnd = (IsLinux) ? "/" : "\\";

				newMirrorFile = MirrorPath + 
					pathEnd + 
					e.Name + 
					"." + 
					GetHash(e.FullPath) + 
					"." + 
					time.ToShortDateString() + 
					"." + 
					time.ToLongTimeString() + 
					"." + 
					time.Millisecond;
				
				if(IsFileReady(e.FullPath)) 
				{
					if (NewFile != null) NewFile(this, e);
					
					if(NewFileCopyStart != null) NewFileCopyStart(this, new CopyFileEventArgs(e.FullPath, newMirrorFile));
					
					StepCopyFile(e.FullPath, newMirrorFile);
					
					if(NewFileCopyFinish != null) NewFileCopyFinish(this, new CopyFileEventArgs(e.FullPath, newMirrorFile));
					
					//fi1 = new FileInfo(newMirrorFile);
					//Console.WriteLine ("FSW: FileInfo: Mirror: " + fi1.Length);
					
					if(NewFileCompressStart != null) NewFileCompressStart(this, new CompressFileEventArgs(newMirrorFile, newMirrorFile + ".gz"));
					
					StepCompressFile(newMirrorFile);
					
					if(NewFileCompressFinish != null) NewFileCompressFinish(this, new CompressFileEventArgs(newMirrorFile, newMirrorFile + ".gz"));
				}
				filePool.Remove(e.FullPath);
			}
			//StepRemoveFile(newMirrorFile);
			
		}
		
		private bool checkFilePool(string filename)
		{
			foreach(string s in filePool)
			{
				//Console.WriteLine ("FSW: CheckFilePool: " + s);
				if(s.Equals(filename)) return true;
			}
			
			return false;
		}
		
		private bool IsFileReady(string inFile)
		{
			if(IsLinux) return true;
			
			while(true)
			{
				try
				{
					FileStream f = File.Open(inFile, FileMode.Open, FileAccess.Read, FileShare.None);
					f.Close();
					break;
				}
				catch (IOException)
				{
				//	Console.WriteLine ("FSW: IsFileReady: EXCEPTION");
				}
			}
			
			Console.WriteLine ("FSW: IsFileReady: " + inFile);
			
			return true;
		}
		
		
		private void StepCopyFile(string inFile, string outFile)
		{

			//FileStream ifs = File.Open(inFile, FileMode.Open, FileAccess.Read, FileShare.Read);
			//FileStream ofs = File.Create(outFile);
			
			//Console.WriteLine ("FSW: Step Copy: IN: " + ifs.Length);
			
			//ifs.CopyTo(ofs);
			
			//Console.WriteLine ("FSW: Step Copy: OUT: " + ofs.Length);
			
			//ifs.Close();
			//ofs.Close();
			
			File.Copy(inFile, outFile);
			//fs.Close();
			
			
		}
		
		private void StepCompressFile(string inFile)
		{
			using ( FileStream iFile = File.OpenRead(inFile) )
			{
				using ( FileStream oFile = File.Create(inFile + ".gz") )
				{
					using (GZipStream Compress = new GZipStream(oFile, CompressionMode.Compress))
					{
						iFile.CopyTo(Compress);
					
						Compress.Close();
						
					}
					//Console.WriteLine ("FSW: Step Compress: " + oFile.Name);
					oFile.Close();
				}
				iFile.Close();
			}
		}
		
		private void StepRemoveFile(string inFile)
		{
			File.Delete(inFile);
			Console.WriteLine ("FSW: Step Deleted: " + inFile);
		}

		private string GetHash(string file)
		{
			using(FileStream stream = File.OpenRead(file))
			{
				MD5CryptoServiceProvider hasher = new MD5CryptoServiceProvider();
				byte[] checksum = hasher.ComputeHash(stream);
				return BitConverter.ToString(checksum);
			}
			return null;
		}
	}
	
}

