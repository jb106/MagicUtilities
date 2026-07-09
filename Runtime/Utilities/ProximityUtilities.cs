using System.Collections.Generic;
using UnityEngine;

namespace MagicUtilities
{
    public interface IProximityTarget<T>
    {
        Vector3 ProximityPosition(T ctx);
        bool IsSelectable(T ctx);
        void SetSelected(T ctx, bool selected);
        int GetPriority(T ctx) => 0;
    }
    
    // Holds the current candidates and the selected one
    public class ProximityState<T> where T : class
    {
        public readonly HashSet<T> Candidates = new();
        public T Current;
        public IProximityTarget<T> Target;
    }

    public static class ProximityUtilities
    {
        // Call from OnTriggerEnter
        public static void AddCandidate<T>(ProximityState<T> state, Collider other) where T : Component
        {
            if (other.TryGetComponent(out T component))
                state.Candidates.Add(component);
        }

        // Call from OnTriggerExit
        public static void RemoveCandidate<T>(ProximityState<T> state, Collider other) where T : Component
        {
            if (other.TryGetComponent(out T component))
            {
                state.Candidates.Remove(component);
                if (ReferenceEquals(component, state.Current))
                {
                    state.Target.SetSelected(component, false);
                    state.Current = null;
                }
            }
        }

        // Call from Update: picks the best candidate by priority then distance
        public static T UpdateNearest<T>(ProximityState<T> state, Vector3 origin) where T : Component
        {
            if (state.Target == null) return state.Current;

            T best = null;
            float bestSqr = float.MaxValue;
            int bestPriority = int.MinValue;
            bool sawNull = false;

            foreach (var c in state.Candidates)
            {
                // Destroyed candidate, clean it up after the loop
                if (c == null)
                {
                    sawNull = true;
                    continue;
                }

                if (!state.Target.IsSelectable(c)) continue;

                int currentPriority = state.Target.GetPriority(c);
                float d2 = (state.Target.ProximityPosition(c) - origin).sqrMagnitude;

                if (currentPriority > bestPriority || (currentPriority == bestPriority && d2 < bestSqr))
                {
                    bestPriority = currentPriority;
                    bestSqr = d2;
                    best = c;
                }
            }

            if (!ReferenceEquals(best, state.Current))
                state.Current = best;

            if (sawNull) state.Candidates.RemoveWhere(x => x == null);

            return state.Current;
        }
    }
}