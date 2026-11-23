using UnityEditor;
using UnityEngine;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// Audio import rules for Assets/_Project/Audio:
    /// - "*loop*" clips: uncompressed PCM, so the loop point has no codec gap (no click).
    /// - Other short SFX: Vorbis, decompressed on load, so they start instantly with no decode cost per play.
    /// - Long clips (siren): compressed in memory.
    /// </summary>
    public sealed class AudioImportPostprocessor : AssetPostprocessor
    {
        private void OnPreprocessAudio()
        {
            if (!assetPath.StartsWith("Assets/_Project/Audio/")) return;
            var ai = (AudioImporter)assetImporter;
            var s = ai.defaultSampleSettings;
            string name = System.IO.Path.GetFileNameWithoutExtension(assetPath).ToLowerInvariant();

            if (name.Contains("loop"))
            {
                s.compressionFormat = AudioCompressionFormat.PCM;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
            }
            else if (name.Contains("siren"))
            {
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.6f;
                s.loadType = AudioClipLoadType.CompressedInMemory;
            }
            else
            {
                s.compressionFormat = AudioCompressionFormat.Vorbis;
                s.quality = 0.75f;
                s.loadType = AudioClipLoadType.DecompressOnLoad;
            }
            s.preloadAudioData = true;
            ai.defaultSampleSettings = s;
            ai.forceToMono = true;
        }
    }
}
