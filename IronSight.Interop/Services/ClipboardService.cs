using System;
using IronSight.Interop.Native.Clipboard;
using IronSight.Interop.Core;

namespace IronSight.Interop.Services
{
    public class ClipboardService : IDisposable
    {
        private ClipboardMethods.OnClipboardChangedCallback _nativeCallback;
        private bool _isListening;

        public event EventHandler ClipboardChanged;

        public void Start()
        {
            if (_isListening) return;

            _nativeCallback = new ClipboardMethods.OnClipboardChangedCallback(OnNativeClipboardChanged);
            _isListening = ClipboardMethods.StartClipboardListener(_nativeCallback);

            if (!_isListening)
            {
                LoggerService.Log(LogLevel.Error, "Failed to start Clipboard Listener");
            }
        }

        public void Stop()
        {
            if (_isListening)
            {
                ClipboardMethods.StopClipboardListener();
                _isListening = false;
                _nativeCallback = null; // Allow GC? No, keep it until stop.
            }
        }

        private void OnNativeClipboardChanged()
        {
            // This comes from a background thread (native thread).
            // We invoke the event. The subscriber (UI) must handle Dispatching.
            try
            {
                 ClipboardChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                LoggerService.Log(LogLevel.Error, $"Error in Clipboard Handler: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
        }
    }
}