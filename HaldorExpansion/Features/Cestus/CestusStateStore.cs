using System.Collections.Generic;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusStateStore
    {

        internal sealed class CestusState
        {
            public Player Player;
            public float Charge;
            public float ShieldRemaining;
            public float ShieldMax;
            public float ShieldDecayPerTick;
            public int ShieldTicksLeft;
            public float CooldownUntil;
            public bool IsCasting;
            public float InputLockedUntil;
            public Coroutine ShieldRoutine;
            public Coroutine CastRoutine;
            public readonly List<AudioSource> PreparedExplosionAudios = new List<AudioSource>();

            public float LastChargeGainTime;
            public Coroutine ChargeDecayRoutine;
            public float LastObservedHealth;
            public float LastObservedMaxHealth;
            public bool HasObservedHealth;
            public bool WasEquippedLastFrame;
            public bool ForceHideHudBar;
            public float LastSyncedShieldRemaining = -1f;
            public float LastSyncedShieldMax = -1f;
            public bool LastSyncedShieldActive;
        }

        internal static readonly Dictionary<long, CestusState> ActiveCestusStates =
            new Dictionary<long, CestusState>();

        internal static long GetCestusStateKey(Player player)
        {
            if (player == null)
                return 0L;

            long playerId = player.GetPlayerID();
            if (playerId != 0L)
                return playerId;

            return player.GetInstanceID();
        }

        internal static CestusState GetOrCreateCestusState(Player player)
        {
            long id = GetCestusStateKey(player);

            if (!ActiveCestusStates.TryGetValue(id, out CestusState state))
            {
                state = new CestusState
                {
                    Player = player,
                    LastChargeGainTime = 0f,
                    ChargeDecayRoutine = null,
                    LastObservedHealth = player.GetHealth(),
                    LastObservedMaxHealth = player.GetMaxHealth(),
                    HasObservedHealth = true,
                    WasEquippedLastFrame = false,
                    ForceHideHudBar = false,
                    Charge = 0f,
                    ShieldRemaining = 0f,
                    ShieldMax = 0f,
                    ShieldDecayPerTick = 0f,
                    ShieldTicksLeft = 0,
                    CooldownUntil = 0f,
                    IsCasting = false,
                    InputLockedUntil = 0f,
                    ShieldRoutine = null,
                    CastRoutine = null
                };

                ActiveCestusStates[id] = state;
            }

            state.Player = player;
            return state;
        }
    }
}
