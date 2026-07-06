class VibrationBridge {
    vibrate(duration) {
        if (navigator.vibrate) {
            navigator.vibrate(duration);
        }
    }

    vibratePattern(pattern) {
        if (navigator.vibrate) {
            navigator.vibrate(pattern);
        }
    }

    stop() {
        if (navigator.vibrate) {
            navigator.vibrate(0);
        }
    }

    isSupported() {
        return !!navigator.vibrate;
    }
}
