﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Actions;
using Microsoft.CognitiveServices.Speech;
using NAudio.Wave;
using Xpand.Extensions.DateTimeExtensions;
using Xpand.Extensions.LinqExtensions;
using Xpand.Extensions.Numeric;
using Xpand.Extensions.ObjectExtensions;
using Xpand.Extensions.Reactive.Filter;
using Xpand.Extensions.Reactive.Transform;
using Xpand.Extensions.Reactive.Utility;
using Xpand.Extensions.XAF.CollectionSourceExtensions;
using Xpand.Extensions.XAF.DetailViewExtensions;
using Xpand.Extensions.XAF.FrameExtensions;
using Xpand.Extensions.XAF.ObjectSpaceExtensions;
using Xpand.Extensions.XAF.ViewExtensions;
using Xpand.XAF.Modules.Reactive.Services;
using Xpand.XAF.Modules.Speech.BusinessObjects;
using View = DevExpress.ExpressApp.View;

namespace Xpand.XAF.Modules.Speech.Services {
    static class SpeechTextInfoService {
        internal static IObservable<Unit> ConnectSpeechTextInfo(this ApplicationModulesManager manager)
            => manager.NewSpeechTextInfo().Merge(manager.CopySpeechTextInfoPath());

        private static IObservable<Unit> NewSpeechTextInfo(this ApplicationModulesManager manager) 
            => manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView)
                    .SelectUntilViewClosed(frame => frame.View.ToDetailView().NestedFrameContainers(typeof(SpeechText))
                        .SelectMany(container => container.Frame.View.WhenSelectionChanged().Throttle(TimeSpan.FromSeconds(1)).ObserveOnContext()
                            .Select(view => view.SelectedObjects.Cast<SpeechText>().ToArray())
                            .StartWith(container.Frame.View.SelectedObjects.Cast<SpeechText>().ToArray()).WhenNotEmpty()
                            .Do(speechTexts => frame.View.NewSpeechInfo(container.Frame.View.ObjectTypeInfo.Type,speechTexts)))))
                .ToUnit();
        private static IObservable<Unit> CopySpeechTextInfoPath(this ApplicationModulesManager manager) 
            => manager.WhenSpeechApplication(application => application.WhenFrameViewChanged().WhenFrame(typeof(SpeechToText),ViewType.DetailView))
                .SelectMany(frame => frame.View.ToDetailView().WhenNestedListViewProcessCustomizeShowViewParameters(typeof(SSMLFile))
                    .Select(e => {
                        var ssmlFile = e.ShowViewParameters.CreatedView.CurrentObject.To<SSMLFile>();
                        Clipboard.SetText(ssmlFile.File.FullName);
                        e.ShowViewParameters.CreatedView = null;
                        return ssmlFile;
                    })
                    .ShowXafMessage(frame.Application,file => $"File {file.File.FileName} copied in memory."))
                .ToUnit();

        private static void NewSpeechInfo(this View view,Type speechTextType, params SpeechText[] speechTexts) {
            var speechToText = view.CurrentObject.To<SpeechToText>();
            var speechTextInfos = speechToText.SpeechInfo;
            string type=speechTextType==typeof(SpeechTranslation)?"Translation":"Recognition";
            speechTextInfos.Remove(speechTextInfos.FirstOrDefault(info => info.SpeechType == type));
            var speechTextInfo = speechToText.ObjectSpace.AdditionalObjectSpace(typeof(SpeechTextInfo)).CreateObject<SpeechTextInfo>();
            speechTextInfo.SpeechType = type;
            speechTextInfo.SelectedLines = speechTexts.Length;
            speechTextInfo.TotalLines = view.ToDetailView().FrameContainers( speechTextType).First()
                .View?.ToListView().CollectionSource.Objects().Count()??0;
            speechTextInfo.Duration = speechTexts.Sum(text => text.Duration.Ticks).TimeSpan();
            speechTextInfo.VoiceDuration = speechTexts.Sum(text => text.VoiceDuration?.Ticks??0).TimeSpan();
            var lastSpeechText = speechTexts.LastOrDefault();
            speechTextInfo.SSMLDuration = lastSpeechText?.Duration.Add(lastSpeechText.Start)??TimeSpan.Zero;
            speechTextInfo.SSMLVoiceDuration = lastSpeechText?.VoiceDuration?.Add(lastSpeechText.Start)??TimeSpan.Zero;
            speechTextInfo.OverTime = speechTexts.Sum(text => (text.VoiceDuration?.Subtract(text.Duration).Ticks??0)).TimeSpan();
            speechTextInfos.Add(speechTextInfo);
            speechTextInfo.RemoveFromModifiedObjects();
        }

    }
    public static class SpeechTextService {

        internal static string GetRateTag(this SpeechText current, int i) {
            if (current.FileDuration.HasValue ) {
                if (!current.CanConvert) {
                    var maxTime = current.Duration.Add(current.SpareTime);
                    if (current.FileDuration.Value > maxTime) {
                        var rate = current.FileDuration.Value.PercentageDifference(maxTime)+i;
                        return @$"<prosody rate=""+{rate}%"">{{0}}</prosody>";    
                    }
                }
                else if(current.Rate>0) {
                    return @$"<prosody rate=""+{current.Rate}%"">{{0}}</prosody>";
                }
            }
            

            return null;
        }

        internal static IEnumerable<string> Breaks(this SpeechText current, SpeechText previous) {
            var waitTime = current.WaitTime( );
	        
            if (waitTime<TimeSpan.Zero) {
                throw new SpeechException($"Negative break after: {previous.Text}");
            }
	        
            // int breakLimit = 5;
            // if (previous.VoiceDuration>previous.Duration) {
            //  waitTime -= previous.VoiceOverTime();
            //  if (waitTime<TimeSpan.Zero) {
            //   return Enumerable.Empty<string>();
            //  }
            // }

            return current.Text.YieldItem();
            // if (waitTime.TotalSeconds > breakLimit) {
            //  var roundedSeconds =waitTime.TotalSeconds>breakLimit? (waitTime.TotalSeconds % breakLimit).Round(2):0;
            //  return Enumerable.Range(0, (int)(waitTime.TotalSeconds / breakLimit))
            //   .Select(_ => $"<break time=\"{breakLimit}s\" />")
            //   .Concat($"<break time=\"{roundedSeconds}s\" />{current.Text}");
            // }
            //
            // if (waitTime.TotalSeconds > 0) {
            //  return $"<break time=\"{waitTime.TotalSeconds}s\" />{current.Text}".YieldItem();
            // }
            // else
            //  return current.Text.YieldItem();
        }

        internal static TimeSpan VoiceOverTime(this SpeechText speechText) 
            => speechText.VoiceDuration > speechText.Duration ? speechText.VoiceDuration.Value.Subtract(speechText.Duration) : TimeSpan.Zero;

        internal static TimeSpan SpareTime(this SpeechText current) {
            var nextSpeechText = current.NextSpeechText();
            return nextSpeechText == null ? TimeSpan.Zero : nextSpeechText.Start.Subtract(current.End).Abs();
        }

        internal static TimeSpan WaitTime(this SpeechText current) {
            var previous = current.PreviousSpeechText();
            return previous == null ? TimeSpan.Zero : current.Start.Subtract(previous.End);
        }
        
        public static (ISampleProvider provider, SpeechText speechText)[] AudioProviders(this SpeechText speechText) {
            var reader = new AudioFileReader(speechText.File.FullName);
            if (reader.TotalTime <= speechText.Duration.Add(speechText.SpareTime)) {
                var nextSpeechText = speechText.NextSpeechText();
                var nextStart = nextSpeechText?.Start??TimeSpan.Zero;
                var waitTime = nextStart.Subtract(speechText.Start.Add(reader.TotalTime)) ;
                if (waitTime > TimeSpan.Zero) {
                    return new[] { reader,new SilenceProvider(reader.WaveFormat).ToSampleProvider().Take(waitTime) }
                        .Select(provider => (provider, speechText)).ToArray();    
                }
                return new[] { reader }.Cast<ISampleProvider>().Select(provider => (provider, speechText)).ToArray();
            }
            throw new NotImplementedException();
        }

        public static string WavFileName(this SpeechText speechText,IModelSpeech model) 
            => $"{model.DefaultStorageFolder}\\{speechText.Oid}.wav";

        public static IObservable<SpeechText> SaveSSMLFile(this SpeechText speechText, string fileName, SpeechSynthesisResult result) 
            => File.WriteAllBytesAsync(fileName, result.AudioData).ToObservable()
                .BufferUntilCompleted().ObserveOnContext()
                .Select(_ => speechText.UpdateSSMLFile(result, fileName))
                .FirstAsync();
        public static SpeechLanguage Language(this SpeechText speechText)
            => speechText is SpeechTranslation translation?translation.Language:speechText.SpeechToText.RecognitionLanguage;

        public static SpeechVoice SpeechVoice(this SpeechText speechText) {
            var speechLanguage = speechText.Language();
            var voices =speechText is SpeechTranslation?speechText.SpeechToText.SpeechVoices: speechText.SpeechToText.Account.Voices;
            return voices.FirstOrDefault(voice => voice.Language.Name == speechLanguage.Name);
        }

        private static SpeechConfig SpeechSynthesisConfig(this SpeechText speechText) {
            var speechConfig = speechText.SpeechToText.Account.SpeechConfig();
            speechConfig.SpeechSynthesisLanguage = speechText is SpeechTranslation translation
                ? translation.Language.Name : speechText.SpeechToText.RecognitionLanguage.Name;
            speechConfig.SpeechSynthesisVoiceName = $"{speechText.SpeechVoice()?.ShortName}";
            return speechConfig;
        }

        public static IObservable<SpeechSynthesisResult> SayIt(this SpeechText speechText) 
            => Observable.Using(() => new SpeechSynthesizer(speechText.SpeechSynthesisConfig()),synthesizer 
                => synthesizer.SpeakTextAsync(speechText.Text).ToObservable().Select(result => result));
    }
}