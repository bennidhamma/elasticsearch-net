﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using System.Linq.Expressions;
using Nest.Resolvers;
using Nest.Domain;

namespace Nest
{
	[DescriptorFor("IndicesCreate")]
	public partial class CreateIndexDescriptor : IPathInfo<CreateIndexQueryString>
	{
		internal string _Index { get; set; }
		internal IndexSettings _IndexSettings = new IndexSettings();
		private readonly IConnectionSettings _connectionSettings;

		public CreateIndexDescriptor(IConnectionSettings connectionSettings)
		{
			this._connectionSettings = connectionSettings;
		}

		/// <summary>
		/// Initialize the descriptor using the values from for instance a previous Get Index Settings call.
		/// </summary>
		public CreateIndexDescriptor InitializeUsing(IndexSettings indexSettings)
		{
			this._IndexSettings = indexSettings;
			return this;
		}

		/// <summary>
		/// Set the number of shards (if possible) for the new index.
		/// </summary>
		/// <param name="shards"></param>
		/// <returns></returns>
		public CreateIndexDescriptor NumberOfShards(int shards)
		{
			this._IndexSettings.NumberOfShards = shards;
			return this;
		}

		/// <summary>
		/// Set the number of replicas (if possible) for the new index.
		/// </summary>
		/// <param name="replicas"></param>
		/// <returns></returns>
		public CreateIndexDescriptor NumberOfReplicas(int replicas)
		{
			this._IndexSettings.NumberOfReplicas = replicas;
			return this;
		}

		/// <summary>
		/// Set/Update settings, the index.* prefix is not needed for the keys.
		/// </summary>
		public CreateIndexDescriptor Settings(Action<FluentDictionary<string, object>> settingsSelector)
		{
			settingsSelector.ThrowIfNull("settingsSelector");
			var dict = new FluentDictionary<string, object>();
			settingsSelector(dict);
			foreach (var kv in dict)
			{
				this._IndexSettings.TryAdd(kv.Key, kv.Value);
			}
			return this;
		}

		/// <summary>
		/// Remove an existing mapping by name
		/// </summary>
		public CreateIndexDescriptor RemoveMapping(string typeName)
		{
			TypeNameMarker marker = typeName;
			return this.RemoveMapping(marker);
		}

		/// <summary>
		/// Remove an exisiting mapping by inferred type name
		/// </summary>
		public CreateIndexDescriptor RemoveMapping<T>() where T : class
		{
			TypeNameMarker marker = typeof(T);
			return this.RemoveMapping(marker);
		}

		private CreateIndexDescriptor RemoveMapping(TypeNameMarker marker)
		{
			this._IndexSettings.Mappings = this._IndexSettings.Mappings.Where(m => m.Type != marker).ToList();
			return this;
		}

		/// <summary>
		/// Add a new mapping for T
		/// </summary>
		public CreateIndexDescriptor AddMapping<T>(Func<PutMappingDescriptor<T>, PutMappingDescriptor<T>> typeMappingDescriptor) where T : class
		{
			typeMappingDescriptor.ThrowIfNull("typeMappingDescriptor");
			var d = typeMappingDescriptor(new PutMappingDescriptor<T>(this._connectionSettings));
			var typeMapping = d._Mapping;
			typeMapping.Name = typeof (T);
			this._IndexSettings.Mappings.Add(typeMapping);

			return this;
		}

		/// <summary>
		/// Add a new mapping using the first rootObjectMapping parameter as the base to construct the new mapping.
		/// Handy if you wish to reuse a mapping.
		/// </summary>
		public CreateIndexDescriptor AddMapping<T>(RootObjectMapping rootObjectMapping, Func<PutMappingDescriptor<T>, PutMappingDescriptor<T>> typeMappingDescriptor) where T : class
		{
			typeMappingDescriptor.ThrowIfNull("typeMappingDescriptor");
			var d = typeMappingDescriptor(new PutMappingDescriptor<T>(this._connectionSettings) { _Mapping = rootObjectMapping,});
			var typeMapping = d._Mapping;
			typeMapping.Name = typeof (T);
			this._IndexSettings.Mappings.Add(typeMapping);

			return this;
		}

		public CreateIndexDescriptor AddWarmer(Func<CreateWarmerDescriptor, CreateWarmerDescriptor> warmerSelector)
		{
			warmerSelector.ThrowIfNull("warmerSelector");
			var descriptor = warmerSelector(new CreateWarmerDescriptor());

			var mapping = new WarmerMapping { Name = descriptor._WarmerName, Types = descriptor._Types, Source = descriptor._SearchDescriptor };
			this._IndexSettings.Warmers.Add(descriptor._WarmerName, mapping);

			return this;
		}

		public CreateIndexDescriptor DeleteWarmer(string warmerName)
		{
			this._IndexSettings.Warmers.Remove(warmerName);
			return this;
		}

		/// <summary>
		/// Set up analysis tokenizers, filters, analyzers
		/// </summary>
		public CreateIndexDescriptor Analysis(Func<AnalysisDescriptor, AnalysisDescriptor> analysisSelector)
		{
			analysisSelector.ThrowIfNull("analysisSelector");
			var analysis = analysisSelector(new AnalysisDescriptor(this._IndexSettings.Analysis));
			this._IndexSettings.Analysis = analysis == null ? null : analysis._AnalysisSettings;
			return this;
		}


		ElasticsearchPathInfo<CreateIndexQueryString> IPathInfo<CreateIndexQueryString>.ToPathInfo(
			IConnectionSettings settings)
		{
			var pathInfo = new ElasticsearchPathInfo<CreateIndexQueryString>();
			pathInfo.HttpMethod = PathInfoHttpMethod.POST;
			pathInfo.Index = this._Index;
			pathInfo.QueryString = this._QueryString;
			return pathInfo;

		}

	}
}
