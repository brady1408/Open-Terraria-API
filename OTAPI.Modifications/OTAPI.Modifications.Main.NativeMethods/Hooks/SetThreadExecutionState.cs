namespace OTAPI
{
    public static partial class Hooks
    {
        public static partial class Main
        {
            public static partial class NativeMethods
            {
                #region Handlers
                public delegate HookResult PreSetThreadExecutionStateHandler(ref uint esFlags);
                public delegate void PostSetThreadExecutionStateHandler(uint esFlags);
                #endregion

                /// <summary>
                /// Todo shit
                /// </summary>
                public static PreSetThreadExecutionStateHandler PreSetThreadExecutionState;

                /// <summary>
                /// Todo shit
                /// </summary>
                public static PostSetThreadExecutionStateHandler PostSetThreadExecutionState;
            }
        }
    }
}
