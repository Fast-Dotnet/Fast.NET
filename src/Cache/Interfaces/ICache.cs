// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using CSRedis;

namespace Fast.Cache;

/// <summary>
/// 默认缓存服务
/// </summary>
public interface ICache : ICache<DefaultCacheContextLocator>
{
}

/// <summary>
/// 缓存服务
/// </summary>
/// <typeparam name="CacheContextLocator">缓存上下文定位器类型，用于隔离不同缓存配置</typeparam>
public interface ICache<out CacheContextLocator> where CacheContextLocator : ICacheContextLocator, new()
{
    /// <summary>
    /// 前缀
    /// </summary>
    string Prefix { get; }

    /// <summary>
    /// 当前 CSRedis 缓存客户端
    /// </summary>
    /// <remarks>
    /// 客户端由缓存服务拥有，调用方不得单独释放。重载成功后新操作使用新代；已借用的旧代
    /// 保留到服务释放。频繁更换配置会保留多个连接池，不适合作为逐请求切换机制。
    /// 服务关闭后，不得继续访问任何一代客户端。
    /// </remarks>
    CSRedisClient Client { get; }

    /// <summary>
    /// 缓存上下文定位器
    /// </summary>
    CacheContextLocator ContextLocator { get; }

    /// <summary>
    /// 删除指定键的缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>实际删除的项数</returns>
    long Del(params string[] key);

    /// <summary>
    /// 异步删除指定键的缓存项
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>表示异步删除缓存的任务，任务结果为实际删除的项数</returns>
    Task<long> DelAsync(params string[] key);

    /// <summary>
    /// 根据匹配模式批量删除缓存
    /// </summary>
    /// <remarks>该操作会扫描并删除所有匹配键，键数量较多时可能造成 Redis 阻塞，请避免在高流量路径中调用</remarks>
    /// <param name="pattern">用于匹配目标项的模式</param>
    /// <returns>实际删除的项数</returns>
    long DelByPattern(string pattern);

    /// <summary>
    /// 异步根据匹配模式批量删除缓存
    /// </summary>
    /// <remarks>该操作会扫描并删除所有匹配键，键数量较多时可能造成 Redis 阻塞，请避免在高流量路径中调用</remarks>
    /// <param name="pattern">用于匹配目标项的模式</param>
    /// <returns>表示异步根据匹配模式批量删除缓存的任务，任务结果为实际删除的项数</returns>
    Task<long> DelByPatternAsync(string pattern);

    /// <summary>
    /// 判断指定缓存键是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool Exists(string key);

    /// <summary>
    /// 异步判断指定缓存键是否存在
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// 获取指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>获取到的缓存</returns>
    string Get(string key);

    /// <summary>
    /// 获取指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>获取到的缓存</returns>
    T Get<T>(string key);

    /// <summary>
    /// 异步获取指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <returns>表示异步获取缓存的任务，任务结果为获取到的缓存</returns>
    Task<string> GetAsync(string key);

    /// <summary>
    /// 异步获取指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>表示异步获取缓存的任务，任务结果为获取到的缓存</returns>
    Task<T> GetAsync<T>(string key);

    /// <summary>
    /// 写入指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">要写入缓存的值</param>
    /// <returns>缓存写入成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool Set(string key, object value);

    /// <summary>
    /// 写入指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">要写入缓存的值</param>
    /// <param name="expireSeconds">有效时长，单位为秒</param>
    /// <returns>缓存写入成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool Set(string key, object value, int expireSeconds);

    /// <summary>
    /// 写入指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">要写入缓存的值</param>
    /// <param name="expireTimeSpan">有效时长</param>
    /// <returns>缓存写入成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool Set(string key, object value, TimeSpan expireTimeSpan);

    /// <summary>
    /// 异步写入指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">要写入缓存的值</param>
    /// <returns>缓存写入成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    Task<bool> SetAsync(string key, object value);

    /// <summary>
    /// 异步写入指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">要写入缓存的值</param>
    /// <param name="expireSeconds">有效时长，单位为秒</param>
    /// <returns>缓存写入成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    Task<bool> SetAsync(string key, object value, int expireSeconds);

    /// <summary>
    /// 异步写入指定键的缓存值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="value">要写入缓存的值</param>
    /// <param name="expireTimeSpan">有效时长</param>
    /// <returns>缓存写入成功时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    Task<bool> SetAsync(string key, object value, TimeSpan expireTimeSpan);

    /// <summary>
    /// 获取当前 Redis 数据库中的所有缓存键
    /// </summary>
    /// <remarks>该操作需要扫描数据库中的键，键数量较多时开销较大，不应在高频请求中调用</remarks>
    /// <returns>获取到的当前 Redis 数据库中的所有缓存键集合</returns>
    List<string> GetAllKeys();

    /// <summary>
    /// 异步获取当前 Redis 数据库中的所有缓存键
    /// </summary>
    /// <remarks>该操作需要扫描数据库中的键，键数量较多时开销较大，不应在高频请求中调用</remarks>
    /// <returns>表示异步获取当前 Redis 数据库中的所有缓存键的任务，任务结果为获取到的当前 Redis 数据库中的所有缓存键集合</returns>
    Task<List<string>> GetAllKeysAsync();

    /// <summary>
    /// 获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <returns>缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    string GetAndSet(string key, Func<string> func);

    /// <summary>
    /// 获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    T GetAndSet<T>(string key, Func<T> func);

    /// <summary>
    /// 获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expireSeconds">有效时长，单位为秒</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <returns>缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    string GetAndSet(string key, int expireSeconds, Func<string> func);

    /// <summary>
    /// 获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expireSeconds">有效时长，单位为秒</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    T GetAndSet<T>(string key, int expireSeconds, Func<T> func);

    /// <summary>
    /// 获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expireTimeSpan">有效时长</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <returns>缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    string GetAndSet(string key, TimeSpan expireTimeSpan, Func<string> func);

    /// <summary>
    /// 获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expireTimeSpan">有效时长</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    T GetAndSet<T>(string key, TimeSpan expireTimeSpan, Func<T> func);

    /// <summary>
    /// 异步获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <returns>表示异步获取并且设置缓存的任务，任务结果为缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    Task<string> GetAndSetAsync(string key, Func<Task<string>> func);

    /// <summary>
    /// 异步获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>表示异步获取并且设置缓存的任务，任务结果为缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    Task<T> GetAndSetAsync<T>(string key, Func<Task<T>> func);

    /// <summary>
    /// 异步获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expireSeconds">有效时长，单位为秒</param>
    /// <param name="func">缓存不存在时用于异步生成值的委托</param>
    /// <returns>表示异步获取并且设置缓存的任务，任务结果为缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    Task<string> GetAndSetAsync(string key, int expireSeconds, Func<Task<string>> func);

    /// <summary>
    /// 异步获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expireSeconds">有效时长，单位为秒</param>
    /// <param name="func">缓存不存在时用于异步生成值的委托</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>表示异步获取并且设置缓存的任务，任务结果为缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    Task<T> GetAndSetAsync<T>(string key, int expireSeconds, Func<Task<T>> func);

    /// <summary>
    /// 异步获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expireTimeSpan">有效时长</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <returns>表示异步获取并且设置缓存的任务，任务结果为缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    Task<string> GetAndSetAsync(string key, TimeSpan expireTimeSpan, Func<Task<string>> func);

    /// <summary>
    /// 异步获取缓存值；缓存未命中时生成并写入新值
    /// </summary>
    /// <param name="key">缓存键</param>
    /// <param name="expireTimeSpan">有效时长</param>
    /// <param name="func">缓存未命中时用于生成并写入值的委托</param>
    /// <typeparam name="T">缓存值的类型</typeparam>
    /// <returns>表示异步获取并且设置缓存的任务，任务结果为缓存中已有的值，或缓存未命中时由值工厂生成并写入的新值</returns>
    Task<T> GetAndSetAsync<T>(string key, TimeSpan expireTimeSpan, Func<Task<T>> func);
}
