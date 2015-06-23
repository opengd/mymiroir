using System;
using System.Threading;
using System.IO;
using System.Collections;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Diagnostics;

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

	public enum FileChangeType
	{
		New, Deleted, Compress, Copy, Change
	};

	public class FileChangeEventArgs {

		public string file;
		public FileChangeType fileChangeType;

		public FileChangeEventArgs(string file, FileChangeType fileChangeType)
		{
			this.file = file;
			this.fileChangeType = fileChangeType;
		}
	}
	
	public class mymiroir
	{
		public delegate void NewFileEventHandler(object sender, FileSystemEventArgs e);
		public delegate void CopyFileEventHandler(object sender, CopyFileEventArgs e);
		public delegate void CompressFileEventHandler(object sender, CompressFileEventArgs e);
		public delegate void FileChangeEventHandler(object sender, FileChangeEventArgs e);

		public event NewFileEventHandler NewFile;
		public event CopyFileEventHandler NewFileCopyStart;
		public event CopyFileEventHandler NewFileCopyFinish;
		public event CompressFileEventHandler NewFileCompressStart;
		public event CompressFileEventHandler NewFileCompressFinish;
		public event FileChangeEventHandler FileChange;

		ArrayList filePool;
		
		private FileSystemWatcher fsw;
		
		private Uri uriMirrorPath;
		private Uri uriWatchPath;

		public string MirrorPath
		{
			get {
				return (uriMirrorPath != null) ? uriMirrorPath.LocalPath : null;
			}
			set {
				if(!string.IsNullOrEmpty(value))
					Uri.TryCreate((value as string).TrimEnd(new char[]{'/', '\\'}), UriKind.RelativeOrAbsolute, out uriMirrorPath);
			}
		}
		
		public string WatchPath
		{
			get {
				return (uriWatchPath != null) ? uriWatchPath.LocalPath : null;
			}			
			set {
				if(!string.IsNullOrEmpty(value))
					Uri.TryCreate((value as string).TrimEnd(new char[]{'/', '\\'}), UriKind.RelativeOrAbsolute, out uriWatchPath);
			}
		}

		public string Exec {
			get;
			set;
		}

		public bool AddHash {
			get;
			set;
		}

		public bool AddTimestamp {
			get;
			set;
		}

		public bool Compress {
			get;
			set;
		}

		public bool Recursive {
			get;
			set;
		}

		private string _Filter = "*";

		public string Filter
		{
			get {
				return _Filter;
			}			
			set {
				_Filter = value;
			}
		}

		public bool IsUnixPlatform
		{
			get {
				return (Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
					? true
					: false;
			}
		}

		public mymiroir()
		{
			
		}

		public bool start (bool showconfigheader = false)
		{
			if(showconfigheader)
				Console.WriteLine(this.ToString());

			filePool = new ArrayList ();
				
			fsw = new FileSystemWatcher ();

			var suc = (Directory.Exists (WatchPath)) ? true : false;

			if (suc) {
				fsw.Path = WatchPath;

				fsw.Filter = Filter;

				fsw.NotifyFilter = NotifyFilters.LastWrite;

				fsw.IncludeSubdirectories = (Recursive) ? true : false; 
			
				fsw.Error += new ErrorEventHandler (fsw_OnError);
			
				//if(IsLinux) fsw.Changed += new FileSystemEventHandler(fsw_OnChanged);
				//else { 
				//fsw.Created += new FileSystemEventHandler(fsw_OnCreated);
				//}

				fsw.Changed += new FileSystemEventHandler (fsw_OnChanged);

				fsw.EnableRaisingEvents = true;
			}

			return suc;
		}
		
		private void fsw_OnError(object sender, ErrorEventArgs e)
		{
			Debug.WriteLine ("FSW: Error: " + e.GetException());
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
			var e = arguments as FileSystemEventArgs;
			
			string newMirrorFile;
			Debug.WriteLine (e.ChangeType.ToString());

			if(!checkFilePool(e.FullPath))
			{
				filePool.Add(e.FullPath);

				if(FileChange != null) FileChange(this, new FileChangeEventArgs(e.FullPath, FileChangeType.Change));

				string hash = null;
				string timestamp = null;

				if(Exec != null)
				{
					string exec = Exec;

					exec = exec.Replace("$0", e.Name);

					exec = exec.Replace("$1", e.FullPath);

					if(Exec.Contains("$2"))
					{
						hash = GetHash(e.FullPath);
						exec = Exec.Replace("$2", hash);
					}

					if(Exec.Contains("$3"))
					{
						timestamp = GetTimestamp();
						exec = exec.Replace("$3", timestamp);
					}

					exec = exec.Trim(new char[]{'"'});

					Console.WriteLine(exec);

					Process.Start(exec);
				}

				if(MirrorPath != null)
				{
					var pathEnd = (IsUnixPlatform) ? "/" : "\\";

					string ifhash = string.Empty;
					string iftimestamp = string.Empty;

					if(AddHash)
						ifhash = (string.IsNullOrEmpty(hash)) ? "." + GetHash(e.FullPath) : "." + hash;

					if(AddTimestamp)
						iftimestamp = (string.IsNullOrEmpty(timestamp)) ? "." + GetTimestamp() : "." + timestamp; 

					newMirrorFile = MirrorPath + 
						pathEnd + 
						e.Name + 
						ifhash + 
						iftimestamp;

					
					if(IsFileReady(e.FullPath)) 
					{
						if (NewFile != null) NewFile(this, e);
						
						if(NewFileCopyStart != null) NewFileCopyStart(this, new CopyFileEventArgs(e.FullPath, newMirrorFile));
						
						StepCopyFile(e.FullPath, newMirrorFile);
						
						if(NewFileCopyFinish != null) NewFileCopyFinish(this, new CopyFileEventArgs(e.FullPath, newMirrorFile));
						
						//fi1 = new FileInfo(newMirrorFile);
						//Console.WriteLine ("FSW: FileInfo: Mirror: " + fi1.Length);

						if(Compress)
						{
							if(NewFileCompressStart != null) NewFileCompressStart(this, new CompressFileEventArgs(newMirrorFile, newMirrorFile + ".gz"));
						
							StepCompressFile(newMirrorFile);
						
							if(NewFileCompressFinish != null) NewFileCompressFinish(this, new CompressFileEventArgs(newMirrorFile, newMirrorFile + ".gz"));
						}
					}
				}
				filePool.Remove(e.FullPath);
			}
			//StepRemoveFile(newMirrorFile);
			
		}
		
		private bool checkFilePool(string filename)
		{
			if(filename == null) 
				return true;

			foreach(string s in filePool)
			{
				//Console.WriteLine ("FSW: CheckFilePool: " + s);
				if(s.Equals(filename)) return true;
			}
			
			return false;
		}
		
		private bool IsFileReady(string inFile)
		{
			if(IsUnixPlatform) return true;
			
			while(true)
			{
				try
				{
					FileStream f = File.Open(inFile, FileMode.Open, FileAccess.Read, FileShare.None);
					f.Close();
					break;
				}
				catch (Exception e)
				{
					Debug.WriteLine(e.Message);
				//	Console.WriteLine ("FSW: IsFileReady: EXCEPTION");
				}
			}
			
			Debug.WriteLine ("FSW: IsFileReady: " + inFile);
			
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
			try
			{
				File.Delete(inFile);
			}
			catch (Exception e)
			{
				Debug.WriteLine(e.Message);
			}

			Debug.WriteLine ("FSW: Step Deleted: " + inFile);
		}

		private string GetHash(string file)
		{
			using(FileStream stream = File.OpenRead(file))
			{
				MD5CryptoServiceProvider hasher = new MD5CryptoServiceProvider();
				byte[] checksum = hasher.ComputeHash(stream);
				return BitConverter.ToString(checksum);
			}
		}

		private string GetTimestamp()
		{
			var time = DateTime.Now;
			return 	time.ToShortDateString() + 
					"." + 
					time.ToLongTimeString() + 
					"." + 
					time.Millisecond;
		}

		public string ToString ()
		{
			var ret = string.Empty;

			ret += "System: " + Environment.OSVersion.Platform + "\n";
			ret += "IsUnixPlatform: " + IsUnixPlatform + "\n";
			ret += "Watch: " + WatchPath + "\n";
			ret += "Mirror: " + MirrorPath + "\n";
			ret += "Recursive: " + Recursive + "\n";
			ret += "Compress: " + Compress + "\n";
			ret += "Hash: " + AddHash + "\n";
			ret += "Timestamp: " + AddTimestamp + "\n";

			return ret;
		}
	}
	
}

