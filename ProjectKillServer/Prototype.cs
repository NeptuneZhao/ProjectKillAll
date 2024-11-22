using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;

namespace ProjectKill
{
	internal static class Prototype
	{
		// 规定发送的数据格式
		public enum MessageType
		{
			name, // 玩家名称
			chat, // 玩家聊天
			ready, // 玩家准备
			unready, // 玩家取消准备
		}

		public static string TakeFormattable(MessageType type, string t) => t + type switch
		{
			MessageType.name => "name:",
			MessageType.chat => "chat:",
			MessageType.ready => "ready:",
			MessageType.unready => "unready:",
			_ => "null:",
		};

		public static string TakeFormattable(string type, string t) => type + t;

		public static string[] ProcessRecvData(string data)
		{
			string[] strings = data.Split(':');
			return strings.Length == 2 ? strings : [ strings[0], $"{string.Join(":", strings.Skip(1))}:"[..^1] ];
		}

		public static IList<T> Shuffle<T>(this IList<T> list) => [.. list.OrderBy(x => Guid.NewGuid())];


	}

	internal class ShareData
	{
		public object SharedLock = new();
		public AutoResetEvent DataReceivedEvent = new(false);
	}

	internal class Player(IPEndPoint iPEndPoint)
	{
		public int InGameSeat { get; set; }

		public IPEndPoint IPEndPoint { get; set; } = iPEndPoint;

		public string Name { get; set; }

		public AutoResetEvent SendToHimEvent = new(false);

		public List<string> HandleCard = [];
	}
}
