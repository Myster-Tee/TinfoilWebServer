namespace TinfoilWebServer.Services;

public interface ISummaryInfoLogger
{
    void LogWelcomeMessage();

    void LogRelevantSettings();

    void LogListenedHosts();

    void LogCurrentMachineInfo();
}