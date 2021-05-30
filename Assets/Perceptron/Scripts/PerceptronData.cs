using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

public static class PerceptronData {
	public struct ModuleData {
		public int inputsCount;
		public int outputsCount;
		public int requiredAccuracy;
		public int maxLearningTime;
		public readonly int[] convergenceRates;
		public readonly int[] answerExample;
		public readonly int[] connectionsDelay;
		public ModuleData(
			int inputsCount,
			int outputsCount,
			int requiredAccuracy,
			int maxLearningTime,
			int[] convergenceRates,
			int[] answerExample,
			int[] connectionsDelay
		) {
			this.inputsCount = inputsCount;
			this.outputsCount = outputsCount;
			this.requiredAccuracy = requiredAccuracy;
			this.maxLearningTime = maxLearningTime;
			this.convergenceRates = convergenceRates;
			this.answerExample = answerExample;
			this.connectionsDelay = connectionsDelay;
		}
	}

	public static ModuleData Generate() {
		int[] convergenceRatesSorted = Enumerable.Range(0, 5).Select(_ => Random.Range(1, 1000)).ToArray();
		Array.Sort(convergenceRatesSorted);
		Array.Reverse(convergenceRatesSorted);
		int[] answerExample = Enumerable.Range(0, 4).Select(_ => Random.Range(1, 5)).ToArray();
		int inputsCount = Random.Range(1, 5);
		int outputsCount = Random.Range(1, 5);
		int[] waysCount = GetWaysCount(inputsCount, outputsCount, answerExample);
		Dictionary<int, HashSet<int>> waysCountToIndices = new Dictionary<int, HashSet<int>>();
		for (int i = 0; i < 5; i++) {
			if (!waysCountToIndices.ContainsKey(waysCount[i])) waysCountToIndices.Add(waysCount[i], new HashSet<int>());
			waysCountToIndices[waysCount[i]].Add(i);
		}
		int[] waysCountSorted = waysCount.Select(a => a).ToArray();
		Array.Sort(waysCountSorted);
		Array.Reverse(waysCountSorted);
		int[] convergenceRates = new int[5];
		for (int i = 0; i < 5; i++) {
			int connections = waysCountSorted[i];
			int layer = waysCountToIndices[connections].PickRandom();
			waysCountToIndices[connections].Remove(layer);
			convergenceRates[layer] = convergenceRatesSorted[i];
		}
		int[] connectionsDelay = Enumerable.Range(0, 5).Select(_ => Random.Range(1, 1000)).ToArray();
		int accuracy = CalculateAccuracy(inputsCount, outputsCount, answerExample, convergenceRates);
		int learningTime = CalculateLearningTime(inputsCount, outputsCount, answerExample, connectionsDelay);
		int requiredAccuracy = accuracy + Random.Range(0, accuracy / 10);
		int maxLearningTime = learningTime + Random.Range(0, learningTime / 10);
		return new ModuleData(inputsCount, outputsCount, requiredAccuracy, maxLearningTime, convergenceRates, answerExample, connectionsDelay);
	}

	public static int CalculateAccuracy(int inputsCount, int outputsCount, int[] nodesCount, int[] convergenceRates) {
		int[] waysCount = GetWaysCount(inputsCount, outputsCount, nodesCount);
		return Enumerable.Range(0, 5).Select(i => waysCount[i] * convergenceRates[i] / 100).Sum();
	}

	public static int CalculateLearningTime(int inputsCount, int outputsCount, int[] nodesCount, int[] connectionsDelay) {
		int[] waysCount = GetWaysCount(inputsCount, outputsCount, nodesCount);
		return Enumerable.Range(0, 5).Select(i => Ceil100(waysCount[i] * connectionsDelay[i])).Sum();
	}

	public static int[] GetWaysCount(int inputsCount, int outputsCount, int[] nodesCount) {
		return Enumerable.Range(0, 5).Select(i => {
			if (i == 0) return nodesCount[0] * inputsCount;
			if (i < 4) return nodesCount[i] * nodesCount[i - 1];
			return nodesCount[3] * outputsCount;
		}).ToArray();
	}

	private static int Ceil100(int value) {
		int result = value / 100;
		return value % 100 > 0 ? result + 1 : result;
	}
}
