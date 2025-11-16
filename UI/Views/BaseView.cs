using CoreFreqWindows.Models;

namespace CoreFreqWindows.UI.Views;

public abstract class BaseView
{
    public abstract string Name { get; }
    public abstract void Render(MonitoringSnapshot snapshot);
    public virtual void OnEnter() { }
    public virtual void OnExit() { }
}

