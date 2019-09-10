using System;
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

        }

        public override AssemblyDefinition ResolveAssembly(AssemblyNameReference name)
        {
            System.Console.WriteLine($"Looking for assembly: {name.FullName}");
            if(name.FullName.Equals("Microsoft.Xna.Framework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=842cf8be1de50553"))
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
