using OTAPI.Patcher.Engine.Extensions;
using OTAPI.Patcher.Engine.Modification;
using System.Linq;

namespace OTAPI.Patcher.Engine.Modifications.Hooks.Main.NativeMethods.SetThreadExecutionState
{
	public class SetThreadExecutionState : ModificationBase
	{
		public override System.Collections.Generic.IEnumerable<string> AssemblyTargets => new[]
		{
			"TerrariaServer, Version=1.4.0.0, Culture=neutral, PublicKeyToken=null"
		};
		public override string Description => "Hooking Main.NativeMethods.SetThreadExecutionState...";
		public override void Run()
		{
			// var vanilla = this.Method(() => Terraria.IO.WorldFile.SaveWorld(false, false));
			var vanilla = this.Method(() => Terraria.Main.NativeMethods.SetThreadExecutionState(2147483649U));

			uint tmp = 2147483649U;
			var cbkBegin = this.Method(() => OTAPI.Callbacks.Terraria.NativeMethods.SetThreadExecutionStateBegin(ref tmp));
			var cbkEnd = this.Method(() => OTAPI.Callbacks.Terraria.NativeMethods.SetThreadExecutionStateEnd(tmp));

			vanilla.Wrap
			(
				beginCallback: cbkBegin,
				endCallback: cbkEnd,
				beginIsCancellable: true,
				noEndHandling: false,
				allowCallbackInstance: false
			);
		}
	}
}
