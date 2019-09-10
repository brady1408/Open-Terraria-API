using System;
using Mod.Framework;

namespace OTAPI.Modules
{
    /// <summary>
	/// This module will iterate over each type defined by the registered assemblies, while transforming all hidden members into public members.
	/// </summary>
	[Module("Hooks most of the application", "death")]
    public class Hook : RunnableModule
    {
        private ModFramework _framework;

        public Hook(ModFramework framework)
        {
            _framework = framework;
        }

        public override void Run()
        {

        }
    }
}
