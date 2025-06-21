using UnityEngine;
using System;
using System.IO;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Video;

public class AlivePhotoHandler : MonoBehaviour
{
    public int sampleRate = 16000;
    public int recordDuration = 5;
    public VideoGenerationClient videoClient; 
    private AudioClip recordedClip;
    private string recordedFilePath;

    [SerializeField] private string imageNumber;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button startRecordingBtn;
    [SerializeField] private Button stopRecordingBtn;


    public class AlivePhoto
    {
        private GameObject placeholderImage;
        private GameObject videoImage;
        private VideoPlayer videoPlayer;

        public AlivePhoto(GameObject placeholderImage, GameObject videoImage, VideoPlayer videoPlayer)
        {
            this.placeholderImage = placeholderImage;
            this.videoImage = videoImage;
            this.videoPlayer = videoPlayer;
        }

        public void SetVideoTexture(string videoPath)
        {
            placeholderImage.SetActive(false);
            videoImage.SetActive(true);
            videoPlayer.url = videoPath;
            videoPlayer.Play();
        }

    }
    public GameObject placeholderImage;
    public GameObject videoImage;
    private AlivePhoto _alivePhoto;


    private void Start()
    {
        startRecordingBtn.onClick.AddListener(StartRecording);
        stopRecordingBtn.onClick.AddListener(StopRecording);
        VideoPlayer videoPlayer = GetComponent<VideoPlayer>();
        _alivePhoto = new AlivePhoto(placeholderImage, videoImage, videoPlayer);
    }

    public void StartRecording()
    {
        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("No microphone detected!");
            return;
        }

        recordedClip = Microphone.Start(null, false, recordDuration, sampleRate);
        statusText.text = "🎙️ Recording...";
    }

    public void StopRecording()
    {

        Microphone.End(null);
        statusText.text = "🔊 Processing audio...";
        //save recorded audio
        SaveRecordingToWav();
        //send audio to colab
        SendAudio();
        
    }

    private void SendAudio()
    {
        if (videoClient != null)
        {
            statusText.text = "🌀 Sending to server...";
            videoClient.StartCoroutine(videoClient.GenerateVideo(recordedFilePath, imageNumber, _alivePhoto));
        }
    }

    private void SaveRecordingToWav()
    {
        string fileName = $"recorded_audio_{DateTime.Now.Ticks}.wav";
        recordedFilePath = Path.Combine(Application.persistentDataPath, fileName);

        byte[] wavData = WavUtility.FromAudioClip(recordedClip, out _, true); // Requires WavUtility.cs
        File.WriteAllBytes(recordedFilePath, wavData);
        Debug.Log($"Audio saved to: {recordedFilePath}");
    }
    
}