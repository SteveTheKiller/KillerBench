namespace KillerBench.Services
{
    // The checks compile UpdateImagePublication.cs without the rest of the kit UpdateService, which
    // owns RelaunchArgument in the app. This stands in for it here only.
    internal static partial class UpdateService
    {
        internal static string RelaunchArgument { get; set; } = "";
    }
}
