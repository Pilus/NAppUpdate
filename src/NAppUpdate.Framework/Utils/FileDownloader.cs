using System;
using System.Globalization;
using System.IO;
using System.Net;
using System.Threading;
using NAppUpdate.Framework.Common;

namespace NAppUpdate.Framework.Utils
{
	public sealed class FileDownloader
	{
		private readonly Uri _uri;
		private const int _bufferSize = 1024;
		public IWebProxy Proxy { get; set; }

		public FileDownloader()
		{
			Proxy = null;
		}

		public FileDownloader(string url)
		{
			_uri = new Uri(url);
		}

		public FileDownloader(Uri uri)
		{
			_uri = uri;
		}

		public byte[] Download()
		{
			using (var client = new WebClient())
				return client.DownloadData(_uri);
		}

		public bool DownloadToFile(string tempLocation)
		{
			return DownloadToFile(tempLocation, null);
		}

		public bool DownloadToFile(string tempLocation, Action<UpdateProgressInfo> onProgress, int retry = 0)
		{
			var request = WebRequest.Create(_uri);
			request.Proxy = Proxy;

			const int reportInterval = 1;
			bool done = false;
			using (WebClient wc = new WebClient())
			{
				DateTime stamp = DateTime.Now.Subtract(new TimeSpan(0, 0, reportInterval));
				long totalBytes = 0;
				wc.DownloadProgressChanged += (object sender, DownloadProgressChangedEventArgs e) =>
				{
					totalBytes = e.TotalBytesToReceive;
					if (onProgress == null || !(DateTime.Now.Subtract(stamp).TotalSeconds >= reportInterval))
					{
						return;
					}

					ReportProgress(onProgress, e.BytesReceived, e.TotalBytesToReceive);
					stamp = DateTime.Now;
				};
				wc.DownloadFileCompleted += (sender, args) =>
				{
					done = true;
				};
				wc.DownloadFileAsync(_uri,tempLocation);

				while (!done)
				{
					Thread.Sleep(10);
				}
				
				//File.Copy(tempLocation, Path.Combine(@"C:\Temp\Kingmaker", (tempLocation)));
				ReportProgress(onProgress, totalBytes, totalBytes);
			}

			return true;

			/*
			try
			{
				long downloadSize;
				long totalBytes = 0;
				using (var response = request.GetResponse())
				{
					using (var tempFile = File.Create(tempLocation))
					{
						using (var responseStream = response.GetResponseStream())
						{
							if (responseStream == null)
								throw new Exception($"No response stream for {_uri}");

							downloadSize = response.ContentLength;
							var buffer = new byte[_bufferSize];
							const int reportInterval = 1;
							DateTime stamp = DateTime.Now.Subtract(new TimeSpan(0, 0, reportInterval));
							int bytesRead;
							do
							{
								bytesRead = responseStream.Read(buffer, 0, buffer.Length);
								totalBytes += bytesRead;
								tempFile.Write(buffer, 0, bytesRead);

								if (onProgress == null || !(DateTime.Now.Subtract(stamp).TotalSeconds >= reportInterval))
								{
									continue;
								}

								ReportProgress(onProgress, totalBytes, downloadSize);
								stamp = DateTime.Now;
							} while (bytesRead > 0 && !UpdateManager.Instance.ShouldStop);

							ReportProgress(onProgress, totalBytes, downloadSize);
							if (totalBytes == downloadSize)
							{
								return true;
							}

							
						}
					}
				}
				
				if (retry <= 0)
				{	
					File.Copy(tempLocation, @"c:\Temp\response");
					throw new Exception($"Byte mismatch for {_uri}. Expected {downloadSize}, got {totalBytes}.");
				}
				else
				{
					Thread.Sleep(100);
					return DownloadToFile(tempLocation, onProgress, retry - 1);
				}
			}
			catch (WebException e)
			{
				var msg = e.Message + $" at {_uri}";
				throw new Exception(msg, e);
			}*/
		}

		private void ReportProgress(Action<UpdateProgressInfo> onProgress, long totalBytes, long downloadSize)
		{
			if (onProgress != null) onProgress(new DownloadProgressInfo
			{
				DownloadedInBytes = totalBytes,
				FileSizeInBytes = downloadSize,
				Percentage = (int)(((float)totalBytes / (float)downloadSize) * 100),
				Message = string.Format("Downloading... ({0} / {1} completed)", ToFileSizeString(totalBytes), ToFileSizeString(downloadSize)),
				StillWorking = totalBytes == downloadSize,
			});
		}

		private string ToFileSizeString(long size)
		{
			if (size < 1000) return String.Format("{0} bytes", size);
			if (size < 1000000) return String.Format("{0:F1} KB", (size / 1000));
			if (size < 1000000000) return String.Format("{0:F1} MB", (size / 1000000));
			if (size < 1000000000000) return String.Format("{0:F1} GB", (size / 1000000000));
			if (size < 1000000000000000) return String.Format("{0:F1} TB", (size / 1000000000000));
			return size.ToString(CultureInfo.InvariantCulture);
		}

		/*
		public void DownloadAsync(Action<byte[]> finishedCallback)
		{
			DownloadAsync(finishedCallback, null);
		}

		public void DownloadAsync(Action<byte[]> finishedCallback, Action<long, long> progressChangedCallback)
		{
			using (var client = new WebClient())
			{
				if (progressChangedCallback != null)
					client.DownloadProgressChanged += (sender, args) => progressChangedCallback(args.BytesReceived, args.TotalBytesToReceive);

				client.DownloadDataCompleted += (sender, args) => finishedCallback(args.Result);
				client.DownloadDataAsync(_uri);
			}
		}*/
	}
}
