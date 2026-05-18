using System;

namespace DayTrader.Interop;

// Minimal subscriber for the AutoRetainer IPC channels DayTrader uses.
// Vendored (not referenced) so DayTrader doesn't take a dependency on the
// AutoRetainerAPI package and its transitive ECommons dependency. Channel
// names and signatures mirror AutoRetainerAPI/ApiConsts.cs at the time of
// writing; if AR's IPC surface changes, only this file needs updating.
internal sealed class AutoRetainerIpc : IDisposable
{
    private const string ChannelOnRetainerAdditionalTask    = "AutoRetainer.OnRetainerAdditionalTask";
    private const string ChannelOnRetainerReadyForPostproc  = "AutoRetainer.OnRetainerReadyForPostprocess";
    private const string ChannelRequestRetainerPostprocess  = "AutoRetainer.RequestPostprocess";
    private const string ChannelFinishRetainerPostprocess   = "AutoRetainer.FinishPostprocessRequest";

    public readonly string PluginName;

    // Fired once per retainer in an AR cycle: "do you want a post-process slot?"
    // Respond by calling RequestRetainerPostprocess() if yes.
    public event Action<string>? OnRetainerPostprocessStep;

    // Fired when AR has the retainer in front of us and is handing control over.
    // The first arg is the plugin name AR is waiting on — only act when it
    // matches ours.
    public event Action<string, string>? OnRetainerReadyToPostprocess;

    public AutoRetainerIpc(Dalamud.Plugin.IDalamudPluginInterface pi, string pluginName)
    {
        PluginName = pluginName;

        pi.GetIpcSubscriber<string, object>(ChannelOnRetainerAdditionalTask)
            .Subscribe(HandleAdditionalTask);
        pi.GetIpcSubscriber<string, string, object>(ChannelOnRetainerReadyForPostproc)
            .Subscribe(HandleReadyToPostprocess);
    }

    public void RequestRetainerPostprocess()
        => Service.PluginInterface.GetIpcSubscriber<string, object>(ChannelRequestRetainerPostprocess)
            .InvokeAction(PluginName);

    public void FinishRetainerPostProcess()
        => Service.PluginInterface.GetIpcSubscriber<object>(ChannelFinishRetainerPostprocess)
            .InvokeAction();

    private void HandleAdditionalTask(string retainerName)
        => OnRetainerPostprocessStep?.Invoke(retainerName);

    private void HandleReadyToPostprocess(string pluginName, string retainerName)
        => OnRetainerReadyToPostprocess?.Invoke(pluginName, retainerName);

    public void Dispose()
    {
        Service.PluginInterface.GetIpcSubscriber<string, object>(ChannelOnRetainerAdditionalTask)
            .Unsubscribe(HandleAdditionalTask);
        Service.PluginInterface.GetIpcSubscriber<string, string, object>(ChannelOnRetainerReadyForPostproc)
            .Unsubscribe(HandleReadyToPostprocess);
    }
}
