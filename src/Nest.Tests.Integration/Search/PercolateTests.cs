﻿using System.Linq;
using FluentAssertions;
using Nest.Tests.MockData;
using Nest.Tests.MockData.Domain;
using NUnit.Framework;

namespace Nest.Tests.Integration.Search
{
	[TestFixture]
	public class PercolateTests : IntegrationTests
	{
		private string _LookFor = NestTestData.Data.First().Followers.First().FirstName;

		[Test]
		public void RegisterPercolateTest()
		{
			var name = "mypercolator";
			var c = this._client;
			var r = c.RegisterPercolator<ElasticsearchProject>(name, p => p
				.Query(q => q
					.Term(f => f.Name, "elasticsearch.pm")
				)
			);
			Assert.True(r.IsValid);
			Assert.True(r.OK);
			Assert.AreEqual(r.Type, ElasticsearchConfiguration.DefaultIndex);
			Assert.AreEqual(r.Id, name);
			Assert.Greater(r.Version, 0);
			r.ConnectionStatus.Request.Should().NotBeNullOrEmpty().And.NotBe("{}");

		}
		[Test]
		public void UnregisterPercolateTest()
		{
			var name = "mypercolator";
			var c = this._client;
			var r = c.RegisterPercolator<ElasticsearchProject>(name, p => p
				.AddMetadata(md=>md.Add("color", "blue"))
				.Query(q => q
					.Term(f => f.Name, "elasticsearch.pm")
				)
			);
			Assert.True(r.IsValid);
			Assert.True(r.OK);
			Assert.AreEqual(r.Type, ElasticsearchConfiguration.DefaultIndex);
			Assert.AreEqual(r.Id, name);
			Assert.Greater(r.Version, 0);

			var re = c.UnregisterPercolator(name, ur=>ur.Index<ElasticsearchProject>());
			Assert.True(re.IsValid);
			Assert.True(re.OK);
			Assert.True(re.Found);
			Assert.AreEqual(re.Type, ElasticsearchConfiguration.DefaultIndex);
			Assert.AreEqual(re.Id, name);
			Assert.Greater(re.Version, 0);
			re = c.UnregisterPercolator(name, ur=>ur.Index<ElasticsearchProject>());
			Assert.True(re.IsValid);
			Assert.True(re.OK);
			Assert.False(re.Found);
		}

		[Test]
		public void PercolateDoc()
		{
			this.RegisterPercolateTest(); // I feel a little dirty.
			var c = this._client;
			var name = "mypercolator";
			var r = c.Percolate<ElasticsearchProject>(new ElasticsearchProject()
			{
				Name = "elasticsearch.pm",
				Country = "netherlands",
				LOC = 100000, //Too many :(
			}, p=>p.Index<ElasticsearchProject>());
			Assert.True(r.IsValid);
			Assert.True(r.OK);
			Assert.NotNull(r.Matches);
			Assert.True(r.Matches.Contains(name));
			var re = c.UnregisterPercolator(name, ur=>ur.Index<ElasticsearchProject>());
		}
		[Test]
		public void PercolateTypedDoc()
		{
			this.RegisterPercolateTest(); // I feel a little dirty.
			var c = this._client;
			var name = "eclecticsearch";
			var r = c.RegisterPercolator<ElasticsearchProject>(name, p => p
				 .Query(q => q
					.Term(f => f.Country, "netherlands")
				 )
			 );
			Assert.True(r.IsValid);
			Assert.True(r.OK);
			var obj = new ElasticsearchProject()
			{
				Name = "NEST",
				Country = "netherlands",
				LOC = 100000, //Too many :(
			};
			var percolateResponse = this._client.Percolate(obj);
			Assert.True(percolateResponse.IsValid);
			Assert.True(percolateResponse.OK);
			Assert.NotNull(percolateResponse.Matches);
			Assert.True(percolateResponse.Matches.Contains(name));

			var re = c.UnregisterPercolator(name, ur=>ur.Index<ElasticsearchProject>());
		}
		[Test]
		public void PercolateTypedDocWithQuery()
		{
			var c = this._client;
			var name = "eclecticsearch" + ElasticsearchConfiguration.NewUniqueIndexName();
			var re = c.UnregisterPercolator(name, ur=>ur.Index<ElasticsearchProject>());
			var r = c.RegisterPercolator<ElasticsearchProject>(name, p => p
				 .AddMetadata(md=>md.Add("color", "blue"))
				 .Query(q => q
					.Term(f => f.Country, "netherlands")
				 )
			 );
			Assert.True(r.IsValid);
			Assert.True(r.OK);
			c.Refresh();
			var obj = new ElasticsearchProject()
			{
				Name = "NEST",
				Country = "netherlands",
				LOC = 100000, //Too many :(
			};
			var percolateResponse = this._client.Percolate(obj,p => p.Query(q=>q.Match(m=>m.OnField("color").Query("blue"))));
			Assert.True(percolateResponse.IsValid);
			Assert.True(percolateResponse.OK);
			Assert.NotNull(percolateResponse.Matches);
			Assert.True(percolateResponse.Matches.Contains(name), percolateResponse.Matches.Count().ToString());

			//should not match since we registered with the color blue
			percolateResponse = this._client.Percolate(obj, p => p.Query(q => q.Term("color", "green")));
			Assert.True(percolateResponse.IsValid);
			Assert.True(percolateResponse.OK);
			Assert.NotNull(percolateResponse.Matches);
			Assert.False(percolateResponse.Matches.Contains(name));

			re = c.UnregisterPercolator(name, ur=>ur.Index<ElasticsearchProject>());

		}
	}
}