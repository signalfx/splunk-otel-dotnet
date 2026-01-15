// <copyright file="ClassA.cs" company="Splunk Inc.">
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
using System.IO;
using System.Threading;

namespace My.Custom.Test.Namespace
{
    internal static class ClassA
    {
        public static void MethodA()
        {
            const int numberOfItems = 1024;
            var items = new List<string>();
            for (var i = 0; i < numberOfItems; i++)
            {
                items.Add(i.ToString("D10000"));
            }

            Thread.Sleep(TimeSpan.FromSeconds(5));

            for (var i = 0; i < numberOfItems; i++)
            {
                var item = items[i];
                TextWriter.Null.Write(item[item.Length - 2]);
            }
        }
    }
}
