using System.Linq;
using UnityEngine;

public class PerceptronModule : MonoBehaviour {
	private const float NODES_INTERVAL_X = 0.025f;
	private const float NODES_INTERVAL_Z = 0.015f;
	private const float NODE_SIZE = 0.01f;

	public GameObject InputNodePrefab;
	public GameObject HiddenNodePrefab;
	public GameObject OutputNodePrefab;
	public GameObject ConnectionPrefab;
	public GameObject PerceptronContainer;
	public KMSelectable LayerButton0;
	public KMSelectable LayerButton1;
	public KMSelectable LayerButton2;
	public KMSelectable LayerButton3;
	public KMBombModule Module;

	private int inputsCount;
	private int outputsCount;
	private int[] hiddenLayers;
	private GameObject[] connections;
	private GameObject[] inputNodes;
	private GameObject[] outputNodes;
	private GameObject[][] hiddenNodes;

	private void Start() {
		hiddenLayers = Enumerable.Range(0, 4).Select(_ => Random.Range(1, 5)).ToArray();
		inputsCount = Random.Range(1, 5);
		outputsCount = Random.Range(1, 5);
		inputNodes = new GameObject[inputsCount];
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
		Module.OnActivate += Activate;
	}

	private void Activate() {
		LayerButton0.OnInteract += () => { PressLayerButton(0); return false; };
		LayerButton1.OnInteract += () => { PressLayerButton(1); return false; };
		LayerButton2.OnInteract += () => { PressLayerButton(2); return false; };
		LayerButton3.OnInteract += () => { PressLayerButton(3); return false; };
	}

	private void PressLayerButton(int layer) {
		hiddenLayers[layer] = hiddenLayers[layer] % 4 + 1;
		foreach (GameObject node in hiddenNodes[layer]) Destroy(node);
		InitHiddenLayer(layer);
	}

	private void InitHiddenLayer(int layer) {
		hiddenNodes[layer] = new GameObject[hiddenLayers[layer]];
		for (int i = 0; i < hiddenLayers[layer]; i++) {
			GameObject hiddenNode = Instantiate(HiddenNodePrefab);
			hiddenNode.transform.parent = PerceptronContainer.transform;
			hiddenNode.transform.localPosition = new Vector3((layer - 1.5f) * NODES_INTERVAL_X, 0f, NODES_INTERVAL_Z * (i - (hiddenLayers[layer] - 1) * 0.5f));
			hiddenNode.transform.localScale = new Vector3(NODE_SIZE, 0.001f, NODE_SIZE);
			hiddenNode.transform.localRotation = Quaternion.identity;
			hiddenNodes[layer][i] = hiddenNode;
		}
	}
}
