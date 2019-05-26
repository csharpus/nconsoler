namespace NConsoler
{
    using System;

    /// <summary>
    /// Uses Console class for message output
    /// </summary>
    public class ConsoleMessenger : IMessenger
    {
        public void Write(string message) => Console.WriteLine(message);
    }
}