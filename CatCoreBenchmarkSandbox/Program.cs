﻿using BenchmarkDotNet.Running;
using CatCoreBenchmarkSandbox.Benchmarks;

namespace CatCoreBenchmarkSandbox
{
	internal class Program
	{
		public static void Main(string[] args)
		{
			var summary = BenchmarkRunner.Run<TwitchIrcMessageTagsDeconstructionBenchmark>();
		}
	}
}