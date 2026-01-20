using Microsoft.AspNetCore.SignalR.Client;
using Trenios.Mobile.Models.Api;

namespace Trenios.Mobile.Services;

public class OrderHubService : IAsyncDisposable
{
    private readonly ApiService _apiService;
    private HubConnection? _hubConnection;
    private Guid? _currentBranchId;

    private const string HubUrl = "https://trenios-api-prod.azurewebsites.net/hubs/orders";

    public event Action<OrderResponse>? OnOrderCreated;
    public event Action<OrderResponse>? OnOrderStatusUpdated;
    public event Action<string>? OnConnectionStateChanged;
    public event Action<Exception>? OnError;

    public bool IsConnected => _hubConnection?.State == HubConnectionState.Connected;

    public OrderHubService(ApiService apiService)
    {
        _apiService = apiService;
    }

    public async Task ConnectAsync(Guid branchId)
    {
        // Disconnect if already connected to a different branch
        if (_hubConnection != null)
        {
            if (_currentBranchId == branchId && IsConnected)
                return;

            await DisconnectAsync();
        }

        var token = _apiService.GetToken();
        if (string.IsNullOrEmpty(token))
        {
            OnError?.Invoke(new InvalidOperationException("No authentication token available"));
            return;
        }

        _hubConnection = new HubConnectionBuilder()
            .WithUrl($"{HubUrl}?access_token={token}")
            .WithAutomaticReconnect(new[] { TimeSpan.Zero, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(10) })
            .Build();

        // Register event handlers
        _hubConnection.On<OrderResponse>("OrderCreated", order =>
        {
            MainThread.BeginInvokeOnMainThread(() => OnOrderCreated?.Invoke(order));
        });

        _hubConnection.On<OrderResponse>("OrderStatusUpdated", order =>
        {
            MainThread.BeginInvokeOnMainThread(() => OnOrderStatusUpdated?.Invoke(order));
        });

        _hubConnection.Closed += async (error) =>
        {
            OnConnectionStateChanged?.Invoke("Disconnected");
            if (error != null)
                OnError?.Invoke(error);
            await Task.CompletedTask;
        };

        _hubConnection.Reconnecting += async (error) =>
        {
            OnConnectionStateChanged?.Invoke("Reconnecting...");
            await Task.CompletedTask;
        };

        _hubConnection.Reconnected += async (connectionId) =>
        {
            OnConnectionStateChanged?.Invoke("Connected");
            // Rejoin the branch group after reconnection
            if (_currentBranchId.HasValue)
            {
                await JoinBranchGroupAsync(_currentBranchId.Value);
            }
        };

        try
        {
            await _hubConnection.StartAsync();
            OnConnectionStateChanged?.Invoke("Connected");

            // Join the branch group
            await JoinBranchGroupAsync(branchId);
            _currentBranchId = branchId;
        }
        catch (Exception ex)
        {
            OnConnectionStateChanged?.Invoke("Failed to connect");
            OnError?.Invoke(ex);
        }
    }

    private async Task JoinBranchGroupAsync(Guid branchId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("JoinBranchGroup", branchId);
        }
    }

    private async Task LeaveBranchGroupAsync(Guid branchId)
    {
        if (_hubConnection?.State == HubConnectionState.Connected)
        {
            await _hubConnection.InvokeAsync("LeaveBranchGroup", branchId);
        }
    }

    public async Task DisconnectAsync()
    {
        if (_hubConnection != null)
        {
            if (_currentBranchId.HasValue && IsConnected)
            {
                try
                {
                    await LeaveBranchGroupAsync(_currentBranchId.Value);
                }
                catch
                {
                    // Ignore errors when leaving group
                }
            }

            await _hubConnection.DisposeAsync();
            _hubConnection = null;
            _currentBranchId = null;
            OnConnectionStateChanged?.Invoke("Disconnected");
        }
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
