using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;

namespace CoreFreqWindows.UI;

public enum KeyAction
{
    None,
    Quit,
    Help,
    FrequencyView,
    TemperatureView,
    VoltageView,
    PowerView,
    TopologyView,
    SystemInfoView,
    SensorsView,
    DashboardView,
    Pause,
    ToggleLogging,
    Export,
    ClearMinMax,
    IncreaseInterval,
    DecreaseInterval
}

public class KeyboardHandler
{
    private CancellationTokenSource? _cancellationTokenSource;
    private KeyAction _lastAction = KeyAction.None;

    public void Start()
    {
        _cancellationTokenSource = new CancellationTokenSource();
        Task.Run(() => ListenForKeys(_cancellationTokenSource.Token));
    }

    public void Stop()
    {
        _cancellationTokenSource?.Cancel();
    }

    public KeyAction GetLastAction()
    {
        var action = _lastAction;
        _lastAction = KeyAction.None;
        return action;
    }

    private void ListenForKeys(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            if (Console.KeyAvailable)
            {
                var key = Console.ReadKey(true);
                _lastAction = MapKeyToAction(key);
            }
            Thread.Sleep(50); // Small delay to avoid high CPU usage
        }
    }

    private static KeyAction MapKeyToAction(ConsoleKeyInfo key)
    {
        // Handle Ctrl combinations
        if (key.Modifiers == ConsoleModifiers.Control)
        {
            return key.Key switch
            {
                ConsoleKey.C or ConsoleKey.X => KeyAction.Quit,
                _ => KeyAction.None
            };
        }

        // Handle function keys and regular keys
        return key.Key switch
        {
            ConsoleKey.Q => KeyAction.Quit,
            ConsoleKey.F1 => KeyAction.Help,
            ConsoleKey.F2 => KeyAction.FrequencyView,
            ConsoleKey.F3 => KeyAction.TemperatureView,
            ConsoleKey.F4 => KeyAction.VoltageView,
            ConsoleKey.F5 => KeyAction.PowerView,
            ConsoleKey.F6 => KeyAction.TopologyView,
            ConsoleKey.F7 => KeyAction.SystemInfoView,
            ConsoleKey.F8 => KeyAction.SensorsView,
            ConsoleKey.D1 => KeyAction.DashboardView,
            ConsoleKey.Spacebar => KeyAction.Pause,
            ConsoleKey.L => KeyAction.ToggleLogging,
            ConsoleKey.E => KeyAction.Export,
            ConsoleKey.C => KeyAction.ClearMinMax,
            ConsoleKey.Add or ConsoleKey.OemPlus => KeyAction.IncreaseInterval,
            ConsoleKey.Subtract or ConsoleKey.OemMinus => KeyAction.DecreaseInterval,
            _ => KeyAction.None
        };
    }
}

