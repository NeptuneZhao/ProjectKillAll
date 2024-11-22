using System;
using System.Net;
using System.Net.Sockets;

namespace ProjectKillClient
{
	internal class Program
	{
		public static TcpClient Client = new();

		static void Main()
		{

		}

		static void Connect()
		{
			try
			{
				Client.Connect(IPAddress.Parse("127.0.0.1"), 13000);
			}
			catch
			{

			}
		}
	}
}
