using System;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using Terraria;
using Terraria.Localization;
using Terraria.Net;
using Terraria.Net.Sockets;

namespace OTAPI.Server.Demo
{
	class MainClass
	{
		public static void Main(string[] args)
		{
			try
			{
				Terraria.Main.ignoreErrors = true;
				Terraria.Program.ForceLoadAssembly(typeof(Terraria.Program).Assembly, true);

				//ModFramework.ModHooks.Netplay.PreServerLoop = (object ctx) =>
				//{
				//	try
				//	{
				//		Terraria.Netplay.ServerLoopDirect(ctx);
				//	}
				//	catch(Exception ex)
				//	{
				//		Console.WriteLine(ex);
				//	}
				//	return false;
				//};

				Terraria.WindowsLaunch.Main(args);
			}
			catch (Exception ex)
			{
				Console.WriteLine(ex);
			}
		}
	}
}

