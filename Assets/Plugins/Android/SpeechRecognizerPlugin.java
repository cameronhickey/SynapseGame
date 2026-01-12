package com.cerebrum.speech;

import android.app.Activity;
import android.content.Intent;
import android.os.Bundle;
import android.speech.RecognitionListener;
import android.speech.RecognizerIntent;
import android.speech.SpeechRecognizer;
import android.util.Log;

import com.unity3d.player.UnityPlayer;

import java.util.ArrayList;
import java.util.Locale;

public class SpeechRecognizerPlugin implements RecognitionListener {
    private static final String TAG = "SpeechRecognizerPlugin";
    private static final String UNITY_OBJECT = "AndroidSpeechBridge";
    
    private static SpeechRecognizerPlugin instance;
    private SpeechRecognizer speechRecognizer;
    private Intent recognizerIntent;
    private boolean isListening = false;
    
    public static SpeechRecognizerPlugin getInstance() {
        if (instance == null) {
            instance = new SpeechRecognizerPlugin();
        }
        return instance;
    }
    
    private SpeechRecognizerPlugin() {
        Activity activity = UnityPlayer.currentActivity;
        
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (SpeechRecognizer.isRecognitionAvailable(activity)) {
                    speechRecognizer = SpeechRecognizer.createSpeechRecognizer(activity);
                    speechRecognizer.setRecognitionListener(SpeechRecognizerPlugin.this);
                    
                    recognizerIntent = new Intent(RecognizerIntent.ACTION_RECOGNIZE_SPEECH);
                    recognizerIntent.putExtra(RecognizerIntent.EXTRA_LANGUAGE_MODEL, 
                        RecognizerIntent.LANGUAGE_MODEL_FREE_FORM);
                    recognizerIntent.putExtra(RecognizerIntent.EXTRA_LANGUAGE, Locale.getDefault());
                    recognizerIntent.putExtra(RecognizerIntent.EXTRA_PARTIAL_RESULTS, true);
                    recognizerIntent.putExtra(RecognizerIntent.EXTRA_MAX_RESULTS, 1);
                    
                    Log.d(TAG, "Speech recognizer initialized");
                    sendMessage("OnInitialized", "true");
                } else {
                    Log.e(TAG, "Speech recognition not available");
                    sendMessage("OnInitialized", "false");
                }
            }
        });
    }
    
    public void startListening() {
        Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (speechRecognizer != null && !isListening) {
                    isListening = true;
                    speechRecognizer.startListening(recognizerIntent);
                    Log.d(TAG, "Started listening");
                    sendMessage("OnListeningStarted", "");
                }
            }
        });
    }
    
    public void stopListening() {
        Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (speechRecognizer != null && isListening) {
                    isListening = false;
                    speechRecognizer.stopListening();
                    Log.d(TAG, "Stopped listening");
                }
            }
        });
    }
    
    public void destroy() {
        Activity activity = UnityPlayer.currentActivity;
        activity.runOnUiThread(new Runnable() {
            @Override
            public void run() {
                if (speechRecognizer != null) {
                    speechRecognizer.destroy();
                    speechRecognizer = null;
                }
            }
        });
    }
    
    public boolean isAvailable() {
        return SpeechRecognizer.isRecognitionAvailable(UnityPlayer.currentActivity);
    }
    
    private void sendMessage(String method, String message) {
        UnityPlayer.UnitySendMessage(UNITY_OBJECT, method, message);
    }
    
    // RecognitionListener callbacks
    
    @Override
    public void onReadyForSpeech(Bundle params) {
        Log.d(TAG, "Ready for speech");
        sendMessage("OnReadyForSpeech", "");
    }
    
    @Override
    public void onBeginningOfSpeech() {
        Log.d(TAG, "Beginning of speech");
        sendMessage("OnBeginningOfSpeech", "");
    }
    
    @Override
    public void onRmsChanged(float rmsdB) {
        // Audio level changed - could be used for visual feedback
    }
    
    @Override
    public void onBufferReceived(byte[] buffer) {
        // Raw audio buffer
    }
    
    @Override
    public void onEndOfSpeech() {
        Log.d(TAG, "End of speech");
        isListening = false;
        sendMessage("OnEndOfSpeech", "");
    }
    
    @Override
    public void onError(int error) {
        isListening = false;
        String errorMessage;
        switch (error) {
            case SpeechRecognizer.ERROR_AUDIO:
                errorMessage = "Audio recording error";
                break;
            case SpeechRecognizer.ERROR_CLIENT:
                errorMessage = "Client side error";
                break;
            case SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS:
                errorMessage = "Insufficient permissions";
                break;
            case SpeechRecognizer.ERROR_NETWORK:
                errorMessage = "Network error";
                break;
            case SpeechRecognizer.ERROR_NETWORK_TIMEOUT:
                errorMessage = "Network timeout";
                break;
            case SpeechRecognizer.ERROR_NO_MATCH:
                errorMessage = "No speech match";
                break;
            case SpeechRecognizer.ERROR_RECOGNIZER_BUSY:
                errorMessage = "Recognizer busy";
                break;
            case SpeechRecognizer.ERROR_SERVER:
                errorMessage = "Server error";
                break;
            case SpeechRecognizer.ERROR_SPEECH_TIMEOUT:
                errorMessage = "Speech timeout";
                break;
            default:
                errorMessage = "Unknown error: " + error;
                break;
        }
        Log.e(TAG, "Error: " + errorMessage);
        sendMessage("OnError", errorMessage);
    }
    
    @Override
    public void onResults(Bundle results) {
        ArrayList<String> matches = results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (matches != null && !matches.isEmpty()) {
            String result = matches.get(0);
            Log.d(TAG, "Result: " + result);
            sendMessage("OnResult", result);
        } else {
            sendMessage("OnResult", "");
        }
        isListening = false;
    }
    
    @Override
    public void onPartialResults(Bundle partialResults) {
        ArrayList<String> matches = partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (matches != null && !matches.isEmpty()) {
            String partial = matches.get(0);
            Log.d(TAG, "Partial: " + partial);
            sendMessage("OnPartialResult", partial);
        }
    }
    
    @Override
    public void onEvent(int eventType, Bundle params) {
        // Reserved for future events
    }
}
