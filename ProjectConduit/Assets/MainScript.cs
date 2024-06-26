using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using Unity.Mathematics;
using TMPro;

public class MainScript : MonoBehaviour
{
    public static event Action<GameObject> Stepped;
    public static event Action<bool> UpdateState;
    public GameObject wireTemplate;
    public List<GameObject> blockList;
    public Canvas canvas;

    public bool userState = true;
    public bool UIState = false;
    public bool running;

    public GameObject wireFolder;
    public GameObject blockFolder;
    public GameObject indicatorFolder;

    public List<GameObject> lastBlocks = new List<GameObject>();
    public List<GameObject> nextBlocks = new List<GameObject>();

    public GameObject stepIndicatorTemplate;
    public List<GameObject> stepIndicators;

    public TextMeshProUGUI statusText;

    public int step = 1;

    float RadToDeg(float input)
    {
        return input * 180 / math.PI;
    }
    float DegToRad(float input)
    {
        return input / 180 * math.PI;
    }


    // Start is called before the first frame update
    void Start()
    {
        BuildButtonBehavior.BuildBlockCommand += WhenBuildCommand;
    }

    // Update is called once per frame
    void Update()
    {
        if (userState) //Is actually doing something
        {
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);

            if (hit.collider != null)
            {
                UIState = hit.collider.gameObject.transform.IsChildOf(canvas.transform);
            }

            if (UIState) //Interacting with UI
            {

            }
            else //Interacting with world
            {
                if (Input.GetMouseButtonDown(0))
                {
                    print("Clicked");

                    if (hit.collider != null)
                    {
                        Debug.Log(hit.transform.name);

                        if (hit.transform.gameObject.layer == 10) { //Clickable
                            print("Clicked Button");
                            print(hit.transform.name);

                            if (hit.transform.name == "StartSwitchButton") {
                                BlockScript startBlockScript = hit.transform.GetComponentInParent<BlockScript>();

                                startBlockScript.active = !startBlockScript.active;
                                hit.transform.Find("ButtonOn").gameObject.SetActive(startBlockScript.active);
                            }
                        }
                        else if (hit.transform.gameObject.layer == 7) //Gates, make wire
                        {
                            StartCoroutine(MakeWireProcess(hit.transform.gameObject));
                        }
                        else if (hit.transform.gameObject.layer == 8) //Gates, make wire
                        {
                            StartCoroutine(MakeWireProcess(hit.transform.gameObject));
                        }
                        else if (hit.transform.gameObject.layer == 9) //Blocks, edit block
                        {
                            print("testedit block");
                            //StartCoroutine(MakeWireProcess(hit.transform.gameObject));
                        }
                    }
                }
                else if (Input.GetMouseButtonDown(1))
                {
                    print("Clicked right");
                    print(hit.transform);

                    if (hit.collider != null)
                    {
                        Debug.Log(hit.transform.name);

                        if (hit.transform.gameObject.layer == 6) //Wire, delete
                        {
                            ClearWire(hit.transform.gameObject);
                        }
                        else if (hit.transform.gameObject.layer == 9) //Blocks, delete
                        {
                            ClearBlock(hit.transform.gameObject);
                        }
                    }
                }
            }
        }
    }

    //Events:
    void WhenBuildCommand(GameObject targetBlock)
    {
        StartCoroutine(MakeBlockProcess(targetBlock));
    }

    void OnClear()
    {
        print("Reset circult");
        statusText.text = "Status: Cleared";
        ResetCircuit();
    }
    void ResetCircuit()
    {
        step = 1;
        nextBlocks = new List<GameObject>();

        List<GameObject> allWires = new List<GameObject>();

        List<GameObject> allBlocks = new List<GameObject>();
        
        //Clear indicators
        foreach (GameObject indicator in stepIndicators)
        {
            Destroy(indicator);
        }
        stepIndicators = new List<GameObject>();

        //Allblock list
        for (int i = 0; i < blockFolder.transform.childCount; i++)
        {
            allBlocks.Add(blockFolder.transform.GetChild(i).gameObject);
        }
        for (int i = 0; i < wireFolder.transform.childCount; i++)
        {
            allWires.Add(wireFolder.transform.GetChild(i).gameObject);
        }

        //Allwire list
        foreach (GameObject wire in allWires)
        {
            wire.GetComponent<WireScript>().powered = false;
        }
        foreach (GameObject block in allBlocks)
        {
            BlockScript blockScript = block.GetComponent<BlockScript>();

            if (blockScript.blockType == 0)
            {
                nextBlocks.Add(block);
            }
            else if (blockScript.blockType == 1)
            {
                blockScript.OutputBlock();
            }

        }

        userState = true;
        if (UpdateState != null) {
            UpdateState.Invoke(false);
        }
    }
    IEnumerator OnRun(InputValue action)
    {
        ResetCircuit();
        print("Run");

        userState = false;

        if (nextBlocks.Count == 0)
        {
            print("Bad circult, no start blocks?");
        }
        else
        {
            running = true;
            while (running)
            {
                yield return null;
                running = !OnStep();
            }
        }

        print("Run finished");
        step = 1;
        userState = true;
    }

    bool OnStep()
    {
        userState = false;
        List<GameObject> allBlocks = new List<GameObject>();
        bool atEnd = true;

        //Clean up and initialize

        for (int i = 0; i < blockFolder.transform.childCount; i++)
        {
            allBlocks.Add(blockFolder.transform.GetChild(i).gameObject);
        }

        foreach (GameObject indicator in stepIndicators)
        {
            Destroy(indicator);
        }
        stepIndicators = new List<GameObject>();

        //Do work

        lastBlocks = nextBlocks;
        nextBlocks = new List<GameObject>();

        foreach (GameObject block in lastBlocks)
        {
            BlockScript blockScript = block.GetComponent<BlockScript>();
            print(block);
            if (block != null) {
                Stepped?.Invoke(block);
            }

            atEnd = false;

            GameObject indicator = Instantiate(stepIndicatorTemplate);
            indicator.transform.parent = indicatorFolder.transform;
            stepIndicators.Add(indicator);
            indicator.transform.position = block.transform.position + new Vector3(0, 1.5f, 0);

            nextBlocks.AddRange(blockScript.outputPorts.Values);

        }

        UpdateState.Invoke(false);
        print("STEP" + step);

        statusText.text = "Status: Running Increment, Current Step: " + step;
        if (atEnd)
        {
            statusText.text = "Status: Finished, Total Step: " + (step - 1);
        }
        step++;

        if (atEnd && !running)
        {
            ResetCircuit();
            statusText.text = "Status: Cleared";
        }

        return atEnd;
    }



    //Actual functions:
    void RenderWire(GameObject wire, Vector2 start, Vector2 end)
    {
        wire.transform.position = start + (end - start) / 2;
        wire.transform.eulerAngles = new Vector3(0, 0, RadToDeg(math.atan2(end.y - start.y, end.x - start.x)));
        wire.transform.localScale = new Vector2((end - start).magnitude, wire.transform.localScale.y);

        Transform arrow = wire.transform.Find("Arrow");
        arrow.localScale = new Vector2(3, 0.3f / wire.transform.localScale.x);
    }
    
    void ClearWire(GameObject wire) {
        WireScript wireScript = wire.GetComponent<WireScript>();

        wireScript.attachment1.GetComponent<BlockScript>().outputPorts.Remove(wire);
        wireScript.attachment2.GetComponent<BlockScript>().inputPorts.Remove(wire);
        Destroy(wire);
        ResetCircuit();
    }
    IEnumerator MakeWireProcess(GameObject port1)
    {
        GameObject wire = Instantiate(wireTemplate);
        WireScript wireScript = wire.GetComponent<WireScript>();
        GameObject potentialPort;
        GameObject parentBlock = port1.transform.parent.gameObject;
        int targetPortType = 0;

        wire.transform.SetParent(wireFolder.transform);

        if (port1.layer == 7)
        {
            wireScript.attachment2 = parentBlock;
            wire.transform.Find("Arrow").transform.eulerAngles = new Vector3(0, 0, 90);
            targetPortType = 8;
        }
        else
        {
            wireScript.attachment1 = parentBlock;
            wire.transform.Find("Arrow").transform.eulerAngles = new Vector3(0, 0, -90);
            targetPortType = 7;
        }

        userState = false;

        while (!userState)
        {
            yield return null;
            potentialPort = null;
            RaycastHit2D hit = Physics2D.Raycast(Camera.main.ScreenToWorldPoint(Input.mousePosition), Vector2.zero);
            if (hit.collider != null)
            {
                if (hit.transform.gameObject.layer == targetPortType)
                {
                    if (hit.transform.parent != port1.transform.parent)
                    {
                        BlockScript blockScript = hit.transform.parent.GetComponent<BlockScript>();

                        if (blockScript != null) {
                            if (targetPortType == 7 && blockScript.inputPorts.ContainsValue(parentBlock)) {
                                //nah
                            }
                            else if (targetPortType == 8 && blockScript.outputPorts.ContainsValue(parentBlock)) {
                                //nah
                            }
                            else {
                                potentialPort = hit.transform.gameObject;
                            }
                        }
                    }
                }
            }

            if (potentialPort != null)
            {
                RenderWire(wire, port1.transform.position, potentialPort.transform.position);

                if (Input.GetMouseButtonDown(0) && !UIState)
                {
                    if (targetPortType == 7)
                    {
                        wireScript.attachment2 = potentialPort.transform.parent.gameObject;
                    }
                    else
                    {
                        wireScript.attachment1 = potentialPort.transform.parent.gameObject;
                    }

                    wireScript.attachment1.GetComponent<BlockScript>().outputPorts.Add(wire, wireScript.attachment2);
                    wireScript.attachment2.GetComponent<BlockScript>().inputPorts.Add(wire, wireScript.attachment1);
                    break;
                }
            }
            else
            {
                RenderWire(wire, port1.transform.position, Camera.main.ScreenToWorldPoint(Input.mousePosition));

                if (Input.GetMouseButtonDown(0))
                {
                    Destroy(wire);
                    break;
                }
            }
        }

        userState = true;
    }
    void ClearBlock(GameObject block)
    {
        foreach (KeyValuePair<GameObject, GameObject> pair in block.GetComponent<BlockScript>().inputPorts) {
            BlockScript targetBlockScript = pair.Value.GetComponent<BlockScript>();
            targetBlockScript.outputPorts.Remove(pair.Key);
            Destroy(pair.Key);
        }

        foreach (KeyValuePair<GameObject, GameObject> pair in block.GetComponent<BlockScript>().outputPorts) {
            BlockScript targetBlockScript = pair.Value.GetComponent<BlockScript>();
            targetBlockScript.inputPorts.Remove(pair.Key);
            Destroy(pair.Key);
        }

        Stepped -= block.GetComponent<BlockScript>().WhenStepped;
        Destroy(block);
        ResetCircuit();
        //to do
    }
    IEnumerator MakeBlockProcess(GameObject blockTemplate)
    {
        GameObject block = Instantiate(blockTemplate);
        block.name = blockTemplate.name;

        block.transform.SetParent(blockFolder.transform);

        userState = false;
        canvas.gameObject.SetActive(false);

        while (!userState)
        {
            yield return null;
            Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            block.transform.position = new Vector3(mousePos.x, mousePos.y, 0);

            if (Input.GetMouseButtonDown(0) && !UIState)
            {
                break;
            }
        }

        canvas.gameObject.SetActive(true);
        userState = true;
    }
}
