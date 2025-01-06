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
            OpenAIKey = "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",
        };
    }
}
