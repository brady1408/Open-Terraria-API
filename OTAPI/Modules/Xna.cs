using System;
using System.Linq;
using Mod.Framework;
using Mono.Cecil;

namespace OTAPI.Modules
{
	/// <summary>
	/// This module will resolve Xna references using the OTAPI.Xna library
	/// </summary>
	[Module("Adding Xna shims for cross platform compatibility", "death")]
	public class Xna : RunnableModule
	{
		private ModFramework _framework;
		private AssemblyDefinition _xna;

		public Xna(ModFramework framework)
		{
			_framework = framework;
			_xna = _framework.CecilAssemblies.Single(x => x.Name.Name == "OTAPI.Xna");
		}

		public override void Run()
		{
			foreach (var asm in this.Assemblies)
			{
				asm.ReplaceReferences("Microsoft.Xna.Framework", _xna.Name);
			}
		}

		public override AssemblyDefinition ResolveAssembly(AssemblyNameReference name)
		{
			if (name.FullName.StartsWith("Microsoft.Xna", StringComparison.CurrentCultureIgnoreCase)
				|| name.Name.Equals("OTAPI.Xna")
			)
			{
				return _xna;
			}
			return base.ResolveAssembly(name);
		}

		public override AssemblyDefinition ResolveAssembly(AssemblyNameReference name, ReaderParameters parameters)
		{
			return base.ResolveAssembly(name, parameters);
		}
	}
}
