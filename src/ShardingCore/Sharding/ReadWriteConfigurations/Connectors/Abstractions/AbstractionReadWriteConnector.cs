﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ShardingCore.Exceptions;
using ShardingCore.Infrastructures;
using ShardingCore.Sharding.ReadWriteConfigurations.Abstractions;

namespace ShardingCore.Sharding.ReadWriteConfigurations.Connectors.Abstractions
{
    public abstract class AbstractionReadWriteConnector:IReadWriteConnector
    {
        protected List<string> ConnectionStrings { get;}
        protected int Length { get; private set; }

        private object slock = new object();
        //private readonly string _tempConnectionString;
        //private readonly OneByOneChecker _oneByOneChecker = new OneByOneChecker();

        public AbstractionReadWriteConnector(string dataSourceName,IEnumerable<string> connectionStrings)
        {
            DataSourceName = dataSourceName;
            ConnectionStrings = connectionStrings.ToList();
            Length = ConnectionStrings.Count;
            //_tempConnectionString = ConnectionStrings[0];
        }
        public  string DataSourceName { get; }

        public  string GetConnectionString()
        {
            return DoGetConnectionString();
        }

        public abstract string DoGetConnectionString();

        /// <summary>
        /// 动态添加数据源
        /// </summary>
        /// <param name="connectionString"></param>
        /// <returns></returns>
        public bool AddConnectionString(string connectionString)
        {
            var acquired = Monitor.TryEnter(slock,TimeSpan.FromSeconds(3));
            if (!acquired)
                throw new ShardingCoreInvalidOperationException($"{nameof(AddConnectionString)} is busy");
            try
            {
                ConnectionStrings.Add(connectionString);
                Length = ConnectionStrings.Count;
                return true;
            }
            finally
            {
                Monitor.Exit(slock);
            }
        }
    }
}
