using UnityEngine;

namespace HaldorExpansion.Features.HelOath
{
    // Lives on the detached aura root, independently of the fading ghost body.
    // No network object, gameplay state, damage or persistent scene object.
    internal sealed class HelOathGhostSoulRelease : MonoBehaviour
    {
        private HelOathGhostAura aura;
        private Transform owner;
        private Vector3 origin;
        private float elapsed;

        internal void Initialize(HelOathGhostAura effect, Transform player)
        {
            aura = effect;
            owner = player;
            origin = transform.position;
        }

        private void Update()
        {
            if (aura == null) return;
            if (owner == null || !owner.gameObject.activeInHierarchy)
            {
                aura.DestroyResources();
                return;
            }

            elapsed += Time.deltaTime;
            float duration = Mathf.Max(0.1f, HelOathGhostVisualSettings.AuraReleaseDuration);
            float t = Mathf.Clamp01(elapsed / duration);
            // A gentle lift that gathers speed; all living particles travel with the
            // local simulation root. The soul is not dragged along by the player.
            float rise = Mathf.Max(0f, HelOathGhostVisualSettings.AuraReleaseHeight)
                         * (0.35f * t + 0.65f * t * t);
            float sway = Mathf.Sin(t * Mathf.PI) * 0.22f;
            transform.position = origin + new Vector3(sway * Mathf.Sin(t * 5f), rise,
                sway * Mathf.Sin(t * 3f));
            float fadeStart = Mathf.Clamp(HelOathGhostVisualSettings.AuraReleaseFadeStart, 0f, 0.95f);
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((t - fadeStart) / (1f - fadeStart)));
            float emissionFraction = Mathf.Clamp(HelOathGhostVisualSettings.AuraReleaseEmissionFraction, 0.01f, 1f);
            float emission = 1f - Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / emissionFraction));
            aura.UpdateRelease(alpha, emission);
            if (t >= 1f) aura.DestroyResources();
        }

        private void OnDestroy()
        {
            // Also releases owned materials/texture if the scene unloads early.
            aura?.DestroyResources();
            aura = null;
        }
    }
}