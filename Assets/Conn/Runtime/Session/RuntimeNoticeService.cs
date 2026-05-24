using Conn.Core.Session;
using UnityEngine;

namespace Conn.Runtime.Session
{
    public static class RuntimeNoticeService
    {
        public static void Set(GameSessionState session, string message)
        {
            session.LastNotice = message;
            Debug.Log(message);
        }
    }
}
