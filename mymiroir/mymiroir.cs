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
using System.Threading;
using System.IO;
using System.Collections;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Diagnostics;

namespace mymiroir
{
	public class FileChangeEventArgs {

		public FileSystemEventArgs file;
		public string hash;
		public string timestamp;

		public FileChangeEventArgs(FileSystemEventArgs file, string hash, string timestamp) {
			this.file = file;
			this.hash = hash;
			this.timestamp = timestamp;
		}
	}
	
	public class mymiroir
	{
		public delegate void FileChangeEventHandler(object sender, FileChangeEventArgs e);

		public event FileChangeEventHandler FileChange;

		ArrayList filePool;
		
		private FileSystemWatcher fsw;

		object changed_lock = new object();
		
		private Uri uriMirrorPath;
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

		private Uri uriWatchPath;
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

		public bool Hash {
			get;
			set;
		}

		public bool Timestamp {
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

		public bool Ignore {
			get;
			set;
		}

		public bool Remove {
			get;
			set;
		}

		public bool Verbose {
			get;
			set;
		}

		private string _Filter = "*";
		public string Filter {
			get {
				return _Filter;
			}			
			set {
				_Filter = value;
			}
		}

		public string MirrorFile {
			get;
			set;
		}

		public bool IsUnixPlatform {
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

			var suc = (Ignore || Directory.Exists (WatchPath)) ? true : false;

			if (suc) {
				fsw.Path = WatchPath;

				fsw.Filter = Filter;

				fsw.NotifyFilter = NotifyFilters.LastWrite;

				fsw.IncludeSubdirectories = (Recursive) ? true : false; 
			
				fsw.Error += new ErrorEventHandler (fsw_OnError);

				fsw.Changed += new FileSystemEventHandler (fsw_OnChanged);

				fsw.EnableRaisingEvents = true;
			}

			return suc;
		}
		
		private void fsw_OnError(object sender, ErrorEventArgs e)
		{
			Console.WriteLine ("mymiroir: Error: " + e.GetException().Message);
		}
		
		private void fsw_OnChanged(object sender, FileSystemEventArgs e)
		{
			Thread t = new Thread(OnNewFile);
			t.Start(e);
		}

		private void OnNewFile (object arguments)
		{
			var e = arguments as FileSystemEventArgs;

			lock (changed_lock) {

				if (filePool.Contains (e.FullPath))
					return;

				filePool.Add(e.FullPath);
			}

			string newMirrorFile;

			string hash = (Hash) ? GetHash(e.FullPath) : null;
			string timestamp = (Timestamp) ? GetTimestamp() : null;

			if(FileChange != null) 
				FileChange(this, new FileChangeEventArgs(e, hash, timestamp));

			if(Exec != null) {
				var exec = Exec.Replace("%0", e.Name);

				if(Exec.Contains("%1")) {
					hash = (string.IsNullOrEmpty(hash)) ? GetHash(e.FullPath) : hash;
					exec = Exec.Replace("%1", hash);
				}

				if(Exec.Contains("%2")) {
					timestamp = (string.IsNullOrEmpty(timestamp)) ? GetTimestamp() : timestamp;
					exec = exec.Replace("%2", timestamp);
				}

				exec = exec.Replace("%3", e.FullPath);

				exec = exec.Trim(new char[]{'"'});

				if(Verbose) 
					Console.WriteLine("Exec: Start: " + exec);

				Process.Start(exec);

				if(Verbose) 
					Console.WriteLine("Exec: Finish: " + exec);
			}

			if(MirrorPath != null) {
				var pathEnd = (IsUnixPlatform) ? "/" : "\\";

				string ifhash = string.Empty;
				string iftimestamp = string.Empty;

				if(string.IsNullOrEmpty(MirrorFile)) {

					ifhash = (string.IsNullOrEmpty(hash)) ? "." + GetHash(e.FullPath) : "." + hash;

					iftimestamp = (string.IsNullOrEmpty(timestamp)) ? "." + GetTimestamp() : "." + timestamp;

					newMirrorFile = MirrorPath + 
						pathEnd + 
						e.Name + 
						ifhash + 
						iftimestamp;
				}
				else {

					newMirrorFile = MirrorFile.Replace("%0", e.Name);

					if(newMirrorFile.Contains("%1")) {
						hash = (string.IsNullOrEmpty(hash)) ? GetHash(e.FullPath) : hash;
						newMirrorFile = newMirrorFile.Replace("%1", hash);
					}

					if(newMirrorFile.Contains("%2")) {
						timestamp = (string.IsNullOrEmpty(timestamp)) ? GetTimestamp() : timestamp;
						newMirrorFile = newMirrorFile.Replace("%2", timestamp);
					}

					newMirrorFile = MirrorPath + pathEnd + newMirrorFile.Trim(new char[]{'"'});
				}

				
				if(IsFileReady(e.FullPath)) 
				{
					if(Verbose) 
						Console.WriteLine("MirrorFile: Start: " + newMirrorFile);

					bool suc_mirror = true;

					try {
						File.Copy(e.FullPath, newMirrorFile);

						if(Verbose) 
							Console.WriteLine("MirrorFile: Finish: " + newMirrorFile);
					}
					catch (Exception ex) {
						suc_mirror = false;

						if(Verbose) 
							Console.WriteLine("MirrorFile: Exception: " + ex.Message);
					}

					if(suc_mirror && Compress) {
						CompressFile(newMirrorFile);
					}
				}
			}

			if (Remove)
				RemoveFile(e.FullPath);

			filePool.Remove(e.FullPath);
		}
		
		private bool IsFileReady(string inFile)
		{
			if(IsUnixPlatform) 
				return true;
			
			while(true) {
				try {
					FileStream f = File.Open(inFile, FileMode.Open, FileAccess.Read, FileShare.None);
					f.Close();
					break;
				}
				catch (Exception e) {
					if(Verbose)
						Console.WriteLine("IsFileReady: Exception: " + e.Message);
				}
			}

			return true;
		}
		
		private void CompressFile(string inFile)
		{
			using ( FileStream iFile = File.OpenRead(inFile) ) {
				using ( FileStream oFile = File.Create(inFile + ".gz") ) {

					if(Verbose)
						Console.WriteLine("Compress: Start: " + oFile.Name);

					using (GZipStream Compress = new GZipStream(oFile, CompressionMode.Compress)) {
						iFile.CopyTo(Compress);
					}

					if(Verbose)
						Console.WriteLine("Compress: Finish: " + oFile.Name);
				}
			}
		}
		
		private void RemoveFile(string inFile)
		{
			if(Verbose)
				Console.WriteLine ("Remove: Start: " + inFile);

			try {
				File.Delete(inFile);
				if(Verbose)
					Console.WriteLine ("Remove: Finish: " + inFile);
			}
			catch (Exception e) {
				Console.WriteLine("Remove: Exception: " + e.Message);
			}
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

		public override string ToString ()
		{
			var ret = string.Empty;

			ret += "System: " + Environment.OSVersion.Platform + "\n";
			ret += "IsUnixPlatform: " + IsUnixPlatform + "\n";
			ret += "Watch: " + WatchPath + "\n";
			ret += "Mirror: " + MirrorPath + "\n";
			ret += "Recursive: " + Recursive + "\n";
			ret += "Compress: " + Compress + "\n";
			ret += "Hash: " + Hash + "\n";
			ret += "Timestamp: " + Timestamp + "\n";
			ret += "Ignore: " + Ignore + "\n";
			ret += "Remove: " + Remove + "\n";
			ret += "Verbose: " + Verbose + "\n";
			ret += "MirrorFile: " + MirrorFile + "\n";

			return ret;
		}
	}
	
}

