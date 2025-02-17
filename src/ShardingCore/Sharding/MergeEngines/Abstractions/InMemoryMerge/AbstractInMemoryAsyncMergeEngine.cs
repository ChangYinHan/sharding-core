﻿using ShardingCore.Sharding.MergeEngines.ParallelExecutors;
using ShardingCore.Sharding.StreamMergeEngines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ShardingCore.Helpers;

namespace ShardingCore.Sharding.MergeEngines.Abstractions.InMemoryMerge
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: 2021/8/17 14:22:10
    * @Ver: 1.0
    * @Email: 326308290@qq.com
    */
    internal abstract class AbstractInMemoryAsyncMergeEngine<TEntity> : AbstractBaseMergeEngine<TEntity>, IInMemoryAsyncMergeEngine<TEntity>
    {
        private readonly StreamMergeContext<TEntity> _mergeContext;

        protected AbstractInMemoryAsyncMergeEngine(StreamMergeContext<TEntity> streamMergeContext)
        {
            _mergeContext = streamMergeContext;
        }

        public async Task<List<RouteQueryResult<TResult>>> ExecuteAsync<TResult>(Func<IQueryable, Task<TResult>> efQuery, CancellationToken cancellationToken = new CancellationToken())
        {
            var routeQueryResults = _mergeContext.PreperExecute(() => new List<RouteQueryResult<TResult>>(0));
            if (routeQueryResults != null)
                return routeQueryResults;
            var defaultSqlRouteUnits = GetDefaultSqlRouteUnits();
            var inMemoryParallelExecutor = new InMemoryParallelExecutor<TEntity,TResult>(_mergeContext,efQuery);
            var waitExecuteQueue = GetDataSourceGroupAndExecutorGroup<RouteQueryResult<TResult>>(true, defaultSqlRouteUnits, inMemoryParallelExecutor).ToArray();

            return (await TaskHelper.WhenAllFastFail(waitExecuteQueue)).SelectMany(o => o).ToList();
        }



        protected override StreamMergeContext<TEntity> GetStreamMergeContext()
        {
            return _mergeContext;
        }
    }
}