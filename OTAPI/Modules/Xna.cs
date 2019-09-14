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

            _xna = AssemblyDefinition.ReadAssembly(System.IO.Path.Combine("Modifications", "netstandard2.0", "OTAPI.Xna.dll"));
        }

        public override void Run()
        {
			foreach (var asm in this.Assemblies)
			{
				var xnaFramework = asm.MainModule.AssemblyReferences
					.Where(x => x.Name.StartsWith("Microsoft.Xna.Framework", StringComparison.CurrentCultureIgnoreCase))
					.ToArray();

				for (var x = 0; x < xnaFramework.Length; x++)
				{
					xnaFramework[x].Name = _xna.Name.Name;
					xnaFramework[x].PublicKey = _xna.Name.PublicKey;
					xnaFramework[x].PublicKeyToken = _xna.Name.PublicKeyToken;
					xnaFramework[x].Version = _xna.Name.Version;
				}
			}
		}

        public override AssemblyDefinition ResolveAssembly(AssemblyNameReference name)
        {
            System.Console.WriteLine($"Looking for assembly: {name.FullName}");
            if(name.FullName.StartsWith("Microsoft.Xna", StringComparison.CurrentCultureIgnoreCase)
				|| name.Name.Equals("OTAPI.Xna")
				|| name.Name.StartsWith("ReLogic", StringComparison.CurrentCultureIgnoreCase)
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
