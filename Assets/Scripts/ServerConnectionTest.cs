using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using TMPro;
using System.IO;
using System;
using UnityEngine.UI;

public class VideoGenerationClient : MonoBehaviour
{
    [Header("Server Configuration")]
    public string serverURL = "https://46f4-34-124-176-214.ngrok-free.app"; // Replace with your server URL
    
    [Header("File Paths")]
    public string audioFilePath = ""; // Path to your audio file
    public string referenceImageName = ""; // Name of reference image on server
    
    [Header("UI Elements")]
    public TextMeshProUGUI statusText;
    [Header("Generated Video")]
    public UnityEngine.Video.VideoPlayer videoPlayer;
    
    private List<string> availableImages = new List<string>();
    private bool isProcessing = false;
    private AlivePhotoHandler.AlivePhoto currentPhoto = null;
    
    void Start()
    {
        // Initial connection test and image list refresh
        StartCoroutine(InitializeClient());
    }
    
    IEnumerator InitializeClient()
    {
        UpdateStatus("Initializing client...");
        yield return StartCoroutine(TestConnection());
        yield return StartCoroutine(RefreshImageList());
        UpdateStatus("Client ready!");
    }
    
    public void UpdateStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
        Debug.Log($"[VideoClient] {message}");
    }
    
    #region Connection Testing
    
    IEnumerator TestConnection()
    {
        UpdateStatus("Testing server connection...");
        
        using (UnityWebRequest www = UnityWebRequest.Get($"{serverURL}/health"))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                UpdateStatus("✅ Server connection successful!");
                Debug.Log("Health response: " + www.downloadHandler.text);
            }
            else
            {
                UpdateStatus($"❌ Connection failed: {www.error}");
                Debug.LogError("Connection error: " + www.error);
            }
        }
    }
    
    #endregion
    
    #region Image Management
    
    IEnumerator RefreshImageList()
    {
        UpdateStatus("Refreshing image list...");
        
        using (UnityWebRequest www = UnityWebRequest.Get($"{serverURL}/list_images"))
        {
            yield return www.SendWebRequest();
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var response = JsonUtility.FromJson<ImageListResponse>(www.downloadHandler.text);
                    availableImages.Clear();
                    availableImages.AddRange(response.images);
                    UpdateStatus($"Found {availableImages.Count} reference images");
                }
                catch (Exception e)
                {
                    UpdateStatus($"Error parsing image list: {e.Message}");
                }
            }
            else
            {
                UpdateStatus($"Failed to get image list: {www.error}");
            }
        }
    }
    
    #endregion
    
    #region Video Generation
    
    public IEnumerator GenerateVideo(string audioPath, string imageNumber, AlivePhotoHandler.AlivePhoto alivePhoto)
    {
        if (isProcessing)
        {
            UpdateStatus("Already processing, please wait...");
            yield break;
        }
        
        
        // Validate inputs
        if (string.IsNullOrEmpty(audioPath) || !File.Exists(audioPath))
        {
            UpdateStatus("❌ Please provide a valid audio file path");
            yield return null;
        }
        
        if (string.IsNullOrEmpty(imageNumber))
        {
            UpdateStatus("❌ Please select or specify a reference image");
            yield return null;
        }
        
        isProcessing = true;
        currentPhoto = alivePhoto;
        UpdateStatus("🎬 Generating video...");
        
        // Create form data
        List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
        
        // create byte array to send
        byte[] audioData = File.ReadAllBytes(audioPath);
        string audioFileName = Path.GetFileName(audioPath);
        
        //add audio
        formData.Add(new MultipartFormFileSection("audio", audioData, audioFileName, "audio/wav"));
        // Add image reference
        formData.Add(new MultipartFormDataSection("image_reference", imageNumber));
        
        using (UnityWebRequest www = UnityWebRequest.Post($"{serverURL}/generate_video", formData))
        {
            // Set a longer timeout for video generation
            www.timeout = 300; // 5 minutes
            
            yield return www.SendWebRequest();
            bool parseSuccess = false;
            
            if (www.result == UnityWebRequest.Result.Success)
            {
                var response = JsonUtility.FromJson<VideoGenerationResponse>(www.downloadHandler.text);
                try
                {
                   
                    
                    if (response.success)
                    {
                        UpdateStatus("✅ Video generated successfully!");
                        parseSuccess = true;
                        
                    }
                    else
                    {
                        UpdateStatus($"❌ Generation failed: {response.error}");
                    }
                }
                catch (Exception e)
                {
                    UpdateStatus($"❌ Error parsing response: {e.Message}");
                    Debug.LogError("Response parsing error: " + e.Message);
                    Debug.LogError("Raw response: " + www.downloadHandler.text);
                }
                if(parseSuccess)
                    yield return StartCoroutine(HandleGeneratedVideo(response));
            }
            else
            {
                UpdateStatus($"❌ Request failed: {www.error}");
                Debug.LogError("Generation error: " + www.downloadHandler.text);
            }
        }
        
        isProcessing = false;
    }
    
    IEnumerator HandleGeneratedVideo(VideoGenerationResponse response)
    {
        try
        {
            // Decode base64 video data
            byte[] videoBytes = Convert.FromBase64String(response.video_data);
            
            // Save video to persistent data path
            string videoPath = Path.Combine(Application.persistentDataPath, response.filename);
            File.WriteAllBytes(videoPath, videoBytes);
            
            UpdateStatus($"✅ Video saved to: {videoPath}");
            
            // Play video if video player is assigned
            if (videoPlayer != null)
            {
                if (currentPhoto != null)
                {
                    currentPhoto.SetVideoTexture("file://" + videoPath);
                    UpdateStatus("🎬 Playing generated video!");
                }
                else 
                    Debug.Log("Current photo is null");   }
            
            Debug.Log($"Video generated and saved: {videoPath}");
            currentPhoto = null;
        }
        catch (Exception e)
        {
            UpdateStatus($"❌ Error handling video: {e.Message}");
            Debug.LogError("Video handling error: " + e.Message);
        }
        
        yield return null;
    }
    
    #endregion
    
    
    #region Data Classes
    
    [System.Serializable]
    public class ImageListResponse
    {
        public string[] images;
    }
    
    [System.Serializable]
    public class VideoGenerationResponse
    {
        public bool success;
        public string message;
        public string video_data;
        public string filename;
        public string error;
    }
    
    [System.Serializable]
    public class UploadResponse
    {
        public bool success;
        public string message;
        public string filename;
        public string error;
    }
    
    #endregion
}