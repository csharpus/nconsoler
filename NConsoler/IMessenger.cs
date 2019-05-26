namespace NConsoler
{
    /// <summary>
    /// Used for getting messages from NConsoler
    /// </summary>
    public interface IMessenger
    {
        void Write(string message);
    }
}