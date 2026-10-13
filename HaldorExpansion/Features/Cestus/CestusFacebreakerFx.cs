using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using HaldorExpansion.Features.HelOath;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace HaldorExpansion.Features.Cestus
{
    /// <summary>
    /// Cosmetic, locally rendered Facebreaker air-grab ribbons. Based on the
    /// existing procedural soft-line texture in HelOathTouchEffects; no external
    /// textures, gameplay hitboxes, animation overrides or physics changes.
    /// The caster sends only the locked cast axis and whether the two-sided
    /// stagger condition was met. Each observing client renders independently.
    /// </summary>
    internal static class CestusFacebreakerFx
    {
        private const string StartRpc = "aveasura_he_cestus_facebreaker_fx_start_v1";
        private const string ClashRpc = "aveasura_he_cestus_facebreaker_fx_clash_v1";
        private const float MaxViewDistance = 50f;

        // Visual-only tuning. Pull itself still begins at 0.54s and lasts 0.42s.
        internal const bool Enabled = true;
        internal const float StartDelaySeconds = 0.07f;
        internal const float OpenDurationSeconds = 0.36f;
        internal const float MaximumReach = 5.6f;
        internal const float RibbonHeight = 1.30f;
        internal const float HazeWidth = 0.76f;
        internal const float CoreWidth = 0.18f;
        internal const float HazeOpacity = 0.22f;
        internal const float CoreOpacity = 0.62f;
        internal const float ClashPulseDurationSeconds = 0.16f;

        private static bool CanRender => Enabled && !Application.isBatchMode &&
                                         SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;

        private static readonly FieldInfo CharacterView =
            AccessTools.Field(typeof(Character), "m_nview");

        private static readonly ConditionalWeakTable<ZNetView, object> Registered =
            new ConditionalWeakTable<ZNetView, object>();

        private sealed class FxState
        {
            internal CestusFacebreakerRibbonController Current;
        }

        private static readonly ConditionalWeakTable<Player, FxState> Active =
            new ConditionalWeakTable<Player, FxState>();

        private static ZNetView View(Player player)
            => player == null || CharacterView == null ? null : CharacterView.GetValue(player) as ZNetView;

        internal static void Register(Character character, ZNetView view = null)
        {
            Player player = character as Player;
            if (player == null) return;
            if (view == null) view = View(player);
            if (view == null || !view.IsValid() || Registered.TryGetValue(view, out _)) return;

            view.Register<Vector3>(StartRpc, (sender, axis) =>
            {
                if (!ValidSender(view, sender) || player == Player.m_localPlayer) return;
                if (!IsFinite(axis) || axis.sqrMagnitude < 0.25f || axis.sqrMagnitude > 4f) return;
                BeginVisual(player, axis, false);
            });
            view.Register<int>(ClashRpc, (sender, cue) =>
            {
                if (!ValidSender(view, sender) || player == Player.m_localPlayer || cue != 1) return;
                MarkClashVisual(player);
            });
            Registered.Add(view, new object());
        }

        private static bool ValidSender(ZNetView view, long sender)
            => view != null && view.IsValid() && view.GetZDO() != null &&
               view.GetZDO().GetOwner() == sender;

        private static bool IsFinite(Vector3 direction)
            => !float.IsNaN(direction.x) && !float.IsInfinity(direction.x) &&
               !float.IsNaN(direction.y) && !float.IsInfinity(direction.y) &&
               !float.IsNaN(direction.z) && !float.IsInfinity(direction.z);

        // Called only after the existing custom cast animation began successfully.
        internal static void StartCast(Player caster, Vector3 axis)
        {
            if (caster == null || caster != Player.m_localPlayer || caster.IsDead()) return;
            BeginVisual(caster, axis, true);
            ZNetView view = View(caster);
            if (view != null && view.IsValid())
                view.InvokeRPC(ZNetView.Everybody, StartRpc, axis);
        }

        // Called after the existing target-selection pass detects both sides.
        // The pulse is displayed at the END of the unchanged physics pull.
        internal static void SelectClash(Player caster)
        {
            if (caster == null || caster != Player.m_localPlayer || caster.IsDead()) return;
            MarkClashVisual(caster);
            ZNetView view = View(caster);
            if (view != null && view.IsValid())
                view.InvokeRPC(ZNetView.Everybody, ClashRpc, 1);
        }

        internal static void Cancel(Player player)
        {
            if (player == null) return;
            FxState state;
            if (Active.TryGetValue(player, out state) && state.Current != null)
            {
                Object.Destroy(state.Current.gameObject);
                state.Current = null;
            }
        }

        private static void BeginVisual(Player player, Vector3 axis, bool local)
        {
            if (!CanRender || player == null || player.IsDead()) return;
            if (!local && Player.m_localPlayer != null &&
                Vector3.Distance(Player.m_localPlayer.transform.position, player.transform.position) > MaxViewDistance)
                return;
            try
            {
                FxState state = Active.GetValue(player, _ => new FxState());
                if (state.Current != null) Object.Destroy(state.Current.gameObject);
                var root = new GameObject("HE_Facebreaker_AirGrab_LocalVFX");
                CestusFacebreakerRibbonController fx = root.AddComponent<CestusFacebreakerRibbonController>();
                state.Current = fx;
                fx.Initialize(player, axis);
            }
            catch (Exception e)
            {
                HaldorExpansionPlugin.Log?.LogWarning("[Facebreaker FX] " + e.Message);
            }
        }

        private static void MarkClashVisual(Player player)
        {
            if (!CanRender || player == null) return;
            FxState state;
            if (Active.TryGetValue(player, out state) && state.Current != null)
                state.Current.MarkClash();
        }
    }

    /// <summary>
    /// Two mirrored soft ribbons: spread during the hand-opening phase and
    /// sweep inward during the 0.42-second creature pull. A very brief soft
    /// circular pressure pulse is shown only for a two-sided stagger grab.
    /// </summary>
    internal sealed class CestusFacebreakerRibbonController : MonoBehaviour
    {
        private const int RibbonPoints = 15;
        private const int PulsePoints = 28;
        private readonly LineRenderer[] haze = new LineRenderer[2];
        private readonly LineRenderer[] core = new LineRenderer[2];
        private LineRenderer pulse;
        private Player caster;
        private Vector3 axis;
        private float startedAt;
        private bool clash;
        private bool initialized;

        private static float PullStart => CestusFacebreaker.PullTriggerDelaySeconds;

        private static float PullEnd => CestusFacebreaker.PullTriggerDelaySeconds +
                                        CestusFacebreaker.PullDurationSeconds;

        internal void Initialize(Player player, Vector3 lockedAxis)
        {
            caster = player;
            axis = lockedAxis;
            axis.y = 0f;
            if (axis.sqrMagnitude < 0.01f) axis = player.transform.forward;
            axis.y = 0f;
            axis.Normalize();
            startedAt = Time.time;

            for (int i = 0; i < 2; i++)
            {
                haze[i] = MakeRibbon("Haze", i, CestusFacebreakerFx.HazeWidth);
                core[i] = MakeRibbon("Core", i, CestusFacebreakerFx.CoreWidth);
            }

            pulse = HelOathTouchEffects.SmoothLine(transform, 0.13f, PulsePoints, true);
            pulse.name = "Facebreaker_ClashPressurePulse";
            pulse.enabled = false;
            pulse.startColor = new Color(0.96f, 0.95f, 0.92f, 0f);
            pulse.endColor = pulse.startColor;
            initialized = true;
            UpdateGeometry(0f);
        }

        internal void MarkClash() => clash = true;

        private LineRenderer MakeRibbon(string name, int side, float width)
        {
            LineRenderer line = HelOathTouchEffects.SmoothLine(transform, width, RibbonPoints, false);
            line.name = "Facebreaker_" + name + (side == 0 ? "_Left" : "_Right");
            line.widthCurve = new AnimationCurve(
                new Keyframe(0f, 0.08f), new Keyframe(0.15f, 0.75f),
                new Keyframe(0.60f, 1f), new Keyframe(1f, 0.01f));
            return line;
        }

        private void LateUpdate()
        {
            if (!initialized) return;
            if (caster == null || caster.IsDead())
            {
                Destroy(gameObject);
                return;
            }

            float elapsed = Time.time - startedAt;
            if (elapsed > PullEnd + CestusFacebreakerFx.ClashPulseDurationSeconds + 0.08f)
            {
                Destroy(gameObject);
                return;
            }

            UpdateGeometry(elapsed);
        }

        private void UpdateGeometry(float elapsed)
        {
            transform.position = caster.transform.position + Vector3.up * CestusFacebreakerFx.RibbonHeight;
            transform.rotation = Quaternion.LookRotation(axis, Vector3.up);
            float reach, visibility;
            if (elapsed < PullStart)
            {
                float open = Mathf.Clamp01((elapsed - CestusFacebreakerFx.StartDelaySeconds) /
                                           CestusFacebreakerFx.OpenDurationSeconds);
                reach = Mathf.Lerp(0.55f, CestusFacebreakerFx.MaximumReach, Mathf.SmoothStep(0f, 1f, open));
                visibility = Mathf.Clamp01((elapsed - CestusFacebreakerFx.StartDelaySeconds) / 0.11f);
            }
            else
            {
                // The ribbons pull IN at exactly the same instant the mobs start
                // moving. Shrinking takes the full, existing pull duration.
                float returnPhase = Mathf.Clamp01((elapsed - PullStart) /
                                                  CestusFacebreaker.PullDurationSeconds);
                reach = Mathf.Lerp(CestusFacebreakerFx.MaximumReach, 0.55f,
                    Mathf.SmoothStep(0f, 1f, returnPhase));
                visibility = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((returnPhase - 0.70f) / 0.30f));
            }

            if (elapsed >= PullEnd) visibility = 0f;

            for (int i = 0; i < 2; i++)
            {
                float sign = i == 0 ? -1f : 1f;
                LineRenderer wide = haze[i];
                LineRenderer sharp = core[i];
                wide.startColor = new Color(0.82f, 0.88f, 0.89f,
                    CestusFacebreakerFx.HazeOpacity * visibility);
                wide.endColor = new Color(0.82f, 0.88f, 0.89f,
                    CestusFacebreakerFx.HazeOpacity * visibility * 0.60f);
                sharp.startColor = new Color(0.94f, 0.96f, 0.95f,
                    CestusFacebreakerFx.CoreOpacity * visibility);
                sharp.endColor = new Color(0.94f, 0.96f, 0.95f,
                    CestusFacebreakerFx.CoreOpacity * visibility * 0.70f);

                for (int j = 0; j < RibbonPoints; j++)
                {
                    float u = j / (RibbonPoints - 1f);
                    float bulge = Mathf.Sin(Mathf.PI * u);
                    float z = sign * Mathf.Lerp(0.40f, reach, u);
                    float sideways = sign * 0.08f + sign * 0.34f * bulge;
                    float y = 0.18f * bulge + 0.09f * u;
                    wide.SetPosition(j, new Vector3(sideways, y, z));
                    sharp.SetPosition(j, new Vector3(sideways * 1.13f, y + 0.055f, z));
                }
            }

            // No artificial impact on a one-sided grab; this is visual-only.
            if (clash && elapsed >= PullEnd &&
                elapsed < PullEnd + CestusFacebreakerFx.ClashPulseDurationSeconds)
            {
                pulse.enabled = true;
                float fraction = Mathf.Clamp01((elapsed - PullEnd) /
                                               CestusFacebreakerFx.ClashPulseDurationSeconds);
                float radius = Mathf.Lerp(0.25f, 1.12f, Mathf.SmoothStep(0f, 1f, fraction));
                float alpha = 0.54f * (1f - Mathf.SmoothStep(0f, 1f, fraction));
                pulse.startColor = new Color(0.94f, 0.94f, 0.88f, alpha);
                pulse.endColor = pulse.startColor;
                for (int j = 0; j < PulsePoints; j++)
                {
                    float angle = j * (2f * Mathf.PI / PulsePoints);
                    pulse.SetPosition(j, new Vector3(
                        Mathf.Cos(angle) * radius, -0.11f, Mathf.Sin(angle) * radius));
                }
            }
            else
            {
                pulse.enabled = false;
            }
        }
    }
}