using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class APIKeys
{
    public string OpenAIKey;
    public string WitAIKey;

    public static APIKeys Load()
    {
        // You can load this from a JSON file, but here's a default example
        //string filePath = Application.dataPath + "/Config/api_keys.json";
        //if (System.IO.File.Exists(filePath))
        //{
        //    string json = System.IO.File.ReadAllText(filePath);
        //    return JsonUtility.FromJson<APIKeys>(json);
        //}
        //else
        //{
        //    Debug.LogError("API keys file not found!");
        //    return null; 
        //}
        return new APIKeys
        {
            OpenAIKey = "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"
        };
    }
}
