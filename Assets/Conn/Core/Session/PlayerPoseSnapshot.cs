namespace Conn.Core.Session
{
    [System.Serializable]
    public sealed class PlayerPoseSnapshot
    {
        public bool Valid;
        public float X;
        public float Y;
        public float Z;
        public float Yaw;

        public void Capture(float x, float y, float z, float yaw)
        {
            Valid = true;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
        }

        public void Clear()
        {
            Valid = false;
            X = 0f;
            Y = 0f;
            Z = 0f;
            Yaw = 0f;
        }
    }
}
