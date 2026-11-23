using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace AirDefense.EditorTools
{
    /// <summary>
    /// Mobile texture rules for imported 3D models under Assets/_Project/Art/Models:
    /// max 1024 px, ASTC on Android, mipmaps on. Each texture's role is read from
    /// the sibling .gltf file: normal maps get the NormalMap type; metallic-roughness
    /// and occlusion maps are imported as linear (not sRGB) data.
    /// </summary>
    public sealed class ModelTexturePostprocessor : AssetPostprocessor
    {
        private const string Folder = "Assets/_Project/Art/Models/";
        private const string TextureFolder = "Assets/_Project/Art/Textures/";

        private void OnPreprocessTexture()
        {
            bool model = assetPath.StartsWith(Folder);
            bool material = assetPath.StartsWith(TextureFolder);
            if (!model && !material) return;
            var ti = (TextureImporter)assetImporter;
            ti.mipmapEnabled = true;
            ti.maxTextureSize = 1024;
            if (material) ti.wrapMode = TextureWrapMode.Repeat;

            // Model textures: role from the .gltf. Material sets (Poly Haven naming): role from the file name.
            Role role = model ? FindRole(assetPath) : RoleFromName(Path.GetFileNameWithoutExtension(assetPath));
            if (role == Role.Normal) ti.textureType = TextureImporterType.NormalMap;
            else if (role == Role.Linear) ti.sRGBTexture = false;

            var android = ti.GetPlatformTextureSettings("Android");
            android.overridden = true;
            android.maxTextureSize = 1024;
            android.format = TextureImporterFormat.ASTC_6x6;
            android.compressionQuality = 50;
            ti.SetPlatformTextureSettings(android);
        }

        private enum Role { Color, Normal, Linear }

        private static Role RoleFromName(string n)
        {
            n = n.ToLowerInvariant();
            if (n.Contains("_nor_gl") || n.Contains("_normal")) return Role.Normal;
            if (n.Contains("_rough") || n.Contains("_ao") || n.Contains("_arm") || n.Contains("_metal")) return Role.Linear;
            return Role.Color;
        }

        [Serializable] private class TexRef { public int index = -1; }
        [Serializable] private class Pbr { public TexRef metallicRoughnessTexture; }
        [Serializable] private class Mat { public TexRef normalTexture; public TexRef occlusionTexture; public Pbr pbrMetallicRoughness; }
        [Serializable] private class Tex { public int source = -1; }
        [Serializable] private class Img { public string uri; }
        [Serializable] private class Gltf { public Mat[] materials; public Tex[] textures; public Img[] images; }

        private static Role FindRole(string texturePath)
        {
            string dir = Path.GetDirectoryName(texturePath);
            string file = Path.GetFileName(texturePath);
            foreach (string gltfPath in Directory.GetFiles(dir, "*.gltf"))
            {
                Gltf g;
                try { g = JsonUtility.FromJson<Gltf>(File.ReadAllText(gltfPath)); }
                catch { continue; }
                if (g?.images == null || g.textures == null || g.materials == null) continue;

                var roles = new Dictionary<string, Role>();
                void Mark(TexRef r, Role role)
                {
                    if (r == null || r.index < 0 || r.index >= g.textures.Length) return;
                    int src = g.textures[r.index].source;
                    if (src < 0 || src >= g.images.Length || g.images[src].uri == null) return;
                    string uri = Uri.UnescapeDataString(g.images[src].uri);
                    if (!roles.ContainsKey(uri) || role == Role.Normal) roles[uri] = role;
                }
                foreach (Mat m in g.materials)
                {
                    Mark(m.normalTexture, Role.Normal);
                    Mark(m.occlusionTexture, Role.Linear);
                    Mark(m.pbrMetallicRoughness?.metallicRoughnessTexture, Role.Linear);
                }
                if (roles.TryGetValue(file, out Role found)) return found;
            }
            return Role.Color;
        }
    }
}
