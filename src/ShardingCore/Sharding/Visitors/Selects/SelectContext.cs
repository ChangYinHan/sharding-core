using System;
using System.Collections.Generic;
using System.Linq;
using ShardingCore.Sharding.Visitors.Selects;

namespace ShardingCore.Core.Internal.Visitors.Selects
{
/*
* @Author: xjm
* @Description:
* @Date: Tuesday, 02 February 2021 08:17:24
* @Email: 326308290@qq.com
*/
    public class SelectContext
    {
        public List<SelectProperty> SelectProperties { get; set; } = new List<SelectProperty>();

        public bool HasAverage()
        {
            return SelectProperties.Any(o => o is SelectAverageProperty);
        }

        public bool HasCount()
        {
            return SelectProperties.Any(o=>o is SelectCountProperty);
        }
    }
}