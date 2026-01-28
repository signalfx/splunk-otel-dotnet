// <copyright file="WeatherForecastController.cs" company="Splunk Inc.">
// Copyright Splunk Inc.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading;
using System.Web.Http;

namespace TestApplication.Snapshots.NetFramework.Controllers
{
    public class WeatherForecastController : ApiController
    {
        private static readonly string[] Summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        private static readonly Random RandomInstance = new Random();

        [HttpGet]
        public IEnumerable<WeatherForecast> Get()
        {
            Thread.Sleep(1000);
            var forecasts = new List<WeatherForecast>();
            for (var i = 1; i <= 5; i++)
            {
                forecasts.Add(new WeatherForecast
                {
                    Date = DateTime.Now.AddDays(i),
                    TemperatureC = RandomInstance.Next(-20, 55),
                    Summary = Summaries[RandomInstance.Next(Summaries.Length)]
                });
            }

            return forecasts;
        }
    }
}
