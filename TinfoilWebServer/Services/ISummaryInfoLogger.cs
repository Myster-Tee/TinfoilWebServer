namespace TinfoilWebServer.Services;

public interface ISummaryInfoLogger
{
    void LogWelcomeMessage();

    void LogBootErrors();

    void LogRelevantSettings();

    void LogListenedHosts();

    void LogCurrentMachineInfo();
}