// AudioVisualizer.jslib
var AudioVisualizerLibrary = {
    $analyzers: {},

    StartSampling: function(namePtr, duration, bufferSize) {
        var name = Pointer_stringify(namePtr);
        if (analyzers[name] != null) return true;

        var acceptableDistance = 0.075;
        var analyzer = null;
        var source = null;

        try {
            if (typeof WEBAudio !== 'undefined' && WEBAudio.audioInstances && WEBAudio.audioInstances.length > 1) {
                for (var i = WEBAudio.audioInstances.length - 1; i >= 0; i--) {
                    var instance = WEBAudio.audioInstances[i];
                    if (instance != null) {
                        var pSource = instance.source;
                        if (pSource != null && pSource.buffer != null && Math.abs(pSource.buffer.duration - duration) < acceptableDistance) {
                            source = pSource;
                            break;
                        }
                    }
                }

                if (source == null) {
                    console.log("StartSampling: No source found with duration " + duration);
                    return false;
                }

                analyzer = source.context.createAnalyser();
                analyzer.fftSize = bufferSize * 2;
                source.connect(analyzer);

                analyzers[name] = {
                    analyzer: analyzer,
                    source: source
                };

                return true;
            } else {
                console.log("StartSampling: WEBAudio not available or no audio instances");
                return false;
            }
        } catch (e) {
            console.log("StartSampling error: " + e);
            if (analyzer != null && source != null) {
                source.disconnect(analyzer);
            }
            return false;
        }
    },

    CloseSampling: function(namePtr) {
        var name = Pointer_stringify(namePtr);
        var analyzerObj = analyzers[name];
        if (analyzerObj != null) {
            try {
                analyzerObj.source.disconnect(analyzerObj.analyzer);
                delete analyzers[name];
                return true;
            } catch (e) {
                console.log("CloseSampling error: " + e);
            }
        }
        return false;
    },

    GetSamples: function(namePtr, bufferPtr, bufferSize) {
        var name = Pointer_stringify(namePtr);
        var analyzerObj = analyzers[name];
        if (analyzerObj == null) return false;

        try {
            var buffer = new Float32Array(Module.HEAPU8.buffer, bufferPtr, bufferSize);
            analyzerObj.analyzer.getFloatTimeDomainData(buffer);
            return true;
        } catch (e) {
            console.log("GetSamples error: " + e);
            return false;
        }
    }
};

autoAddDeps(AudioVisualizerLibrary, '$analyzers');
mergeInto(LibraryManager.library, AudioVisualizerLibrary);