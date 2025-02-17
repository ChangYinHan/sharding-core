﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using ShardingCore.Core;
using ShardingCore.Exceptions;
using ShardingCore.Extensions;
using ShardingCore.Extensions.InternalExtensions;
using ShardingCore.Helpers;
using ShardingCore.Sharding.Abstractions;
using ShardingCore.Sharding.Abstractions.ParallelExecutors;
using ShardingCore.Sharding.Enumerators;
using ShardingCore.Sharding.Enumerators.StreamMergeAsync;
using ShardingCore.Sharding.MergeEngines.Abstractions;
using ShardingCore.Sharding.MergeEngines.Abstractions.StreamMerge;
using ShardingCore.Sharding.MergeEngines.Common;
using ShardingCore.Sharding.MergeEngines.EnumeratorStreamMergeEngines;
using ShardingCore.Sharding.MergeEngines.EnumeratorStreamMergeEngines.StreamMergeCombines;
using ShardingCore.Sharding.MergeEngines.ParallelControls;
using ShardingCore.Sharding.MergeEngines.ParallelControls.Enumerators;
using ShardingCore.Sharding.MergeEngines.ParallelExecutors;
using ShardingCore.Sharding.PaginationConfigurations;
using ShardingCore.Sharding.StreamMergeEngines.EnumeratorStreamMergeEngines.Base;

namespace ShardingCore.Sharding.StreamMergeEngines.EnumeratorStreamMergeEngines.EnumeratorAsync
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: 2021/9/2 16:29:06
    * @Ver: 1.0
    * @Email: 326308290@qq.com
    */
    internal class SequenceEnumeratorAsyncStreamMergeEngine<TShardingDbContext, TEntity> : AbstractEnumeratorStreamMergeEngine<TEntity>
        where TShardingDbContext : DbContext, IShardingDbContext
    {
        private readonly PaginationSequenceConfig _dataSourceSequenceMatchOrderConfig;
        private readonly PaginationSequenceConfig _tableSequenceMatchOrderConfig;
        private readonly ICollection<RouteQueryResult<long>> _routeQueryResults;
        private readonly bool _isAsc;
        public SequenceEnumeratorAsyncStreamMergeEngine(StreamMergeContext<TEntity> streamMergeContext, PaginationSequenceConfig dataSourceSequenceMatchOrderConfig, PaginationSequenceConfig tableSequenceMatchOrderConfig, ICollection<RouteQueryResult<long>> routeQueryResults, bool isAsc) : base(streamMergeContext, new SequenceStreamMergeCombine<TEntity>())
        {
            _dataSourceSequenceMatchOrderConfig = dataSourceSequenceMatchOrderConfig;
            _tableSequenceMatchOrderConfig = tableSequenceMatchOrderConfig;
            _routeQueryResults = routeQueryResults;
            _isAsc = isAsc;
        }

        public override IStreamMergeAsyncEnumerator<TEntity>[] GetRouteQueryStreamMergeAsyncEnumerators(bool async, CancellationToken cancellationToken = new CancellationToken())
        {
            cancellationToken.ThrowIfCancellationRequested();
            var skip = StreamMergeContext.Skip.GetValueOrDefault();
            if (skip < 0)
                throw new ShardingCoreException("skip must ge 0");

            var take = StreamMergeContext.Take;
            if (take.HasValue && take.Value <= 0)
                throw new ShardingCoreException("take must gt 0");
            //分库是主要排序
            var dataSourceOrderMain = _dataSourceSequenceMatchOrderConfig != null;
            var sortRouteResults = _routeQueryResults.Select(o => new
            {
                DataSourceName = o.DataSourceName,
                Tail = o.TableRouteResult.ReplaceTables.First().Tail,
                RouteQueryResult = o
            });
            if (dataSourceOrderMain)
            {
                //是否有两级排序
                var useThenBy = _tableSequenceMatchOrderConfig != null;
                if (_isAsc)
                {
                    sortRouteResults = sortRouteResults.OrderBy(o => o.DataSourceName,
                        _dataSourceSequenceMatchOrderConfig.RouteComparer).ThenByIf(o => o.Tail, useThenBy, _tableSequenceMatchOrderConfig.RouteComparer);
                }
                else
                {
                    sortRouteResults = sortRouteResults.OrderByDescending(o => o.DataSourceName,
                        _dataSourceSequenceMatchOrderConfig.RouteComparer).ThenByDescendingIf(o => o.Tail, useThenBy, _tableSequenceMatchOrderConfig.RouteComparer);
                }
            }
            else
            {
                if (_isAsc)
                {
                    sortRouteResults =
                        sortRouteResults.OrderBy(o => o.Tail, _tableSequenceMatchOrderConfig.RouteComparer);
                }
                else
                {
                    sortRouteResults =
                        sortRouteResults.OrderByDescending(o => o.Tail, _tableSequenceMatchOrderConfig.RouteComparer);
                }
            }


            var sequenceResults = new SequencePaginationList(sortRouteResults.Select(o => o.RouteQueryResult)).Skip(skip).Take(take).ToList();
            var sqlSequenceRouteUnits = sequenceResults.Select(sequenceResult => new SqlSequenceRouteUnit(sequenceResult));
            var sequenceEnumeratorParallelExecutor = new SequenceEnumeratorParallelExecutor<TEntity>(GetStreamMergeContext(), async);
            var enumeratorTasks = GetDataSourceGroupAndExecutorGroup<IStreamMergeAsyncEnumerator<TEntity>>(async, sqlSequenceRouteUnits, sequenceEnumeratorParallelExecutor, cancellationToken);
            var streamEnumerators = TaskHelper.WhenAllFastFail(enumeratorTasks).WaitAndUnwrapException().SelectMany(o => o).ToArray();
            return streamEnumerators;
        }

        protected override IParallelExecuteControl<IStreamMergeAsyncEnumerator<TEntity>> CreateParallelExecuteControl0(IParallelExecutor<IStreamMergeAsyncEnumerator<TEntity>> executor)
        {
            return new SequenceEnumeratorParallelExecuteControl<TEntity>(GetStreamMergeContext(), executor, GetStreamMergeCombine());
        }
    }
}
