// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System.Reflection;
using SqlSugar;

namespace Fast.SqlSugar;

/// <summary>
/// 为 <see cref="ISugarQueryable{T}"/> 提供 ISugarQueryable 分页扩展方法
/// </summary>
[SuppressSniffer]
public static class SqlSugarPageExtension
{
    /// <summary>
    /// 分页转换类型
    /// </summary>
    /// <param name="pagedResult">要写入数据的分页结果</param>
    /// <param name="selectExpression">用于分页转换类型的表达式</param>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <typeparam name="TResult">操作结果类型</typeparam>
    /// <returns>分页转换类型</returns>
    public static PagedResult<TResult> ToPagedData<TEntity, TResult>(this PagedResult<TEntity> pagedResult,
        Func<TEntity, TResult> selectExpression)
    {
        return new PagedResult<TResult>
        {
            PageIndex = pagedResult.PageIndex,
            PageSize = pagedResult.PageSize,
            Rows = pagedResult.Rows.Select(selectExpression).ToList(),
            TotalRows = pagedResult.TotalRows,
            TotalPage = pagedResult.TotalPage,
            HasNextPages = pagedResult.HasNextPages,
            HasPrevPages = pagedResult.HasPrevPages
        };
    }

    /// <summary>
    /// SqlSugar 分页扩展
    /// </summary>
    /// <remarks>
    /// 多表查询（LeftJoin/Join）场景需注意：分页统计依赖 MergeTable，建议在分页前进行结构收敛
    /// <code>
    /// .Select((t1, t2) => new { ... }).MergeTable()
    /// </code>
    /// 或
    /// <code>
    /// .SelectMergeTable((t1, t2) => new { ... })
    /// </code>
    /// 未使用 MergeTable 可能导致总数统计异常或结果不准确
    /// </remarks>
    /// <param name="queryable">要继续构建的查询对象</param>
    /// <param name="input">PagedInput 通用 SqlSugar 分页输入</param>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>查询记录与分页信息；禁用分页时恰好达到上限仍允许返回，超过上限抛出异常。</returns>
    public static PagedResult<TEntity> ToPagedList<TEntity>(this ISugarQueryable<TEntity> queryable, PagedInput input)
    {
        return queryable.SugarPaged(input).ToPagedList(input.PageIndex, input.PageSize, input.EnablePaged);
    }


    /// <summary>
    /// SqlSugar 分页扩展
    /// </summary>
    /// <remarks>
    /// 多表查询（LeftJoin/Join）场景需注意：分页统计依赖 MergeTable，建议在分页前进行结构收敛
    /// <code>
    /// .Select((t1, t2) => new { ... }).MergeTable()
    /// </code>
    /// 或
    /// <code>
    /// .SelectMergeTable((t1, t2) => new { ... })
    /// </code>
    /// 未使用 MergeTable 可能导致总数统计异常或结果不准确
    /// </remarks>
    /// <param name="queryable">要继续构建的查询对象</param>
    /// <param name="input">PagedInput 通用 SqlSugar 分页输入</param>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>包含查询记录与分页信息的任务；非分页查询超过配置上限时抛出异常。</returns>
    public static async Task<PagedResult<TEntity>> ToPagedListAsync<TEntity>(this ISugarQueryable<TEntity> queryable,
        PagedInput input)
    {
        return await queryable.SugarPaged(input).ToPagedListAsync(input.PageIndex, input.PageSize, input.EnablePaged);
    }

    /// <summary>
    /// SqlSugar 分页扩展
    /// </summary>
    /// <param name="queryable">要继续构建的查询对象</param>
    /// <param name="pageIndex">页码，从 1 开始</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="enablePaged">是否启用分页查询</param>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>查询记录与分页信息；禁用分页时恰好达到上限仍允许返回，超过上限抛出异常。</returns>
    public static PagedResult<TEntity> ToPagedList<TEntity>(this ISugarQueryable<TEntity> queryable, int pageIndex,
        int pageSize = 20, bool enablePaged = true)
    {
        if (enablePaged)
        {
            int totalRows = 0;
            List<TEntity> rows = queryable.ToPageList(pageIndex, pageSize, ref totalRows);
            int totalPage = (int)Math.Ceiling(totalRows / (double)pageSize);

            return new PagedResult<TEntity>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Rows = rows,
                TotalRows = totalRows,
                TotalPage = totalPage,
                HasNextPages = pageIndex < totalPage,
                HasPrevPages = pageIndex - 1 > 0
            };
        }
        else
        {
            int limit = SqlSugarContext.MaxNotPageSize;
            if (limit < 1 || limit == int.MaxValue)
            {
                throw new InvalidOperationException("MaxNotPageSize 必须是 1 至 int.MaxValue - 1 的整数。");
            }

            var rows = queryable.Take(limit + 1).ToList();
            if (rows.Count > limit)
            {
                throw new SqlSugarException($"当前查询数据量超过 {limit} 条，请使用分页查询或缩小查询范围。");
            }

            return new PagedResult<TEntity>
            {
                PageIndex = 1,
                PageSize = rows.Count,
                Rows = rows,
                TotalRows = rows.Count,
                TotalPage = 1,
                HasNextPages = false,
                HasPrevPages = false
            };
        }
    }

    /// <summary>
    /// SqlSugar 分页扩展
    /// </summary>
    /// <param name="queryable">要继续构建的查询对象</param>
    /// <param name="pageIndex">页码，从 1 开始</param>
    /// <param name="pageSize">每页记录数</param>
    /// <param name="enablePaged">是否启用分页查询</param>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>包含查询记录与分页信息的任务；非分页查询超过配置上限时抛出异常。</returns>
    public static async Task<PagedResult<TEntity>> ToPagedListAsync<TEntity>(this ISugarQueryable<TEntity> queryable,
        int pageIndex, int pageSize = 20, bool enablePaged = true)
    {
        if (enablePaged)
        {
            RefAsync<int> totalRows = 0;
            List<TEntity> rows = await queryable.ToPageListAsync(pageIndex, pageSize, totalRows);
            int totalPage = (int)Math.Ceiling(totalRows.Value / (double)pageSize);

            return new PagedResult<TEntity>
            {
                PageIndex = pageIndex,
                PageSize = pageSize,
                Rows = rows,
                TotalRows = totalRows.Value,
                TotalPage = totalPage,
                HasNextPages = pageIndex < totalPage,
                HasPrevPages = pageIndex - 1 > 0
            };
        }
        else
        {
            int limit = SqlSugarContext.MaxNotPageSize;
            if (limit < 1 || limit == int.MaxValue)
            {
                throw new InvalidOperationException("MaxNotPageSize 必须是 1 至 int.MaxValue - 1 的整数。");
            }

            List<TEntity> rows = await queryable.Take(limit + 1).ToListAsync();
            if (rows.Count > limit)
            {
                throw new SqlSugarException($"当前查询数据量超过 {limit} 条，请使用分页查询或缩小查询范围。");
            }

            return new PagedResult<TEntity>
            {
                PageIndex = 1,
                PageSize = rows.Count,
                Rows = rows,
                TotalRows = rows.Count,
                TotalPage = 1,
                HasNextPages = false,
                HasPrevPages = false
            };
        }
    }

    /// <summary>
    /// Sugar 统一分页处理
    /// </summary>
    /// <remarks>支持多库</remarks>
    /// <param name="queryable">要继续构建的查询对象</param>
    /// <param name="input">方法的输入数据</param>
    /// <typeparam name="TEntity">实体类型</typeparam>
    /// <returns>Sugar 统一分页处理</returns>
    public static ISugarQueryable<TEntity> SugarPaged<TEntity>(this ISugarQueryable<TEntity> queryable, PagedInput input)
    {
        // 这里必须要判断，字段是否存在于 TEntity 中，不然会执行到 Db 层面的报错
        Type type = typeof(TEntity);

        var properties = type.GetProperties()
            .Select(sl => new
            {
                propertyInfo = sl,
                sugarColumn = sl.GetCustomAttribute<SugarColumn>(true),
                navigate = sl.GetCustomAttribute<Navigate>(true),
                sugarSearchValueAttribute = sl.GetCustomAttribute<SugarSearchValueAttribute>(),
                sugarSearchTimeAttribute = sl.GetCustomAttribute<SugarSearchTimeAttribute>()
            })
            .ToList();

        var whereList = new List<IConditionalModel>();

        // 搜索列
        if (!string.IsNullOrEmpty(input.SearchValue))
        {
            var searchList = new List<KeyValuePair<WhereType, ConditionalModel>>();

            int index = 0;

            foreach (var item in properties.Where(wh => wh.sugarSearchValueAttribute != null).ToList())
            {
                WhereType whereType = WhereType.Or;
                if (index == 0)
                {
                    whereType = WhereType.And;
                }

                searchList.Add(new KeyValuePair<WhereType, ConditionalModel>(whereType,
                    new ConditionalModel
                    {
                        FieldName = string.IsNullOrEmpty(item.sugarColumn?.ColumnName)
                            ? item.propertyInfo.Name
                            : item.sugarColumn.ColumnName,
                        ConditionalType = ConditionalType.Like,
                        FieldValue = input.SearchValue
                    }));

                index++;
            }

            if (searchList.Any())
            {
                whereList.Add(new ConditionalCollections {ConditionalList = searchList});
            }
        }

        // 搜素时间列
        if (input.SearchTimeList is {Count: > 0})
        {
            DateTime? time1 = null;
            DateTime? time2 = null;

            switch (input.SearchTimeList.Count)
            {
                case >= 2:
                    time1 = input.SearchTimeList[0];
                    time2 = input.SearchTimeList[1];
                    break;
                case 1:
                    time1 = input.SearchTimeList[0];
                    break;
            }

            // 如果两个时间都存在，则使用范围搜索，如果只存在一个，则使用 >= 或者 <=
            var searchList = new List<KeyValuePair<WhereType, ConditionalModel>>();

            foreach (var item in properties.Where(wh => wh.sugarSearchTimeAttribute != null).ToList())
            {
                if (time1 != null && time2 != null)
                {
                    // 范围搜索
                    searchList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And,
                        new ConditionalModel
                        {
                            FieldName = string.IsNullOrEmpty(item.sugarColumn?.ColumnName)
                                ? item.propertyInfo.Name
                                : item.sugarColumn.ColumnName,
                            ConditionalType = ConditionalType.GreaterThanOrEqual,
                            FieldValue = time1.ToString(),
                            CSharpTypeName = nameof(DateTime)
                        }));
                    searchList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And,
                        new ConditionalModel
                        {
                            FieldName = string.IsNullOrEmpty(item.sugarColumn?.ColumnName)
                                ? item.propertyInfo.Name
                                : item.sugarColumn.ColumnName,
                            ConditionalType = ConditionalType.LessThanOrEqual,
                            FieldValue = time2.ToString(),
                            CSharpTypeName = nameof(DateTime)
                        }));
                }
                else if (time1 != null)
                {
                    // 大于等于开始时间
                    searchList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And,
                        new ConditionalModel
                        {
                            FieldName = string.IsNullOrEmpty(item.sugarColumn?.ColumnName)
                                ? item.propertyInfo.Name
                                : item.sugarColumn.ColumnName,
                            ConditionalType = ConditionalType.GreaterThanOrEqual,
                            FieldValue = time1.ToString(),
                            CSharpTypeName = nameof(DateTime)
                        }));
                }
                else if (time2 != null)
                {
                    // 小于等于结束时间
                    searchList.Add(new KeyValuePair<WhereType, ConditionalModel>(WhereType.And,
                        new ConditionalModel
                        {
                            FieldName = string.IsNullOrEmpty(item.sugarColumn?.ColumnName)
                                ? item.propertyInfo.Name
                                : item.sugarColumn.ColumnName,
                            ConditionalType = ConditionalType.LessThanOrEqual,
                            FieldValue = time2.ToString(),
                            CSharpTypeName = nameof(DateTime)
                        }));
                }
            }

            if (searchList.Any())
            {
                whereList.Add(new ConditionalCollections {ConditionalList = searchList});
            }
        }

        foreach (PagedSearchInput searchInput in input.SearchList)
        {
            var item = properties.FirstOrDefault(f =>
                f.propertyInfo.Name.Equals(searchInput.EnField, StringComparison.InvariantCultureIgnoreCase));

            if (item == null)
            {
                throw new SqlSugarException($"搜索字段 [{searchInput.ChField}] 不存在于类型 [{type.Name}] 中！");
            }

            if (item.sugarColumn?.IsIgnore == true)
            {
                // 如果存在特性，且 IsIgnore = true，则代表不是 Db 列，不能进行搜索
                throw new SqlSugarException($"类型 [{type.Name}] 中的搜索字段 [{searchInput.ChField}] 不存在于对应的Db中！");
            }

            if (item.navigate != null)
            {
                // 如果存在特性，则代表是一个导航属性，不能进行搜索
                throw new SqlSugarException($"类型 [{type.Name}] 中的搜索字段 [{searchInput.ChField}] 是一个导航属性！");
            }

            ConditionalType conditionalType = searchInput.Type switch
            {
                PagedSearchTypeEnum.Equal => ConditionalType.Equal,
                PagedSearchTypeEnum.NotEqual => ConditionalType.NoEqual,
                PagedSearchTypeEnum.GreaterThan => ConditionalType.GreaterThan,
                PagedSearchTypeEnum.GreaterThanOrEqual => ConditionalType.GreaterThanOrEqual,
                PagedSearchTypeEnum.LessThan => ConditionalType.LessThan,
                PagedSearchTypeEnum.LessThanOrEqual => ConditionalType.LessThanOrEqual,
                PagedSearchTypeEnum.Include => ConditionalType.In,
                PagedSearchTypeEnum.NotInclude => ConditionalType.NotIn,
                _ => ConditionalType.Like
            };

            whereList.Add(new ConditionalModel
            {
                FieldName = string.IsNullOrEmpty(item.sugarColumn?.ColumnName)
                    ? item.propertyInfo.Name
                    : item.sugarColumn.ColumnName,
                FieldValue = searchInput.Value,
                ConditionalType = conditionalType
            });
        }

        if (whereList.Any())
        {
            queryable = queryable.Where(whereList);
        }

        var orderList = new List<OrderByModel>();

        foreach (PagedSortInput sortInput in input.SortList)
        {
            var item = properties.FirstOrDefault(f =>
                f.propertyInfo.Name.Equals(sortInput.EnField, StringComparison.InvariantCultureIgnoreCase));

            if (item == null)
            {
                throw new SqlSugarException($"排序字段 [{sortInput.ChField}] 不存在于类型 [{type.Name}] 中！");
            }

            if (item.sugarColumn?.IsIgnore == true)
            {
                // 如果存在特性，且 IsIgnore = true，则代表不是 Db 列，不能进行排序
                throw new SqlSugarException($"类型 [{type.Name}] 中的排序字段 [{sortInput.ChField}] 不存在于对应的Db中！");
            }

            if (item.navigate != null)
            {
                // 如果存在特性，则代表是一个导航属性，不能进行排序
                throw new SqlSugarException($"类型 [{type.Name}] 中的排序字段 [{sortInput.ChField}] 是一个导航属性！");
            }

            orderList.Add(new OrderByModel
            {
                FieldName = string.IsNullOrEmpty(item.sugarColumn?.ColumnName)
                    ? item.propertyInfo.Name
                    : item.sugarColumn.ColumnName,
                OrderByType = sortInput.IsDescending ? OrderByType.Desc : OrderByType.Asc
            });
        }

        if (orderList.Any())
        {
            queryable = queryable.OrderBy(orderList);
        }

        return queryable;
    }
}
