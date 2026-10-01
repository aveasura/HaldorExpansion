using System;
using System.Collections.Generic;

namespace HaldorExpansion.Features.HelOath
{
    internal sealed class HelOathDotLedger
    {
        private readonly Dictionary<string, float> remaining = new Dictionary<string, float>();
        internal void Clear() => remaining.Clear();
        internal void Add(string source, float amount)
        {
            if (string.IsNullOrEmpty(source) || amount <= 0f || float.IsNaN(amount) || float.IsInfinity(amount)) return;
            float previous;
            remaining.TryGetValue(source, out previous);
            remaining[source] = previous + amount;
        }
        internal Dictionary<string, float> Consume(float totalPool, float tick)
        {
            var result = new Dictionary<string, float>();
            if (totalPool <= 0f) { remaining.Clear(); return result; }
            float fraction = Math.Min(1f, Math.Max(0f, tick / totalPool));
            foreach (string key in new List<string>(remaining.Keys))
            {
                float spent = remaining[key] * fraction;
                if (spent > 0f) result[key] = spent;
                remaining[key] -= spent;
                if (remaining[key] <= 0.0001f) remaining.Remove(key);
            }
            return result;
        }
    }
}
