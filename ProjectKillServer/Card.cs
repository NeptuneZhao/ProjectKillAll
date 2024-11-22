using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ProjectKill
{
    /// <summary>
    /// 被这个特性标记的方法在调用之前，会通过反射调用<see cref="Card"/>的<see cref="Card.ProcessCard"/>方法进行洗牌处理。
    /// <para></para>反射见<see cref="CardProxy.Invoke"/>。
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
	public class ProcessCardAttribute() : Attribute;

	/// <summary>
	/// 实现Card的接口。所有公共方法或字段必须在ICard中声明。
	/// </summary>
	public interface ICard
	{
		public static readonly List<string> CardGroup =
		[
			// R/Y/G/B/N 分别代表红、黄、绿、蓝、指定颜色
			// p/r/s/n/o分别代表禁止、调转、加二、指定颜色、指定颜色并加四
			"R0", "R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", "Rp", "Rr", "Rs", "Nn",
			"Y0", "Y1", "Y2", "Y3", "Y4", "Y5", "Y6", "Y7", "Y8", "Y9", "Yp", "Yr", "Ys", "Nn",
			"G0", "G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9", "Gp", "Gr", "Gs", "Nn",
			"B0", "B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "Bp", "Br", "Bs", "Nn",
			"R1", "R2", "R3", "R4", "R5", "R6", "R7", "R8", "R9", "Rp", "Rr", "Rs", "No",
			"Y1", "Y2", "Y3", "Y4", "Y5", "Y6", "Y7", "Y8", "Y9", "Yp", "Yr", "Ys", "No",
			"G1", "G2", "G3", "G4", "G5", "G6", "G7", "G8", "G9", "Gp", "Gr", "Gs", "No",
			"B1", "B2", "B3", "B4", "B5", "B6", "B7", "B8", "B9", "Bp", "Br", "Bs", "No"
		];

		void OnInitialize();

		string TakeCard();

		List<string> TakeCard(int n);

		List<string> OnFallingBackCard { get; }
	}

	/// <summary>
	/// Card的代理类。
	/// </summary>
	public class CardProxy() : DispatchProxy
	{
		private ICard _decorated;

		public void SetDecorated(ICard deco)
		{
			_decorated = deco;
		}

		protected override object Invoke(MethodInfo targetMethod, object[] args)
		{
			Console.WriteLine("Hey, I'm Invoke!");
			var implementationMethod = GetImplementationMethod(_decorated, targetMethod);
			if (implementationMethod.GetCustomAttribute<ProcessCardAttribute>() is not null)
			{
				MethodInfo processCardMethod = _decorated.GetType().GetMethod("ProcessCard", BindingFlags.NonPublic | BindingFlags.Instance);
				processCardMethod.Invoke(_decorated, null);
			}
			return targetMethod.Invoke(_decorated, args);
		}

		private static MethodInfo GetImplementationMethod(object obj, MethodInfo interfaceMethod)
		{
			var type = obj.GetType();
			var interfaceType = interfaceMethod.DeclaringType;
			var map = type.GetInterfaceMap(interfaceType);

			for (int i = 0; i < map.InterfaceMethods.Length; i++)
			{
				if (map.InterfaceMethods[i] == interfaceMethod)
					return map.TargetMethods[i];
			}

			throw new InvalidOperationException("Method not found in interface mapping.");
		}

	}

	/// <summary>
	/// <para>所有公共方法必须在ICard中声明为抽象。</para>
	/// <para>使用ICard实例来访问。实例化方法：<code><see cref="ICard"/> instance = <see cref="Instance"/>;</code></para>
	/// </summary>
	public class Card() : ICard
	{
		private static ICard CreateProxy(ICard target)
		{
			var proxy = DispatchProxy.Create<ICard, CardProxy>() as CardProxy;
			proxy.SetDecorated(target);
			return proxy as ICard;
		}

		/// <summary>
		/// <see cref="ICard"/>的实例。访问牌组时，使用这个。
		/// </summary>
		public static readonly ICard Instance = CreateProxy(new Card());

		private List<string> OnGoingCardGroup = [];
		public List<string> OnFallingBackCard { get; private set; } = [];

		public void OnInitialize() => OnGoingCardGroup = [.. ICard.CardGroup.OrderBy(x => Guid.NewGuid())];

		[ProcessCard]
		public string TakeCard()
		{
			string s = OnGoingCardGroup.First();
			OnGoingCardGroup.RemoveAt(0);
			return s;
		}

		[ProcessCard]
		public List<string> TakeCard(int n)
		{
			List<string> t = [];
			for (int i = 0; i < n; i++)
			{
				t.Add(OnGoingCardGroup.First());
				OnGoingCardGroup.RemoveAt(0);
			}

			return t;
		}

		[SuppressMessage("CodeQuality", "IDE0051:删除未使用的私有成员", Justification = "<挂起>")]
		private void ProcessCard()
		{
			if (OnGoingCardGroup.Count == 0)
			{
				OnGoingCardGroup = [.. OnFallingBackCard.Shuffle()];
				OnFallingBackCard = [];
			}
		}

	}
}