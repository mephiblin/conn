using Conn.Core.Session;
using UnityEngine;

namespace Conn.Runtime.Session
{
    public sealed class GameSession : MonoBehaviour
    {
        private static GameSession instance;

        [SerializeField] private GameSessionState state = new GameSessionState();

        public static GameSession Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                var existing = FindAnyObjectByType<GameSession>();
                if (existing != null)
                {
                    instance = existing;
                    return instance;
                }

                var created = new GameObject(nameof(GameSession));
                instance = created.AddComponent<GameSession>();
                DontDestroyOnLoad(created);
                return instance;
            }
        }

        public GameSessionState State => state;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void StartNewGame()
        {
            state.StartNewGame();
            SaveGame();
        }

        public bool TryContinue()
        {
            return SaveRuntimeService.TryLoad(state);
        }

        public void SaveGame()
        {
            SaveRuntimeService.Save(state);
        }
    }
}
