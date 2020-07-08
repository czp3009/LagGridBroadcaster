using System;
using System.Reflection;
using NLog;
using Profiler;
using Profiler.Core;
using VRage.Game;

namespace LagGridBroadcaster
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ProfilerDataProxy
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static readonly string ProfilerDataClassName = "Profiler.Core.ProfilerData";

        private static readonly Type ProfilerData;

        private static readonly MethodInfo SubmitMethodInfo;

        public static readonly bool initialized;

        static ProfilerDataProxy()
        {
            initialized = false;
            ProfilerData = Assembly.GetAssembly(typeof(ProfilerPlugin))?.GetType(ProfilerDataClassName);
            if (ProfilerData == null)
            {
                Log.Error($"Can't find class {ProfilerDataClassName}, init failed");
                return;
            }

            SubmitMethodInfo = ProfilerData.GetMethod("Submit", BindingFlags.Public | BindingFlags.Static);
            initialized = true;
        }

        public static bool Submit(ProfilerRequest req)
        {
            return (bool) SubmitMethodInfo.Invoke(null, new object[] {req, null, null, null});
        }
    }
}