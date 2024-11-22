using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace ProjectKill
{
	internal class Game : IDisposable
	{
		/// <summary>
		/// 用于存储玩家的列表。传入时，已经初始化为 <see cref="Program.PlayerList"/>。
		/// </summary>
		private List<Player> Players = null;

		private readonly ICard _Card = Card.Instance;

		/// <summary>
		/// <para>用于接收数据的锁。</para>
		/// <para>数据格式严格遵循如下约定：</para>
		/// <para>(IPv4 Address):(EndPoint),(DataType):(Message)</para>
		/// </summary>
		public object ReceiveMessage = null;
		public AutoResetEvent DataReceivedEvent = new(false);

		public void Dispose() => GC.SuppressFinalize(this);

		/// <summary>
		/// 初始化游戏。
		/// </summary>
		public void OnInitialize()
		{
			ReceiveMessage = new();
			_Card.OnInitialize();
			Players = [.. Program.PlayerList.Shuffle()];

			// 为每个玩家分配卡牌8张
			int i = 0;
			foreach (Player player in Players)
			{
				player.InGameSeat = i++;
				player.HandleCard = _Card.TakeCard(8);
			}

			new Thread(new ThreadStart(OnReceiveData)) { Name = "游戏中接收" }.Start();
		}

		private void OnReceiveData()
		{
			while (Program.isRunning)
			{
				DataReceivedEvent.WaitOne();
				lock (ReceiveMessage)
				{
					// 在此处处理数据
					string[] splits = ReceiveMessage.ToString().Split(':');
					OnProcessData(new IPEndPoint(IPAddress.Parse(splits[0]), Convert.ToInt32(splits[1])), splits[2], splits[3]);
				}
			}
		}

		private void OnProcessData(IPEndPoint client, string operation, string message)
		{
			string name = string.Empty;
			foreach (Player player in Players)
			{
				if (player.IPEndPoint == client)
					name = player.Name;
			}

			switch (operation)
			{
				case "chat":
					OnSendingMsg(0, $"{name} says: {message}");
					break;
				default:
					break;

			}
		}

		/// <summary>
		/// <para>Also declare as:</para>
		/// <para><code>
		/// private static void OnSendingMsg(object, string) => <see cref="Program.SendTargetHandle(object, string)"/>;
		/// </code></para>
		/// </summary>
		private static readonly Action<object, string> OnSendingMsg = Program.SendTargetHandle;
	}
}
