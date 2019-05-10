using System;
using System.Xml.Serialization;

namespace Musegician.Core
{
    [Serializable]
    public enum RecordingType
    {
        [XmlEnum(Name = "Std")]
        Standard = 0,

        [XmlEnum(Name = "Alt")]
        Alternate,

        [XmlEnum(Name = "Acs")]
        Acoustic,

        [XmlEnum(Name = "Live")]
        Live,

        MAX
    }

    public static class RecordingTypeExtensions
    {
        public static string ToLabel(this RecordingType recordingType)
        {
            switch (recordingType)
            {
                case RecordingType.Standard: return "";
                case RecordingType.Alternate: return "🎙️";
                case RecordingType.Acoustic: return "🔌";
                case RecordingType.Live: return "🎤";

                default:
                    Console.WriteLine($"Unexpected RecordingType: {recordingType}");
                    goto case RecordingType.Standard;
            }
        }
    }
}
