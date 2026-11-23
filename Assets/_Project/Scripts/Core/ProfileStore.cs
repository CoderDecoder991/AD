using UnityEngine;

namespace AirDefense.Core
{
    /// <summary>
    /// The one loaded profile for this run, plus save on demand. Loaded once at
    /// startup by RuntimeBootstrap; saved after missions and settings changes.
    /// </summary>
    public static class ProfileStore
    {
        private static SaveService _service;
        private static PlayerProfile _profile;

        public static PlayerProfile Profile
        {
            get
            {
                if (_profile == null) Load();
                return _profile;
            }
        }

        public static void Load()
        {
            _service ??= new SaveService(Application.persistentDataPath);
            _profile = _service.Load();
            if (_service.LastLoadIssue != null) Debug.LogWarning("[ProfileStore] " + _service.LastLoadIssue);
        }

        public static void Save()
        {
            if (_profile == null) return;
            _service ??= new SaveService(Application.persistentDataPath);
            try { _service.Save(_profile); }
            catch (System.Exception e) { Debug.LogError("[ProfileStore] save failed: " + e.Message); }
        }
    }
}
