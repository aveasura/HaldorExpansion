using System.Reflection;
using UnityEngine;

namespace HaldorExpansion.Features.Cestus
{
    internal static class CestusEffects
    {

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

        internal static void PlayCestusCastFx(Player player, CestusStateStore.CestusState state, float shieldAmount)
        {
            if (player == null || ZNetScene.instance == null)
                return;

            if (string.IsNullOrWhiteSpace(CestusCastFxPrefabName))
                return;

            GameObject fxPrefab = ZNetScene.instance.GetPrefab(CestusCastFxPrefabName);
            if (fxPrefab == null)
            {
                HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Missing Cestus FX prefab: {CestusCastFxPrefabName}");
                return;
            }

            Vector3 origin = GetCestusCastFxOrigin(player);
            Quaternion rotation = Quaternion.LookRotation(player.transform.forward, Vector3.up);

            float t = Mathf.Clamp01(shieldAmount / Mathf.Max(1f, CestusCastFxReferenceShield));
            float scale = Mathf.Lerp(CestusCastFxBaseScale, CestusCastFxMaxScale, t);

            int amount = Mathf.Max(1, CestusCastFxAmount);

            for (int i = 0; i < amount; i++)
            {
                GameObject fxInstance = UnityEngine.Object.Instantiate(fxPrefab, origin, rotation);
                fxInstance.transform.localScale *= scale;

                TrySpeedUpCestusFx(fxInstance, state);
            }
        }

        private static void TrySpeedUpCestusFx(GameObject fxInstance, CestusStateStore.CestusState state)
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
            HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] ZSFX found: {sfxComponents.Length}");

            foreach (ZSFX sfx in sfxComponents)
            {
                if (sfx == null)
                    continue;

                AudioSource audioSource = GetCestusFxAudioSource(sfx);
                AudioClip[] audioClips = GetCestusFxAudioClips(sfx);

                if (audioSource == null)
                    continue;

                string sfxName = sfx.gameObject.name.ToLowerInvariant();

                HaldorExpansionPlugin.DebugLog(
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

                    HaldorExpansionPlugin.DebugLog(
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

                HaldorExpansionPlugin.DebugLog(
                    $"[HaldorExpansion] Manual FX sound play: " +
                    $"name={sfx.gameObject.name}, clip={audioSource.clip.name}, " +
                    $"pitch={audioSource.pitch:0.##}, startOffset={startOffset:0.##}");
            }
        }

        internal static void PlayPreparedCestusExplosionAudio(CestusStateStore.CestusState state)
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

                HaldorExpansionPlugin.DebugLog(
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

        internal static void PlayCestusCastSfx(Player player)
        {
            if (player == null || ZNetScene.instance == null)
                return;

            if (string.IsNullOrWhiteSpace(CestusCastSfxPrefabName))
                return;

            GameObject sfxPrefab = ZNetScene.instance.GetPrefab(CestusCastSfxPrefabName);
            if (sfxPrefab == null)
            {
                HaldorExpansionPlugin.DebugLog($"[HaldorExpansion] Missing Cestus SFX prefab: {CestusCastSfxPrefabName}");
                return;
            }

            Vector3 origin = GetCestusCastFxOrigin(player);
            Quaternion rotation = Quaternion.LookRotation(player.transform.forward, Vector3.up);

            UnityEngine.Object.Instantiate(sfxPrefab, origin, rotation);
        }

        private static Vector3 GetCestusCastFxOrigin(Player player)
        {
            if (player == null)
                return Vector3.zero;

            return player.GetCenterPoint()
                   + player.transform.forward * CestusCastFxForwardOffset
                   + Vector3.up * CestusCastFxUpOffset;
        }
    }
}
