using System;
using System.Linq;
using System.Reflection;
using Mod.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace OTAPI.Modules
{
	/// <summary>
	/// This module removes the windows SetConsoleCtrlHandler call as mono is
	/// unable to execute it.
	/// </summary>
	[Module("Mono: Removing SetConsoleCtrlHandler", "death")]
	public class SetConsoleCtrlHandler : RunnableModule
	{
		private ModFramework _framework;

		public SetConsoleCtrlHandler(ModFramework framework)
		{
			_framework = framework;
		}

		public override void Run()
		{
			foreach (var asm in this.Assemblies
				.Where(x => x.Name.Name.IndexOf("Terraria", StringComparison.CurrentCultureIgnoreCase) > -1))
			{
				var method = asm.Type("Terraria.WindowsLaunch").Method("Main");
				var target = method.Body.Instructions.Single(x =>
					x.OpCode == OpCodes.Call
					&& (x.Operand as MethodReference).Name == "SetConsoleCtrlHandler"
				);

				method.RemoveCall(target);
			}
		}
	}
}
