﻿#if CLIENT
using System;

namespace OTA.Plugin
{
    public static partial class HookArgs
    {
        public struct GUIChatBoxSend
        {
            public string Message { get; set; }
        }
    }

    public static partial class HookPoints
    {
        public static readonly HookPoint<HookArgs.GUIChatBoxSend> GUIChatBoxSend = new HookPoint<HookArgs.GUIChatBoxSend>();
    }
}
#endif