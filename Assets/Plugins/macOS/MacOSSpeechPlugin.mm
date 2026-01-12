#import <Foundation/Foundation.h>
#import <Speech/Speech.h>
#import <AVFoundation/AVFoundation.h>

// Callback types for Unity
typedef void (*TranscriptionCallback)(const char* text);
typedef void (*ErrorCallback)(const char* error);

// Static references
static SFSpeechRecognizer* speechRecognizer = nil;
static SFSpeechAudioBufferRecognitionRequest* recognitionRequest = nil;
static SFSpeechRecognitionTask* recognitionTask = nil;
static AVAudioEngine* audioEngine = nil;
static TranscriptionCallback onTranscription = nil;
static ErrorCallback onError = nil;
static BOOL isListening = NO;

// Forward declarations
extern "C" void MacOSSpeech_StopListening();

extern "C" {

    void MacOSSpeech_Initialize() {
        if (speechRecognizer == nil) {
            speechRecognizer = [[SFSpeechRecognizer alloc] initWithLocale:[NSLocale localeWithLocaleIdentifier:@"en-US"]];
            audioEngine = [[AVAudioEngine alloc] init];
        }
    }

    BOOL MacOSSpeech_IsAvailable() {
        if (@available(macOS 10.15, *)) {
            return speechRecognizer != nil && [speechRecognizer isAvailable];
        }
        return NO;
    }

    BOOL MacOSSpeech_IsListening() {
        return isListening;
    }

    void MacOSSpeech_RequestAuthorization(void (*callback)(int status)) {
        NSLog(@"[MacOSSpeechPlugin] Requesting speech recognition authorization...");
        
        // First request microphone permission (required for speech)
        if (@available(macOS 10.14, *)) {
            [AVCaptureDevice requestAccessForMediaType:AVMediaTypeAudio completionHandler:^(BOOL granted) {
                NSLog(@"[MacOSSpeechPlugin] Microphone access: %@", granted ? @"granted" : @"denied");
            }];
        }
        
        // Then request speech recognition
        [SFSpeechRecognizer requestAuthorization:^(SFSpeechRecognizerAuthorizationStatus status) {
            NSLog(@"[MacOSSpeechPlugin] Speech authorization result: %d", (int)status);
            dispatch_async(dispatch_get_main_queue(), ^{
                if (callback) {
                    callback((int)status);
                }
            });
        }];
    }

    int MacOSSpeech_GetAuthorizationStatus() {
        return (int)[SFSpeechRecognizer authorizationStatus];
    }

    void MacOSSpeech_StartListening(TranscriptionCallback transcriptionCb, ErrorCallback errorCb) {
        if (isListening) {
            if (errorCb) errorCb("Already listening");
            return;
        }

        if (!MacOSSpeech_IsAvailable()) {
            if (errorCb) errorCb("Speech recognition not available");
            return;
        }

        if ([SFSpeechRecognizer authorizationStatus] != SFSpeechRecognizerAuthorizationStatusAuthorized) {
            if (errorCb) errorCb("Speech recognition not authorized");
            return;
        }

        onTranscription = transcriptionCb;
        onError = errorCb;

        // Cancel any existing task
        if (recognitionTask != nil) {
            [recognitionTask cancel];
            recognitionTask = nil;
        }

        // Create recognition request
        recognitionRequest = [[SFSpeechAudioBufferRecognitionRequest alloc] init];
        recognitionRequest.shouldReportPartialResults = YES;

        AVAudioInputNode* inputNode = [audioEngine inputNode];

        // Start recognition task
        recognitionTask = [speechRecognizer recognitionTaskWithRequest:recognitionRequest
            resultHandler:^(SFSpeechRecognitionResult* result, NSError* error) {
                
                if (result != nil) {
                    NSString* transcript = [[result bestTranscription] formattedString];
                    
                    // If final result, send it
                    if ([result isFinal]) {
                        const char* cStr = [transcript UTF8String];
                        if (onTranscription) {
                            onTranscription(cStr);
                        }
                        MacOSSpeech_StopListening();
                    }
                }

                if (error != nil) {
                    NSString* errorMsg = [error localizedDescription];
                    const char* cStr = [errorMsg UTF8String];
                    if (onError) {
                        onError(cStr);
                    }
                    MacOSSpeech_StopListening();
                }
            }];

        // Configure audio session
        AVAudioFormat* recordingFormat = [inputNode outputFormatForBus:0];
        [inputNode installTapOnBus:0 bufferSize:1024 format:recordingFormat
            block:^(AVAudioPCMBuffer* buffer, AVAudioTime* when) {
                [recognitionRequest appendAudioPCMBuffer:buffer];
            }];

        [audioEngine prepare];

        NSError* audioError = nil;
        if (![audioEngine startAndReturnError:&audioError]) {
            if (onError) {
                const char* cStr = [[audioError localizedDescription] UTF8String];
                onError(cStr);
            }
            return;
        }

        isListening = YES;
    }

    void MacOSSpeech_StopListening() {
        if (!isListening) return;

        isListening = NO;

        if (audioEngine != nil && [audioEngine isRunning]) {
            [audioEngine stop];
            [[audioEngine inputNode] removeTapOnBus:0];
        }

        if (recognitionRequest != nil) {
            [recognitionRequest endAudio];
            recognitionRequest = nil;
        }

        if (recognitionTask != nil) {
            [recognitionTask cancel];
            recognitionTask = nil;
        }

        onTranscription = nil;
        onError = nil;
    }

    void MacOSSpeech_Cleanup() {
        MacOSSpeech_StopListening();
        speechRecognizer = nil;
        audioEngine = nil;
    }
}
