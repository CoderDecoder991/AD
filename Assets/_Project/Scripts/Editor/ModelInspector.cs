using System.Text;
using UnityEditor;
using UnityEngine;

namespace AirDefense.EditorTools
{
    /// <summary>Debug helper: logs the transform hierarchy of an imported model asset.</summary>
    public static class ModelInspector
    {
        public static void DumpZu23() => Dump(M1SceneBuilder.Zu23ModelPath);

        public static void Dump(string path)
        {
            var main = AssetDatabase.LoadMainAssetAtPath(path);
            Debug.Log($"[ModelInspector] main asset: {main} ({main?.GetType().Name})");
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
                if (o is GameObject go && go.transform.parent == null)
                {
                    var sb = new StringBuilder();
                    Walk(go.transform, 0, sb);
                    Debug.Log("[ModelInspector] hierarchy:\n" + sb);
                }
        }

        private static void Walk(Transform t, int depth, StringBuilder sb)
        {
            var mf = t.GetComponent<MeshFilter>();
            sb.Append(' ', depth * 2).Append(t.name)
              .Append(mf ? $"  [mesh {mf.sharedMesh?.name}, {mf.sharedMesh?.triangles.Length / 3} tris]" : "")
              .Append($"  pos {t.localPosition}\n");
            foreach (Transform c in t) Walk(c, depth + 1, sb);
        }
    }
}
