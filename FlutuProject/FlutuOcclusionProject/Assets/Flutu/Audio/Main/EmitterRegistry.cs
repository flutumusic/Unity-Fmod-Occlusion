using FMODUnity;
using System;
using System.Collections.Generic;

namespace Flutu.Audio
{
    public static class EmitterRegistry
    {
        private static readonly HashSet<StudioEventEmitter> emitters = new();

        public static event Action<StudioEventEmitter> onEmitterRegistered;
        public static event Action<StudioEventEmitter> onEmitterUnregistered;

        public static void Register(StudioEventEmitter emitter)
        {
            if (emitters.Add(emitter))
                onEmitterRegistered?.Invoke(emitter);
        }

        public static void Unregister(StudioEventEmitter emitter)
        {
            if (emitters.Remove(emitter))
                onEmitterUnregistered?.Invoke(emitter);
        }

        public static IEnumerable<StudioEventEmitter> GetEmitters() => emitters;

        public static int Count => emitters.Count;
    }
}
