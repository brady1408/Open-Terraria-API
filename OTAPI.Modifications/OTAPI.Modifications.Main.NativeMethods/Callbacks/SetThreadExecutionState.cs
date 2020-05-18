namespace OTAPI.Callbacks.Terraria
{
    internal static partial class NativeMethods
    {
        /// <summary>
        /// This method is injected into the start of the saveWorld(bool) method.
        /// The return value will dictate if normal vanilla code should continue to run.
        /// </summary>
        /// <returns>True to continue on to vanilla code, otherwise false</returns>
        internal static bool SetThreadExecutionStateBegin(ref uint esFlags)
        {
            var res = Hooks.Main.NativeMethods.PreSetThreadExecutionState?.Invoke(ref esFlags);
            if (res.HasValue) return res.Value == HookResult.Continue;
            return true;
        }

        /// <summary>
        /// This method is injected into the end of the saveWorld(bool) method.
        /// </summary>
        internal static void SetThreadExecutionStateEnd(uint esFlags) =>
            Hooks.Main.NativeMethods.PostSetThreadExecutionState?.Invoke(esFlags);
    }
}
