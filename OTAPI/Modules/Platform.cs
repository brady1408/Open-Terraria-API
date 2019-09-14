using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Mod.Framework;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace OTAPI.Modules
{
	/// <summary>
	/// Generate the ReLogic.OS.Platform.Current variable based on the current
	/// OS rather than the .exe build...
	/// </summary>
	[Module("Fixing platform initialisation", "death")]
	public class Platform : RunnableModule
	{
		private ModFramework _framework;

		public Platform(ModFramework framework)
		{
			_framework = framework;
		}

		public override void Run()
		{
			foreach (var asm in this.Assemblies
				.Where(x => x.Name.Name.IndexOf("ReLogic", StringComparison.CurrentCultureIgnoreCase) > -1))
			{
				// remove the readonly flag so we can change it at runtime.
				asm.Type("ReLogic.OS.Platform").Field("Current").IsInitOnly = false;

				// now add in the platform checks manually...
				var method = asm.Type("ReLogic.OS.Platform").Method(".cctor");

				var scriptMethod = this.TryGetCSharpScript(@"
					if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX)) {
						ReLogic.OS.Platform.Current = new ReLogic.OS.OsxPlatform();
					}
					else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux)) {
						ReLogic.OS.Platform.Current = new ReLogic.OS.LinuxPlatform();
					}
					else {
						ReLogic.OS.Platform.Current = new ReLogic.OS.WindowsPlatform();
					}
				",
					references: new[]
					{
						MetadataReference.CreateFromFile(typeof(RuntimeInformation).Assembly.Location), // platform
						MetadataReference.CreateFromFile(Path.Combine("Output", "ReLogic.dll")),
					},
					ingoreAccessAssemblies: new string[] { "ReLogic" }
				);

				method.Body.Instructions.Clear();
				method.Body.Variables.Clear();

				foreach (var variable in scriptMethod.Body.Variables)
				{
					method.Body.Variables.Add(variable);
				}

				foreach(var ins in scriptMethod.Body.Instructions)
				{
					method.Body.Instructions.Add(asm.MainModule.EnsureImported(ins));
				}
			}
		}
	}
}
