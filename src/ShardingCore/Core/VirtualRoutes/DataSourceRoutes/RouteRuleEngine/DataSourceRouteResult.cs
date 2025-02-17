﻿using System;
using System.Collections.Generic;
using System.Text;
using ShardingCore.Core.VirtualDatabase.VirtualDataSources.PhysicDataSources;

namespace ShardingCore.Core.VirtualRoutes.DataSourceRoutes.RouteRuleEngine
{
    /*
    * @Author: xjm
    * @Description:
    * @Date: 2021/9/16 12:58:34
    * @Ver: 1.0
    * @Email: 326308290@qq.com
    */
    public class DataSourceRouteResult
    {
        public DataSourceRouteResult(ISet<string> intersectDataSources)
        {
            IntersectDataSources = intersectDataSources;
        }
        /// <summary>
        /// 交集
        /// </summary>
        public ISet<string> IntersectDataSources { get; }
    }
}
