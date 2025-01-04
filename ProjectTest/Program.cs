using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace ProjectTest
{

	internal class Tile(string name, bool chong = false, bool outside = false) : IComparable
	{
		public string TileName { get; set; } = name;
		public bool Chong { get; set; } = chong;
		public bool Outside { get; set; } = outside;

		public int CompareTo(object obj)
		{
			if (obj == null) return 1;
			if (obj is Tile otherTile)
				return _compareOrder.GetValueOrDefault(TileName) - _compareOrder.GetValueOrDefault(otherTile.TileName);
			else
				throw new ArgumentException("Object is not a Tile");
		}

		private static readonly Dictionary<string, int> _compareOrder = new()
		{
			{ "1w", 00 }, { "2w", 01 }, { "3w", 02 }, { "4w", 03 }, { "5w", 04 }, { "6w", 05 }, { "7w", 06 }, { "8w", 07 }, { "9w", 08 },
			{ "1p", 09 }, { "2p", 10 }, { "3p", 11 }, { "4p", 12 }, { "5p", 13 }, { "6p", 14 }, { "7p", 15 }, { "8p", 16 }, { "9p", 17 },
			{ "1s", 18 }, { "2s", 19 }, { "3s", 20 }, { "4s", 21 }, { "5s", 22 }, { "6s", 23 }, { "7s", 24 }, { "8s", 25 }, { "9s", 26 },
			{ "0e", 27 }, { "0s", 28 }, { "0w", 29 }, { "0n", 30 }, { "0b", 31 }, { "0f", 32 }, { "0z", 33 }
		};
	}

	internal class Tiles : ICollection<Tile>, IEnumerable<Tile>
	{
		private const int count = 14;

		public int Count => count;

		public List<Tile> Pai = new(count);

		public bool CheckPai()
		{
			// 满足 13 + 1
			if (Pai.Count != Count) return false;
			// 集合中必须只有一个牌是铳
			if (Pai.Count(tile => tile.Chong) != 1) return false;

			return true;
		}

		public IEnumerator<Tile> GetEnumerator()
		{
			return Pai.GetEnumerator();
		}
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(Tile item)
		{
			Pai.Add(item);
		}

		public void Clear()
		{
			Pai.Clear();
		}

		public bool Contains(Tile item)
		{
			return Pai.Contains(item);
		}

		public void CopyTo(Tile[] array, int arrayIndex)
		{
			Pai.CopyTo(array, arrayIndex);
		}

		public bool Remove(Tile item)
		{
			return Pai.Remove(item);
		}

		public bool IsReadOnly => false;

	}
	public enum Wind
	{
		East,
		South,
		West,
		North
	}

	internal class Player(Wind Changwind, Wind Menwind, Tiles tiles)
	{
		public Wind Changwind = Changwind, Menwind = Menwind;
		public bool Richi { get; internal set; } = false;
		public bool DoubleRichi { get; internal set; } = false;
		public bool Yifa { get; internal set; } = false;

		/// <summary>
		/// 包括岭上和枪杠。
		/// </summary>
		public bool Lingshang { get; internal set; } = false;
		public bool Haidilao { get; internal set; } = false;
		
		/// <summary>
		/// 拔北宝牌。
		/// </summary>
		public int Bei { get; internal set; } = 0;

		public Tiles Tiles { get; internal set; } = tiles;
	}

	internal class Yaku(Player _player)
	{
		private int yaku = 0;
		private readonly Player player = _player;
		private readonly Tiles tiles = _player.Tiles;

		public void Initialize()
		{

		}

		public int Yaku1()
		{
			int _yaku = 0;

			// Richi
			if (player.Richi) _yaku++;
			if (player.DoubleRichi) _yaku += 2;

			// Yifa
			if (player.Yifa) _yaku++;

			// Menqing
			foreach (Tile tile in tiles)
			{

			}
				// Yibeikou
				// Pinfu
				// Duanyao

			
			return _yaku;
		}

	}

	internal class Program
	{
		public static void Main()
		{
			Tiles tiles =
			[
				new Tile("1p")
			];
		}
	}
}