using System;
using System.Linq;
using System.Reflection;
using Mod.Framework;

namespace OTAPI.Modules
{
	[Module("Ensuring patched changes are .net 4 compatible", "death", 999)]
	public class Fw4 : RunnableModule
	{
		private ModFramework _framework;

		public Fw4(ModFramework framework)
		{
			_framework = framework;
		}

		public override void Run()
		{
			foreach (var asm in this.Assemblies
				.Where(x => x.Name.Name.IndexOf("Terraria", StringComparison.CurrentCultureIgnoreCase) > -1))
			{
				asm.ReplaceReferences("System.Private.CoreLib", "mscorlib.dll");
				asm.ReplaceReferences("System.Console", "mscorlib.dll");
				asm.ReplaceReferences("System.Net", "System.dll");
				asm.ReplaceReferences("System.Net.Primitives", "System.dll");
			}
		}
	}
}
