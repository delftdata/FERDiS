﻿using BlackSP.Core.Operators;
using BlackSP.Core.Operators.Concrete;
using BlackSP.CRA.Utilities;
using BlackSP.CRA.Vertices;
using BlackSP.Interfaces.Events;
using CRA.ClientLibrary;
using CRA.DataProvider;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace BlackSP.ThroughputExperiment.Utilities
{

    public class Cluster
    {
        public class MyFilterOperatorConfiguration : IFilterOperatorConfiguration
        {
            public int? OutputEndpointCount { get; set; }

            public IEvent Filter(IEvent @event)
            {
                Console.WriteLine($"Sick yo, filtering {@event.Key}");
                return @event;
            }
        }


        /// <summary>
        /// Utility method to setup a sample CRA cluster <br/>
        /// primarily useful for development purposes
        /// </summary>
        /// <returns></returns>
        public static async Task Setup(IDataProvider dataProvider)
        {
            var builder = new ClusterBuilder(dataProvider);

            Console.WriteLine($">> Wiping exising data from {dataProvider.GetType().Name}");
            await builder.ResetClusterAsync();

            //TODO: refactor below to clusterbuilder
            //Requirements:
            //- create source + sink + operators
            //- chain inputs to outputs
            //- each vertex pair should have their own endpoints connected
            //--- NO SHARED ENDPOINTS
            var client = builder.GetClientLibrary();

            Console.WriteLine(">> Defining vertex types");
            await client.DefineVertexAsync(typeof(OperatorVertex).Name.ToLowerInvariant(), () => new OperatorVertex());

            //Refactor step: launch instance + if not defined do define?
            Console.WriteLine(">> Instantiating operator vertex 1");
            await client.InstantiateVertexAsync(
                new[] { "crainst01" },
                "operator1",
                typeof(OperatorVertex).Name.ToLowerInvariant(),
                new VertexParameter { 
                    OperatorType = typeof(FilterOperator),
                    OperatorConfiguration = new MyFilterOperatorConfiguration { OutputEndpointCount = 2 }
                },
                1
            );

            Console.WriteLine(">> Instantiating operator vertex 2");
            await client.InstantiateVertexAsync(
                new[] { "crainst02" }, 
                "operator2", 
                typeof(OperatorVertex).Name.ToLowerInvariant(), 
                null, 
                1);

            //Console.WriteLine(">> Instantiating operator vertex 3");
            //await client.InstantiateVertexAsync(new[] { "crainst03" }, "operator3", typeof(OperatorVertex).Name.ToLowerInvariant(), null, 1);


            //Refactor step: connect endpoints instantiated on vertices to one another
            Console.WriteLine(">> Connecting vertices");
            await client.ConnectAsync("operator1", "output", "operator2", "input");
            //await client.ConnectAsync("operator1", "output2", "operator3", "input");

            await client.ConnectAsync("operator2", "output", "operator1", "input");
            //await client.ConnectAsync("operator2", "output2", "operator3", "input2");

            //await client.ConnectAsync("operator3", "output", "operator1", "input2");
            //await client.ConnectAsync("operator3", "output2", "operator2", "input2");

            Console.WriteLine(">> CRA Setup completed. Press any key to exit.");
            Console.ReadLine();
        }
    }
}