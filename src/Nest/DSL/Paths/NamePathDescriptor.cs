﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Nest.Resolvers.Converters;
using System.Linq.Expressions;
using Nest.Resolvers;

namespace Nest
{

	public class NamePathDescriptor<P, K>
		where P : NamePathDescriptor<P, K> 
		where K : FluentQueryString<K>, new()
	{
		internal string _Name { get; set; }

		/// <summary>
		/// Specify the {name} part of the operation
		/// </summary>
		public P Name(string name)
		{
			this._Name = name;
			return (P)this;
		}

		internal virtual ElasticSearchPathInfo<K> ToPathInfo<K>(IConnectionSettings settings)
			where K : FluentQueryString<K>, new()
		{
			if (this._Name.IsNullOrEmpty())
				throw new DslException("missing Name()");

			var pathInfo = new ElasticSearchPathInfo<K>()
			{
				Name = this._Name
			};
			pathInfo.QueryString = new K();
			return pathInfo;
		}

	}
}
