using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.CodeAnalysis;
using Mod.Framework;

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
			// the idea is to modify the ReLogic dll to allow the Platform.Current
			// variable to be modified at runtime, then i can update this on startup.
			// to modify the variable on startup i use roslyn to compile C# to IL

			foreach (var asm in this.Assemblies
				.Where(x => x.Name.Name.IndexOf("ReLogic", StringComparison.CurrentCultureIgnoreCase) > -1))
			{
				// remove the readonly flag so we can change it at runtime.
				asm.Type("ReLogic.OS.Platform").Field("Current").IsInitOnly = false;

				// in addition to the above modification, this also allows rosyln to compile our script so we need to feed the updated assembly to it too
				byte[] updated_relogic = null;
				using (var ms = new MemoryStream())
				{
					asm.Write(ms);
					updated_relogic = ms.ToArray();
				}

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
						MetadataReference.CreateFromFile(typeof(RuntimeInformation).Assembly.Location),
						MetadataReference.CreateFromImage(updated_relogic)
					}
				);

				if (method.Body.Instructions.Count != 3)
					throw new NotImplementedException($"Only expected 3 instruction in {method.FullName}");

				method.Body.Instructions.Clear();
				method.Body.Variables.Clear();

				foreach (var variable in scriptMethod.Body.Variables)
				{
					method.Body.Variables.Add(variable);
				}

				foreach (var ins in scriptMethod.Body.Instructions)
				{
					method.Body.Instructions.Add(asm.MainModule.EnsureImported(ins));
				}
			}
		}
	}
}
