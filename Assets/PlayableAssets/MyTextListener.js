pc.MyTextListener = function () {
    this.onInitPlayable = function () {
        if (typeof window.playableAnalytics != "undefined") {
            window.playableAnalytics.onInitPlayable();
            window.parent.postMessage({ type: 'onInitPlayable' }, '*');
        }
    };

    this.onLoaded = function () {
        if (typeof window.playableAnalytics != "undefined") {
            window.playableAnalytics.onLoaded();
            window.parent.postMessage({ type: 'onLoaded' }, '*');
        }
    };

    this.onDisplay = function () {
        if (typeof window.playableAnalytics != "undefined") {
            window.playableAnalytics.onDisplay();
            window.parent.postMessage({ type: 'onDisplay' }, '*');
        }
    };

    this.gameProgress = function (progress) {
        if (typeof window.playableAnalytics != "undefined") {
            window.playableAnalytics.trackCustomEvent("challenge_progress", { progress });
            window.parent.postMessage({ type: 'challenge_progress_' + progress }, '*');
        }
    };

    this.onGameOver = function () {
        if (typeof window.playableAnalytics != "undefined") {
            window.playableAnalytics.onShowEndCard();
            window.parent.postMessage({ type: 'onShowEndCard' }, '*');
        }
    };

    this.onRetry = function () {
        if (typeof window.playableAnalytics != "undefined") {
            window.playableAnalytics.onRetry();
            window.parent.postMessage({ type: 'onRetry' }, '*');
            window.playableAnalytics.trackCustomEvent("challenge_progress", { progress: -1 });
            window.parent.postMessage({ type: 'challenge_progress_' + -1 }, '*');
        }
    };

    this.onInstall = function () {
        if (typeof window.playableAnalytics != "undefined") {
            window.playableAnalytics.onInstall('Induce');
            window.parent.postMessage({ type: 'onInstall' }, '*');
        }
    };

    this.onCompleted = function () {
        if (typeof window.playableAnalytics != "undefined") {
            window.playableAnalytics.onCompleted();
            window.parent.postMessage({ type: 'onCompleted' }, '*');
        }
    };
};
