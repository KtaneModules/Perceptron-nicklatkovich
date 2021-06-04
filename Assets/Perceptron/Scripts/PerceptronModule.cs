using System.Linq;
using UnityEngine;

public class PerceptronModule : MonoBehaviour {
	private const float NODES_INTERVAL_X = 0.025f;
	private const float NODES_INTERVAL_Z = 0.015f;
	private const float NODE_SIZE = 0.01f;

	public GameObject InputNodePrefab;
	public GameObject HiddenNodePrefab;
	public GameObject OutputNodePrefab;
	public GameObject PerceptronContainer;
	public KMSelectable LayerButton0;
	public KMSelectable LayerButton1;
	public KMSelectable LayerButton2;
	public KMSelectable LayerButton3;
	public KMBombModule Module;
	public ConnectionComponent ConnectionPrefab;

	private bool training = true;
	private int inputsCount;
	private int outputsCount;
	private Vector2Int trainingProcess;
	private int[] hiddenLayers;
	private GameObject[] inputNodes;
	private GameObject[] outputNodes;
	private GameObject[][] hiddenNodes;
	private ConnectionComponent[][] connections;

	private void Start() {
		hiddenLayers = Enumerable.Range(0, 4).Select(_ => Random.Range(1, 5)).ToArray();
		inputsCount = Random.Range(1, 5);
		outputsCount = Random.Range(1, 5);
		inputNodes = new GameObject[inputsCount];
		connections = new ConnectionComponent[5][];
		for (int i = 0; i < inputsCount; i++) {
			GameObject input = Instantiate(InputNodePrefab);
			input.transform.parent = PerceptronContainer.transform;
			input.transform.localPosition = new Vector3(-NODES_INTERVAL_X * 2.5f, 0f, NODES_INTERVAL_Z * (i - (inputsCount - 1) * 0.5f));
			input.transform.localScale = new Vector3(NODE_SIZE, 0.001f, NODE_SIZE);
			input.transform.localRotation = Quaternion.identity;
			inputNodes[i] = input;
		}
		outputNodes = new GameObject[outputsCount];
		for (int i = 0; i < outputsCount; i++) {
			GameObject output = Instantiate(OutputNodePrefab);
			output.transform.parent = PerceptronContainer.transform;
			output.transform.localPosition = new Vector3(NODES_INTERVAL_X * 2.5f, 0f, NODES_INTERVAL_Z * (i - (outputsCount - 1) * 0.5f));
			output.transform.localScale = new Vector3(NODE_SIZE, 0.001f, NODE_SIZE);
			output.transform.localRotation = Quaternion.identity;
			outputNodes[i] = output;
		}
		hiddenNodes = new GameObject[4][];
		for (int layer = 0; layer < 4; layer++) InitHiddenLayer(layer);
		for (int layer = 0; layer < 5; layer++) InitConnectionLayer(layer);
		trainingProcess = new Vector2Int(0, Random.Range(0, inputsCount * hiddenLayers[0]));
		Module.OnActivate += Activate;
	}

	private void Activate() {
		LayerButton0.OnInteract += () => { PressLayerButton(0); return false; };
		LayerButton1.OnInteract += () => { PressLayerButton(1); return false; };
		LayerButton2.OnInteract += () => { PressLayerButton(2); return false; };
		LayerButton3.OnInteract += () => { PressLayerButton(3); return false; };
	}

	private void Update() {
		if (training) {
			if (connections[trainingProcess.x].Length <= trainingProcess.y) trainingProcess = new Vector2Int(0, Random.Range(0, inputsCount * hiddenLayers[0]));
			connections[trainingProcess.x][trainingProcess.y].weight = Random.Range(0f, 1f);
			if (trainingProcess.x == 4) trainingProcess = new Vector2Int(0, Random.Range(0, inputsCount * hiddenLayers[0]));
			else {
				int nodeIndexFrom = trainingProcess.y / (trainingProcess.x == 0 ? inputsCount : hiddenLayers[trainingProcess.x - 1]);
				int leftNodesCount = hiddenLayers[trainingProcess.x];
				int nodeIndexTo = Random.Range(0, trainingProcess.x == 3 ? outputsCount : hiddenLayers[trainingProcess.x + 1]);
				trainingProcess = new Vector2Int(trainingProcess.x + 1, nodeIndexTo * leftNodesCount + nodeIndexFrom);
			}
		}
	}

	private void PressLayerButton(int layer) {
		hiddenLayers[layer] = hiddenLayers[layer] % 4 + 1;
		foreach (GameObject node in hiddenNodes[layer]) Destroy(node);
		foreach (ConnectionComponent connection in connections[layer]) Destroy(connection.gameObject);
		foreach (ConnectionComponent connection in connections[layer + 1]) Destroy(connection.gameObject);
		InitHiddenLayer(layer);
		InitConnectionLayer(layer);
		InitConnectionLayer(layer + 1);
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
