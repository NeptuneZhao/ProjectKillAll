using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ProjectKill
{
	internal class Program
	{
		public static readonly ShareData CommonDataLock = new();
		public static readonly TcpListener server = new(IPAddress.Any, 13000);

		private static readonly AutoResetEvent StartGameEvent = new(false);

		public static bool isRunning = true, isStartedGame = false;
		public static int ConnectedClients = 0, GetReadyPlayers = 0;

		public static List<Player> PlayerList = new(4);
		private static Game Game = null;

		[MTAThread]
		static void Main()
		{
			try
			{
				server.Start(4);
				Console.WriteLine("服务器已监听于0.0.0.0:13000。");

				new Thread(new ThreadStart(AcceptClients)) { Name = "连接客户端" }.Start();
				new Thread(new ThreadStart(BroadcastConnection)) { Name = "广播连接" }.Start();
				new Thread(new ThreadStart(StartGame)) { Name = "人满后开始游戏" }.Start();

				// 这里是主线程
				Console.ReadKey();
			}
			catch (SocketException e)
			{
				Console.WriteLine("SocketException: {0}", e);
				isRunning = false;
			}
		}

		/// <summary>
		/// 开始游戏。这是已经写好的方法，不需要修改。
		/// </summary>
		static void StartGame()
		{
			StartGameEvent.WaitOne();
			StartGameEvent.Dispose();

			Game = new();
			Game.OnInitialize();
			isStartedGame = true;

			Console.WriteLine("游戏开始啦！");
			SendTargetHandle(0, "游戏开始啦！");
		}

		/// <summary>
		/// 接受客户端连接。
		/// </summary>
		static void AcceptClients()
		{
			while (isRunning)
			{
				try
				{
					TcpClient client = server.AcceptTcpClient();
					Console.WriteLine("连接到客户端: {0}", client.Client.RemoteEndPoint.ToString());
					lock (CommonDataLock.SharedLock)
						ConnectedClients++;

					Console.WriteLine("当前连接数: {0}/4", ConnectedClients);
					PlayerList.Add( new((IPEndPoint)client.Client.RemoteEndPoint));

					// 为每个客户端开启一个接收、一个通信线程
					new Thread(new ParameterizedThreadStart(ReceiveHandle)) { Name = $"接收{client.Client.RemoteEndPoint}" }.Start(client);
					new Thread(new ParameterizedThreadStart(SendHandle)) { Name = $"发送{client.Client.RemoteEndPoint}" }.Start(client);
				}
				catch (SocketException)
				{
					break;
				}
			}

		}

		/// <summary>
		/// 广播连接的线程。
		/// </summary>
		static void BroadcastConnection()
		{
			while (isRunning && ConnectedClients < 4)
			{
				// 阻塞，直到收到通知
				CommonDataLock.DataReceivedEvent.WaitOne();

				lock (CommonDataLock.SharedLock)
				{
					foreach (Player player in PlayerList)
					{
						string msg = $"连接到的玩家：端口{player.IPEndPoint}，名称{player.Name}";
						Console.WriteLine(msg);
						SendTargetHandle(0, msg);
					}
				}
			}
			if (ConnectedClients == 4)
			{
				Console.WriteLine("已连接满4人，开始游戏。");
				if (GetReadyPlayers == 4)
				{
					Console.WriteLine("所有玩家已准备，开始游戏。");
					StartGameEvent.Set();
				}
				return;
			}
		}

		/// <summary>
		/// <para>处理发送请求的最终方法。</para>
		/// <para>该方法作为一个线程在程序的生命周期内一直存在。</para>
		/// <para>当尝试在其他区域进行发送操作时，不要调用此方法。</para>
		/// <para>请调用<see cref="SendTargetHandle(object, string)"/></para>
		/// </summary>
		/// <param name="obj">委托给线程的TcpClient。</param>
		static void SendHandle(object obj)
		{

			TcpClient client = (TcpClient)obj;
			NetworkStream stream = client.GetStream();
			Player currentPlayer = null;

			lock (CommonDataLock.SharedLock)
			{
				foreach (Player player in PlayerList)
				{
					if (player.IPEndPoint == (IPEndPoint)client.Client.RemoteEndPoint)
					{
						currentPlayer = player;
						break;
					}
				}
			}

			if (currentPlayer == null)
				return;

			try
			{
				while (isRunning && client.Connected)
				{
					if (!currentPlayer.SendToHimEvent.WaitOne(1000))
					{
						if (!client.Connected)
							break;
						continue;
					}

					lock (CommonDataLock.SharedLock)
					{
						byte[] msg = Encoding.UTF8.GetBytes((string)CommonDataLock.SharedLock);
						stream.Write(msg, 0, msg.Length);
						Console.WriteLine("Send to player {0} OK.", currentPlayer.IPEndPoint);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("客户端通信异常：{0}", e.Message);
			}
			finally
			{
				stream.Close();
				client.Close();
				Console.WriteLine("发送线程已关闭。");
			}

		}

		/// <summary>
		/// <para>发送消息给指定玩家。</para>
		/// <para>该方法应该在其他地方发送时调用。</para>
		/// <para>此方法的终点位置为<see cref="SendHandle(object)"/></para>
		/// </summary>
		/// <param name="e">发送指定的客户端。如果参数不为<see cref="Player"/>，则为发送到所有客户端。</param>
		/// <param name="msg">待发送的消息。</param>
		public static void SendTargetHandle(object e, string msg)
		{
			lock (CommonDataLock.SharedLock)
				CommonDataLock.SharedLock = msg;
			if (e is not Player)
			{
				foreach (Player player in PlayerList)
					player.SendToHimEvent.Set();
			}
			else
				((Player)e).SendToHimEvent.Set();
		}

		public static void ReceiveHandle(object obj)
		{
			TcpClient client = (TcpClient)obj;
			NetworkStream stream = client.GetStream();

			byte[] bytes = new byte[4096];
			int i = 0;

			try
			{
				while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
				{
					// 转换数据
					string data = Encoding.UTF8.GetString(bytes, 0, i);
					Console.WriteLine("收到客户端消息： {0}", data);

					if (isStartedGame)
					{
						lock (Game.ReceiveMessage)
						{
							Game.ReceiveMessage = $"{client.Client.RemoteEndPoint}," + data;
							Game.DataReceivedEvent.Set();
						}
					}

					string[] strings = Prototype.ProcessRecvData(data);

					foreach (Player player in PlayerList)
					{
						if (player.IPEndPoint == (IPEndPoint)client.Client.RemoteEndPoint)
							ProcessRecvMsg(player, strings[0], strings[1]);
					}
				}
			}
			catch (Exception e)
			{
				Console.WriteLine("客户端通信异常：{0}", e.Message);
			}
			finally
			{
				lock (CommonDataLock.SharedLock)
				{
					ConnectedClients--;
					foreach (Player player in PlayerList)
					{
						if (player.IPEndPoint == (IPEndPoint)client.Client.RemoteEndPoint)
						{
							Console.WriteLine("{0}移除玩家实例。", PlayerList.Remove(player) ? "成功" : "没能成功");
							break;
						}
					}
				}
				stream.Close();
				client.Close();
				Console.WriteLine($"客户端已断开连接。\n当前连接数: {ConnectedClients}/4\n接收线程已关闭。");
			}

		}

		public static bool ProcessRecvMsg(Player player, string operation, string msg)
		{
			switch (operation)
			{
				case "name":
					lock (CommonDataLock.SharedLock)
					{
						CommonDataLock.DataReceivedEvent.Set();
						// 检查是否有重名
						foreach (Player p in PlayerList)
						{
							if (p.Name == msg && p != player)
							{
								Console.WriteLine("Name already exists!");
								SendTargetHandle(player, "name:rename");
								CommonDataLock.DataReceivedEvent.Reset();
								return false;
							}
						}
						player.Name = msg;
						SendTargetHandle(player, "name:ok");
					}
					return true;
				case "chat":
					SendTargetHandle(0, $"Player {player.Name} says: {msg}");
					return true;
				case "ready":
					lock (CommonDataLock.SharedLock)
					{
						CommonDataLock.SharedLock = $"Player {0} is ready!";
						SendTargetHandle(0, (string)CommonDataLock.SharedLock);
						GetReadyPlayers++;
					}
					return true;
				case "unready":
					lock (CommonDataLock.SharedLock)
					{
						CommonDataLock.SharedLock = $"Player {0} is unready!";
						SendTargetHandle(0, (string)CommonDataLock.SharedLock);
						GetReadyPlayers--;
					}
					return true;
				default:
					return false;
			}
		}

	}

}