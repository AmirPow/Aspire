using Microsoft.AspNetCore.SignalR.Client;

namespace ContentPlatform.Presentation.Services;

public class SignalRService
{
	private HubConnection? _hubConnection;

	public event Action? OnUserAdded;

	public async Task StartConnection(string hubUrl)
	{
		_hubConnection = new HubConnectionBuilder()
			.WithUrl(hubUrl)
			.WithAutomaticReconnect()
			.Build();

		_hubConnection.On("Update", () =>
		{
			OnUserAdded?.Invoke();
		});

		await _hubConnection.StartAsync();
	}
}