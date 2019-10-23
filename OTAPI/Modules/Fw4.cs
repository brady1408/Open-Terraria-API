using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
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
				ReplaceReferences(asm, "System.Private.CoreLib", "mscorlib.dll");
				ReplaceReferences(asm, "System.Console", "mscorlib.dll");
				ReplaceReferences(asm, "System.Net", "System.dll");
				ReplaceReferences(asm, "System.Net.Primitives", "System.dll");
			}
		}

		void ReplaceReferences(Mono.Cecil.AssemblyDefinition asm, string source, string dll)
		{
			var coreDir = Directory.GetParent(typeof(Object).GetTypeInfo().Assembly.Location);

			var msc = coreDir.FullName + Path.DirectorySeparatorChar + dll;
			var mscasm = Mono.Cecil.AssemblyDefinition.ReadAssembly(msc);

			var reference = asm.MainModule.AssemblyReferences
				.Where(x => x.Name.StartsWith(source, StringComparison.CurrentCultureIgnoreCase))
				.ToArray();

			for (var x = 0; x < reference.Length; x++)
			{
				reference[x].Name = mscasm.Name.Name;
				reference[x].PublicKey = mscasm.Name.PublicKey;
				reference[x].PublicKeyToken = mscasm.Name.PublicKeyToken;
				reference[x].Version = mscasm.Name.Version;
			}
		}
	}
}
