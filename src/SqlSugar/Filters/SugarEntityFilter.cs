// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using SqlSugar;
using Yitter.IdGenerator;

namespace Fast.SqlSugar;

/// <summary>
/// Sugar 实体过滤器
/// </summary>
[SuppressSniffer]
public static class SugarEntityFilter
{
    /// <summary>
    /// 加载 Sugar AOP
    /// </summary>
    /// <param name="isDevelopment">当前环境是否为开发环境</param>
    /// <param name="_db">当前仓储使用的 SqlSugar 客户端</param>
    /// <param name="sugarSqlExecMaxSeconds">SQL 执行耗时告警阈值，单位为秒</param>
    /// <param name="diffLog">是否记录数据变更前后的差异</param>
    /// <param name="disableAop">是否禁用 SqlSugar AOP 回调</param>
    /// <param name="sqlSugarEntityHandler">实体保存前后的扩展处理器</param>
    public static void LoadSugarAop(bool isDevelopment, ISqlSugarClient _db, int? sugarSqlExecMaxSeconds = null,
        bool diffLog = false, bool disableAop = true, ISqlSugarEntityHandler sqlSugarEntityHandler = null)
    {
        sugarSqlExecMaxSeconds ??= SqlSugarContext.ConnectionSettings?.SugarSqlExecMaxSeconds!.Value;
        _db.Aop.OnLogExecuted = (rawSql, pars) =>
        {
            if (isDevelopment)
            {
                string handleSql = UtilMethods.GetSqlString(_db.CurrentConnectionConfig.DbType, rawSql, pars);

                MAppContext.ConsoleWrite(console =>
                {
                    console.BackgroundColor = ConsoleColor.Black;
                    console.ForegroundColor = ConsoleColor.DarkGray;
                    console.Write("fsql");
                    console.ResetColor();
                    console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                    console.BackgroundColor = ConsoleColor.Black;
                    console.ForegroundColor = ConsoleColor.DarkGray;
                    console.WriteLine($"      Time: {_db.Ado.SqlExecutionTime}");
                    console.WriteLine($"      {handleSql}");
                });
            }

            if (!disableAop && sqlSugarEntityHandler != null)
            {
                string handleSql = UtilMethods.GetSqlString(_db.CurrentConnectionConfig.DbType, rawSql, pars);

                // 将已执行的 SQL 及耗时交给自定义处理器
                try
                {
                    sqlSugarEntityHandler.ExecuteAsync(rawSql, pars, _db.Ado.SqlExecutionTime, handleSql)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception ex)
                {
                    MAppContext.ConsoleWrite(console =>
                    {
                        console.BackgroundColor = ConsoleColor.DarkRed;
                        console.ForegroundColor = ConsoleColor.Black;
                        console.Write("fsql");
                        console.ResetColor();
                        console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                        console.BackgroundColor = ConsoleColor.DarkRed;
                        console.ForegroundColor = ConsoleColor.Black;
                        console.WriteLine("      Exec [Fast.SqlSugar.ISqlSugarEntityHandler].[ExecuteAsync] method error.");
                        console.WriteLine(ex);
                    });
                }
            }

            // 仅在 SQL 耗时超过配置阈值时触发超时处理
            if (_db.Ado.SqlExecutionTime.TotalSeconds > sugarSqlExecMaxSeconds)
            {
                string handleSql = UtilMethods.GetSqlString(_db.CurrentConnectionConfig.DbType, rawSql, pars);

                string fileName = _db.Ado.SqlStackTrace.FirstFileName;
                int fileLine = _db.Ado.SqlStackTrace.FirstLine;
                string firstMethodName = _db.Ado.SqlStackTrace.FirstMethodName;
                // 消息
                string message =
                    $"Sql执行时间超过 {sugarSqlExecMaxSeconds} 秒，建议优化。{Environment.NewLine}FileName：{fileName}{Environment.NewLine}FileLine：{fileLine}{Environment.NewLine}FirstMethodName：{firstMethodName}{Environment.NewLine}Sql：{handleSql}{Environment.NewLine}SqlExecutionTime：{_db.Ado.SqlExecutionTime}";

                // 控制台输出
                MAppContext.ConsoleWrite(console =>
                {
                    console.BackgroundColor = ConsoleColor.Black;
                    console.ForegroundColor = ConsoleColor.Yellow;
                    console.Write("fsql");
                    console.ResetColor();
                    console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                    console.BackgroundColor = ConsoleColor.Black;
                    console.ForegroundColor = ConsoleColor.Yellow;
                    console.WriteLine($"      {message}");
                });

                if (!disableAop && sqlSugarEntityHandler != null)
                {
                    // 将超时 SQL 和调用位置交给自定义处理器
                    try
                    {
                        sqlSugarEntityHandler.ExecuteTimeoutAsync(fileName, fileLine, firstMethodName, rawSql, pars,
                                _db.Ado.SqlExecutionTime, handleSql, message)
                            .GetAwaiter()
                            .GetResult();
                    }
                    catch (Exception ex)
                    {
                        MAppContext.ConsoleWrite(console =>
                        {
                            console.BackgroundColor = ConsoleColor.DarkRed;
                            console.ForegroundColor = ConsoleColor.Black;
                            console.Write("fsql");
                            console.ResetColor();
                            console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                            console.BackgroundColor = ConsoleColor.DarkRed;
                            console.ForegroundColor = ConsoleColor.Black;
                            console.WriteLine(
                                "      Exec [Fast.SqlSugar.ISqlSugarEntityHandler].[ExecuteTimeoutAsync] method error.");
                            console.WriteLine(ex);
                        });
                    }
                }
            }
        };

        // 判断是否启用差异日志
        if (diffLog)
        {
            _db.Aop.OnDiffLogEvent = diff =>
            {
                if (!disableAop && sqlSugarEntityHandler != null)
                {
                    // 差异日志
                    if ((diff.AfterData != null && diff.AfterData.Any()) || (diff.BeforeData != null && diff.BeforeData.Any()))
                    {
                        string handleSql =
                            UtilMethods.GetSqlString(_db.CurrentConnectionConfig.DbType, diff.Sql, diff.Parameters);

                        DiffLogTableInfo firstData = null;
                        if (diff.AfterData != null && diff.AfterData.Any())
                        {
                            firstData = diff.AfterData.First();
                        }
                        else if (diff.BeforeData != null && diff.BeforeData.Any())
                        {
                            firstData = diff.BeforeData.First();
                        }

                        string tableName = firstData?.TableName;
                        string tableDescription = firstData?.TableDescription;

                        // 将数据变更前后的差异交给自定义处理器
                        try
                        {
                            sqlSugarEntityHandler.ExecuteDiffLogAsync(diff.DiffType, tableName, tableDescription,
                                    diff.BusinessData, diff.BeforeData?.Select(sl => sl.Columns).ToList(),
                                    diff.AfterData?.Select(sl => sl.Columns).ToList(), diff.Sql, diff.Parameters, diff.Time,
                                    handleSql)
                                .GetAwaiter()
                                .GetResult();
                        }
                        catch (Exception ex)
                        {
                            MAppContext.ConsoleWrite(console =>
                            {
                                console.BackgroundColor = ConsoleColor.DarkRed;
                                console.ForegroundColor = ConsoleColor.Black;
                                console.Write("fsql");
                                console.ResetColor();
                                console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                                console.BackgroundColor = ConsoleColor.DarkRed;
                                console.ForegroundColor = ConsoleColor.Black;
                                console.WriteLine(
                                    "      Exec [Fast.SqlSugar.ISqlSugarEntityHandler].[ExecuteDiffLogAsync] method error.");
                                console.WriteLine(ex);
                            });
                        }
                    }
                }
            };
        }

        _db.Aop.OnError = exp =>
        {
            var param = (SugarParameter[])exp.Parametres;

            string handleSql = UtilMethods.GetSqlString(_db.CurrentConnectionConfig.DbType, exp.Sql, param);

            string fileName = _db.Ado.SqlStackTrace.FirstFileName;
            int fileLine = _db.Ado.SqlStackTrace.FirstLine;
            string firstMethodName = _db.Ado.SqlStackTrace.FirstMethodName;

            if (isDevelopment)
            {
                MAppContext.ConsoleWrite(console =>
                {
                    console.BackgroundColor = ConsoleColor.DarkRed;
                    console.ForegroundColor = ConsoleColor.Black;
                    console.Write("fsql");
                    console.ResetColor();
                    console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                    console.BackgroundColor = ConsoleColor.DarkRed;
                    console.ForegroundColor = ConsoleColor.Black;
                    console.WriteLine(
                        $"      Sql 执行异常{Environment.NewLine}FileName：{fileName}{Environment.NewLine}FileLine：{fileLine}{Environment.NewLine}FirstMethodName：{firstMethodName}{Environment.NewLine}Sql：{handleSql}");
                });
            }

            if (!disableAop && sqlSugarEntityHandler != null)
            {
                // 将 SQL 异常和调用位置交给自定义处理器
                try
                {
                    sqlSugarEntityHandler.ExecuteErrorAsync(fileName, fileLine, firstMethodName, exp.Sql, param, handleSql, exp)
                        .GetAwaiter()
                        .GetResult();
                }
                catch (Exception ex)
                {
                    MAppContext.ConsoleWrite(console =>
                    {
                        console.BackgroundColor = ConsoleColor.DarkRed;
                        console.ForegroundColor = ConsoleColor.Black;
                        console.Write("fsql");
                        console.ResetColor();
                        console.WriteLine($": {DateTime.Now:yyyy-MM-dd HH:mm:ss.fffffff zzz dddd}");
                        console.BackgroundColor = ConsoleColor.DarkRed;
                        console.ForegroundColor = ConsoleColor.Black;
                        console.WriteLine("      Exec [Fast.SqlSugar.ISqlSugarEntityHandler].[ExecuteErrorAsync] method error.");
                        console.WriteLine(ex);
                    });
                }
            }
        };

        // 在 SqlSugar 写入实体字段前统一填充框架约定字段
        _db.Aop.DataExecuting = (_, entityInfo) =>
        {
            switch (entityInfo.OperationType)
            {
                // 新增操作
                case DataFilterType.InsertByObject:
                    // 主键，这里一条记录只会匹配一次
                    if (!entityInfo.EntityColumnInfo.IsIdentity && entityInfo.EntityColumnInfo.IsPrimarykey)
                    {
                        // 赋值雪花Id（long）
                        if (entityInfo.EntityColumnInfo.PropertyInfo.PropertyType == typeof(long))
                        {
                            if (SqlSugarContext.EntityValueCheck([null, 0L], entityInfo))
                            {
                                entityInfo.SetValue(YitIdHelper.NextId());
                            }
                        }
                        // 赋值 Guid
                        else if (entityInfo.EntityColumnInfo.PropertyInfo.PropertyType == typeof(Guid))
                        {
                            if (SqlSugarContext.EntityValueCheck([null, Guid.Empty], entityInfo))
                            {
                                entityInfo.SetValue(Guid.NewGuid());
                            }
                        }
                    }

                    // 更新版本控制字段
                    SqlSugarContext.SetEntityValue(nameof(IUpdateVersion.RowVersion), [null, 0L], 1L, entityInfo);

                    // 创建时间
                    SqlSugarContext.SetEntityValue(nameof(IBaseEntity.CreatedTime), [null], DateTime.Now, entityInfo);

                    if (sqlSugarEntityHandler != null)
                    {
                        // 部门Id
                        SqlSugarContext.SetEntityValue(nameof(IBaseEntity.DepartmentId), [null, 0L],
                            sqlSugarEntityHandler.AssignDepartmentId(), entityInfo);

                        // 部门名称
                        SqlSugarContext.SetEntityValue(nameof(IBaseEntity.DepartmentName), [null, ""],
                            sqlSugarEntityHandler.AssignDepartmentName(), entityInfo);

                        // 创建者Id
                        SqlSugarContext.SetEntityValue(nameof(IBaseEntity.CreatedUserId), [null, 0L],
                            sqlSugarEntityHandler.AssignUserId(), entityInfo);

                        // 创建者名称
                        SqlSugarContext.SetEntityValue(nameof(IBaseEntity.CreatedUserName), [null, ""],
                            sqlSugarEntityHandler.AssignUserName(), entityInfo);

                        // 租户Id
                        SqlSugarContext.SetEntityValue(nameof(IBaseTEntity.TenantId), [null, 0L],
                            sqlSugarEntityHandler.AssignTenantId() ?? 0L, entityInfo);
                    }

                    break;
                // 更新操作
                case DataFilterType.UpdateByObject:
                    // 更新时间
                    SqlSugarContext.SetEntityValue(nameof(IBaseEntity.UpdatedTime), null, DateTime.Now, entityInfo);

                    if (sqlSugarEntityHandler != null)
                    {
                        // 更新者Id
                        SqlSugarContext.SetEntityValue(nameof(IBaseEntity.UpdatedUserId), null,
                            sqlSugarEntityHandler.AssignUserId(), entityInfo);

                        // 更新者名称
                        SqlSugarContext.SetEntityValue(nameof(IBaseEntity.UpdatedUserName), null,
                            sqlSugarEntityHandler.AssignUserName(), entityInfo);
                    }

                    break;
                case DataFilterType.DeleteByObject:
                    break;
            }
        };
    }

    /// <summary>
    /// 加载 Sugar 过滤器
    /// </summary>
    /// <param name="_db">当前仓储使用的 SqlSugar 客户端</param>
    /// <param name="sqlSugarEntityHandler">实体保存前后的扩展处理器</param>
    public static void LoadSugarFilter(ISqlSugarClient _db, ISqlSugarEntityHandler sqlSugarEntityHandler)
    {
        if (sqlSugarEntityHandler != null)
        {
            // 配置多租户全局过滤器
            if (!sqlSugarEntityHandler.IsSuperAdmin())
            {
                _db.QueryFilter.AddTableFilter<IBaseTEntity>(it => it.TenantId == (sqlSugarEntityHandler.AssignTenantId() ?? 0));
            }
        }

        // 配置软删除全局过滤器
        _db.QueryFilter.AddTableFilter<IDeletedEntity>(it => it.IsDeleted == false);
    }
}
