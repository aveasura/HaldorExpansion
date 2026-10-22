using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace HaldorExpansion.Features.HelOath
{
    internal static class HelOathSoulSwapEffects
    {
        private static bool CanRender => SystemInfo.graphicsDeviceType != GraphicsDeviceType.Null;
        private static bool loggedRenderFailure;

        internal static void PlayGhostAura(HelOathGhostVisual ghost)
        {
            if (!CanRender || ghost == null) return;
            try
            {
                ghost.PlaySoulSwapAura();
            }
            catch (Exception error)
            {
                LogFailure(error);
            }
        }

        // Effects are best-effort. A shader or particle failure must never prevent
        // the actual swap, local Rigidbody update, or remote companion relocation.
        internal static void BeginAura(Player player)
        {
            if (!CanRender) return;
            try
            {
                if (player != null && player.gameObject.activeInHierarchy)
                {
                    var aura = player.GetComponent<HelOathSoulSwapPlayerAura>();
                    if (aura == null) aura = player.gameObject.AddComponent<HelOathSoulSwapPlayerAura>();
                    aura.Begin(player);
                }
            }
            catch (Exception error)
            {
                LogFailure(error);
            }
        }

        internal static void PlayVisuals(Player player, Vector3 fromFoot, Vector3 toFoot,
            bool startAura = true)
        {
            if (!CanRender) return;
            if (startAura) BeginAura(player);
            try
            {
                if (!Finite(fromFoot) || !Finite(toFoot) || (fromFoot - toFoot).sqrMagnitude < 0.04f) return;
                var root = new GameObject("HelOath_SoulSwapFx");
                var from = fromFoot + Vector3.up * 0.95f;
                var to = toFoot + Vector3.up * 0.95f;
                root.transform.position = (from + to) * 0.5f;
                root.AddComponent<HelOathSoulSwapFx>().Initialize(from, to, HelOathEffects.GetMaterial());
            }
            catch (Exception error)
            {
                LogFailure(error);
            }
        }

        private static void LogFailure(Exception error)
        {
            if (loggedRenderFailure) return;
            loggedRenderFailure = true;
            HaldorExpansionPlugin.Log?.LogWarning("[Hel Soul Swap FX] Visual-only error: " + error);
        }

        private static bool Finite(Vector3 point) =>
            !float.IsNaN(point.x) && !float.IsNaN(point.y) && !float.IsNaN(point.z)
            && !float.IsInfinity(point.x) && !float.IsInfinity(point.y) && !float.IsInfinity(point.z);
    }

    // A short-lived, player-attached instance of the ghost's actual soul-cloud aura.
    // Unlike the ghost, the player has no persistent veil: it fades to transparent.
    internal sealed class HelOathSoulSwapPlayerAura : MonoBehaviour
    {
        private const float Hold = 0.16f;
        private const float FadeSeconds = 0.84f;
        private HelOathGhostAura aura;
        private float beganAt;

        internal void Begin(Player player)
        {
            if (aura == null)
            {
                aura = new HelOathGhostAura();
                aura.Initialize(player.transform, player.transform);
                aura.Start();
            }

            beganAt = Time.time;
            aura.FlashSwap(Hold + FadeSeconds, 0.05f);
            aura.SetFade(1f, false);
        }

        private void Update()
        {
            if (aura == null) return;
            float progress = Mathf.Clamp01((Time.time - beganAt - Hold) / FadeSeconds);
            float visibility = 1f - Mathf.SmoothStep(0f, 1f, progress);
            aura.SetFade(visibility, false);
            if (progress >= 1f) Destroy(this);
        }

        private void OnDisable() => DisposeAura();
        private void OnDestroy() => DisposeAura();

        private void DisposeAura()
        {
            if (aura != null)
            {
                aura.Dispose();
                aura = null;
            }
        }
    }

    // The original Soul Swap connector is deliberately preserved; the player
    // and ghost now supply the noticeable apparition effect themselves.
    internal sealed class HelOathSoulSwapFx : MonoBehaviour
    {
        private const int BeamPoints = 24;
        private const float Duration = 0.44f;
        private readonly Vector3[] beamPoints = new Vector3[BeamPoints];
        private Vector3 from, to, side, normal;
        private LineRenderer beam;
        private float bornAt;

        internal void Initialize(Vector3 fromWorld, Vector3 toWorld, Material material)
        {
            bornAt = Time.time;
            from = transform.InverseTransformPoint(fromWorld);
            to = transform.InverseTransformPoint(toWorld);
            Vector3 axis = to - from;
            Vector3 direction = axis.sqrMagnitude > 0.0001f ? axis.normalized : Vector3.forward;
            side = Vector3.Cross(direction, Vector3.up);
            if (side.sqrMagnitude < 0.0001f) side = Vector3.Cross(direction, Vector3.right);
            side.Normalize();
            normal = Vector3.Cross(direction, side).normalized;

            beam = HelOathEffects.Line(transform, material, 0.085f, BeamPoints);
            beam.numCornerVertices = 5;
            beam.numCapVertices = 4;
            beam.textureMode = LineTextureMode.Stretch;
            beam.alignment = LineAlignment.View;
        }

        private void Update()
        {
            float age = Time.time - bornAt;
            float t = Mathf.Clamp01(age / Duration);
            if (t >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float alpha = Mathf.SmoothStep(0f, 1f, 1f - Mathf.Clamp01((t - 0.12f) / 0.88f));
            float amplitude = Mathf.Lerp(0.34f, 0.05f, t);
            float tighten = Mathf.Lerp(1.15f, 0.35f, t);
            for (int i = 0; i < BeamPoints; i++)
            {
                float u = i / (float)(BeamPoints - 1);
                Vector3 point = Vector3.Lerp(from, to, u);
                float wave = Mathf.Sin(u * Mathf.PI * 3f + age * 28f) * amplitude;
                float curl = Mathf.Cos(u * Mathf.PI * 2f - age * 22f) * amplitude * 0.45f;
                float edgeFade = Mathf.Sin(u * Mathf.PI);
                point += side * wave * edgeFade * tighten;
                point += normal * curl * edgeFade;
                beamPoints[i] = point;
            }

            beam.SetPositions(beamPoints);
            beam.widthMultiplier = Mathf.Lerp(0.16f, 0.025f, t) * (0.92f + 0.08f * Mathf.Sin(age * 45f));
            Color beamColor = new Color(0.56f, 0.94f, 1f, alpha * 0.95f);
            beam.startColor = beam.endColor = beamColor;
        }
    }
}