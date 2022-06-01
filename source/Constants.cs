using Playnite.SDK.Models;

namespace DuplicateHider
{
    public class Constants
    {
        public const string UNDEFINED_SOURCE = "Playnite";
        public static readonly GameSource DEFAULT_SOURCE = new GameSource(UNDEFINED_SOURCE) { Id = System.Guid.Empty };
        public const string UNDEFINED_PLATFORM = "Undefined";
        public const int NUMBEROFSOURCESELECTORS = 10;
    }
}
