using Microsoft.AspNetCore.SignalR;

namespace RestApiService.SignalR;

public sealed class TelemetryHub : Hub
{
    public const string DashboardGroup = "telemetry-dashboard";

    public Task JoinDashboard() => Groups.AddToGroupAsync(Context.ConnectionId, DashboardGroup);
}
