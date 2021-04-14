using AngleSharp;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace WallpapersManager {
	class Program {
		private static int page = 1;
		private static DateTime dateTime;
		private const int threadSleepMin = 15;
		private const int switchTimeInHours = 2;

		private const string savePath = @"C:\Users\moddem\Downloads\wallpapers\";
		private const string baseUrl = "https://wallpaperscraft.ru/all/2560x1080/";
		private const string downloadPage = "https://images.wallpaperscraft.ru/image/";

		private static List<string> imagesPath = new List<string>();
		private static bool firstRun = false;

		[STAThread]
		static void Main() {
			LoadSettings();

			while(true) {
				Console.Clear();

				if(TimeSpan.FromHours(switchTimeInHours) <= DateTime.Now - dateTime) {
					Console.WriteLine("Current application status: updating");
					Console.WriteLine("Please wait...");
					UpdateImage();
				}

				Console.WriteLine("Current application status: sleeping");
				Console.WriteLine("Images provider(website): {0}", baseUrl);
				Console.WriteLine("Current page: {0}", page);
				Console.WriteLine("Images location: {0}", savePath);
				Console.WriteLine("Next update in: {0}", dateTime + TimeSpan.FromHours(switchTimeInHours));
				Console.WriteLine("Next awake in: {0}", DateTime.Now.TimeOfDay + TimeSpan.FromMinutes(threadSleepMin));
				Console.WriteLine("Images amount in list: {0}", imagesPath.Count);
				for(int i = 0; i < imagesPath.Count; i++) {
					Console.WriteLine($" - ({i + 1}) {imagesPath[i]}");
				}

				Thread.Sleep(TimeSpan.FromMinutes(threadSleepMin));
			}
		}

		private static void LoadSettings() {
			firstRun = !Directory.Exists(savePath);
			Directory.CreateDirectory(savePath);

			WriteReadSettings();

			foreach(var item in Directory.GetFiles(savePath)) {
				if(item.Contains(".jpg"))
					imagesPath.Add(item.Substring(item.LastIndexOf('\\') + 1));
			}

			if(firstRun) UpdateImage();
		}

		private static async void UpdateImage() {
			if(imagesPath.Count <= 1) {
				if(!firstRun) {
					File.Delete(savePath + imagesPath[0]);
					imagesPath.RemoveAt(0);

					page++;
					dateTime = DateTime.Now;

					File.WriteAllText(savePath + ".sett", $"{dateTime},{page}");
				}
				
				DownloadImages(baseUrl + $"page{page}");
				return;
			}
			Wallpaper.Set(new Uri(savePath + imagesPath[0]), Wallpaper.Style.Span);

			await Task.Delay(500);

			File.Delete(savePath + imagesPath[0]);
			imagesPath.RemoveAt(0);

			dateTime = DateTime.Now;
			File.WriteAllText(savePath + ".sett", $"{dateTime},{page}");

			firstRun = false;
		}

		private static void WriteReadSettings() {
			using(var fs = new FileStream(savePath + ".sett", FileMode.OpenOrCreate)) {
				byte[] byteStr;

				if(fs.Length == 0) {
					dateTime = DateTime.Now;
					string toWrite = $"{dateTime},1";

					byteStr = Encoding.ASCII.GetBytes(toWrite);
					fs.Write(byteStr, 0, byteStr.Length);
				} else {
					byteStr = new byte[fs.Length];
					fs.Read(byteStr, 0, byteStr.Length);
					string fromFile = Encoding.ASCII.GetString(byteStr);

					dateTime = DateTime.Parse(fromFile.Split(',')[0]);
					page = int.Parse(fromFile.Split(',')[1]);
				}
			}
		}

		private static void DownloadImages(string pageUrl) {
			WebClient client = new WebClient();

			var context = BrowsingContext.New(Configuration.Default.WithDefaultLoader());
			var document = context.OpenAsync(pageUrl).Result;
			
			var images = document.QuerySelectorAll(".wallpapers__link");
			foreach(var image in images) {
				string url = image.GetAttribute("href");
				string name = url.Replace("/download/", string.Empty).Replace("/", "_") + ".jpg";

				imagesPath.Add(name);
				client.DownloadFile(downloadPage + name, savePath + name);
			}

			client.Dispose();
			document.Close();
			document.Dispose();

			UpdateImage();
		}
	}
}
