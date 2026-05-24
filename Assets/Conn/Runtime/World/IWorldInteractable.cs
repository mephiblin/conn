namespace Conn.Runtime.World
{
    public interface IWorldInteractable
    {
        string Prompt { get; }
        bool CanInteract { get; }
        void Interact();
    }
}
