using System.Text.Json;
using System.Text.Json.Nodes;
using TrafficHunt.Application.Interfaces;

namespace TrafficHunt.Web.Services;

/// <summary>
/// Persists settings into appsettings.json (read-modify-write, indented output).
/// Used for API keys and OAuth tokens; created per-use via AppSettingsStoreFactory.
/// </summary>
public class AppSettingsStore : ISettingsStore
{
    private readonly string _path;
    private JsonObject? _root;

    public AppSettingsStore(string path) => _path = path;

    private JsonObject Load()
    {
        if (_root != null) return _root;
        _root = File.Exists(_path)
            ? JsonNode.Parse(File.ReadAllText(_path))?.AsObject() ?? new JsonObject()
            : new JsonObject();
        return _root;
    }

    public string? Get(string key)
    {
        var node = Load();
        foreach (var part in key.Split(':'))
        {
            if (node is null || !node.TryGetPropertyValue(part, out var child))
                return null;
            node = child as JsonObject;
            if (node is null)
                return child?.GetValue<string>();
        }
        return null;
    }

    public void Set(string key, string? value)
    {
        var node = Load();
        var parts = key.Split(':');
        for (var i = 0; i < parts.Length - 1; i++)
        {
            if (!node.TryGetPropertyValue(parts[i], out var child) || child is not JsonObject obj)
            {
                obj = new JsonObject();
                node[parts[i]] = obj;
            }
            node = obj;
        }
        node[parts[^1]] = value;
    }

    public void Save()
    {
        if (_root is null) return;
        File.WriteAllText(_path, _root.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }
}

public class AppSettingsStoreFactory : ISettingsStoreFactory
{
    private readonly string _path;
    public AppSettingsStoreFactory(string path) => _path = path;
    public ISettingsStore Create() => new AppSettingsStore(_path);
}