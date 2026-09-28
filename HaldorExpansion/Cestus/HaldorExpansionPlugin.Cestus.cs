using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx.Configuration;
using Jotunn.Configs;
using Jotunn.Managers;
using UnityEngine;

namespace HaldorExpansion
{
    public partial class HaldorExpansionPlugin
    {
        private static readonly HashSet<string> CestusItemKeys = new HashSet<string>
        {
            "$item_weapon_gritcestus"
        };

        internal const float CestusSetBonusMultiplier = 1.5f; // Множитель, если надета сетовая броня вместе с кастетом.

        internal static ConfigEntry<KeyCode> CestusAbilityKeyConfig;
        internal static ButtonConfig CestusAbilityButton;

        internal const float CestusChargeMax = 100f;

        internal const float CestusShieldMaxHpRatio = 0.40f; // 40% от максимального хп станивится временным щитом
        internal const float CestusShieldMinFlat = 20f; // минимально возможный щит == 20 единиц

        internal const float CestusShieldDuration = 6f;
        internal const int CestusShieldTicks = 40;

        internal const float CestusCooldown = 15f;

        private const string CestusShieldZdoRemainingKey = "he_cestus_shield_remaining";
        private const string CestusShieldZdoMaxKey = "he_cestus_shield_max";
        private const string CestusShieldZdoActiveKey = "he_cestus_shield_active";

        internal const string CestusActivationEmote = "point";
        internal const float CestusCastDuration = 1.05f;

        // Урон активки (множитель урона)
        internal const float CestusNovaDamageMultiplier = 2.7f;
        internal const float CestusNovaRadius = 4f;
        internal const float CestusNovaPushForce = 20f;

        internal const string CestusCastFxPrefabName = "vfx_StaminaUpgrade";
        internal const string CestusCastSfxPrefabName = "";
        internal const float CestusCastFxAudioPitch = 1.25f;
        internal const float CestusCastFxAudioStartOffset = 0.08f;
        internal const int CestusCastFxAmount = 2;

        internal const float CestusCastFxForwardOffset = 1.0f;
        internal const float CestusCastFxUpOffset = 0.8f;
        internal const float CestusCastFxBaseScale = 1.0f;
        internal const float CestusCastFxMaxScale = 1.6f;
        internal const float CestusCastFxReferenceShield = 100f;
        internal const float CestusCastFxPlaybackSpeed = 2f;

        // распад заряда со временем =====
        internal const float CestusChargePerPostArmorDamage = 0.8f; // за 10 полученного урона стакает 8 шкалы
        internal const float CestusChargeDecayDelay = 7f; // после последнего входящего урона ждём 7 секунд
        internal const float CestusChargeDecayPerSecond = 6f; // потом теряем 6 заряда в секунду
        internal const float CestusChargeDecayTickInterval = 0.20f; // decay считается каждые 0.2 сек

        private sealed class CestusState
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

        private static readonly Dictionary<long, CestusState> ActiveCestusStates =
            new Dictionary<long, CestusState>();

        private static long GetCestusStateKey(Player player)
        {
            if (player == null)
                return 0L;

            long playerId = player.GetPlayerID();
            if (playerId != 0L)
                return playerId;

            return player.GetInstanceID();
        }

        private static CestusState GetOrCreateCestusState(Player player)
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

        internal static bool HasCestusEquipped(Player player)
        {
            if (player == null)
                return false;

            ItemDrop.ItemData currentWeapon = player.GetCurrentWeapon();
            if (IsCestusWeapon(currentWeapon))
                return true;

            ItemDrop.ItemData rightItem = GetPlayerHandItem(player, "m_rightItem");
            if (IsCestusWeapon(rightItem))
                return true;

            ItemDrop.ItemData leftItem = GetPlayerHandItem(player, "m_leftItem");
            if (IsCestusWeapon(leftItem))
                return true;

            Inventory inventory = player.GetInventory();
            if (inventory == null)
                return false;

            List<ItemDrop.ItemData> items = inventory.GetAllItems();
            if (items == null)
                return false;

            foreach (ItemDrop.ItemData item in items)
            {
                if (item == null || !item.m_equipped)
                    continue;

                if (IsCestusWeapon(item))
                    return true;

                if (item.m_shared == null)
                    continue;

                string itemKey = item.m_shared.m_name;
                if (!string.IsNullOrEmpty(itemKey) && CestusItemKeys.Contains(itemKey))
                    return true;
            }

            return false;
        }

        internal static bool ShouldProcessCestusLocally(Player player)
        {
            return player != null &&
                   Player.m_localPlayer != null &&
                   player == Player.m_localPlayer;
        }

        internal static string GetCestusDebugPlayerTag(Player player)
        {
            if (player == null)
                return "player=null";

            ItemDrop.ItemData currentWeapon = player.GetCurrentWeapon();
            ItemDrop.ItemData rightItem = GetPlayerHandItem(player, "m_rightItem");
            ItemDrop.ItemData leftItem = GetPlayerHandItem(player, "m_leftItem");

            bool isLocal = player == Player.m_localPlayer;
            ZNetView nview = GetPlayerNView(player);
            bool isOwner = nview != null && nview.IsValid() && nview.IsOwner();

            return
                $"name={player.name}, " +
                $"local={isLocal}, " +
                $"owner={isOwner}, " +
                $"pid={player.GetPlayerID()}, " +
                $"iid={player.GetInstanceID()}, " +
                $"current={GetCestusItemDebugName(currentWeapon)}, " +
                $"right={GetCestusItemDebugName(rightItem)}, " +
                $"left={GetCestusItemDebugName(leftItem)}";
        }

        private static ZNetView GetPlayerNView(Player player)
        {
            if (player == null)
                return null;

            FieldInfo field = typeof(Character).GetField(
                "m_nview",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return field?.GetValue(player) as ZNetView;
        }

        private static bool IsPlayerNViewOwner(Player player)
        {
            ZNetView nview = GetPlayerNView(player);
            return nview != null && nview.IsValid() && nview.IsOwner();
        }

        private static ZDO GetPlayerZdo(Player player)
        {
            ZNetView nview = GetPlayerNView(player);
            return nview != null && nview.IsValid() ? nview.GetZDO() : null;
        }

        internal static bool IsCestusShieldAuthorityRuntime(Player player)
        {
            if (!ShouldProcessCestusLocally(player))
                return false;

            ZNetView nview = GetPlayerNView(player);
            return nview == null || !nview.IsValid() || nview.IsOwner();
        }

        internal static float GetSyncedCestusShieldRemaining(Player player)
        {
            ZDO zdo = GetPlayerZdo(player);
            return zdo != null ? Mathf.Max(0f, zdo.GetFloat(CestusShieldZdoRemainingKey, 0f)) : 0f;
        }

        internal static float GetSyncedCestusShieldMax(Player player)
        {
            ZDO zdo = GetPlayerZdo(player);
            return zdo != null ? Mathf.Max(0f, zdo.GetFloat(CestusShieldZdoMaxKey, 0f)) : 0f;
        }

        internal static float GetCestusEffectiveShieldRemaining(Player player)
        {
            if (player == null)
                return 0f;

            CestusState state = GetOrCreateCestusState(player);
            return Mathf.Max(Mathf.Max(0f, state.ShieldRemaining), GetSyncedCestusShieldRemaining(player));
        }

        private static void SyncCestusShieldState(Player player, CestusState state, bool force = false)
        {
            if (player == null || state == null || !IsPlayerNViewOwner(player))
                return;

            float remaining = Mathf.Max(0f, state.ShieldRemaining);
            float max = Mathf.Max(0f, state.ShieldMax);
            bool active = remaining > 0.001f;

            if (!force &&
                Mathf.Abs(state.LastSyncedShieldRemaining - remaining) <= 0.01f &&
                Mathf.Abs(state.LastSyncedShieldMax - max) <= 0.01f &&
                state.LastSyncedShieldActive == active)
            {
                return;
            }

            ZDO zdo = GetPlayerZdo(player);
            if (zdo == null)
                return;

            zdo.Set(CestusShieldZdoRemainingKey, remaining);
            zdo.Set(CestusShieldZdoMaxKey, max);
            zdo.Set(CestusShieldZdoActiveKey, active);

            state.LastSyncedShieldRemaining = remaining;
            state.LastSyncedShieldMax = max;
            state.LastSyncedShieldActive = active;
        }

        private static void ClearCestusShieldState(Player player, CestusState state, bool stopRoutine, bool syncNow)
        {
            if (state == null)
                return;

            if (stopRoutine && state.ShieldRoutine != null && Instance != null)
            {
                Instance.StopCoroutine(state.ShieldRoutine);
                state.ShieldRoutine = null;
            }

            state.ShieldRemaining = 0f;
            state.ShieldMax = 0f;
            state.ShieldDecayPerTick = 0f;
            state.ShieldTicksLeft = 0;

            if (syncNow)
            {
                SyncCestusShieldState(player, state, true);
            }
            else
            {
                state.LastSyncedShieldRemaining = -1f;
                state.LastSyncedShieldMax = -1f;
                state.LastSyncedShieldActive = false;
            }
        }

        private static ItemDrop.ItemData GetPlayerHandItem(Player player, string fieldName)
        {
            if (player == null || string.IsNullOrWhiteSpace(fieldName))
                return null;

            FieldInfo field = typeof(Humanoid).GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return field?.GetValue(player) as ItemDrop.ItemData;
        }

        private static string GetCestusItemDebugName(ItemDrop.ItemData item)
        {
            if (item == null)
                return "null";

            if (item.m_dropPrefab != null && !string.IsNullOrWhiteSpace(item.m_dropPrefab.name))
                return item.m_dropPrefab.name;

            if (item.m_shared != null && !string.IsNullOrWhiteSpace(item.m_shared.m_name))
                return item.m_shared.m_name;

            return "unknown";
        }

        internal static bool HasFullCestusCharge(Player player)
        {
            if (player == null)
                return false;

            CestusState state = GetOrCreateCestusState(player);
            return state.Charge >= CestusChargeMax - 0.001f;
        }

        internal static float GetCestusCharge(Player player)
        {
            if (player == null)
                return 0f;

            CestusState state = GetOrCreateCestusState(player);
            return Mathf.Clamp(state.Charge, 0f, CestusChargeMax);
        }

        internal static bool ShouldMirrorCestusToAdrenaline(Player player)
        {
            if (player == null)
                return false;

            CestusState state = GetOrCreateCestusState(player);

            return HasCestusEquipped(player)
                   || state.Charge > 0.001f
                   || state.ShieldRemaining > 0.001f
                   || state.IsCasting;
        }

        internal static bool ShouldForceHideCestusHudBar(Player player)
        {
            if (player == null)
                return false;

            CestusState state = GetOrCreateCestusState(player);
            return !HasCestusEquipped(player)
                   && state.ForceHideHudBar
                   && state.Charge <= 0.001f
                   && state.ShieldRemaining <= 0.001f
                   && !state.IsCasting;
        }

        internal static bool IsCestusShieldActive(Player player)
        {
            return GetCestusEffectiveShieldRemaining(player) > 0.001f;
        }

        internal static bool IsCestusCasting(Player player)
        {
            if (player == null)
                return false;

            CestusState state = GetOrCreateCestusState(player);
            return state.IsCasting;
        }

        internal static bool IsCestusInputLocked(Player player)
        {
            if (player == null)
                return false;

            CestusState state = GetOrCreateCestusState(player);
            return state.IsCasting && Time.time < state.InputLockedUntil;
        }

        internal static void UpdateCestusLocalEquipState(Player player)
        {
            if (!ShouldProcessCestusLocally(player))
                return;

            CestusState state = GetOrCreateCestusState(player);
            bool isEquippedNow = HasCestusEquipped(player);

            if (isEquippedNow)
            {
                state.ForceHideHudBar = false;
            }
            else if (state.WasEquippedLastFrame)
            {
                ResetCestusState(player, state, "unequipped");
                state.ForceHideHudBar = true;
            }

            state.WasEquippedLastFrame = isEquippedNow;
        }

        private static void ResetCestusState(Player player, CestusState state, string reason)
        {
            if (state == null)
                return;

            if (state.ChargeDecayRoutine != null && Instance != null)
            {
                Instance.StopCoroutine(state.ChargeDecayRoutine);
            }

            if (state.ShieldRoutine != null && Instance != null)
            {
                Instance.StopCoroutine(state.ShieldRoutine);
            }

            if (state.CastRoutine != null && Instance != null)
            {
                Instance.StopCoroutine(state.CastRoutine);
            }

            state.ChargeDecayRoutine = null;
            state.ShieldRoutine = null;
            state.CastRoutine = null;

            state.Charge = 0f;
            state.LastChargeGainTime = 0f;

            ClearCestusShieldState(player, state, false, true);

            state.IsCasting = false;
            state.InputLockedUntil = 0f;

            if (player != null)
            {
                state.LastObservedHealth = Mathf.Max(0f, player.GetHealth());
                state.HasObservedHealth = true;
            }

            DebugLog($"[HaldorExpansion] Cestus state reset: reason={reason}, {GetCestusDebugPlayerTag(player)}");
        }

        internal static void UpdateCestusLocalDamageCharge(Player player)
        {
            if (!ShouldProcessCestusLocally(player))
                return;

            CestusState state = GetOrCreateCestusState(player);
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

            if (!HasCestusEquipped(player))
                return;

            float observedHealthLoss = previousHealth - currentHealth;
            float observedMaxHealthLoss = Mathf.Max(0f, previousMaxHealth - currentMaxHealth);
            float observedDamage = observedHealthLoss - observedMaxHealthLoss;

            if (observedDamage <= 0.05f)
                return;

            DebugLog(
                $"[HaldorExpansion] Observed local Cestus damage: " +
                $"{GetCestusDebugPlayerTag(player)}, " +
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
                    SetCharacterHealth(player, correctedHealth);
                    state.LastObservedHealth = correctedHealth;

                    if (state.ShieldRemaining <= 0.001f)
                    {
                        ClearCestusShieldState(player, state, false, true);
                    }
                    else
                    {
                        SyncCestusShieldState(player, state);
                    }

                    DebugLog(
                        $"[HaldorExpansion] Cestus shield corrected via local observer: " +
                        $"observedDamage={observedDamage:0.##}, " +
                        $"absorbed={absorbedByShield:0.##}, " +
                        $"correctedHealth={correctedHealth:0.##}, " +
                        $"remaining={state.ShieldRemaining:0.##}, " +
                        $"{GetCestusDebugPlayerTag(player)}");

                    observedDamage = Mathf.Max(0f, observedDamage - absorbedByShield);
                }
            }

            AddCestusCharge(player, observedDamage);
        }

        internal static void AddCestusCharge(Player player, float postArmorDamage)
        {
            if (player == null || postArmorDamage <= 0f)
                return;

            if (!ShouldProcessCestusLocally(player))
            {
                DebugLog(
                    $"[HaldorExpansion] Skip Cestus charge on non-local player: {GetCestusDebugPlayerTag(player)}");
                return;
            }

            if (!HasCestusEquipped(player))
            {
                DebugLog(
                    $"[HaldorExpansion] Skip Cestus charge because weapon not detected: {GetCestusDebugPlayerTag(player)}");
                return;
            }

            CestusState state = GetOrCreateCestusState(player);

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

            DebugLog(
                $"[HaldorExpansion] Cestus charge added: " +
                $"damage={postArmorDamage:0.##}, " +
                $"added={added:0.##}, " +
                $"before={before:0.##}, " +
                $"after={state.Charge:0.##}");
        }

        private static void EnsureCestusChargeDecayRoutine(CestusState state)
        {
            if (state == null || Instance == null)
                return;

            if (state.ChargeDecayRoutine != null)
                return;

            state.ChargeDecayRoutine = Instance.StartCoroutine(CestusChargeDecayRoutine(state));
        }

        private static IEnumerator CestusChargeDecayRoutine(CestusState state)
        {
            if (state == null)
                yield break;

            long playerId = state.Player != null ? GetCestusStateKey(state.Player) : 0L;
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

                    DebugLog(
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
                    ActiveCestusStates.TryGetValue(playerId, out CestusState current) &&
                    ReferenceEquals(current, state))
                {
                    current.ChargeDecayRoutine = null;
                }
            }
        }

        internal static float AbsorbCestusShield(Player player, float incomingDamage)
        {
            if (player == null || incomingDamage <= 0f)
                return 0f;

            if (!IsCestusShieldAuthorityRuntime(player))
                return 0f;

            CestusState state = GetOrCreateCestusState(player);
            if (state.ShieldRemaining <= 0.001f)
                return 0f;

            float absorbed = Mathf.Min(state.ShieldRemaining, incomingDamage);
            state.ShieldRemaining = Mathf.Max(0f, state.ShieldRemaining - absorbed);

            if (state.ShieldRemaining <= 0.001f)
            {
                ClearCestusShieldState(player, state, false, true);
            }
            else
            {
                SyncCestusShieldState(player, state);
            }

            DebugLog(
                $"[HaldorExpansion] Cestus shield absorbed: " +
                $"incoming={incomingDamage:0.##}, " +
                $"absorbed={absorbed:0.##}, " +
                $"remaining={state.ShieldRemaining:0.##}, " +
                $"syncedRemaining={GetSyncedCestusShieldRemaining(player):0.##}, " +
                $"{GetCestusDebugPlayerTag(player)}");

            return absorbed;
        }

        internal static float CorrectCestusShieldAfterDamage(Player player, float beforeHealth, float afterHealth)
        {
            if (player == null)
                return 0f;

            if (!IsCestusShieldAuthorityRuntime(player))
                return 0f;

            if (player.IsDead())
                return 0f;

            float healthLoss = beforeHealth - afterHealth;
            if (healthLoss <= 0.001f)
                return 0f;

            CestusState state = GetOrCreateCestusState(player);
            if (state.ShieldRemaining <= 0.001f)
                return 0f;

            float absorbed = Mathf.Min(state.ShieldRemaining, healthLoss);
            if (absorbed <= 0.001f)
                return 0f;

            state.ShieldRemaining = Mathf.Max(0f, state.ShieldRemaining - absorbed);

            float correctedHealth = Mathf.Min(GetCharacterMaxHealth(player), Mathf.Max(0f, afterHealth + absorbed));
            SetCharacterHealth(player, correctedHealth);

            if (state.ShieldRemaining <= 0.001f)
            {
                ClearCestusShieldState(player, state, false, true);
            }
            else
            {
                SyncCestusShieldState(player, state);
            }

            DebugLog(
                $"[HaldorExpansion] Cestus shield corrected post-damage: " +
                $"before={beforeHealth:0.##}, " +
                $"after={afterHealth:0.##}, " +
                $"absorbed={absorbed:0.##}, " +
                $"newHealth={correctedHealth:0.##}, " +
                $"remaining={state.ShieldRemaining:0.##}, " +
                $"{GetCestusDebugPlayerTag(player)}");

            return absorbed;
        }

        internal static bool TryActivateCestusShield(Player player)
        {
            if (Instance == null || player == null)
                return false;

            if (!ShouldProcessCestusLocally(player))
                return false;

            if (!HasCestusEquipped(player))
                return false;

            CestusState state = GetOrCreateCestusState(player);

            if (state.IsCasting)
                return false;

            if (state.ShieldRemaining > 0.001f)
                return false;

            if (state.Charge < CestusChargeMax - 0.001f)
                return false;

            if (Time.time < state.CooldownUntil)
                return false;

            float maxHealth = GetCharacterMaxHealth(player);
            if (maxHealth <= 0f)
                return false;

            float baseShieldAmount = Mathf.Max(CestusShieldMinFlat, maxHealth * CestusShieldMaxHpRatio);
            bool hasSetBonus = HasCestusSetBonus(player);
            float shieldMultiplier = hasSetBonus ? CestusSetBonusMultiplier : 1f;
            float shieldAmount = baseShieldAmount * shieldMultiplier;

            state.Charge = 0f;
            state.CooldownUntil = Time.time + CestusCooldown;

            if (state.CastRoutine != null)
            {
                Instance.StopCoroutine(state.CastRoutine);
                state.CastRoutine = null;
            }

            state.CastRoutine = Instance.StartCoroutine(CestusCastRoutine(state, shieldAmount));

            DebugLog(
                $"[HaldorExpansion] Cestus cast started: " +
                $"baseShield={baseShieldAmount:0.##}, " +
                $"shieldMultiplier={shieldMultiplier:0.##}, " +
                $"shield={shieldAmount:0.##}, " +
                $"setBonus={hasSetBonus}, " +
                $"maxHealth={maxHealth:0.##}, " +
                $"cooldownUntil={state.CooldownUntil:0.##}");

            return true;
        }

        private static IEnumerator CestusCastRoutine(CestusState state, float shieldAmount)
        {
            if (state == null)
                yield break;

            Player player = state.Player;
            long playerId = player != null ? GetCestusStateKey(player) : 0L;

            state.IsCasting = true;
            state.InputLockedUntil = Time.time + CestusCastDuration;
            state.PreparedExplosionAudios.Clear();

            try
            {
                if (player == null || player.IsDead())
                    yield break;

                player.StartEmote(CestusActivationEmote, true);
                ClearHumanoidBlockState(player);

                state.ShieldRemaining = shieldAmount;
                state.ShieldMax = shieldAmount;
                state.ShieldTicksLeft = CestusShieldTicks;
                state.ShieldDecayPerTick = shieldAmount / CestusShieldTicks;

                SyncCestusShieldState(player, state, true);

                if (state.ShieldRoutine != null)
                {
                    Instance.StopCoroutine(state.ShieldRoutine);
                    state.ShieldRoutine = null;
                }

                PlayCestusCastFx(player, state, shieldAmount);
                PlayCestusCastSfx(player);

                yield return new WaitForSeconds(CestusCastDuration);

                if (player == null || player.IsDead())
                    yield break;

                state.ShieldRoutine = Instance.StartCoroutine(CestusShieldRoutine(state));
                TriggerCestusNova(player, shieldAmount);
                PlayPreparedCestusExplosionAudio(state);

                DebugLog(
                    $"[HaldorExpansion] Cestus cast resolved: " +
                    $"shield={shieldAmount:0.##}, " +
                    $"duration={CestusCastDuration:0.##}");
            }
            finally
            {
                state.IsCasting = false;
                state.InputLockedUntil = 0f;
                state.CastRoutine = null;

                if (playerId != 0 &&
                    ActiveCestusStates.TryGetValue(playerId, out CestusState current) &&
                    ReferenceEquals(current, state))
                {
                    current.IsCasting = false;
                    current.InputLockedUntil = 0f;
                    current.CastRoutine = null;
                }
            }
        }

        private static void TriggerCestusNova(Player player, float shieldAmount)
        {
            if (player == null || shieldAmount <= 0f)
                return;

            float damage = shieldAmount * CestusNovaDamageMultiplier;
            if (damage <= 0f)
                return;

            Vector3 origin = GetCestusNovaOrigin(player);
            int hits = 0;

            List<Character> characters = Character.GetAllCharacters();
            if (characters == null)
                return;

            foreach (Character target in characters)
            {
                if (!ShouldHitCestusNovaTarget(player, target, origin))
                    continue;

                ApplyCestusNovaDamage(player, target, origin, damage);
                hits++;
            }

            DebugLog(
                $"[HaldorExpansion] Cestus nova triggered: " +
                $"shield={shieldAmount:0.##}, " +
                $"damage={damage:0.##}, " +
                $"radius={CestusNovaRadius:0.##}, " +
                $"hits={hits}");
        }

        private static bool ShouldHitCestusNovaTarget(Player player, Character target, Vector3 origin)
        {
            if (player == null || target == null)
                return false;

            if (ReferenceEquals(player, target))
                return false;

            if (target.IsDead() || target.IsPlayer())
                return false;

            if (!BaseAI.IsEnemy(player, target))
                return false;

            float maxDistance = CestusNovaRadius + Mathf.Max(0f, target.GetRadius());
            float distance = Vector3.Distance(origin, GetCestusNovaTargetPoint(target));
            return distance <= maxDistance;
        }

        private static void ApplyCestusNovaDamage(Player player, Character target, Vector3 origin, float damage)
        {
            if (player == null || target == null || damage <= 0f)
                return;

            Vector3 targetPoint = GetCestusNovaTargetPoint(target);
            Vector3 dir = targetPoint - origin;
            if (dir.sqrMagnitude <= 0.0001f)
                dir = player.transform.forward;

            HitData hit = new HitData();
            hit.m_damage.m_blunt = damage;
            hit.m_point = targetPoint;
            hit.m_dir = dir.normalized;
            hit.m_pushForce = CestusNovaPushForce;
            hit.m_skill = Skills.SkillType.Unarmed;

            TryAssignHitAttacker(hit, player);
            target.Damage(hit);
        }

        private static Vector3 GetCestusNovaOrigin(Player player)
        {
            if (player == null)
                return Vector3.zero;

            return player.GetCenterPoint();
        }

        private static Vector3 GetCestusNovaTargetPoint(Character target)
        {
            if (target == null)
                return Vector3.zero;

            return target.GetCenterPoint();
        }

        private static void TryAssignHitAttacker(HitData hit, Character attacker)
        {
            if (hit == null || attacker == null)
                return;

            MethodInfo setAttackerMethod = typeof(HitData).GetMethod(
                "SetAttacker",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                null,
                new[] { typeof(Character) },
                null);

            if (setAttackerMethod != null)
            {
                setAttackerMethod.Invoke(hit, new object[] { attacker });
                return;
            }

            FieldInfo attackerField = typeof(HitData).GetField(
                "m_attacker",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            if (attackerField != null && attackerField.FieldType.IsAssignableFrom(attacker.GetType()))
                attackerField.SetValue(hit, attacker);
        }

        private static void PlayCestusCastFx(Player player, CestusState state, float shieldAmount)
        {
            if (player == null || ZNetScene.instance == null)
                return;

            if (string.IsNullOrWhiteSpace(CestusCastFxPrefabName))
                return;

            GameObject fxPrefab = ZNetScene.instance.GetPrefab(CestusCastFxPrefabName);
            if (fxPrefab == null)
            {
                DebugLog($"[HaldorExpansion] Missing Cestus FX prefab: {CestusCastFxPrefabName}");
                return;
            }

            Vector3 origin = GetCestusCastFxOrigin(player);
            Quaternion rotation = Quaternion.LookRotation(player.transform.forward, Vector3.up);

            float t = Mathf.Clamp01(shieldAmount / Mathf.Max(1f, CestusCastFxReferenceShield));
            float scale = Mathf.Lerp(CestusCastFxBaseScale, CestusCastFxMaxScale, t);

            int amount = Mathf.Max(1, CestusCastFxAmount);

            for (int i = 0; i < amount; i++)
            {
                GameObject fxInstance = Instantiate(fxPrefab, origin, rotation);
                fxInstance.transform.localScale *= scale;

                TrySpeedUpCestusFx(fxInstance, state);
            }
        }

        private static void TrySpeedUpCestusFx(GameObject fxInstance, CestusState state)
        {
            if (fxInstance == null)
                return;

            if (CestusCastFxPlaybackSpeed > 0.001f)
            {
                Animator[] animators = fxInstance.GetComponentsInChildren<Animator>(true);
                foreach (Animator animator in animators)
                {
                    if (animator != null)
                        animator.speed *= CestusCastFxPlaybackSpeed;
                }

                ParticleSystem[] particleSystems =
                    fxInstance.GetComponentsInChildren<ParticleSystem>(true);

                foreach (ParticleSystem particleSystem in particleSystems)
                {
                    if (particleSystem == null)
                        continue;

                    ParticleSystem.MainModule main = particleSystem.main;
                    main.simulationSpeed *= CestusCastFxPlaybackSpeed;
                }
            }

            ZSFX[] sfxComponents = fxInstance.GetComponentsInChildren<ZSFX>(true);
            DebugLog($"[HaldorExpansion] ZSFX found: {sfxComponents.Length}");

            foreach (ZSFX sfx in sfxComponents)
            {
                if (sfx == null)
                    continue;

                AudioSource audioSource = GetCestusFxAudioSource(sfx);
                AudioClip[] audioClips = GetCestusFxAudioClips(sfx);

                if (audioSource == null)
                    continue;

                string sfxName = sfx.gameObject.name.ToLowerInvariant();

                DebugLog(
                    $"[HaldorExpansion] ZSFX name={sfx.gameObject.name}, " +
                    $"clips={(audioClips != null ? audioClips.Length : 0)}, " +
                    $"clip={(audioSource.clip != null ? audioSource.clip.name : "null")}, " +
                    $"isPlaying={audioSource.isPlaying}, playOnAwake={audioSource.playOnAwake}, " +
                    $"pitch={audioSource.pitch:0.##}");

                sfx.enabled = false;
                audioSource.playOnAwake = false;
                audioSource.Stop();

                if (audioClips == null || audioClips.Length == 0 || audioClips[0] == null)
                    continue;

                audioSource.clip = audioClips[0];

                if (sfxName.Contains("expl"))
                {
                    if (state != null)
                        state.PreparedExplosionAudios.Add(audioSource);

                    DebugLog(
                        $"[HaldorExpansion] Prepared explosion audio: {sfx.gameObject.name}, clip={audioSource.clip.name}");
                    continue;
                }

                audioSource.pitch *= CestusCastFxAudioPitch;

                float startOffset = 0f;
                if (CestusCastFxAudioStartOffset > 0.001f)
                {
                    startOffset = Mathf.Clamp(
                        CestusCastFxAudioStartOffset,
                        0f,
                        Mathf.Max(0f, audioSource.clip.length - 0.01f));
                }

                audioSource.time = startOffset;
                audioSource.Play();

                DebugLog(
                    $"[HaldorExpansion] Manual FX sound play: " +
                    $"name={sfx.gameObject.name}, clip={audioSource.clip.name}, " +
                    $"pitch={audioSource.pitch:0.##}, startOffset={startOffset:0.##}");
            }
        }

        internal static bool HasCestusSetBonus(Player player)
        {
            return player != null &&
                   HasCestusEquipped(player) &&
                   HasPitKingChestEquipped(player);
        }

        private static void PlayPreparedCestusExplosionAudio(CestusState state)
        {
            if (state == null || state.PreparedExplosionAudios == null || state.PreparedExplosionAudios.Count == 0)
                return;

            foreach (AudioSource audioSource in state.PreparedExplosionAudios)
            {
                if (audioSource == null || audioSource.clip == null)
                    continue;

                audioSource.Stop();
                audioSource.time = 0f;
                audioSource.Play();

                DebugLog(
                    $"[HaldorExpansion] Explosion audio played: " +
                    $"clip={audioSource.clip.name}, pitch={audioSource.pitch:0.##}");
            }

            state.PreparedExplosionAudios.Clear();
        }

        private static AudioSource GetCestusFxAudioSource(ZSFX sfx)
        {
            if (sfx == null)
                return null;

            FieldInfo field = typeof(ZSFX).GetField(
                "m_audioSource",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return field?.GetValue(sfx) as AudioSource;
        }

        private static AudioClip[] GetCestusFxAudioClips(ZSFX sfx)
        {
            if (sfx == null)
                return null;

            FieldInfo field = typeof(ZSFX).GetField(
                "m_audioClips",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            return field?.GetValue(sfx) as AudioClip[];
        }

        private static void PlayCestusCastSfx(Player player)
        {
            if (player == null || ZNetScene.instance == null)
                return;

            if (string.IsNullOrWhiteSpace(CestusCastSfxPrefabName))
                return;

            GameObject sfxPrefab = ZNetScene.instance.GetPrefab(CestusCastSfxPrefabName);
            if (sfxPrefab == null)
            {
                DebugLog($"[HaldorExpansion] Missing Cestus SFX prefab: {CestusCastSfxPrefabName}");
                return;
            }

            Vector3 origin = GetCestusCastFxOrigin(player);
            Quaternion rotation = Quaternion.LookRotation(player.transform.forward, Vector3.up);

            Instantiate(sfxPrefab, origin, rotation);
        }

        private static Vector3 GetCestusCastFxOrigin(Player player)
        {
            if (player == null)
                return Vector3.zero;

            return player.GetCenterPoint()
                   + player.transform.forward * CestusCastFxForwardOffset
                   + Vector3.up * CestusCastFxUpOffset;
        }

        private static IEnumerator CestusShieldRoutine(CestusState state)
        {
            if (state == null)
                yield break;

            float waitPerTick = CestusShieldDuration / CestusShieldTicks;
            long playerId = state.Player != null ? GetCestusStateKey(state.Player) : 0L;

            try
            {
                while (true)
                {
                    yield return new WaitForSeconds(waitPerTick);

                    Player player = state.Player;
                    if (player == null || player.IsDead())
                        yield break;

                    if (state.ShieldRemaining <= 0.001f || state.ShieldTicksLeft <= 0)
                        yield break;

                    float decay = Mathf.Min(state.ShieldRemaining, state.ShieldDecayPerTick);

                    state.ShieldRemaining = Mathf.Max(0f, state.ShieldRemaining - decay);
                    state.ShieldTicksLeft--;

                    if (state.ShieldRemaining <= 0.001f)
                    {
                        ClearCestusShieldState(player, state, false, true);
                    }
                    else
                    {
                        SyncCestusShieldState(player, state);
                    }

                    DebugLog(
                        $"[HaldorExpansion] Cestus shield decay: " +
                        $"decay={decay:0.##}, " +
                        $"remaining={state.ShieldRemaining:0.##}, " +
                        $"ticksLeft={state.ShieldTicksLeft}");

                    if (state.ShieldRemaining <= 0.001f)
                        yield break;
                }
            }
            finally
            {
                state.ShieldRoutine = null;
                ClearCestusShieldState(state.Player, state, false, true);

                if (playerId != 0 &&
                    ActiveCestusStates.TryGetValue(playerId, out CestusState current) &&
                    ReferenceEquals(current, state))
                {
                    current.ShieldRoutine = null;
                    ClearCestusShieldState(current.Player, current, false, true);
                }
            }
        }

        internal static void ClearHumanoidAttackState(Humanoid humanoid)
        {
            if (humanoid == null)
                return;

            SetFieldIfExists(humanoid, "m_attack", false);
            SetFieldIfExists(humanoid, "m_secondaryAttack", false);
            SetFieldIfExists(humanoid, "m_attackHold", false);
        }

        internal static void ClearHumanoidBlockState(Humanoid humanoid)
        {
            if (humanoid == null)
                return;

            SetFieldIfExists(humanoid, "m_blocking", false);
            SetFieldIfExists(humanoid, "m_blockTimer", 0f);
        }

        internal static float GetCestusShieldNormalized(Player player)
        {
            if (player == null)
                return 0f;

            CestusState state = GetOrCreateCestusState(player);

            if (state.ShieldRemaining <= 0.001f || state.ShieldMax <= 0.001f)
                return 0f;

            return Mathf.Clamp01(state.ShieldRemaining / state.ShieldMax);
        }

        internal static float GetCestusShieldBarNormalized(Player player)
        {
            if (player == null)
                return 0f;

            CestusState state = GetOrCreateCestusState(player);
            if (state.ShieldRemaining <= 0.001f)
                return 0f;

            float maxHealth = GetCharacterMaxHealth(player);
            if (maxHealth <= 0f)
                return 0f;

            return Mathf.Clamp01(state.ShieldRemaining / maxHealth);
        }

        internal static float GetCestusHealthNormalized(Player player)
        {
            if (player == null)
                return 0f;

            float maxHealth = GetCharacterMaxHealth(player);
            if (maxHealth <= 0f)
                return 0f;

            return Mathf.Clamp01(player.GetHealth() / maxHealth);
        }

        private void RegisterCestusInput()
        {
            CestusAbilityKeyConfig = Config.Bind(
                "Cestus",
                "Ability Key",
                KeyCode.Mouse2,
                "Key for activating the Cestus ability.\n" +
                "Mouse0 = Left Mouse Button.\n" +
                "Mouse1 = Right Mouse Button.\n" +
                "Mouse2 = Middle Mouse Button.\n" +
                "Uses UnityEngine.KeyCode names. " +
                "Examples: Mouse0-Mouse6, A-Z, Alpha0-Alpha9, F1-F15, Space, Tab, LeftShift, RightShift, LeftControl, RightControl, LeftAlt, RightAlt.");

            CestusAbilityButton = new ButtonConfig
            {
                Name = "CestusAbility",
                Config = CestusAbilityKeyConfig,
                HintToken = "$cestus_ability",
                BlockOtherInputs = true
            };

            InputManager.Instance.AddButton(ModGuid, CestusAbilityButton);
        }
    }
}