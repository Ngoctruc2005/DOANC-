using Microsoft.Maui.Media;

public class AudioService
{
    public async Task Speak(string text)
    {
        await TextToSpeech.SpeakAsync(text);
    }
}