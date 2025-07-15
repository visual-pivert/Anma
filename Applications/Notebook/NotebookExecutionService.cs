using System.Text.Json;
using Websocket.Client;
using System.Net.WebSockets;

namespace Anma.Applications.Notebook;

public class NotebookExecutionService : IDisposable
{
    private readonly IHttpClientFactory _httpClientFactory;
    private HttpClient _httpClient;
    private WebsocketClient? _wsClient;
    private string? _kernelId;

    private string _token = ""; // token dynamique
    private int _workspaceId = 0; // workspace dynamique
    private string _baseUrl = "http://localhost:5105";

    public NotebookExecutionService(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
        _httpClient = _httpClientFactory.CreateClient();
    }

    public void SetTokenAndWorkspace(string token, int workspaceId)
    {
        _token = token;
        _workspaceId = workspaceId;
    }

    public async Task InitializeKernelAsync()
    {
        var kernelResp = await _httpClient.PostAsync("http://localhost:8888/api/kernels", null);
        kernelResp.EnsureSuccessStatusCode();

        var kernelJson = await kernelResp.Content.ReadAsStringAsync();
        _kernelId = JsonDocument.Parse(kernelJson).RootElement.GetProperty("id").GetString();

        var wsUrl = new Uri($"ws://localhost:8888/api/kernels/{_kernelId}/channels");
        _wsClient = new WebsocketClient(wsUrl);
        await _wsClient.Start();

        var preloadCode = GeneratePreloadCode();
        var preloadDto = new ExecuteCodeDto { Input = preloadCode };
        await ExecuteCellAsync(preloadDto);
    }

    public async Task<ExecuteCodeDto> ExecuteCellAsync(ExecuteCodeDto input)
    {
        if (_wsClient == null || string.IsNullOrEmpty(_kernelId))
            throw new InvalidOperationException("Kernel not initialized.");

        var result = new ExecuteCodeDto { Input = input.Input };

        using var subscription = _wsClient.MessageReceived.Subscribe(msg =>
        {
            var json = JsonDocument.Parse(msg.Text!);
            if (json.RootElement.TryGetProperty("msg_type", out var msgType))
            {
                switch (msgType.GetString())
                {
                    case "stream":
                        var streamText = json.RootElement.GetProperty("content").GetProperty("text").GetString();
                        result.Outputs.Add(new NotebookOutputItem { Type = "text", Value = streamText });
                        break;

                    case "display_data":
                        var data = json.RootElement.GetProperty("content").GetProperty("data");
                        if (data.TryGetProperty("image/png", out var image))
                        {
                            result.Outputs.Add(new NotebookOutputItem { Type = "image", Value = image.GetString() });
                        }
                        if (data.TryGetProperty("text/plain", out var plain))
                        {
                            result.Outputs.Add(new NotebookOutputItem { Type = "text", Value = plain.GetString() });
                        }
                        break;

                    case "error":
                        var err = json.RootElement.GetProperty("content").GetProperty("traceback").EnumerateArray()
                            .Select(x => x.GetString()).ToArray();
                        result.Outputs.Add(new NotebookOutputItem { Type = "error", Value = string.Join("\n", err) });
                        break;
                }
            }
        });

        var request = new
        {
            header = new
            {
                msg_id = Guid.NewGuid().ToString(),
                username = "user",
                session = Guid.NewGuid().ToString(),
                msg_type = "execute_request",
                version = "5.3"
            },
            parent_header = new { },
            metadata = new { },
            content = new
            {
                code = input.Input,
                silent = false
            }
        };

        var jsonString = JsonSerializer.Serialize(request);
        await _wsClient.SendInstant(jsonString);

        await Task.Delay(2000);

        return result;
    }

    public async Task ShutdownKernelAsync()
    {
        if (_wsClient != null)
        {
            await _wsClient.Stop(WebSocketCloseStatus.NormalClosure, "Closing");
            _wsClient.Dispose();
            _wsClient = null;
        }

        if (!string.IsNullOrEmpty(_kernelId))
        {
            await _httpClient.DeleteAsync($"http://localhost:8888/api/kernels/{_kernelId}");
            _kernelId = null;
        }
    }

    public void Dispose()
    {
        _wsClient?.Dispose();
    }

    private string GeneratePreloadCode()
    {
        return $@"
token = '{_token}'
workspace_id = {_workspaceId}
base_url = '{_baseUrl.TrimEnd('/')}'  # enlève un éventuel '/' final

import requests
import pandas as pd
import json

def fetch_databases():
    headers = {{'Authorization': f'Bearer {{token}}'}}
    url = f'{{base_url}}/workspaces/{{workspace_id}}/databases'
    r = requests.get(url, headers=headers)
    r.raise_for_status()
    return r.json()

def fetch_tables(database_slug):
    headers = {{'Authorization': f'Bearer {{token}}'}}
    url = f'{{base_url}}/workspaces/{{workspace_id}}/databases/{{database_slug}}/tables'
    r = requests.get(url, headers=headers)
    r.raise_for_status()
    return r.json()

def fetch_table_as_df(database_slug, table_slug):
    headers = {{'Authorization': f'Bearer {{token}}'}}
    url = f'{{base_url}}/workspaces/{{workspace_id}}/databases/{{database_slug}}/tables'
    r = requests.get(url, headers=headers)
    r.raise_for_status()
    tables = r.json()

    table_entry = next((t for t in tables if t['slug'] == table_slug), None)
    if table_entry is None:
        raise ValueError(f'Table slug ""{{table_slug}}"" not found')

    columns_data = json.loads(table_entry['columnsJson'])

    columns = {{}}
    for col_name, col_info in columns_data.items():
        columns[col_name] = col_info['value']

    return pd.DataFrame(columns)
";
    }


}

