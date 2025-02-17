﻿using Microsoft.EntityFrameworkCore;
using ShardingCore.Extensions;
using ShardingCore.Sharding.Abstractions;
using ShardingCore.Sharding.MergeEngines.Abstractions.InMemoryMerge;
using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using ShardingCore.Sharding.Abstractions.ParallelExecutors;
using ShardingCore.Sharding.MergeEngines.ParallelControls;

namespace ShardingCore.Sharding.StreamMergeEngines
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: 2021/8/18 14:26:40
    * @Ver: 1.0
    * @Email: 326308290@qq.com
    */
    internal class SingleOrDefaultAsyncInMemoryMergeEngine<TEntity> : AbstractTrackEnsureMethodCallWhereInMemoryAsyncMergeEngine<TEntity>
    {
        public SingleOrDefaultAsyncInMemoryMergeEngine(StreamMergeContext<TEntity> streamMergeContext) : base(streamMergeContext)
        {
        }

        public override async Task<TEntity> DoMergeResultAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            var result = await base.ExecuteAsync( queryable =>  ((IQueryable<TEntity>)queryable).SingleOrDefaultAsync(cancellationToken), cancellationToken);
            var notNullResult = result.Where(o => o != null&&o.QueryResult!=null).Select(o=>o.QueryResult).ToList();

            if (notNullResult.Count > 1)
                throw new InvalidOperationException("Sequence contains more than one element.");
            return notNullResult.SingleOrDefault();
        }

        protected override IParallelExecuteControl<TResult> CreateParallelExecuteControl<TResult>(IParallelExecutor<TResult> executor)
        {
            return SingleOrSingleOrDefaultParallelExecuteControl<TResult>.Create(GetStreamMergeContext(), executor);
        }
    }
}
