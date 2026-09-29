// Copyright © 2018-Present 小方
// SPDX-License-Identifier: Apache-2.0
// 
// 本文件依据 Apache License 2.0 授权，完整条款见仓库根目录 LICENSE。
// 本软件按“原样”提供，相关免责声明及责任限制以许可证及适用法律为准。
// 版权来源、合法使用与二次开发责任说明见仓库根目录 README.md。

using System;
using System.Collections.Generic;
using System.Linq;

namespace Fast.IaaS;

/// <summary>
/// 递归工具类，用于遍历有父子关系的节点，例如菜单树，字典树等等
/// </summary>
/// <typeparam name="TEntity">实体类型</typeparam>
/// <typeparam name="TProperty">属性值类型</typeparam>
public class TreeBuildUtil<TEntity, TProperty> where TEntity : ITreeNode<TProperty>
    where TProperty : struct, IComparable, IConvertible, IFormattable
{
    /// <summary>
    /// 顶级节点的父节点Id（默认值为 0）
    /// </summary>
    private TProperty _rootParentId;

    /// <summary>
    /// 设置构建树时使用的根节点父级标识
    /// </summary>
    /// <param name="rootParentId">顶级节点应匹配的父级标识；默认为 default</param>
    public void SetRootParentId(TProperty rootParentId)
    {
        _rootParentId = rootParentId;
    }

    /// <summary>
    /// 构造树节点
    /// </summary>
    /// <param name="nodes">要构建为树的节点集合</param>
    /// <returns>构造树节点集合</returns>
    public List<TEntity> Build(List<TEntity> nodes)
    {
        if (nodes == null || nodes.Count == 0)
        {
            return [];
        }

        // 当前集合中的全部节点Id
        var nodeIds = nodes.Select(sl => sl.GetId()).ToHashSet();

        // 按父节点Id建立索引，避免递归过程中重复遍历全部节点
        ILookup<TProperty, TEntity> nodeLookup = nodes.ToLookup(node => node.GetPid());

        /*
         * 根节点满足以下任一条件：
         * 1.ParentId 等于指定的根节点父级标识
         * 2.ParentId 对应的父节点不在当前集合中
         */
        var result = nodes.Where(wh => wh.GetPid().Equals(_rootParentId) || !nodeIds.Contains(wh.GetPid()))
            .OrderBy(ob => ob.GetSort())
            .ToList();
        result.ForEach(u => BuildChildNodes(nodeLookup, u));
        return result;
    }

    /// <summary>
    /// 构造子节点集合
    /// </summary>
    /// <param name="nodeLookup">按父节点Id分组的节点集合</param>
    /// <param name="node">当前正在挂接子节点的树节点</param>
    private void BuildChildNodes(ILookup<TProperty, TEntity> nodeLookup, TEntity node)
    {
        var nodeSubList = nodeLookup[node.GetId()].OrderBy(ob => ob.GetSort()).ToList();
        nodeSubList.ForEach(u => BuildChildNodes(nodeLookup, u));
        node.SetChildren(nodeSubList);
    }
}
