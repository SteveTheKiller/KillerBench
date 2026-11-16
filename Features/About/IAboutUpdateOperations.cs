using System;
using System.Threading.Tasks;
using KillerBench.Services;

namespace KillerBench.Features.About
{
    /// <summary>The release network and process boundary used by the About update action.</summary>
    internal interface IAboutUpdateOperations
    {
        Task<string?> CheckAsync(Version? current);
        Task<string> DownloadAsync(string tag);
        void StartSwap(string path, string version);
    }

    internal sealed class SystemAboutUpdateOperations : IAboutUpdateOperations
    {
        internal static readonly SystemAboutUpdateOperations Instance = new();
        public Task<string?> CheckAsync(Version? current) => UpdateService.CheckAsync(current);
        public Task<string> DownloadAsync(string tag) => UpdateService.DownloadAsync(tag);
        public void StartSwap(string path, string version) => UpdateService.StartSwap(path, version);
    }
}
