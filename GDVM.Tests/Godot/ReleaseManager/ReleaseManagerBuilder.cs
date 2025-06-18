using GDVM.Environment;
using GDVM.Godot;
using Microsoft.Extensions.Logging;
using Moq;
using System.Runtime.InteropServices;

namespace GDVM.Test.Godot.ReleaseManager;

public class ReleaseManagerBuilder
{
    private readonly Mock<IDownloadClient> _mockDownloadClient;
    private Action<Mock<IDownloadClient>>? _downloadClientConfig;
    private IEnumerable<string> _releases;
    private SystemInfo _systemInfo;

    public ReleaseManagerBuilder()
    {
        _systemInfo = new SystemInfo(OS.Windows, Architecture.X64);
        _mockDownloadClient = new Mock<IDownloadClient>();
        _releases = TestData.TestReleases;

        _mockDownloadClient
            .Setup(x => x.ListReleases(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(_releases));
    }

    public ReleaseManagerBuilder WithOSAndArch(OS os, Architecture arch)
    {
        _systemInfo = new SystemInfo(os, arch);
        return this;
    }

    public ReleaseManagerBuilder WithReleases(IEnumerable<string> releases)
    {
        _releases = releases.ToArray();
        _mockDownloadClient
            .Setup(x => x.ListReleases(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(_releases));

        return this;
    }

    public ReleaseManagerBuilder ConfigureDownloadClient(Action<Mock<IDownloadClient>> config)
    {
        _downloadClientConfig = config;
        return this;
    }

    public GDVM.Godot.ReleaseManager Build()
    {
        _downloadClientConfig?.Invoke(_mockDownloadClient);

        return new GDVM.Godot.ReleaseManager(
            new HostSystem(_systemInfo, new Mock<IPathService>().Object, new Mock<ILogger<HostSystem>>().Object),
            new PlatformStringProvider(_systemInfo),
            _mockDownloadClient.Object);
    }
}
