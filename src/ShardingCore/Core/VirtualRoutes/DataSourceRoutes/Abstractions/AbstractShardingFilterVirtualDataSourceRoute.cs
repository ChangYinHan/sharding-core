﻿using System.Collections.Generic;
using System.Linq;
using ShardingCore.Core.EntityMetadatas;
using ShardingCore.Core.PhysicTables;
using ShardingCore.Core.QueryRouteManagers;
using ShardingCore.Core.QueryRouteManagers.Abstractions;
using ShardingCore.Core.VirtualRoutes.TableRoutes.Abstractions;
using ShardingCore.Exceptions;
using ShardingCore.Extensions;

namespace ShardingCore.Core.VirtualRoutes.DataSourceRoutes.Abstractions
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: 2021/8/25 17:23:42
    * @Ver: 1.0
    * @Email: 326308290@qq.com
    */
    /// <summary>
    /// 过滤虚拟路由用于处理强制路由、提示路由、路由断言
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="TKey"></typeparam>
    public abstract class AbstractShardingFilterVirtualDataSourceRoute<T, TKey> : AbstractVirtualDataSourceRoute<T, TKey> where T : class
    {

        public  ShardingRouteContext CurrentShardingRouteContext =>
            ShardingContainer.GetService<IShardingRouteManager>().Current;
        /// <summary>
        /// 启用提示路由
        /// </summary>
         protected virtual bool EnableHintRoute => false;
        /// <summary>
        /// 启用断言路由
        /// </summary>
        protected virtual bool EnableAssertRoute => false;
        public override List<string> RouteWithPredicate(IQueryable queryable,bool isQuery)
        {
            var allDataSourceNames = GetAllDataSourceNames();
            if (!isQuery)
            {
                //后拦截器
                return AfterDataSourceFilter(allDataSourceNames, DoRouteWithPredicate(allDataSourceNames, queryable));
            }
            //强制路由不经过断言
            if (EnableHintRoute)
            {
                if (CurrentShardingRouteContext != null)
                {
                    if (CurrentShardingRouteContext.TryGetMustDataSource<T>(out HashSet<string> mustDataSources) && mustDataSources.IsNotEmpty())
                    {
                        var dataSources = allDataSourceNames.Where(o => mustDataSources.Contains(o)).ToList();
                        if (dataSources.IsEmpty()||dataSources.Count!=mustDataSources.Count)
                            throw new ShardingCoreException(
                                $" sharding data source route must error:[{EntityMetadata.EntityType.FullName}]-->[{string.Join(",",mustDataSources)}]");
                        return dataSources;
                    }

                    if (CurrentShardingRouteContext.TryGetHintDataSource<T>(out HashSet<string> hintDataSouces) && hintDataSouces.IsNotEmpty())
                    {
                        var dataSources = allDataSourceNames.Where(o => hintDataSouces.Contains(o)).ToList();
                        if (dataSources.IsEmpty()||dataSources.Count!=hintDataSouces.Count)
                            throw new ShardingCoreException(
                                $" sharding data source route hint error:[{EntityMetadata.EntityType.FullName}]-->[{string.Join(",",hintDataSouces)}]");
                        ProcessAssertRoutes(allDataSourceNames, dataSources);
                        return dataSources;
                    }
                }
            }


            var filterDataSources = DoRouteWithPredicate(allDataSourceNames, queryable);
            //后拦截器
            var resultDataSources = AfterDataSourceFilter(allDataSourceNames, filterDataSources);
            //最后处理断言
            ProcessAssertRoutes(allDataSourceNames, resultDataSources);
            return resultDataSources;
        }

        private void ProcessAssertRoutes(List<string> allDataSources,List<string> filterDataSources)
        {
            if (EnableAssertRoute)
            {
                if (CurrentShardingRouteContext != null && CurrentShardingRouteContext.TryGetAssertDataSource<T>(out ICollection<IDataSourceRouteAssert> routeAsserts) && routeAsserts.IsNotEmpty())
                {
                    foreach (var routeAssert in routeAsserts)
                    {
                        routeAssert.Assert(allDataSources, filterDataSources);
                    }
                }
            }
        }

        protected abstract List<string> DoRouteWithPredicate(List<string> allDataSourceNames, IQueryable queryable);


        /// <summary>
        /// 物理表过滤后
        /// </summary>
        /// <param name="allDataSourceNames">所有的物理表</param>
        /// <param name="filterDataSources">过滤后的物理表</param>
        /// <returns></returns>
        protected virtual List<string> AfterDataSourceFilter(List<string> allDataSourceNames, List<string> filterDataSources)
        {
            return filterDataSources;
        }
    }
}