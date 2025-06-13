﻿using System.Text.Json;
using KworkNotify.Core.Interfaces;
using StackExchange.Redis;

namespace KworkNotify.Core.Service.Cache;

public class AppCache(IConnectionMultiplexer connection) : IAppCache
{
    public async Task<string?> GetAsync(string key)
    {
        var db = connection.GetDatabase();
        return await db.StringGetAsync(key);
    }

    public async Task SetAsync(string key, string value, TimeSpan? expiry = null)
    {
        var db = connection.GetDatabase();
        await db.StringSetAsync(key, value, expiry);
    }
    
    public async Task<T?> GetAsync<T>(string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var db = connection.GetDatabase();
        var value = await db.StringGetAsync(key);
        return value.IsNullOrEmpty ? default : JsonSerializer.Deserialize<T>(value!);
    }

    public async Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null)
    {
        var db = connection.GetDatabase();
        var jsonValue = JsonSerializer.Serialize(value);
        return await db.StringSetAsync(key, jsonValue, expiry);
    }
    
    public async Task<bool> ReplaceIfExistsAsync<T>(string key, T value, TimeSpan? expiry = null, bool keepTtl = false)
    {
        var db = connection.GetDatabase();
        var jsonValue = JsonSerializer.Serialize(value);
        return await db.StringSetAsync(key, jsonValue, expiry, keepTtl: keepTtl, when: When.Exists);
    }
    
    public async Task<bool> SetKeyAsync(string key, TimeSpan? expiry = null)
    {
        var db = connection.GetDatabase();
        return await db.StringSetAsync(key, "1", expiry);
    }

    public async Task<bool> KeyExistsAsync(string key)
    {
        var db = connection.GetDatabase();
        return await db.KeyExistsAsync(key);
    }
}