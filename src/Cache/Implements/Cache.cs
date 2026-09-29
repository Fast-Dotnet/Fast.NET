// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Collections;
using System.Diagnostics;
using CSRedis;
using Microsoft.Extensions.Options;

namespace Fast.Cache;

/// <inheritdoc cref="ICache" />
internal sealed class Cache : Cache<DefaultCacheContextLocator>, ICache
{
    public Cache(IOptionsMonitor<RedisSettingsOptions> redisSettings) : base(redisSettings)
    {
    }
}

/// <inheritdoc cref="ICache{CacheContextLocator}" />
internal class Cache<CacheContextLocator> : ICache<CacheContextLocator>, IDisposable
    where CacheContextLocator : ICacheContextLocator, new()
{
    internal readonly IDisposable _optionsReloadToken;

    private readonly object _reloadLock = new();
    private CacheSnapshot _snapshot;
    private bool _disposed;

    // 配置重载采用硬切换：新快照发布后立即释放旧客户端，切换瞬间正在使用旧客户端的操作可能失败。
    private sealed record CacheSnapshot(CSRedisClient Client, string Prefix, string ConnectionString);

    private CacheSnapshot GetSnapshot()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed), this);
        return Volatile.Read(ref _snapshot);
    }

    /// <summary>
    /// 空值
    /// </summary>
    internal const string _nullValue = "×Null×";

    /// <inheritdoc />
    public string Prefix => GetSnapshot().Prefix;

    /// <inheritdoc />
    public CSRedisClient Client => GetSnapshot().Client;

    /// <inheritdoc />
    public CacheContextLocator ContextLocator { get; }

    public Cache(IOptionsMonitor<RedisSettingsOptions> redisSettings)
    {
        ContextLocator = new CacheContextLocator();

        // 初始化服务
        Create(redisSettings.CurrentValue);

        // 监听配置更改
        _optionsReloadToken = redisSettings.OnChange(settings =>
        {
            try
            {
                Create(settings);
            }
            catch (Exception)
            {
                // 不输出异常消息或连接字符串，避免在重载失败时泄露连接凭据。
                Trace.TraceError("Redis 配置重载失败，继续使用上一代客户端。请检查配置与依赖服务。");
            }
        });
    }

    /// <summary>
    /// 创建/初始化服务
    /// </summary>
    /// <param name="redisSettings">redis Settings 配置</param>
    private void Create(RedisSettingsOptions redisSettings)
    {
        Debugging.Info($"Creating cache, Service = {ContextLocator.ServiceName}......");

        string connectionStr;
        string prefix;

        if (ContextLocator.ServiceName == "Default")
        {
            connectionStr =
                $"{redisSettings.ServiceIp}:{redisSettings.Port ?? 6379},password={redisSettings.DbPwd},defaultDatabase={redisSettings.DbName},prefix={redisSettings.Prefix},poolsize={redisSettings.Poolsize},ssl={(redisSettings.SSL == true ? "true" : "false")}";

            prefix = redisSettings.Prefix;
        }
        else
        {
            // 根据缓存上下文定位器，获取到服务名称
            RedisServiceSettingsOptions redisServiceSettings =
                redisSettings.Services.FirstOrDefault(f => f.ServiceName == ContextLocator.ServiceName);

            if (redisServiceSettings == null)
            {
                throw new InvalidOperationException($"服务名称“{ContextLocator.ServiceName}”不存在于“RedisSetting”配置节点中！");
            }

            connectionStr =
                $"{redisServiceSettings.ServiceIp ?? redisSettings.ServiceIp}:{redisServiceSettings.Port ?? redisSettings.Port ?? 6379},password={redisServiceSettings.DbPwd ?? redisSettings.DbPwd},defaultDatabase={redisServiceSettings.DbName ?? redisSettings.DbName},prefix={redisServiceSettings.Prefix ?? redisSettings.Prefix},poolsize={redisServiceSettings.Poolsize ?? redisSettings.Poolsize},ssl={((redisServiceSettings.SSL ?? redisSettings.SSL) == true ? "true" : "false")}";

            prefix = redisServiceSettings.Prefix ?? redisSettings.Prefix;
        }

        CacheSnapshot previous;
        lock (_reloadLock)
        {
            if (_disposed || _snapshot?.ConnectionString == connectionStr)
            {
                return;
            }

            // 先成功构造再发布，构造失败时继续使用当前快照。
            var replacement = new CacheSnapshot(new CSRedisClient(connectionStr), prefix, connectionStr);
            previous = Interlocked.Exchange(ref _snapshot, replacement);
        }

        if (previous != null)
        {
            try
            {
                previous.Client.Dispose();
            }
            catch (Exception)
            {
                Trace.TraceError("Redis 旧客户端释放失败；新客户端已继续提供服务。");
            }
        }
    }

    /// <summary>停止配置监听，并释放当前客户端。</summary>
    public void Dispose()
    {
        CSRedisClient client;
        lock (_reloadLock)
        {
            if (_disposed)
            {
                return;
            }

            Volatile.Write(ref _disposed, true);
            client = _snapshot?.Client;
        }

        // 监听器取消可能等待正在执行的回调，不在重载锁内调用。
        _optionsReloadToken?.Dispose();
        try
        {
            client?.Dispose();
        }
        catch (Exception)
        {
            Trace.TraceError("Redis 客户端释放失败。");
        }
    }

    /// <inheritdoc />
    public long Del(params string[] key)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return snapshot.Client.Del(key);
    }

    /// <inheritdoc />
    public async Task<long> DelAsync(params string[] key)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return await snapshot.Client.DelAsync(key);
    }

    /// <inheritdoc />
    public long DelByPattern(string pattern)
    {
        CacheSnapshot snapshot = GetSnapshot();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return 0;
        }

        // 判断是否已 * 结尾
        if (!pattern.EndsWith('*'))
        {
            pattern += "*";
        }

        // 处理前缀，这里 Scan 扫描不会默认带前缀
        if (!string.IsNullOrWhiteSpace(snapshot.Prefix))
        {
            pattern = snapshot.Prefix + pattern;
        }

        long cursor = 0L;
        long totalDeleted = 0L;

        do
        {
            // 每次返回最多 100 个
            RedisScan<string> keys = snapshot.Client.Scan(cursor, pattern, 100);
            if (keys == null)
            {
                cursor = 0;
                continue;
            }

            cursor = keys.Cursor;
            if (keys.Items.Length > 0)
            {
                string[] keyItems = keys.Items;
                // 处理前缀，这里 Del 删除又会默认带前缀
                if (!string.IsNullOrWhiteSpace(snapshot.Prefix))
                {
                    keyItems = keys.Items.Select(sl => sl[snapshot.Prefix.Length..]).ToArray();
                }

                totalDeleted += snapshot.Client.Del(keyItems);
            }
        } while (cursor != 0);

        return totalDeleted;
    }

    /// <inheritdoc />
    public async Task<long> DelByPatternAsync(string pattern)
    {
        CacheSnapshot snapshot = GetSnapshot();
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return 0;
        }

        // 判断是否已 * 结尾
        if (!pattern.EndsWith("*"))
        {
            pattern += "*";
        }

        // 处理前缀，这里 ScanAsync 扫描不会默认带前缀
        if (!string.IsNullOrWhiteSpace(snapshot.Prefix))
        {
            pattern = snapshot.Prefix + pattern;
        }

        long cursor = 0L;
        long totalDeleted = 0L;

        do
        {
            // 每次返回最多 100 个
            RedisScan<string> keys = await snapshot.Client.ScanAsync(cursor, pattern, 100);
            if (keys == null)
            {
                cursor = 0;
                continue;
            }

            cursor = keys.Cursor;
            if (keys.Items.Length > 0)
            {
                string[] keyItems = keys.Items;
                // 处理前缀，这里 DelAsync 删除又会默认带前缀
                if (!string.IsNullOrWhiteSpace(snapshot.Prefix))
                {
                    keyItems = keys.Items.Select(sl => sl[snapshot.Prefix.Length..]).ToArray();
                }

                totalDeleted += await snapshot.Client.DelAsync(keyItems);
            }
        } while (cursor != 0);

        return totalDeleted;
    }

    /// <inheritdoc />
    public bool Exists(string key)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return snapshot.Client.Exists(key);
    }

    /// <inheritdoc />
    public async Task<bool> ExistsAsync(string key)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return await snapshot.Client.ExistsAsync(key);
    }

    /// <inheritdoc />
    public string Get(string key)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return snapshot.Client.Get(key);
    }

    /// <inheritdoc />
    public async Task<string> GetAsync(string key)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return await snapshot.Client.GetAsync(key);
    }

    /// <inheritdoc />
    public T Get<T>(string key)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return snapshot.Client.Get<T>(key);
    }

    /// <inheritdoc />
    public async Task<T> GetAsync<T>(string key)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return await snapshot.Client.GetAsync<T>(key);
    }

    /// <inheritdoc />
    public bool Set(string key, object value)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return snapshot.Client.Set(key, value);
    }

    /// <inheritdoc />
    public async Task<bool> SetAsync(string key, object value)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return await snapshot.Client.SetAsync(key, value);
    }

    /// <inheritdoc />
    public bool Set(string key, object value, int expireSeconds)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return snapshot.Client.Set(key, value, expireSeconds);
    }

    /// <inheritdoc />
    public async Task<bool> SetAsync(string key, object value, int expireSeconds)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return await snapshot.Client.SetAsync(key, value, expireSeconds);
    }

    /// <inheritdoc />
    public bool Set(string key, object value, TimeSpan expireTimeSpan)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return snapshot.Client.Set(key, value, expireTimeSpan);
    }

    /// <inheritdoc />
    public async Task<bool> SetAsync(string key, object value, TimeSpan expireTimeSpan)
    {
        CacheSnapshot snapshot = GetSnapshot();
        return await snapshot.Client.SetAsync(key, value, expireTimeSpan);
    }

    /// <inheritdoc />
    public List<string> GetAllKeys()
    {
        CacheSnapshot snapshot = GetSnapshot();
        string[] result = snapshot.Client.Keys("*");
        return result.ToList();
    }

    /// <inheritdoc />
    public async Task<List<string>> GetAllKeysAsync()
    {
        CacheSnapshot snapshot = GetSnapshot();
        string[] result = await snapshot.Client.KeysAsync("*");
        return result.ToList();
    }

    /// <inheritdoc />
    public string GetAndSet(string key, Func<string> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string result = snapshot.Client.Get(key);

        if (result is _nullValue)
        {
            return null;
        }

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    if (TryGetCachedValue(snapshot.Client, key, out result))
                    {
                        return result;
                    }

                    result = func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        snapshot.Client.Set(key, result);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    snapshot.Client.Set(key, result);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<string> GetAndSetAsync(string key, Func<Task<string>> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string result = await snapshot.Client.GetAsync(key);

        if (result is _nullValue)
        {
            return null;
        }

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    (bool Found, string Value) cachedValue = await TryGetCachedValueAsync(snapshot.Client, key, result)
                        .ConfigureAwait(false);
                    if (cachedValue.Found)
                    {
                        return cachedValue.Value;
                    }

                    result = await func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        await snapshot.Client.SetAsync(key, result);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = await func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    await snapshot.Client.SetAsync(key, result);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public T GetAndSet<T>(string key, Func<T> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string value = snapshot.Client.Get(key);

        if (value is _nullValue)
        {
            return default;
        }

        T result = snapshot.Client.Get<T>(key);

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    if (TryGetCachedValue(snapshot.Client, key, out result))
                    {
                        return result;
                    }

                    result = func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        snapshot.Client.Set(key, result);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    snapshot.Client.Set(key, result);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<T> GetAndSetAsync<T>(string key, Func<Task<T>> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string value = await snapshot.Client.GetAsync(key);

        if (value is _nullValue)
        {
            return default;
        }

        T result = await snapshot.Client.GetAsync<T>(key);

        if (result is _nullValue)
        {
            return default;
        }

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    (bool Found, T Value) cachedValue = await TryGetCachedValueAsync(snapshot.Client, key, result)
                        .ConfigureAwait(false);
                    if (cachedValue.Found)
                    {
                        return cachedValue.Value;
                    }

                    result = await func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        await snapshot.Client.SetAsync(key, result);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = await func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    await snapshot.Client.SetAsync(key, result);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public string GetAndSet(string key, int expireSeconds, Func<string> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string result = snapshot.Client.Get(key);

        if (result is _nullValue)
        {
            return null;
        }

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    if (TryGetCachedValue(snapshot.Client, key, out result))
                    {
                        return result;
                    }

                    result = func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        snapshot.Client.Set(key, result, expireSeconds);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    snapshot.Client.Set(key, result, expireSeconds);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<string> GetAndSetAsync(string key, int expireSeconds, Func<Task<string>> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string result = await snapshot.Client.GetAsync(key);

        if (result is _nullValue)
        {
            return null;
        }

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    (bool Found, string Value) cachedValue = await TryGetCachedValueAsync(snapshot.Client, key, result)
                        .ConfigureAwait(false);
                    if (cachedValue.Found)
                    {
                        return cachedValue.Value;
                    }

                    result = await func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        await snapshot.Client.SetAsync(key, result, expireSeconds);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = await func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    await snapshot.Client.SetAsync(key, result, expireSeconds);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public T GetAndSet<T>(string key, int expireSeconds, Func<T> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string value = snapshot.Client.Get(key);

        if (value is _nullValue)
        {
            return default;
        }

        T result = snapshot.Client.Get<T>(key);

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    if (TryGetCachedValue(snapshot.Client, key, out result))
                    {
                        return result;
                    }

                    result = func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        snapshot.Client.Set(key, result, expireSeconds);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    snapshot.Client.Set(key, result, expireSeconds);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<T> GetAndSetAsync<T>(string key, int expireSeconds, Func<Task<T>> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string value = await snapshot.Client.GetAsync(key);

        if (value is _nullValue)
        {
            return default;
        }

        T result = await snapshot.Client.GetAsync<T>(key);

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    (bool Found, T Value) cachedValue = await TryGetCachedValueAsync(snapshot.Client, key, result)
                        .ConfigureAwait(false);
                    if (cachedValue.Found)
                    {
                        return cachedValue.Value;
                    }

                    result = await func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        await snapshot.Client.SetAsync(key, result, expireSeconds);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = await func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    await snapshot.Client.SetAsync(key, result, expireSeconds);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public string GetAndSet(string key, TimeSpan expireTimeSpan, Func<string> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string result = snapshot.Client.Get(key);

        if (result is _nullValue)
        {
            return null;
        }

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    if (TryGetCachedValue(snapshot.Client, key, out result))
                    {
                        return result;
                    }

                    result = func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        snapshot.Client.Set(key, result, expireTimeSpan);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    snapshot.Client.Set(key, result, expireTimeSpan);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<string> GetAndSetAsync(string key, TimeSpan expireTimeSpan, Func<Task<string>> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string result = await snapshot.Client.GetAsync(key);

        if (result is _nullValue)
        {
            return null;
        }

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    (bool Found, string Value) cachedValue = await TryGetCachedValueAsync(snapshot.Client, key, result)
                        .ConfigureAwait(false);
                    if (cachedValue.Found)
                    {
                        return cachedValue.Value;
                    }

                    result = await func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        await snapshot.Client.SetAsync(key, result, expireTimeSpan);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = await func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    await snapshot.Client.SetAsync(key, result, expireTimeSpan);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public T GetAndSet<T>(string key, TimeSpan expireTimeSpan, Func<T> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string value = snapshot.Client.Get(key);

        if (value is _nullValue)
        {
            return default;
        }

        T result = snapshot.Client.Get<T>(key);

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    if (TryGetCachedValue(snapshot.Client, key, out result))
                    {
                        return result;
                    }

                    result = func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        snapshot.Client.Set(key, result, expireTimeSpan);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    snapshot.Client.Set(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    snapshot.Client.Set(key, result, expireTimeSpan);
                }
            }
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<T> GetAndSetAsync<T>(string key, TimeSpan expireTimeSpan, Func<Task<T>> func)
    {
        CacheSnapshot snapshot = GetSnapshot();
        string value = await snapshot.Client.GetAsync(key);

        if (value is _nullValue)
        {
            return default;
        }

        T result = await snapshot.Client.GetAsync<T>(key);

        if (IsEmpty(result))
        {
            CSRedisClientLock acquired = snapshot.Client.Lock($"{key}_lock", 5);
            if (acquired != null)
            {
                try
                {
                    // 等待锁期间其他实例可能已经回填缓存，进入临界区后必须再次检查
                    (bool Found, T Value) cachedValue = await TryGetCachedValueAsync(snapshot.Client, key, result)
                        .ConfigureAwait(false);
                    if (cachedValue.Found)
                    {
                        return cachedValue.Value;
                    }

                    result = await func.Invoke();

                    // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                    if (IsEmpty(result))
                    {
                        await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                    }
                    else
                    {
                        await snapshot.Client.SetAsync(key, result, expireTimeSpan);
                    }
                }
                finally
                {
                    acquired.Unlock();
                }
            }
            else
            {
                result = await func.Invoke();

                // 缓存未命中时写入 _nullValue 空值哨兵并保留 2 小时，避免缓存穿透
                if (IsEmpty(result))
                {
                    await snapshot.Client.SetAsync(key, _nullValue, TimeSpan.FromHours(2));
                }
                else
                {
                    await snapshot.Client.SetAsync(key, result, expireTimeSpan);
                }
            }
        }

        return result;
    }

    /// <summary>
    /// 尝试读取缓存，并区分“缓存未命中”和“已缓存空值”。
    /// </summary>
    /// <param name="client">本次操作持有的客户端快照</param>
    /// <param name="key">缓存键</param>
    /// <param name="result">缓存值读取结果</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>成功取得目标值时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    private bool TryGetCachedValue<T>(CSRedisClient client, string key, out T result)
    {
        string rawValue = client.Get(key);
        if (string.Equals(rawValue, _nullValue, StringComparison.Ordinal))
        {
            result = default;
            return true;
        }

        result = typeof(T) == typeof(string) ? (T)(object)rawValue : client.Get<T>(key);
        return !IsEmpty(result);
    }

    /// <summary>
    /// 异步尝试读取缓存，并区分“缓存未命中”和“已缓存空值”
    /// </summary>
    /// <param name="client">本次操作持有的客户端快照</param>
    /// <param name="key">缓存键</param>
    /// <param name="_">扩展方法接收者；该值不参与输出</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>表示异步尝试读取缓存，并区分“缓存未命中”和“已缓存空值”的任务，任务结果为尝试读取缓存，并区分“缓存未命中”和“已缓存空值”</returns>
    private async Task<(bool Found, T Value)> TryGetCachedValueAsync<T>(CSRedisClient client, string key, T _)
    {
        string rawValue = await client.GetAsync(key).ConfigureAwait(false);
        if (string.Equals(rawValue, _nullValue, StringComparison.Ordinal))
        {
            return (true, default);
        }

        T result = typeof(T) == typeof(string) ? (T)(object)rawValue : await client.GetAsync<T>(key).ConfigureAwait(false);
        return (!IsEmpty(result), result);
    }

    /// <summary>
    /// 检查对象或集合是否为 <see langword="null"/>、空字符串或空集合
    /// </summary>
    /// <param name="value">要检查的值</param>
    /// <typeparam name="T">要检查是否为空的值类型</typeparam>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    private static bool IsEmpty<T>(T value)
    {
        if (value == null)
        {
            return true;
        }

        if (value is string text)
        {
            return string.IsNullOrWhiteSpace(text);
        }

        if (value is ICollection collection)
        {
            return collection.Count == 0;
        }

        if (value is IEnumerable enumerable)
        {
            IEnumerator enumerator = enumerable.GetEnumerator();
            try
            {
                return !enumerator.MoveNext();
            }
            finally
            {
                (enumerator as IDisposable)?.Dispose();
            }
        }

        return false;
    }
}
