using System;
using System.Linq;
using Mod.Framework;

namespace OTAPI.Modules
{
	/// <summary>
	/// This module will handle any command line data such as what assemblies and query patterns
	/// </summary>
	[Module("Command line arguments", "death")]
	[AssemblyTarget("TerrariaServer, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null")]
	[AssemblyTarget("Terraria, Version=1.3.5.3, Culture=neutral, PublicKeyToken=null")]
	public class CLI : RunnableModule
	{
		private ModFramework _framework;

		public CLI(ModFramework framework)
		{
			_framework = framework;
		}

		private HookOptions ParseFromPattern(ref string pattern)
		{
			var flags = HookOptions.None;
			var segments = pattern.Split('$');

			if (segments.Length == 2)
			{
				// valid
				pattern = segments[0];

				var character_flags = segments[1].ToCharArray();
				foreach (var flag in character_flags)
				{
					switch (flag)
					{
						case 'b': // begin hook
							flags |= HookOptions.Pre;
							break;
						case 'e': // end hook
							flags |= HookOptions.Post;
							break;
						case 'r': // reference parameters
							flags |= HookOptions.ReferenceParameters;
							break;
						case 'c': // begin hook can cancel
							flags |= HookOptions.Cancellable;
							break;
						case 'a': // begin hook can alter non-void method return value
							flags |= HookOptions.AlterResult;
							break;
						default:
							throw new Exception($"Assembly Modification Pattern Flag is not valid: `{flag}`");
					}
				}
			}
			else if (segments.Length > 2)
			{
				throw new Exception("Assembly Modification Patterns (AMP) only support flags defined at the end of the pattern");
			}

			return flags;
		}

		class Hook
		{
			public string FullName { get; set; }
			public HookOptions Options { get; set; } = HookOptions.Default;

			public Hook(string fullname)
			{
				this.FullName = fullname;
			}

			public Hook Clear() { this.Options = HookOptions.None; return this; }
			public Hook Pre() { this.Options |= HookOptions.Pre; return this; }
			public Hook Post() { this.Options |= HookOptions.Post; return this; }
			public Hook Reference() { this.Options |= HookOptions.ReferenceParameters; return this; }
			public Hook Cancellable() { this.Options |= HookOptions.Cancellable; return this; }
			public Hook Alterable() { this.Options |= HookOptions.AlterResult; return this; }

			//public Hook AllFeatures() => Pre().Post().Reference().Cancellable().Alterable();

			public override string ToString()
			{
				return $"{FullName} [{Options}]";
			}
		}

		public override void Run()
		{
			var modifications = new[]
			{
				new Hook("Terraria.Net.NetManager.SendData*"),
				new Hook("Terraria.Netplay.StartListening*"),
				new Hook("Terraria.Netplay.ServerLoop*"),
			};
			foreach (var hook in modifications)
			{
				Console.WriteLine($"\t-> Hooking: {hook}");
				var query_start = DateTime.Now;
				var res = new Query(hook.FullName, this.Assemblies).Hook(hook.Options);
				Console.WriteLine($"\t\t-> Took: {(int)(DateTime.Now - query_start).TotalMilliseconds}ms Matches: {res.Applied}");
			}
		}
	}
}
