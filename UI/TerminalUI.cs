using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Spectre.Console;
using CoreFreqWindows.Config;
using CoreFreqWindows.Models;
using CoreFreqWindows.Services;
using CoreFreqWindows.UI.Components;
using CoreFreqWindows.UI.Views;

namespace CoreFreqWindows.UI;

public class TerminalUI
{
    private readonly DataCollectionService _dataService;
    private readonly UpdateService _updateService;
    private readonly ColorScheme _colorScheme;
    private readonly Header _header;
    private readonly StatusBar _statusBar;
    private readonly KeyboardHandler _keyboardHandler;
    private BaseView _currentView;
    private bool _isRunning = false;
    private bool _isPaused = false;
    private CancellationTokenSource? _cancellationTokenSource;

    public TerminalUI(
        DataCollectionService dataService,
        UpdateService updateService,
        BaseView initialView)
    {
        _dataService = dataService;
        _updateService = updateService;
        _colorScheme = ColorScheme.Default;
        _header = new Header(_colorScheme);
        _statusBar = new StatusBar(_colorScheme);
        _keyboardHandler = new KeyboardHandler();
        _currentView = initialView;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        _cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _isRunning = true;
        _keyboardHandler.Start();

        try
        {
            try
            {
                AnsiConsole.Clear();
            }
            catch (IOException)
            {
                // Console may not be available in non-interactive environments
                // Continue without clearing
            }
            _currentView.OnEnter();

            while (_isRunning && !_cancellationTokenSource.Token.IsCancellationRequested)
            {
                // Handle keyboard input
                var action = _keyboardHandler.GetLastAction();
                HandleKeyAction(action);

                if (!_isPaused)
                {
                    // Update data
                    _updateService.Update();

                    // Collect snapshot
                    var snapshot = _dataService.GetSnapshot();

                    // Render UI
                    try
                    {
                        AnsiConsole.Clear();
                    }
                    catch (IOException)
                    {
                        // Console may not be available, continue anyway
                    }
                    _header.Render(snapshot.SystemInfo);
                    _currentView.Render(snapshot);
                    _statusBar.Render();
                }

                // Wait for next update
                await Task.Delay(_updateService.UpdateInterval, _cancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        finally
        {
            _keyboardHandler.Stop();
            _currentView.OnExit();
            AnsiConsole.Reset();
        }
    }

    private void HandleKeyAction(KeyAction action)
    {
        switch (action)
        {
            case KeyAction.Quit:
                _isRunning = false;
                _cancellationTokenSource?.Cancel();
                break;

            case KeyAction.Pause:
                _isPaused = !_isPaused;
                break;

            case KeyAction.DashboardView:
                SwitchView(new DashboardView(_colorScheme));
                break;

            case KeyAction.FrequencyView:
                // Will be implemented in Phase 2
                break;

            case KeyAction.TemperatureView:
                // Will be implemented in Phase 2
                break;

            case KeyAction.VoltageView:
                // Will be implemented in Phase 2
                break;

            case KeyAction.PowerView:
                // Will be implemented in Phase 2
                break;

            case KeyAction.TopologyView:
                // Will be implemented in Phase 3
                break;

            case KeyAction.SystemInfoView:
                // Will be implemented in Phase 3
                break;

            case KeyAction.SensorsView:
                // Will be implemented in Phase 2
                break;

            case KeyAction.Help:
                // Will be implemented in Phase 4
                break;
        }
    }

    private void SwitchView(BaseView newView)
    {
        _currentView.OnExit();
        _currentView = newView;
        _currentView.OnEnter();
        _statusBar.SetCurrentView(_currentView.Name);
    }

    public void Stop()
    {
        _isRunning = false;
        _cancellationTokenSource?.Cancel();
    }
}

