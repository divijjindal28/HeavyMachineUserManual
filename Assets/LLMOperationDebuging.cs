using UnityEngine;

public class LLMOperationDebuging : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log("LLMOperationDebuging start : ");
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void SendMessageDebugLog(string message)
    {
        Debug.Log("LLMOperationDebuging send : " + message);
    }

    public void ReceiveMessageDebugLog(string message)
    {
        Debug.Log("LLMOperationDebuging receive : " + message);
    }
}
