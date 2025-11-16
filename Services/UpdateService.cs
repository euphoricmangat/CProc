using CoreFreqWindows.Services;

namespace CoreFreqWindows.Services;

public class UpdateService
{
    private readonly DataCollectionService _dataCollectionService;
    private int _updateInterval;

    public UpdateService(DataCollectionService dataCollectionService, int updateInterval = 1000)
    {
        _dataCollectionService = dataCollectionService;
        _updateInterval = updateInterval;
    }

    public int UpdateInterval
    {
        get => _updateInterval;
        set => _updateInterval = Math.Max(100, Math.Min(10000, value));
    }

    public void Update()
    {
        _dataCollectionService.CollectData();
    }
}

