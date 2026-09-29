// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using Microsoft.AspNetCore.Http;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// 定义 SqlSugar 实体审计、租户和 SQL 事件处理契约
/// </summary>
/// <remarks>
/// <para>实现类型的构造函数不得注入 <see cref="ISqlSugarClient"/>，否则会形成循环依赖</para>
/// <para>实现类型的构造函数不得注入 <see cref="IHttpContextAccessor"/> 或 <see cref="HttpContext"/>
/// 否则处理器生命周期可能跨越请求边界并访问已释放的请求资源</para>
/// </remarks>
public interface ISqlSugarEntityHandler
{
    /// <summary>
    /// 异步获取指定实体使用的数据库连接配置
    /// </summary>
    /// <param name="sqlSugarClient">SqlSugar 客户端实例</param>
    /// <param name="sugarDbType">实体声明的数据库类型特性；未声明时为 <see langword="null"/></param>
    /// <param name="entityType">当前实体类型</param>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>实体使用的数据库连接配置；返回 <see langword="null"/> 时使用全局默认配置</returns>
    Task<ConnectionSettingsOptions> GetConnectionSettings<TEntity>(ISqlSugarClient sqlSugarClient,
        SugarDbTypeAttribute sugarDbType, Type entityType);

    /// <summary>
    /// 异步处理已执行的 SQL 语句
    /// </summary>
    /// <param name="rawSql">原始 SQL 文本</param>
    /// <param name="parameters">方法或查询使用的参数集合</param>
    /// <param name="executeTime">操作耗时</param>
    /// <param name="handlerSql">要检查或改写的 SQL 文本</param>
    /// <returns>表示 SQL 处理操作的任务</returns>
    Task ExecuteAsync(string rawSql, SugarParameter[] parameters, TimeSpan executeTime, string handlerSql);

    /// <summary>
    /// 异步处理执行超时的 SQL 语句
    /// </summary>
    /// <param name="fileName">源文件名称</param>
    /// <param name="fileLine">源代码行号</param>
    /// <param name="methodName">目标方法名称</param>
    /// <param name="rawSql">原始 SQL 文本</param>
    /// <param name="parameters">方法或查询使用的参数集合</param>
    /// <param name="executeTime">操作耗时</param>
    /// <param name="handlerSql">要检查或改写的 SQL 文本</param>
    /// <param name="message">要记录或返回的消息</param>
    /// <returns>表示 SQL 超时处理操作的任务</returns>
    Task ExecuteTimeoutAsync(string fileName, int fileLine, string methodName, string rawSql, SugarParameter[] parameters,
        TimeSpan executeTime, string handlerSql, string message);

    /// <summary>
    /// 异步记录 SQL 数据变更差异
    /// </summary>
    /// <param name="diffType">数据变更类型</param>
    /// <param name="tableName">数据库表名称</param>
    /// <param name="tableDescription">数据库表说明</param>
    /// <param name="businessData">响应中承载的业务数据</param>
    /// <param name="beforeColumnList">变更前的列值集合</param>
    /// <param name="afterColumnList">变更后的列值集合</param>
    /// <param name="rawSql">原始 SQL 文本</param>
    /// <param name="parameters">方法或查询使用的参数集合</param>
    /// <param name="executeTime">操作耗时</param>
    /// <param name="handlerSql">要检查或改写的 SQL 文本</param>
    /// <returns>表示数据变更差异记录操作的任务</returns>
    Task ExecuteDiffLogAsync(DiffType diffType, string tableName, string tableDescription, object businessData,
        List<List<DiffLogColumnInfo>> beforeColumnList, List<List<DiffLogColumnInfo>> afterColumnList, string rawSql,
        SugarParameter[] parameters, TimeSpan? executeTime, string handlerSql);

    /// <summary>
    /// 异步处理 SQL 执行异常
    /// </summary>
    /// <param name="fileName">源文件名称</param>
    /// <param name="fileLine">源代码行号</param>
    /// <param name="methodName">目标方法名称</param>
    /// <param name="rawSql">原始 SQL 文本</param>
    /// <param name="parameters">方法或查询使用的参数集合</param>
    /// <param name="handlerSql">要检查或改写的 SQL 文本</param>
    /// <param name="exception">SQL 执行期间引发的异常</param>
    /// <returns>表示 SQL 异常处理操作的任务</returns>
    Task ExecuteErrorAsync(string fileName, int fileLine, string methodName, string rawSql, SugarParameter[] parameters,
        string handlerSql, SqlSugarException exception);

    /// <summary>
    /// 是否为超级管理员
    /// </summary>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool IsSuperAdmin();

    /// <summary>
    /// 是否为管理员
    /// </summary>
    /// <returns>满足条件时返回 <see langword="true"/>；否则返回 <see langword="false"/></returns>
    bool IsAdmin();

    /// <summary>
    /// 获取新实体使用的租户Id
    /// </summary>
    /// <returns>租户Id；无法确定时返回 <see langword="null"/></returns>
    long? AssignTenantId();

    /// <summary>
    /// 获取新实体使用的部门Id
    /// </summary>
    /// <returns>部门Id；无法确定时返回 <see langword="null"/></returns>
    long? AssignDepartmentId();

    /// <summary>
    /// 获取新实体使用的部门名称
    /// </summary>
    /// <returns>部门名称；无法确定时返回 <see langword="null"/></returns>
    string AssignDepartmentName();

    /// <summary>
    /// 获取新实体使用的用户Id
    /// </summary>
    /// <returns>用户Id；无法确定时返回 <see langword="null"/></returns>
    long? AssignUserId();

    /// <summary>
    /// 获取新实体使用的用户名称
    /// </summary>
    /// <returns>用户名称；无法确定时返回 <see langword="null"/></returns>
    string AssignUserName();
}
