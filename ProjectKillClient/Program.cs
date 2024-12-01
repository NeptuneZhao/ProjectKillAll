using System;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace ProjectKillClient
{
	internal class Program
	{
		public static TcpClient Client = new();
		public static NetworkStream Stream = null;
		public static IPEndPoint IPEndPoint = new(IPAddress.Parse("127.0.0.1"), 13000);
		public static string Name;

		public static bool isRunning = true;

		static void Main()
		{
			Console.Write("正在连接到服务器");
			Write(IPEndPoint.ToString(), ConsoleColor.Gray);
			Console.WriteLine("...");
			Client.Connect(IPEndPoint);
			WriteLine("已连接到服务器！", ConsoleColor.Green);
			Stream = Client.GetStream();
			WriteLine("成功与服务器建立数据流连接。", ConsoleColor.DarkGreen);

			new Thread(new ThreadStart(ReceiveMsg)) { Name = "接收消息" }.Start();
			
			Console.Write("输入名字：");
			Name = Read(ConsoleColor.Yellow);
			SendMsg($"name:{Name}");

			while (isRunning)
			{
				Console.Title = "现在是等待时间，输入name:更改名字，输入chat:进行聊天";
				string msg = Read(ConsoleColor.Yellow);
				if (msg.StartsWith("name:") || msg.StartsWith("chat:"))
					SendMsg(msg);

			}
			Console.ReadKey();

			// Here is the main thread.
		}

		public static void ReceiveMsg()
		{
			byte[] bytes = new byte[4096];
			int i = 0;
			try
			{
				while ((i = Stream.Read(bytes, 0, bytes.Length)) != 0)
				{
					string msg = Encoding.UTF8.GetString(bytes, 0, i);
					Console.WriteLine(msg);
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("服务器通信异常：{0}", e.Message);
			}

		}


		public static void SendMsg(string msg)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(msg);
			Stream.Write(bytes, 0, bytes.Length);
		}

		public static void WriteLine(string write, ConsoleColor color)
		{
			Console.ForegroundColor = color;
			Console.WriteLine(write);
			Console.ResetColor();
		}

		public static void Write(string write, ConsoleColor color)
		{
			Console.ForegroundColor = color;
			Console.Write(write);
			Console.ResetColor();
		}

		public static string Read(ConsoleColor color)
		{
			Console.ForegroundColor = color;
			string p = Console.ReadLine();
			Console.ResetColor();
			return p;
		}

	}
}
