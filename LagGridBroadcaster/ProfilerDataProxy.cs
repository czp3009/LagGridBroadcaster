using System;
using System.Reflection;
using Profiler;
using Profiler.Core;
using VRage.Game;

namespace LagGridBroadcaster
{
    // ReSharper disable once ClassNeverInstantiated.Global
    internal class ProfilerDataProxy
    {
        private static readonly Type ProfilerData = Assembly.GetAssembly(typeof(ProfilerPlugin))
            .GetType("Profiler.Core.ProfilerData");

        private static readonly MethodInfo ChangeMaskMethodInfo =
            ProfilerData.GetMethod("ChangeMask", BindingFlags.Public | BindingFlags.Static);

        private static readonly MethodInfo SubmitMethodInfo =
            ProfilerData.GetMethod("Submit", BindingFlags.Public | BindingFlags.Static);

        public static bool ChangeMask(long? playerMask, long? factionMask, long? entityMask, MyModContext modMask)
        {
            return (bool) ChangeMaskMethodInfo.Invoke(
                null, new object[] {playerMask, factionMask, entityMask, modMask}
            );
        }

        public static void Submit(ProfilerRequest req)
        {
            SubmitMethodInfo.Invoke(null, new object[] {req});
        }
    }
}