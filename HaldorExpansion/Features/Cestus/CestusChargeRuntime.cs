using HaldorExpansion.Compatibility;
using HaldorExpansion.Diagnostics;
using System.Collections;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusChargeRuntime
    {

        internal const float CestusChargeMax = 100f;

        // распад заряда со временем =====
        internal const float CestusChargePerPostArmorDamage = 1.3f; // за 10 полученного урона стакает 13 шкалы
        internal const float CestusChargeDecayDelay = 9f; // после последнего входящего урона ждём 7 секунд
        internal const float CestusChargeDecayPerSecond = 4f; // потом теряем 4 заряда в секунду
        internal const float CestusChargeDecayTickInterval = 0.20f; // decay считается каждые 0.2 сек

        internal static bool HasFullCestusCharge(Player player)
        {
            if (player == null)
                return false;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            return state.Charge >= CestusChargeMax - 0.001f;
        }

        internal static float GetCestusCharge(Player player)
        {
            if (player == null)
                return 0f;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            return Mathf.Clamp(state.Charge, 0f, CestusChargeMax);
        }

        internal static void UpdateCestusLocalDamageCharge(Player player)
        {
            if (!CestusRuntime.ShouldProcessCestusLocally(player))
                return;

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);
            float currentHealth = Mathf.Max(0f, player.GetHealth());
            float currentMaxHealth = Mathf.Max(0f, player.GetMaxHealth());

            if (!state.HasObservedHealth)
            {
                state.LastObservedHealth = currentHealth;
                state.LastObservedMaxHealth = currentMaxHealth;
                state.HasObservedHealth = true;
                return;
            }

            float previousHealth = state.LastObservedHealth;
            float previousMaxHealth = state.LastObservedMaxHealth;
            state.LastObservedHealth = currentHealth;
            state.LastObservedMaxHealth = currentMaxHealth;

            if (!CestusEquipment.HasCestusEquipped(player))
                return;

            float observedHealthLoss = previousHealth - currentHealth;
            float observedMaxHealthLoss = Mathf.Max(0f, previousMaxHealth - currentMaxHealth);
            float observedDamage = observedHealthLoss - observedMaxHealthLoss;

            if (observedDamage <= 0.05f)
                return;

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Observed local Cestus damage: " +
                $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}, " +
                $"prevHp={previousHealth:0.##}, " +
                $"currentHp={currentHealth:0.##}, " +
                $"prevMaxHp={previousMaxHealth:0.##}, " +
                $"currentMaxHp={currentMaxHealth:0.##}, " +
                $"effectiveDelta={observedDamage:0.##}");

            if (state.ShieldRemaining > 0.001f && !player.IsDead())
            {
                float absorbedByShield = Mathf.Min(state.ShieldRemaining, observedDamage);
                if (absorbedByShield > 0.001f)
                {
                    state.ShieldRemaining = Mathf.Max(0f, state.ShieldRemaining - absorbedByShield);

                    float correctedHealth =
                        Mathf.Min(currentMaxHealth, Mathf.Max(0f, currentHealth + absorbedByShield));
                    CharacterHealthAccess.SetCharacterHealth(player, correctedHealth);
                    state.LastObservedHealth = correctedHealth;

                    if (state.ShieldRemaining <= 0.001f)
                    {
                        CestusShieldRuntime.ClearCestusShieldState(player, state, false, true);
                    }
                    else
                    {
                        CestusNetworkState.SyncCestusShieldState(player, state);
                    }

                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Cestus shield corrected via local observer: " +
                        $"observedDamage={observedDamage:0.##}, " +
                        $"absorbed={absorbedByShield:0.##}, " +
                        $"correctedHealth={correctedHealth:0.##}, " +
                        $"remaining={state.ShieldRemaining:0.##}, " +
                        $"{PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");

                    observedDamage = Mathf.Max(0f, observedDamage - absorbedByShield);
                }
            }

            AddCestusCharge(player, observedDamage);
        }

        internal static void AddCestusCharge(Player player, float postArmorDamage)
        {
            if (player == null || postArmorDamage <= 0f)
                return;

            if (!CestusRuntime.ShouldProcessCestusLocally(player))
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Skip Cestus charge on non-local player: {PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");
                return;
            }

            if (!CestusEquipment.HasCestusEquipped(player))
            {
                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Skip Cestus charge because weapon not detected: {PlayerDiagnostics.GetCestusDebugPlayerTag(player)}");
                return;
            }

            CestusStateStore.CestusState state = CestusStateStore.GetOrCreateCestusState(player);

            if (Time.time < state.CooldownUntil)
                return;

            // Пока щит активен или идёт каст - не копим заряд
            if (state.ShieldRemaining > 0.001f || state.IsCasting)
                return;

            float before = state.Charge;
            float added = postArmorDamage * CestusChargePerPostArmorDamage;

            state.Charge = Mathf.Clamp(state.Charge + added, 0f, CestusChargeMax);
            state.LastChargeGainTime = Time.time;

            EnsureCestusChargeDecayRoutine(state);

            HaldorExpansionPlugin.DebugLog(
                $"[HaldorExpansion] Cestus charge added: " +
                $"damage={postArmorDamage:0.##}, " +
                $"added={added:0.##}, " +
                $"before={before:0.##}, " +
                $"after={state.Charge:0.##}");
        }

        private static void EnsureCestusChargeDecayRoutine(CestusStateStore.CestusState state)
        {
            if (state == null || HaldorExpansionPlugin.Instance == null)
                return;

            if (state.ChargeDecayRoutine != null)
                return;

            state.ChargeDecayRoutine = HaldorExpansionPlugin.Instance.StartCoroutine(CestusChargeDecayRoutine(state));
        }

        private static IEnumerator CestusChargeDecayRoutine(CestusStateStore.CestusState state)
        {
            if (state == null)
                yield break;

            long playerId = state.Player != null ? CestusStateStore.GetCestusStateKey(state.Player) : 0L;
            float wait = Mathf.Max(0.02f, CestusChargeDecayTickInterval);

            try
            {
                while (true)
                {
                    yield return new WaitForSeconds(wait);

                    Player player = state.Player;
                    if (player == null || player.IsDead())
                        yield break;

                    if (state.Charge <= 0.001f)
                        yield break;

                    // Пока каст, щит или кд - заряд не тает
                    if (state.IsCasting || state.ShieldRemaining > 0.001f || Time.time < state.CooldownUntil)
                        continue;

                    // Ждём окно без получения урона
                    if (Time.time < state.LastChargeGainTime + CestusChargeDecayDelay)
                        continue;

                    float before = state.Charge;
                    float decay = CestusChargeDecayPerSecond * wait;

                    state.Charge = Mathf.Max(0f, state.Charge - decay);

                    HaldorExpansionPlugin.DebugLog(
                        $"[HaldorExpansion] Cestus charge decay: " +
                        $"before={before:0.##}, " +
                        $"decay={decay:0.##}, " +
                        $"after={state.Charge:0.##}");

                    if (state.Charge <= 0.001f)
                    {
                        state.Charge = 0f;
                        yield break;
                    }
                }
            }
            finally
            {
                state.ChargeDecayRoutine = null;

                if (playerId != 0 &&
                    CestusStateStore.ActiveCestusStates.TryGetValue(playerId, out CestusStateStore.CestusState current) &&
                    ReferenceEquals(current, state))
                {
                    current.ChargeDecayRoutine = null;
                }
            }
        }
    }
}
