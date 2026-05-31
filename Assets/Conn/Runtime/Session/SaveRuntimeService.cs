using System.IO;
using Conn.Core.Scenes;
using Conn.Core.Session;
using Conn.Runtime.Scenes;
using UnityEngine;

namespace Conn.Runtime.Session
{
    public static class SaveRuntimeService
    {
        private const string SaveFileName = "conn_save_slot_0.json";

        public static bool HasSave => File.Exists(SavePath);

        private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

        public static void Save(GameSessionState session)
        {
            var directory = Path.GetDirectoryName(SavePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(SavePath, ToJson(session));
        }

        public static bool TryLoad(GameSessionState target)
        {
            if (!HasSave)
            {
                return false;
            }

            OverwriteFromJson(File.ReadAllText(SavePath), target);
            target.Combat.Clear();
            return true;
        }

        public static string ToJson(GameSessionState session)
        {
            return JsonUtility.ToJson(session, true);
        }

        public static void OverwriteFromJson(string json, GameSessionState target)
        {
            JsonUtility.FromJsonOverwrite(json, target);
        }

        public static GameSceneId SceneForLoadedState(GameSessionState session)
        {
            return session.Mode switch
            {
                GameMode.CharacterCreation => GameSceneId.Title,
                GameMode.Town => GameSceneId.Town,
                GameMode.Dungeon => GameSceneId.Dungeon,
                GameMode.Combat => GameSceneId.Dungeon,
                GameMode.Ending => GameSceneId.Ending,
                _ => GameSceneId.Town
            };
        }
    }
}
