using System.Collections;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;
using KModkit;

public class PerceptronModule : MonoBehaviour {
	private const float NODES_INTERVAL_X = 0.025f;
	private const float NODES_INTERVAL_Z = 0.015f;
	private const float NODE_SIZE = 0.01f;

	private static int moduleIdCounter = 1;

	public readonly string TwitchHelpMessage = new string[] {
		"\"!{0} cycle\" - view all connections' info",
		"\"!{0} inspect 012\" - view specific connections' info: 0 - connections between input and 1st layer, 1 - between 1st and 2nd, ..., 4 - between 4th and output layer",
		"\"!{0} set 1234\" - set hidden layers' sizes",
		"\"!{0} train\" - start training",
		"\"!{0} submit 1234\" - set hidden layers' sizes and start training",
		"\"!{0} reset\" - exit training mode (when training failed)",
	}.Join(" | ");

	public GameObject InputNodePrefab;
	public GameObject HiddenNodePrefab;
	public GameObject OutputNodePrefab;
	public GameObject PerceptronContainer;
	public TextMesh CurrentLearningRate;
	public TextMesh ConvergenceRate;
	public TextMesh RequiredAccuracy;
	public TextMesh CurrentLearningTime;
	public TextMesh ConnectionDelay;
	public TextMesh MaxLearningTime;
	public KMSelectable LayerButton0;
	public KMSelectable LayerButton1;
	public KMSelectable LayerButton2;
	public KMSelectable LayerButton3;
	public KMSelectable ConnectionButton0;
	public KMSelectable ConnectionButton1;
	public KMSelectable ConnectionButton2;
	public KMSelectable ConnectionButton3;
	public KMSelectable ConnectionButton4;
	public KMSelectable Screen;
	public KMBombModule Module;
	public KMBombInfo Bomb;
	public KMAudio Audio;
	public ConnectionComponent ConnectionPrefab;

	public bool TwitchShouldCancelCommand;

	private bool solved = false;
	private bool shouldSolve = false;
	private bool training = false;
	private bool trainingStage = false;
	private int moduleId;
	private int targetAccuracy;
	private int currentAccuracy;
	private int accuracyIntervalTo;
	private int accuracyTimeFrom;
	private int accuracyTimeTo;
	private int targetLearningTime;
	private int startingLearningTime;
	private Vector2Int trainingProcess;
	private PerceptronData.ModuleData data;
	private KMAudio.KMAudioRef trainingSound;
	private int[] hiddenLayers;
	private GameObject[] inputNodes;
	private GameObject[] outputNodes;
	private GameObject[][] hiddenNodes;
	private ConnectionComponent[][] connections;

	private void Start() {
		moduleId = moduleIdCounter++;
		hiddenLayers = Enumerable.Range(0, 4).Select(_ => Random.Range(1, 5)).ToArray();
		data = PerceptronData.Generate();
		inputNodes = new GameObject[data.inputsCount];
		connections = new ConnectionComponent[5][];
		for (int i = 0; i < data.inputsCount; i++) {
			GameObject input = Instantiate(InputNodePrefab);
			input.transform.parent = PerceptronContainer.transform;
			input.transform.localPosition = new Vector3(-NODES_INTERVAL_X * 2.5f, 0f, NODES_INTERVAL_Z * (i - (data.inputsCount - 1) * 0.5f));
			input.transform.localScale = new Vector3(NODE_SIZE, 0.001f, NODE_SIZE);
			input.transform.localRotation = Quaternion.identity;
			inputNodes[i] = input;
		}
		outputNodes = new GameObject[data.outputsCount];
		for (int i = 0; i < data.outputsCount; i++) {
			GameObject output = Instantiate(OutputNodePrefab);
			output.transform.parent = PerceptronContainer.transform;
			output.transform.localPosition = new Vector3(NODES_INTERVAL_X * 2.5f, 0f, NODES_INTERVAL_Z * (i - (data.outputsCount - 1) * 0.5f));
			output.transform.localScale = new Vector3(NODE_SIZE, 0.001f, NODE_SIZE);
			output.transform.localRotation = Quaternion.identity;
			outputNodes[i] = output;
		}
		hiddenNodes = new GameObject[4][];
		for (int layer = 0; layer < 4; layer++) InitHiddenLayer(layer);
		for (int layer = 0; layer < 5; layer++) InitConnectionLayer(layer);
		Module.OnActivate += Activate;
	}

	private void Activate() {
		Debug.LogFormat("[Perceptron #{0}] Required learning accuracy: {1}", moduleId, data.requiredAccuracy);
		Debug.LogFormat("[Perceptron #{0}] Max training time: {1}", moduleId, (ParseTime(data.maxLearningTime) / 1e3f).ToString("n3"));
		Debug.LogFormat("[Perceptron #{0}] Inputs count: {1}", moduleId, data.inputsCount);
		Debug.LogFormat("[Perceptron #{0}] Outputs count: {1}", moduleId, data.outputsCount);
		Debug.LogFormat("[Perceptron #{0}] Convergence rates: {1}", moduleId, data.convergenceRates.Select(s => (s / 100f).ToString("n2")).Join(", "));
		Debug.LogFormat("[Perceptron #{0}] Connection delays: {1}", moduleId, data.connectionsDelay.Select(s => (s / 100f).ToString("n2")).Join(", "));
		Debug.LogFormat("[Perceptron #{0}] Answer example: {1}", moduleId, data.answerExample.Join(""));
		UnholdConnectionButton(false);
		LayerButton0.OnInteract += () => { PressLayerButton(0); return false; };
		LayerButton1.OnInteract += () => { PressLayerButton(1); return false; };
		LayerButton2.OnInteract += () => { PressLayerButton(2); return false; };
		LayerButton3.OnInteract += () => { PressLayerButton(3); return false; };
		ConnectionButton0.OnInteract += () => { HoldConnectionButton(0); return false; };
		ConnectionButton1.OnInteract += () => { HoldConnectionButton(1); return false; };
		ConnectionButton2.OnInteract += () => { HoldConnectionButton(2); return false; };
		ConnectionButton3.OnInteract += () => { HoldConnectionButton(3); return false; };
		ConnectionButton4.OnInteract += () => { HoldConnectionButton(4); return false; };
		foreach (KMSelectable button in new[] { ConnectionButton0, ConnectionButton1, ConnectionButton2, ConnectionButton3, ConnectionButton4 }) {
			button.OnInteractEnded += () => UnholdConnectionButton();
		}
		Bomb.OnBombExploded += () => { if (trainingSound != null) trainingSound.StopSound(); };
		Screen.OnInteract += () => { PressScreen(); return false; };
	}

	private void Update() {
		if (training) {
			if (connections[trainingProcess.x].Length <= trainingProcess.y) trainingProcess = new Vector2Int(0, Random.Range(0, data.inputsCount * hiddenLayers[0]));
			connections[trainingProcess.x][trainingProcess.y].weight = Random.Range(0f, 1f);
			if (trainingProcess.x == 4) trainingProcess = new Vector2Int(0, Random.Range(0, data.inputsCount * hiddenLayers[0]));
			else {
				int nodeIndexFrom = trainingProcess.y / (trainingProcess.x == 0 ? data.inputsCount : hiddenLayers[trainingProcess.x - 1]);
				int leftNodesCount = hiddenLayers[trainingProcess.x];
				int nodeIndexTo = Random.Range(0, trainingProcess.x == 3 ? data.outputsCount : hiddenLayers[trainingProcess.x + 1]);
				trainingProcess = new Vector2Int(trainingProcess.x + 1, nodeIndexTo * leftNodesCount + nodeIndexFrom);
			}
			if (solved) return;
			int passedTime = Mathf.CeilToInt(Time.time * 1e3f) - startingLearningTime;
			if (passedTime >= targetLearningTime) {
				if (trainingSound != null) {
					trainingSound.StopSound();
					trainingSound = null;
				}
				CurrentLearningTime.text = (targetLearningTime / 1e3f).ToString("n3");
				CurrentLearningRate.text = targetAccuracy.ToString();
				Debug.LogFormat("[Perceptron #{0}] Training finished. Accuracy: {1}. Spent time: {2}", moduleId, targetAccuracy, (targetLearningTime / 1e3f).ToString("n3"));
				if (shouldSolve) {
					Debug.LogFormat("[Perceptron #{0}] Module solved", moduleId);
					Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.CorrectChime, transform);
					Module.HandlePass();
					solved = true;
					training = true;
				} else {
					training = false;
					Debug.LogFormat("[Perceptron #{0}] Strike", moduleId);
					Module.HandleStrike();
				}
			} else {
				CurrentLearningTime.text = (passedTime / 1e3f).ToString("n3");
				if (passedTime > accuracyTimeTo) {
					accuracyTimeFrom = passedTime;
					accuracyTimeTo = Random.Range(passedTime + 1, targetLearningTime);
					currentAccuracy = accuracyIntervalTo;
					accuracyIntervalTo = Random.Range(currentAccuracy, targetAccuracy);
				}
				int accuracy = currentAccuracy + (accuracyIntervalTo - currentAccuracy) * (passedTime - accuracyTimeFrom) / (accuracyTimeTo - accuracyTimeFrom);
				CurrentLearningRate.text = accuracy.ToString();
			}
		}
	}

	private void PressLayerButton(int layer) {
		if (trainingStage) return;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		hiddenLayers[layer] = hiddenLayers[layer] % 4 + 1;
		foreach (GameObject node in hiddenNodes[layer]) Destroy(node);
		foreach (ConnectionComponent connection in connections[layer]) Destroy(connection.gameObject);
		foreach (ConnectionComponent connection in connections[layer + 1]) Destroy(connection.gameObject);
		InitHiddenLayer(layer);
		InitConnectionLayer(layer);
		InitConnectionLayer(layer + 1);
	}

	private void HoldConnectionButton(int layer) {
		if (trainingStage) return;
		Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, transform);
		CurrentLearningRate.text = "";
		CurrentLearningTime.text = "";
		ConvergenceRate.text = (data.convergenceRates[layer] / 100f).ToString("n2");
		ConnectionDelay.text = (data.connectionsDelay[layer] / 100f).ToString("n2");
		RequiredAccuracy.text = "";
		MaxLearningTime.text = "";
	}

	private void UnholdConnectionButton(bool playSound = true) {
		if (trainingStage) return;
		if (playSound) Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
		CurrentLearningRate.text = "-";
		CurrentLearningTime.text = "-";
		ConvergenceRate.text = "/";
		ConnectionDelay.text = "/";
		RequiredAccuracy.text = data.requiredAccuracy.ToString();
		MaxLearningTime.text = (ParseTime(data.maxLearningTime) / 1e3f).ToString("n3");
	}

	private void PressScreen() {
		if (training) return;
		if (trainingStage) {
			trainingStage = false;
			Audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonRelease, transform);
			UnholdConnectionButton(false);
			return;
		}
		trainingSound = Audio.PlaySoundAtTransformWithRef("PerceptronTrainingProcess", transform);
		Debug.LogFormat("[Perceptron #{0}] Start training: {1}", moduleId, hiddenLayers.Join(""));
		training = true;
		trainingStage = true;
		trainingProcess = new Vector2Int(0, Random.Range(0, data.inputsCount * hiddenLayers[0]));
		targetAccuracy = PerceptronData.CalculateAccuracy(data.inputsCount, data.outputsCount, hiddenLayers, data.convergenceRates);
		targetLearningTime = ParseTime(PerceptronData.CalculateLearningTime(data.inputsCount, data.outputsCount, hiddenLayers, data.connectionsDelay));
		startingLearningTime = Mathf.CeilToInt(Time.time * 1e3f);
		accuracyTimeFrom = 0;
		accuracyTimeTo = Random.Range(1, targetLearningTime);
		currentAccuracy = 0;
		accuracyIntervalTo = Random.Range(0, targetAccuracy);
		shouldSolve = targetLearningTime <= ParseTime(data.maxLearningTime) && targetAccuracy >= data.requiredAccuracy;
	}

	public IEnumerator ProcessTwitchCommand(string command) {
		command = command.Trim().ToLower();
		if (command == "cycle") command = "inspect 01234";
		if (Regex.IsMatch(command, "^inspect [0-4 ]+$")) {
			yield return null;
			if (trainingStage) {
				yield return "sendtochat {0}, !{1} can't view connections' info, finish current training first.";
				yield break;
			}
			int[] connectionsIndices = command.Split(' ').Skip(1).Where(s => s.Length > 0).Join("").ToArray().Select(c => c - '0').ToArray();
			if (connectionsIndices.Length > 5) yield return "waiting music";
			KMSelectable[] connectionsButtons = new[] { ConnectionButton0, ConnectionButton1, ConnectionButton2, ConnectionButton3, ConnectionButton4 };
			foreach (int index in connectionsIndices) {
				HoldConnectionButton(index);
				GameObject highlight = connectionsButtons[index].Highlight.transform.GetChild(0).gameObject;
				highlight.SetActive(true);
				yield return new WaitForSeconds(3f);
				yield return null;
				UnholdConnectionButton();
				highlight.SetActive(false);
				if (TwitchShouldCancelCommand) {
					yield return "cancelled";
					break;
				}
			}
			yield break;
		}
		bool shouldTrain = false;
		if (Regex.IsMatch(command, "^submit ([1-4] *){4}$")) {
			shouldTrain = true;
			command = "set " + command.Split(' ').Skip(1).Join("");
		}
		if (Regex.IsMatch(command, "^set ([1-4] *){4}$")) {
			yield return null;
			if (trainingStage) {
				yield return "sendtochat {0}, !{1} can't set hidden layers, finish current training first.";
				yield break;
			}
			int[] expectedLayersSizes = command.Split(' ').Skip(1).Where(s => s.Length > 0).Join("").ToArray().Select(c => c - '0').ToArray();
			for (int i = 0; i < 4; i++) {
				while (hiddenLayers[i] != expectedLayersSizes[i]) {
					PressLayerButton(i);
					yield return new WaitForSeconds(.1f);
				}
			}
		}
		if (command == "train") shouldTrain = true;
		if (shouldTrain) {
			yield return null;
			if (trainingStage) {
				yield return "sendtochat {0}, !{1} can't start training, finish current training first.";
				yield break;
			}
			PressScreen();
			if (shouldSolve) yield return "solve";
			yield break;
		}
		if (command == "reset") {
			yield return null;
			if (!trainingStage) {
				yield return "sendtochat {0}, !{1} can't exit training mode: training not started.";
				yield break;
			}
			if (training) {
				yield return "sendtochat {0}, !{1} can't exit training mode: training not finished.";
				yield break;
			}
			yield return new[] { Screen };
			yield break;
		}
	}

	private IEnumerator TwitchHandleForcedSolve() {
		yield return null;
		while (training) yield return new WaitForSeconds(.1f);
		if (trainingStage) PressScreen();
		yield return ProcessTwitchCommand("submit " + data.answerExample.Join(""));
	}

	private int ParseTime(int time) {
		return time * Bomb.GetModuleIDs().Count + Bomb.GetIndicators().Count() * 300;
	}

	private void InitHiddenLayer(int layer) {
		hiddenNodes[layer] = new GameObject[hiddenLayers[layer]];
		for (int i = 0; i < hiddenLayers[layer]; i++) {
			GameObject hiddenNode = Instantiate(HiddenNodePrefab);
			hiddenNode.transform.parent = PerceptronContainer.transform;
			hiddenNode.transform.localPosition = new Vector3((layer - 1.5f) * NODES_INTERVAL_X, 0f, NODES_INTERVAL_Z * (i - (hiddenLayers[layer] - 1) * 0.5f));
			hiddenNode.transform.localScale = new Vector3(NODE_SIZE, 0.002f, NODE_SIZE);
			hiddenNode.transform.localRotation = Quaternion.identity;
			hiddenNodes[layer][i] = hiddenNode;
		}
	}

	private void InitConnectionLayer(int layer) {
		GameObject[] leftNodes = layer == 0 ? inputNodes : hiddenNodes[layer - 1];
		GameObject[] rightNodes = layer == 4 ? outputNodes : hiddenNodes[layer];
		connections[layer] = new ConnectionComponent[leftNodes.Length * rightNodes.Length];
		for (int i = 0; i < leftNodes.Length; i++) {
			GameObject from = leftNodes[i];
			for (int j = 0; j < rightNodes.Length; j++) {
				GameObject to = rightNodes[j];
				ConnectionComponent connection = Instantiate(ConnectionPrefab);
				connection.transform.parent = PerceptronContainer.transform;
				connection.transform.localPosition = (from.transform.localPosition + to.transform.localPosition) * .5f;
				connection.transform.localScale = new Vector3(.001f, .001f, (from.transform.localPosition - to.transform.localPosition).magnitude);
				connection.transform.localRotation = Quaternion.LookRotation(to.transform.localPosition - from.transform.localPosition, Vector3.up);
				connections[layer][j * leftNodes.Length + i] = connection;
			}
		}
	}
}
